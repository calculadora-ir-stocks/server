﻿using Api.Clients.B3;
using Api.Database;
using Api.Notification;
using Api.Services.Auth;
using Api.Services.B3;
using Api.Services.Jwt;
using Billing.Services;
using Common;
using Common.Models;
using Core.Calculators;
using Core.Calculators.Assets;
using Core.Clients.InfoSimples;
using Core.Filters;
using Core.Services.Account;
using Core.Services.Email;
using Core.Services.Hangfire.AverageTradedPriceUpdater;
using Core.Services.Hangfire.EmailCodeRemover;
using Core.Services.IncomeTaxes;
using Core.Services.Plan;
using Core.Services.TaxesService;
using Hangfire;
using Hangfire.PostgreSql;
using Infrastructure.Repositories;
using Infrastructure.Repositories.Account;
using Infrastructure.Repositories.AverageTradedPrice;
using Infrastructure.Repositories.EmailCode;
using Infrastructure.Repositories.Plan;
using Infrastructure.Repositories.Taxes;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Polly;
using Stripe;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Api
{
    public static class DependencyInjection
    {
        public static void AddServices(this IServiceCollection services, WebApplicationBuilder builder)
        {
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            // 3rd parties
            services.AddScoped<IB3Client, B3Client>();
            services.AddScoped<IInfoSimplesClient, InfoSimplesClient>();

            services.AddScoped<IAccountService, Core.Services.Account.AccountService>();
            services.AddScoped<ITaxesService, TaxesService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IIncomeTaxesService, IncomeTaxesService>();
            services.AddScoped<IStripeService, StripeService>();
            services.AddScoped<IPlanService, Core.Services.Plan.PlanService>();

            services.AddScoped<NotificationContext>();

            services.AddTransient<IIncomeTaxesCalculator, BDRsIncomeTaxes>();
            services.AddTransient<IIncomeTaxesCalculator, ETFsIncomeTaxes>();
            services.AddTransient<IIncomeTaxesCalculator, FIIsIncomeTaxes>();
            services.AddTransient<IIncomeTaxesCalculator, GoldIncomeTaxes>();
            services.AddTransient<IIncomeTaxesCalculator, InvestmentsFundsIncomeTaxes>();
            services.AddTransient<IIncomeTaxesCalculator, StocksIncomeTaxes>();

            services.AddTransient<IJwtCommon, JwtCommon>();

            services.AddMvc(options => options.Filters.Add<NotificationFilter>());

            services.Configure<JwtProperties>(options =>
            {
                options.Secret = builder.Configuration["Jwt:Token"];
                options.Issuer = builder.Configuration["Jwt:Issuer"];
                options.Audience = builder.Configuration["Jwt:Audience"];
            });

            services.Configure<StripeSecret>(options =>
            {
                options.WebhookSecret = builder.Configuration["Services:Stripe:WebhookToken"];
                options.ApiSecret = builder.Configuration["Services:Stripe:ApiToken"];
            });

            services.Configure<InfoSimplesToken>(options =>
            {
                options.Secret = builder.Configuration["Services:InfoSimples:Token"];
            });

            services.Configure<B3ClientParams>(options =>
            {
                options.ClientId = builder.Configuration["Services:B3:ClientId"];
                options.ClientSecret = builder.Configuration["Services:B3:ClientSecret"];
                options.Scope = builder.Configuration["Services:B3:Scope"];
                options.GrantType = builder.Configuration["Services:B3:GrantType"];
            });
        }

        public static void AddStripeServices(this IServiceCollection services, IConfiguration configuration)
        {
            StripeConfiguration.ApiKey = configuration.GetValue<string>("Services:Stripe:ApiToken");

            services.AddScoped<ChargeService>();
            services.AddScoped<CustomerService>();
            services.AddScoped<TokenService>();
        }


        public static void AddHangfireServices(this IServiceCollection services, WebApplicationBuilder builder)
        {
            services.AddHangfireServer();

            services.AddScoped<IAverageTradedPriceUpdaterHangfire, AverageTradedPriceUpdaterHangfire>();
            services.AddScoped<IEmailCodeRemoverHangfire, EmailCodeRemoverHangfire>();
        }

        public static void AddHangFireRecurringJob(this IServiceCollection services, WebApplicationBuilder builder)
        {
            services.AddHangfire(x =>
                x.UsePostgreSqlStorage(builder.Configuration.GetConnectionString("DefaultConnection"))
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
            );

            GlobalConfiguration.Configuration.UsePostgreSqlStorage(builder.Configuration.GetConnectionString("DefaultConnection"));

            RecurringJob.RemoveIfExists(nameof(AverageTradedPriceUpdaterHangfire));
            RecurringJob.RemoveIfExists(nameof(EmailCodeRemoverHangfire));

            RecurringJob.AddOrUpdate<IAverageTradedPriceUpdaterHangfire>(
                nameof(AverageTradedPriceUpdaterHangfire),
                x => x.Execute(),
                Cron.Monthly
            );

            RecurringJob.AddOrUpdate<IEmailCodeRemoverHangfire>(
                nameof(EmailCodeRemoverHangfire),
                x => x.Execute(),
                Cron.Minutely
            );
        }

        public static void Add3rdPartiesClientServices(this IServiceCollection services)
        {
            var handler = new HttpClientHandler();
            // TO-DO: uncomment for production
            // AddCertificate(handler);

            services.AddHttpClient("B3", c =>
                c.BaseAddress = new Uri("https://apib3i-cert.b3.com.br:2443/api/")).ConfigurePrimaryHttpMessageHandler(() => handler)
                .AddTransientHttpErrorPolicy(policy => policy.WaitAndRetryAsync(5, _ => TimeSpan.FromSeconds(10)))
                .AddTransientHttpErrorPolicy(policy => policy.CircuitBreakerAsync(5, TimeSpan.FromSeconds(10)));

            services.AddHttpClient("Microsoft", c =>
                c.BaseAddress = new Uri("https://login.microsoftonline.com/")).ConfigurePrimaryHttpMessageHandler(() => handler)
                .AddTransientHttpErrorPolicy(policy => policy.WaitAndRetryAsync(5, _ => TimeSpan.FromSeconds(10)))
                .AddTransientHttpErrorPolicy(policy => policy.CircuitBreakerAsync(5, TimeSpan.FromSeconds(10)));

            services.AddHttpClient("Infosimples", c =>
                c.BaseAddress = new Uri("https://api.infosimples.com/api/v2/consultas/")).ConfigurePrimaryHttpMessageHandler(() => handler)
                .AddTransientHttpErrorPolicy(policy => policy.WaitAndRetryAsync(5, _ => TimeSpan.FromSeconds(10)))
                .AddTransientHttpErrorPolicy(policy => policy.CircuitBreakerAsync(5, TimeSpan.FromSeconds(10)));
        }

        private static void AddCertificate(HttpClientHandler handler)
        {
            handler.ClientCertificateOptions = ClientCertificateOption.Manual;
            handler.SslProtocols = SslProtocols.Tls12;

            // C:\Users\Biscoitinho\Documents\Certificates\31788887000158.pfx
            // /home/dickmann/Documents/certificates/31788887000158.pfx
            handler.ClientCertificates.Add(new X509Certificate2("/home/dickmann/Documents/certificates/31788887000158.pfx", "C3MOHH", X509KeyStorageFlags.PersistKeySet));
        }

        public static void AddRepositories(this IServiceCollection services)
        {
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddScoped<IAccountRepository, AccountRepository>();
            services.AddScoped<IAverageTradedPriceRepostory, AverageTradedPriceRepository>();
            services.AddScoped<IEmailCodeRepository, EmailCodeRepository>();
            services.AddScoped<ITaxesRepository, TaxesRepository>();
            services.AddScoped<IPlanRepository, PlanRepository>();
        }

        public static void AddJwtAuthentications(this IServiceCollection services, WebApplicationBuilder builder)
        {
            services.AddAuthentication(opt =>
            {
                opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = false,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Token"]))
                };
            });
        }

        public static void AddSwaggerConfiguration(this IServiceCollection services)
        {
            services.AddSwaggerGen(options =>
            {
                var jwtSecurityScheme = new OpenApiSecurityScheme
                {
                    BearerFormat = "JWT",
                    Name = "JWT Authentication",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = JwtBearerDefaults.AuthenticationScheme,
                    Description = "Insert your JWT token below.",

                    Reference = new OpenApiReference
                    {
                        Id = JwtBearerDefaults.AuthenticationScheme,
                        Type = ReferenceType.SecurityScheme
                    }
                };

                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "Stocks - plataforma para pagamento de imposto na bolsa de valores.",
                });

                options.AddSecurityDefinition(jwtSecurityScheme.Reference.Id, jwtSecurityScheme);
                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    { jwtSecurityScheme, Array.Empty<string>() }
                });

                var xml = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xml));
            });
        }

        public static void AddDatabase(this IServiceCollection services, WebApplicationBuilder builder)
        {
            services.AddDbContext<StocksContext>(options =>
            {
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            });
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        }
    }
}

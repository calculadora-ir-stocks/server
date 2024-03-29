﻿using Api.Clients.B3;
using Api.Database;
using Api.Handler;
using Api.Services.Auth;
using Billing.Services.Stripe;
using Common;
using Common.Configurations;
using Common.Models;
using Common.Models.Handlers;
using Common.Models.Secrets;
using Core.Calculators;
using Core.Calculators.Assets;
using Core.Clients.B3;
using Core.Clients.InfoSimples;
using Core.Filters;
using Core.Hangfire.PlanExpirer;
using Core.Notification;
using Core.Services.Account;
using Core.Services.B3Syncing;
using Core.Services.DarfGenerator;
using Core.Services.Hangfire.AverageTradedPriceUpdater;
using Core.Services.IncomeTaxes;
using Core.Services.Plan;
using Core.Services.Taxes;
using Hangfire;
using Hangfire.PostgreSql;
using Infrastructure.Repositories;
using Infrastructure.Repositories.Account;
using Infrastructure.Repositories.AverageTradedPrice;
using Infrastructure.Repositories.Plan;
using Infrastructure.Repositories.Taxes;
using Infrastructure.UnitOfWork;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Polly;
using Stripe;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;

namespace Api
{
    public static class DependencyInjection
    {
        public static void AddServices(this IServiceCollection services, WebApplicationBuilder builder)
        {
            // Scope handler
            builder.Services.AddSingleton<IAuthorizationHandler, HasScopeHandler>();
            builder.Services.AddScoped<IAuthorizationHandler, CanAccessResourceHandler>();

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<JsonSerializerConfiguration>();

            services.AddTransient<IUnitOfWork, UnitOfWork>();

            // 3rd parties
            services.AddScoped<IB3Client, B3Client>();
            services.AddScoped<IInfoSimplesClient, InfoSimplesClient>();

            // Services layer
            services.AddScoped<IAccountService, Core.Services.Account.AccountService>();
            services.AddScoped<ITaxesService, TaxesService>();
            services.AddScoped<IB3SyncingService, B3SyncingService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IB3ResponseCalculatorService, B3ResponseCalculatorService>();
            services.AddScoped<IDarfGeneratorService, DarfGeneratorService>();
            services.AddScoped<IStripeService, StripeService>();
            services.AddScoped<IPlanService, Core.Services.Plan.PlanService>();

            services.AddScoped<NotificationManager>();

            // Calculators
            services.AddTransient<IIncomeTaxesCalculator, BDRsIncomeTaxes>();
            services.AddTransient<IIncomeTaxesCalculator, ETFsIncomeTaxes>();
            services.AddTransient<IIncomeTaxesCalculator, FIIsIncomeTaxes>();
            services.AddTransient<IIncomeTaxesCalculator, GoldIncomeTaxes>();
            services.AddTransient<IIncomeTaxesCalculator, InvestmentsFundsIncomeTaxes>();
            services.AddTransient<IIncomeTaxesCalculator, StocksIncomeTaxes>();

            services.AddMvc(options => options.Filters.Add<NotificationFilter>());
        }

        public static void AddStripeServices(this IServiceCollection services)
        {
            StripeConfiguration.ApiKey = Environment.GetEnvironmentVariable("STRIPE_API_TOKEN");

            services.AddScoped<ChargeService>();
            services.AddScoped<CustomerService>();
            services.AddScoped<TokenService>();
        }

        public static void AddAuth0Authentication(this IServiceCollection _, WebApplicationBuilder builder)
        {
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
            {
                options.Authority = builder.Configuration["Auth0:Domain"];
                options.Audience = builder.Configuration["Auth0:Audience"];
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = ClaimTypes.NameIdentifier
                };
            });

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy(
                    "read:taxes",
                    policy => policy.Requirements.Add(
                        new HasScopeRequirement("read:taxes", builder.Configuration["Auth0:Domain"])
                    )
                );
                options.AddPolicy(
                    "read:own_information",
                    policy => policy.Requirements.Add(
                        new CanAccessResourceRequirement("read:own_information")
                    )
                );
            });
        }

        public static void AddHangfireServices(this IServiceCollection services)
        {
            services.AddHangfireServer();

            services.AddScoped<IAverageTradedPriceUpdaterHangfire, AverageTradedPriceUpdaterHangfire>();
            services.AddScoped<IPlanExpirerHangfire, PlanExpirerHangfire>();
        }

        public static void ConfigureHangfireServices(this IServiceCollection services, WebApplicationBuilder builder)
        {
            services.AddHangfire(x =>
                x.UsePostgreSqlStorage(builder.Configuration.GetConnectionString("DefaultConnection"))
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
            );

            // GlobalConfiguration.Configuration.UsePostgreSqlStorage(builder.Configuration.GetConnectionString("DefaultConnection"));

            RecurringJob.RemoveIfExists(nameof(AverageTradedPriceUpdaterHangfire));
            RecurringJob.RemoveIfExists(nameof(PlanExpirerHangfire));

            RecurringJob.AddOrUpdate<IAverageTradedPriceUpdaterHangfire>(
                nameof(AverageTradedPriceUpdaterHangfire),
                x => x.Execute(),
                Cron.Monthly
            );

            RecurringJob.AddOrUpdate<IPlanExpirerHangfire>(
                nameof(PlanExpirerHangfire),
                x => x.Execute(),
                Cron.Daily
            );
        }

        public static void Add3rdPartiesClients(this IServiceCollection services)
        {
            var handler = new HttpClientHandler();
            // TODO: uncomment for production
            // AddCertificate(handler);

            // TODO get from appsettings
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

            services.AddHttpClient("Auth0", c =>
                c.BaseAddress = new Uri("https://dev-cfdhp4yerdn6st6a.us.auth0.com/oauth/")).ConfigurePrimaryHttpMessageHandler(() => handler)
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
            services.AddScoped<ITaxesRepository, TaxesRepository>();
            services.AddScoped<IPlanRepository, PlanRepository>();
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
            DatabaseSecret secret = new();

            services.AddDbContext<StocksContext>(options =>
            {
                options.UseNpgsql(secret.GetConnectionString());
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            });
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        }

        public static void InitializeEnvironmentVariables(this IServiceCollection _, string[] envFilesOnRoot)
        {
            string root = Directory.GetCurrentDirectory();

            foreach (string envFile in envFilesOnRoot)
            {
                string env = Path.Combine(root, envFile);
                EnvironmentVariableInitializer.Load(env);
            }
        }
    }
}

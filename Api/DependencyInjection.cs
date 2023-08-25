using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Polly;
using Api.Clients.B3;
using Api.Database;
using Api.Notification;
using Infrastructure.Repositories;
using Infrastructure.Repositories.Account;
using Api.Services.Auth;
using Api.Services.B3;
using Api.Services.IncomeTaxes;
using Api.Services.Jwt;
using Common;
using Core.Calculators;
using Core.Calculators.Assets;
using Core.Services.Account;
using Core.Services.EmailSender;
using Core.Services.Hangfire.AverageTradedPriceUpdater;
using Core.Services.Hangfire.EmailCodeRemover;
using Core.Services.Hangfire.UserPlansValidity;
using Core.Services.IncomeTaxes;
using Core.Services.Plan;
using Core.Services.PremiumCode;
using Infrastructure.Repositories.AverageTradedPrice;
using Infrastructure.Repositories.EmailCode;
using Infrastructure.Repositories.Taxes;
using Stripe;

namespace Api
{
    public static class DependencyInjection
    {
        public static void AddServices(this IServiceCollection services, WebApplicationBuilder builder)
        {
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            services.AddSingleton<IB3Client, B3Client>();

            services.AddScoped<IAccountService, Core.Services.Account.AccountService>();
            services.AddScoped<IAssetsService, AssetsService>();
            services.AddScoped<IPlanService, Core.Services.Plan.PlanService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IEmailSenderService, EmailSenderService>();
            services.AddScoped<IIncomeTaxesService, IncomeTaxesService>();
            services.AddScoped<IPremiumCodeService, PremiumCodeService>();

            services.AddScoped<NotificationContext>();

            // Classes responsáveis pelos algoritmos para cálculo de imposto de renda
            services.AddScoped<IIncomeTaxesCalculator, BDRsIncomeTaxes>();
            services.AddScoped<IIncomeTaxesCalculator, ETFsIncomeTaxes>();
            services.AddScoped<IIncomeTaxesCalculator, FIIsIncomeTaxes>();
            services.AddScoped<IIncomeTaxesCalculator, GoldIncomeTaxes>();
            services.AddScoped<IIncomeTaxesCalculator, InvestmentsFundsIncomeTaxes>();
            services.AddScoped<IIncomeTaxesCalculator, StocksIncomeTaxes>();

            services.AddTransient<IJwtCommon, JwtCommon>();

            services.AddMvc(options => options.Filters.Add<NotificationFilter>());

            services.Configure<AppSettings>(options =>
            {
                options.Secret = builder.Configuration["Jwt:Token"];
                options.Issuer = builder.Configuration["Jwt:Issuer"];
                options.Audience = builder.Configuration["Jwt:Audience"];
            });
        }

        public static void AddStripeServices(this IServiceCollection services, IConfiguration configuration)
        {
            StripeConfiguration.ApiKey = configuration.GetValue<string>("StripeSettings:SecretKey");

            services.AddScoped<ChargeService>();
            services.AddScoped<CustomerService>();
            services.AddScoped<TokenService>();
        }


        public static void AddHangfireServices(this IServiceCollection services, WebApplicationBuilder builder)
        {
            services.AddScoped<IAverageTradedPriceUpdaterHangfire, AverageTradedPriceUpdaterHangfire>();
            services.AddScoped<IEmailCodeRemoverHangfire, EmailCodeRemoverHangfire>();
            services.AddScoped<IUserPlansValidityHangfire, UserPlansValidityHangfire>();
        }

        public static void AddHangFireRecurringJob(this IServiceCollection services, WebApplicationBuilder builder)
        {
            services.AddHangfire(x =>
                x.UsePostgreSqlStorage(builder.Configuration.GetConnectionString("DefaultConnection"))
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
            );

            services.AddHangfireServer();

            GlobalConfiguration.Configuration.UsePostgreSqlStorage(builder.Configuration.GetConnectionString("DefaultConnection"));

            RecurringJob.RemoveIfExists(nameof(AverageTradedPriceUpdaterHangfire));
            RecurringJob.RemoveIfExists(nameof(EmailCodeRemoverHangfire));
            RecurringJob.RemoveIfExists(nameof(UserPlansValidityHangfire));

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

            RecurringJob.AddOrUpdate<IUserPlansValidityHangfire>(
                nameof(UserPlansValidityHangfire),
                x => x.UpdateUsersPlanExpiration(),
                Cron.Daily
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
            services.AddTransient(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddTransient<IAccountRepository, AccountRepository>();
            services.AddTransient<IAverageTradedPriceRepostory, AverageTradedPriceRepository>();
            services.AddTransient<IEmailCodeRepository, EmailCodeRepository>();
            services.AddTransient<ITaxesRepository, TaxesRepository>();
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

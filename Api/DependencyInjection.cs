using Api.Database;
using Api.Generics;
using Api.Handler;
using Api.Services.Auth;
using Audit.Core;
using Audit.PostgreSql.Configuration;
using Billing.Services.Stripe;
using Common.Configurations;
using Common.Models.Handlers;
using Common.Options;
using Core.Calculators;
using Core.Calculators.Assets;
using Core.Clients.InfoSimples;
using Core.Filters;
using Core.Hangfire.PlanExpirer;
using Core.Notification;
using Core.Refit;
using Core.Refit.B3;
using Core.Services.Account;
using Core.Services.B3ResponseCalculator;
using Core.Services.B3Syncing;
using Core.Services.DarfGenerator;
using Core.Services.Hangfire.AverageTradedPriceUpdater;
using Core.Services.Plan;
using Core.Services.Taxes;
using Hangfire;
using Hangfire.IncomeTaxesAdder;
using Hangfire.PostgreSql;
using Infrastructure.Repositories;
using Infrastructure.Repositories.Account;
using Infrastructure.Repositories.AverageTradedPrice;
using Infrastructure.Repositories.BonusShare;
using Infrastructure.Repositories.Plan;
using Infrastructure.Repositories.Taxes;
using Infrastructure.UnitOfWork;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Polly;
using Refit;
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

            // TODO enquanto eu que consumir os endpoints, não teremos esta porra, porque o token gerado por mim pelo Auth0 não põe o AccountId no subject do JWT.
            //builder.Services.AddScoped<IAuthorizationHandler, CanAccessResourceHandler>();

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

            services.AddTransient<IAverageTradedPriceUpdaterHangfire, AverageTradedPriceUpdaterHangfire>();
            services.AddTransient<IPlanExpirerHangfire, PlanExpirerHangfire>();
            services.AddTransient<IIncomeTaxesAdderHangfire, IncomeTaxesAdderHangfire>();

            services.AddMvc(options => options.Filters.Add<NotificationFilter>());
        }

        public static void AddStripeServices(this IServiceCollection services, IConfiguration configuration)
        {
            StripeConfiguration.ApiKey = configuration["Secrets:Stripe:Api:Token"];

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
            });

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy(
                    "read:own_information",
                    policy => policy.Requirements.Add(
                        new CanAccessResourceRequirement("read:own_information")
                    )
                );
            });

            builder.Services.AddSingleton<IAuthorizationHandler, HasScopeHandler>();
        }

        public static void ConfigureHangfireDatabase(this IServiceCollection services, string connectionString)
        {
            services.AddHangfire(config =>
                config.UsePostgreSqlStorage(c =>
                    c.UseNpgsqlConnection(connectionString)));
        }

        public static void ConfigureHangfireServices(this WebApplication _)
        {
            RecurringJob.AddOrUpdate<IAverageTradedPriceUpdaterHangfire>(
                    nameof(AverageTradedPriceUpdaterHangfire),
                    x => x.Execute(),
                    Cron.Monthly(1));

            RecurringJob.AddOrUpdate<IIncomeTaxesAdderHangfire>(
                nameof(IncomeTaxesAdderHangfire),
                x => x.Execute(),
                Cron.Monthly(2));

            RecurringJob.AddOrUpdate<IPlanExpirerHangfire>(
                nameof(PlanExpirerHangfire),
                x => x.Execute(),
                Cron.Daily);

        }

        public static void Add3rdPartiesClients(this IServiceCollection services, IConfiguration configuration)
        {
            string? b3CertLocation = Environment.GetEnvironmentVariable("B3_CERT_LOCATION");

            if (b3CertLocation.IsNullOrEmpty())
                throw new InvalidOperationException("Configure a variável de ambiente B3_CERT_LOCATION contendo a localização do arquivo de certificação" +
                    " da API da Área Logada da B3.");

            services.AddRefitClient<IB3Refit>()
                .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://investidor.b3.com.br:2443/api"))
                .ConfigurePrimaryHttpMessageHandler(() => new B3HttpClientHandler(b3CertLocation!, configuration["Certificates:B3:Password"]))
                .AddTransientHttpErrorPolicy(policy => policy.WaitAndRetryAsync(5, _ => TimeSpan.FromSeconds(10)))
                .AddTransientHttpErrorPolicy(policy => policy.CircuitBreakerAsync(5, TimeSpan.FromSeconds(10)));

            services.AddRefitClient<IMicrosoftRefit>()
                .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://login.microsoftonline.com/"))
                .ConfigurePrimaryHttpMessageHandler(() => new B3HttpClientHandler(b3CertLocation!, configuration["Certificates:B3:Password"]))
                .AddTransientHttpErrorPolicy(policy => policy.WaitAndRetryAsync(5, _ => TimeSpan.FromSeconds(10)))
                .AddTransientHttpErrorPolicy(policy => policy.CircuitBreakerAsync(5, TimeSpan.FromSeconds(10)))
                .SetHandlerLifetime(TimeSpan.FromMinutes(5));

            services.AddHttpClient("Infosimples", c =>
                c.BaseAddress = new Uri("https://api.infosimples.com/api/v2/consultas/")).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler())
                .AddTransientHttpErrorPolicy(policy => policy.WaitAndRetryAsync(5, _ => TimeSpan.FromSeconds(10)))
                .AddTransientHttpErrorPolicy(policy => policy.CircuitBreakerAsync(5, TimeSpan.FromSeconds(10)))
                .SetHandlerLifetime(TimeSpan.FromMinutes(5));

            services.AddHttpClient("Auth0", c =>
                c.BaseAddress = new Uri("https://dev-cfdhp4yerdn6st6a.us.auth0.com/oauth/")).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler())
                .AddTransientHttpErrorPolicy(policy => policy.WaitAndRetryAsync(5, _ => TimeSpan.FromSeconds(10)))
                .AddTransientHttpErrorPolicy(policy => policy.CircuitBreakerAsync(5, TimeSpan.FromSeconds(10)))
                .SetHandlerLifetime(TimeSpan.FromMinutes(5));
        }

        public static void AddRepositories(this IServiceCollection services)
        {
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddScoped<IAccountRepository, AccountRepository>();
            services.AddScoped<IAverageTradedPriceRepostory, AverageTradedPriceRepository>();
            services.AddScoped<IIncomeTaxesRepository, IncomeTaxesRepository>();
            services.AddScoped<IPlanRepository, PlanRepository>();
            services.AddScoped<IBonusShareRepository, BonusShareRepository>();
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

        public static void AddSecretOptions(this IServiceCollection services, IConfiguration configuration)
        {
#pragma warning disable CS8601 // Possible null reference assignment.
            services.Configure<DatabaseOptions>(options =>
            {
                options.ConnectionString = configuration["ConnectionsString:Database"];
            });
            services.Configure<DatabaseEncryptionKeyOptions>(options =>
            {
                options.Value = configuration["Keys:PgCrypto"];
            });
            services.Configure<B3ApiOptions>(options =>
            {
                options.ClientId = configuration["Secrets:B3:ClientId"];
                options.ClientSecret = configuration["Secrets:B3:ClientSecret"];
                options.Scope = configuration["Secrets:B3:Scope"];
            });
            services.Configure<StripeOptions>(options =>
            {
                options.WebhookToken = configuration["Secrets:Stripe:Webhook:Token"];
                options.ApiToken = configuration["Secrets:Stripe:Api:Token"];
            });
            services.Configure<InfoSimplesOptions>(options =>
            {
                options.ApiToken = configuration["Secrets:InfoSimples:Api:Token"];
            });
#pragma warning restore CS8601 // Possible null reference assignment.
        }

        public static void AddDatabaseContext(this IServiceCollection services, string connectionString)
        {
            services.AddDbContext<StocksContext>(options =>
            {
                options.UseNpgsql(connectionString);
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            });
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        }

        public static void AddAudiTrail(this IServiceCollection _, string connectionString)
        {
            Configuration.Setup().UsePostgreSql(config => config
                .ConnectionString(connectionString)
                .TableName("Audits")
                .IdColumnName("Id")
                .DataColumn("Data", DataType.JSONB)
                .LastUpdatedColumnName("UpdatedAt")
                .CustomColumn("EventType", ev => ev.EventType));
        }
    }
}

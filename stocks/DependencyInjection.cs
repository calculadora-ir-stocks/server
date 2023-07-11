using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using stocks.Clients.B3;
using stocks.Commons.Jwt;
using stocks.Database;
using stocks.Notification;
using stocks.Repositories;
using stocks.Repositories.Account;
using stocks.Services.Auth;
using stocks.Services.B3;
using stocks.Services.IncomeTaxes;
using stocks_common;
using stocks_core.Calculators;
using stocks_core.Calculators.Assets;
using stocks_core.Services.IncomeTaxes;
using stocks_infrastructure.Repositories.AverageTradedPrice;
using stocks_infrastructure.Repositories.IncomeTaxes;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace stocks
{
    public static class DependencyInjection
    {
        [Obsolete]
        public static void AddServices(this IServiceCollection services, WebApplicationBuilder builder)
        {
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IAssetsService, AssetsService>();
            services.AddScoped<IIncomeTaxesService, IncomeTaxesService>();
            services.AddSingleton<IB3Client, B3Client>();
            services.AddScoped<NotificationContext>();

            // Classes responsáveis pelos algoritmos para cálculo de imposto de renda
            services.AddScoped<IIncomeTaxesCalculator, BDRsIncomeTaxes>();
            services.AddScoped<IIncomeTaxesCalculator, ETFsIncomeTaxes>();
            services.AddScoped<IIncomeTaxesCalculator, FIIsIncomeTaxes>();
            services.AddScoped<IIncomeTaxesCalculator, GoldIncomeTaxes>();
            services.AddScoped<IIncomeTaxesCalculator, InvestmentsFundsIncomeTaxes>();
            services.AddScoped<IIncomeTaxesCalculator, StocksIncomeTaxes>();

            services.AddTransient<IJwtCommon, JwtCommon>();

            // TODO: SetCompatibilityVersion is deprecated, fix this.
            services.AddMvc(options => options.Filters.Add<NotificationFilter>()).SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            services.Configure<AppSettings>(options =>
            {
                options.Secret = builder.Configuration["Jwt:Token"];
                options.Issuer = builder.Configuration["Jwt:Issuer"];
                options.Audience = builder.Configuration["Jwt:Audience"];
            });
        }

        public static void Add3rdPartiesClientConfigurations(this IServiceCollection services) {
            var handler = new HttpClientHandler();
            AddCertificate(handler);

            services.AddHttpClient("B3", c => c.BaseAddress = new Uri("https://apib3i-cert.b3.com.br:2443/api/"))
                .ConfigurePrimaryHttpMessageHandler(() => handler);

            services.AddHttpClient("Microsoft", c => c.BaseAddress = new Uri("https://login.microsoftonline.com/"))
                .ConfigurePrimaryHttpMessageHandler(() => handler);
        }

        private static void AddCertificate(HttpClientHandler handler)
        {
            handler.ClientCertificateOptions = ClientCertificateOption.Manual;
            handler.SslProtocols = SslProtocols.Tls12;

            // C:\Users\Biscoitinho\Documents\Certificates
            // /home/dickmann/Documents/certificates/31788887000158.pfx
            handler.ClientCertificates.Add(new X509Certificate2("C:\\Users\\Biscoitinho\\Documents\\Certificates\\31788887000158.pfx", "C3MOHH", X509KeyStorageFlags.PersistKeySet));
        }

        public static void AddRepositories(this IServiceCollection services)
        {
            services.AddTransient(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddTransient<IAccountRepository, AccountRepository>();
            services.AddTransient<IAverageTradedPriceRepostory, AverageTradedPriceRepository>();
            services.AddTransient<IIncomeTaxesRepository, IncomeTaxesRepository>();
        }

        public static void AddJwtAuthentications(this IServiceCollection services, WebApplicationBuilder builder)
        {
            services.AddAuthentication(opt => {
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
                    Title = "Stocks - plataforma para pagamento e declaração de IR.",
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

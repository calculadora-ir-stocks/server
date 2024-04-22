using Api;
using Api.Database;
using Api.Middlewares;
using Azure.Identity;
using Hangfire;

var builder = WebApplication.CreateBuilder(args);

builder.Services.InitializeEnvironmentVariables(new string[] { ".database.env", ".apis.env" });
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddHttpContextAccessor();

if (builder.Environment.IsProduction())
{
    // Sets Key Vault credentiais
    builder.Configuration.AddAzureKeyVault(new("https://server-keys-and-secrets.vault.azure.net/"), new DefaultAzureCredential());
}

string databaseConnectionString = builder.Environment.IsProduction() 
    ? builder.Configuration["ConnectionsString--Database"] 
    : builder.Configuration["ConnectionsString:Database"];

builder.Services.AddSecretsOptions(builder.Configuration, isProduction: builder.Environment.IsProduction());
builder.Services.AddDatabaseContext(databaseConnectionString);
builder.Services.AddAudiTrail(databaseConnectionString);
builder.Services.ConfigureHangfireDatabase(databaseConnectionString);

builder.Services.AddStripeServices();
builder.Services.AddServices(builder);
builder.Services.Add3rdPartiesClients();
builder.Services.AddRepositories();

builder.Services.AddAuth0Authentication(builder);

builder.Services.AddRouting(options => options.LowercaseUrls = true);

builder.Services.AddSwaggerConfiguration();

builder.Services.AddCors(policyBuilder =>
    policyBuilder.AddDefaultPolicy(policy =>
        policy.WithOrigins("*").AllowAnyHeader().SetIsOriginAllowed((host) => true))
);

var app = builder.Build();

using (var scope = app.Services.CreateAsyncScope())
{
    scope.ServiceProvider.GetRequiredService<StocksContext>();
}

app.UseCors();
app.UseMiddleware<ExceptionHandlerMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(c =>
    c.SwaggerEndpoint("v1/swagger.json", "Stocks v1")
);

// app.UseHttpsRedirection();
app.UseForwardedHeaders();

app.UseAuthentication();
app.UseRouting();
app.UseAuthorization();

app.MapControllers();

app.UseHangfireServer();
app.ConfigureHangfireServices();

app.Run();

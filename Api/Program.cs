using Api;
using Api.Database;
using Api.Middlewares;
using Azure.Identity;
using Hangfire;
using System.Collections;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddHttpContextAccessor();

if (builder.Environment.IsProduction())
{
    var credentials = Environment.GetEnvironmentVariables();

    string? azureTenantId = credentials["AZURE_TENANT_ID"]?.ToString();
    if (azureTenantId is null) throw new Exception("A vari�vel de ambiente AZURE_TENANT_ID n�o est� configurada.");

    string? azureClientId = credentials["AZURE_CLIENT_ID"]?.ToString();
    if (azureClientId is null) throw new Exception("A vari�vel de ambiente AZURE_CLIENT_ID n�o est� configurada.");

    string? azureSecretId = credentials["AZURE_CLIENT_SECRET"]?.ToString();
    if (azureSecretId is null) throw new Exception("A vari�vel de ambiente AZURE_CLIENT_SECRET n�o est� configurada.");

    // Sets Key Vault credentiais
    builder.Configuration.AddAzureKeyVault(new("https://server-keys-and-secrets.vault.azure.net/"), new ClientSecretCredential(
        azureTenantId,
        azureClientId,
        azureSecretId));
}

builder.Services.AddSecretOptions(builder.Configuration);

builder.Services.AddDatabaseContext(builder.Configuration["ConnectionsString:Database"]);
builder.Services.AddAudiTrail(builder.Configuration["ConnectionsString:Database"]);
// builder.Services.ConfigureHangfireDatabase(builder.Configuration["ConnectionsString:Database"]);

builder.Services.AddStripeServices(builder.Configuration);
builder.Services.AddServices(builder);
builder.Services.Add3rdPartiesClients(builder.Configuration);
builder.Services.AddRepositories();

builder.Services.AddAuth0Authentication(builder);

builder.Services.AddRouting(options => options.LowercaseUrls = true);

builder.Services.AddSwaggerConfiguration();

builder.Services.AddCors(policyBuilder =>
    policyBuilder.AddDefaultPolicy(policy =>
        policy.WithOrigins("*").AllowAnyHeader().SetIsOriginAllowed((host) => true))
);

var app = builder.Build();

app.Logger.LogInformation($"Environment is production: {builder.Environment.IsProduction()}");

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

// app.UseHangfireServer();
// app.ConfigureHangfireServices();

app.Run();

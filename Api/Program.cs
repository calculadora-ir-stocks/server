using Api;
using Api.Database;
using Api.Middlewares;
using Azure.Identity;
using Hangfire;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddHttpContextAccessor();

//if (builder.Environment.IsProduction())
//{
//    var credentials = Environment.GetEnvironmentVariables();

//    string? azureTenantId = credentials["AZURE_TENANT_ID"]?.ToString();
//    if (azureTenantId is null) throw new Exception("A variável de ambiente AZURE_TENANT_ID não está configurada.");

//    string? azureClientId = credentials["AZURE_CLIENT_ID"]?.ToString();
//    if (azureClientId is null) throw new Exception("A variável de ambiente AZURE_CLIENT_ID não está configurada.");

//    string? azureSecretId = credentials["AZURE_CLIENT_SECRET"]?.ToString();
//    if (azureSecretId is null) throw new Exception("A variável de ambiente AZURE_CLIENT_SECRET não está configurada.");

//    // Sets Key Vault credentiais
//    builder.Configuration.AddAzureKeyVault(new("https://server-keys-and-secrets.vault.azure.net/"), new ClientSecretCredential(
//        azureTenantId,
//        azureClientId,
//        azureSecretId));
//}

//builder.Services.AddSecretOptions(builder.Configuration);

//builder.Services.AddDatabaseContext(builder.Configuration["ConnectionsString:Database"]);
//builder.Services.AddAudiTrail(builder.Configuration["ConnectionsString:Database"]);
//builder.Services.ConfigureHangfireDatabase(builder.Configuration["ConnectionsString:Database"]);

//builder.Services.AddStripeServices(builder.Configuration);
//builder.Services.AddServices(builder);
//builder.Services.Add3rdPartiesClients(builder.Configuration);
//builder.Services.AddRepositories();

//builder.Services.AddAuth0Authentication(builder);

builder.Services.AddRouting(options => options.LowercaseUrls = true);

builder.Services.AddSwaggerConfiguration();

builder.Services.AddCors(policyBuilder =>
    policyBuilder.AddDefaultPolicy(policy =>
        policy.WithOrigins("*").AllowAnyHeader().SetIsOriginAllowed((host) => true))
);

var app = builder.Build();

var credentials = Environment.GetEnvironmentVariables();
foreach(var credential in credentials)
{
    app.Logger.LogInformation("Envs: " + credential.ToString());
}

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

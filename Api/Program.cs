using Api;
using Api.Database;
using Api.Middlewares;
using Hangfire;

var builder = WebApplication.CreateBuilder(args);

builder.Services.InitializeEnvironmentVariables(new string[] { ".database.env", ".apis.env"});
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddHttpContextAccessor();

builder.Services.AddDatabase(builder);

builder.Services.AddStripeServices();
builder.Services.AddServices(builder);
builder.Services.Add3rdPartiesClients();
builder.Services.AddRepositories();


//builder.Services.AddHangfireServices();
//builder.Services.ConfigureHangfireServices(builder);

builder.Services.AddAuth0Authentication(builder);

builder.Services.AddRouting(options => options.LowercaseUrls = true);

builder.Services.AddSwaggerConfiguration();

builder.Services.AddCors(policyBuilder =>
    policyBuilder.AddDefaultPolicy(policy =>
        policy.WithOrigins("*").AllowAnyHeader().AllowAnyHeader().SetIsOriginAllowed((host) => true))
);

var app = builder.Build();

// app.UseHangfireDashboard("/dashboard");
app.UseCors();

// app.UseMiddleware<AuthorizationMiddleware>();
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

app.Run();

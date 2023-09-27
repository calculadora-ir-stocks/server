using Api;
using Api.Middlewares;
using Hangfire;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddHttpContextAccessor();


builder.Services.AddStripeServices(builder.Configuration);
builder.Services.AddServices(builder);
builder.Services.Add3rdPartiesClients();
builder.Services.AddRepositories();

builder.Services.AddDatabase(builder);

builder.Services.AddJwtAuthentications(builder);

var app = builder.Build();

//builder.Services.AddHangfireServices();
//builder.Services.ConfigureHangfireServices(builder);
//app.UseHangfireDashboard("/dashboard");

app.UseMiddleware<JwtMiddleware>();
app.UseMiddleware<CustomExceptionHandlerMiddleware>();

// Obligatory lower case routing
builder.Services.AddRouting(options => options.LowercaseUrls = true);

builder.Services.AddSwaggerConfiguration();
app.UseSwagger();

app.UseSwaggerUI(c =>
    c.SwaggerEndpoint("v1/swagger.json", "Stocks v1")
);

// app.UseHttpsRedirection();

app.UseAuthentication();

app.UseRouting();

app.UseAuthorization();

app.MapControllers();

app.Run();

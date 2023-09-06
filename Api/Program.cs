using Api;
using Api.Middlewares;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();

builder.Services.AddStripeServices(builder.Configuration);
builder.Services.AddServices(builder);
// builder.Services.AddHangfireServices(builder);
builder.Services.Add3rdPartiesClientServices();

builder.Services.AddRepositories();
builder.Services.AddJwtAuthentications(builder);

// Obligatory lower case routing
builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder.Services.AddDatabase(builder);
// builder.Services.AddHangFireRecurringJob(builder);
builder.Services.AddSwaggerConfiguration();


var app = builder.Build();

// app.UseHangfireDashboard("/dashboard");

app.UseSwagger();

app.UseSwaggerUI(c =>
    c.SwaggerEndpoint("v1/swagger.json", "Stocks v1")
);

app.UseMiddleware<JwtMiddleware>();
app.UseMiddleware<CustomExceptionHandlerMiddleware>();

// app.UseHttpsRedirection();

app.UseAuthentication();

app.UseRouting();

app.UseAuthorization();

app.MapControllers();

app.Run();

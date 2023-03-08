using stocks;
using stocks.Middlewares;
using stocks_common;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();

builder.Services.AddServices(builder);
builder.Services.Add3rdPartiesClientConfigurations();
builder.Services.AddRepositories();
builder.Services.AddJwtAuthentications(builder);
// Obligatory lower case routing
builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder.Services.AddDatabase(builder);
builder.Services.AddSwaggerConfiguration();

builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseSwaggerUI(c => 
        c.SwaggerEndpoint("v1/swagger.json", "Stocks v1")
    );

    app.UseMiddleware<JwtMiddleware>();
    app.UseMiddleware<CustomExceptionHandlerMiddleware>();
}

// app.UseHttpsRedirection();

app.UseAuthentication();

app.UseRouting();

app.UseAuthorization();

app.MapControllers();

app.Run();

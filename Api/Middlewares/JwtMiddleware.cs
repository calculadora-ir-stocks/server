using Infrastructure.Repositories;
using Api.Services.Jwt;
using Infrastructure.Models;

namespace Api.Middlewares;
public class JwtMiddleware
{

    private readonly RequestDelegate _next;

    public JwtMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context, IGenericRepository<Account> repository, IJwtCommon jwtCommon)
    {
        var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

        // TO-DO: not working

        Guid? userId = jwtCommon.CreateToken(token);

        if (userId != null)
        {
            context.Items["User"] = repository.GetById(userId.Value);
        }

        await _next(context);
    }
}
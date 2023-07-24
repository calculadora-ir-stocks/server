using stocks.Commons.Jwt;
using stocks.Repositories;
using stocks_infrastructure.Models;

namespace stocks.Middlewares;
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
        Guid? userId = jwtCommon.CreateToken(token);

        if (userId != null)
        {
            context.Items["User"] = repository.GetById(userId.Value);
        }

        await _next(context);
    }
}
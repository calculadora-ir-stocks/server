using Infrastructure.Repositories;
using Api.Services.JwtCommon;
using Infrastructure.Models;

namespace Api.Middlewares;
public class JwtMiddleware
{

    private readonly RequestDelegate next;

    public JwtMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public async Task Invoke(HttpContext context, IGenericRepository<Account> repository, IJwtCommonService jwtCommon)
    {
        var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

        Guid? userId = jwtCommon.CreateToken(token);

        if (userId != null)
        {
            context.Items["User"] = repository.GetById(userId.Value);
        }

        await next(context);
    }
}
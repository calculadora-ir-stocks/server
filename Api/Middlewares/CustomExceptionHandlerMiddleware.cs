using Common.Exceptions;
using Newtonsoft.Json;
using System.Net;

namespace Api.Middlewares
{
    public class CustomExceptionHandlerMiddleware
    {
        private readonly RequestDelegate next;

        public CustomExceptionHandlerMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (Exception error)
            {
                var response = context.Response;
                response.ContentType = "application/json";

                response.StatusCode = error switch
                {
                    BadRequestException => (int)HttpStatusCode.BadRequest,
                    NotFoundException => (int)HttpStatusCode.NotFound,
                    ForbiddenException => (int)HttpStatusCode.Forbidden,
                    InternalServerErrorException => (int)HttpStatusCode.InternalServerError,
                    KeyNotFoundException _ => (int)HttpStatusCode.NotFound,
                    InvalidOperationException => (int)HttpStatusCode.Unauthorized, // thrown exception by Auth0 when a JWT is invalid
                    _ => (int)HttpStatusCode.InternalServerError,
                };

                if (error is InvalidOperationException)
                {
                    return;
                }

                await response.WriteAsync(
                    JsonConvert.SerializeObject(new List<Core.Notification.Notification> { new Core.Notification.Notification(error?.Message) })
                );
            }
        }
    }
}

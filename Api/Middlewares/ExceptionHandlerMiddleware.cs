using Common.Configurations;
using Common.Exceptions;
using Newtonsoft.Json;
using System.Net;

namespace Api.Middlewares
{
    public class ExceptionHandlerMiddleware
    {
        private readonly RequestDelegate next;
        private readonly JsonSerializerConfiguration configuration;

        public ExceptionHandlerMiddleware(RequestDelegate next, JsonSerializerConfiguration configuration)
        {
            this.next = next;
            this.configuration = configuration;
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
                    _ => (int)HttpStatusCode.InternalServerError,
                };

                await response.WriteAsync(
                    JsonConvert.SerializeObject(new Core.Notification.Notification(error?.Message),
                    configuration.Settings)
                );
            }
        }
    }
}

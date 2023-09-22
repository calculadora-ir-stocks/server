using Newtonsoft.Json;
using Api.Exceptions;
using Common.Exceptions;
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

                switch (error)
                {
                    case BadRequestException:
                        response.StatusCode = (int)HttpStatusCode.BadRequest;
                        break;
                    case NotFoundException:
                        response.StatusCode = (int)HttpStatusCode.NotFound;
                        break;
                    case ForbiddenException:
                        response.StatusCode = (int)HttpStatusCode.Forbidden;
                        break;
                    case InternalServerErrorException:
                        response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        break;
                    case KeyNotFoundException _:
                        response.StatusCode = (int)HttpStatusCode.NotFound;
                        break;
                    default:
                        response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        break;
                }

                await response.WriteAsync(JsonConvert.SerializeObject(new List<Notification.Notification> { new Notification.Notification(error?.Message) }));
            }
        }
    }
}

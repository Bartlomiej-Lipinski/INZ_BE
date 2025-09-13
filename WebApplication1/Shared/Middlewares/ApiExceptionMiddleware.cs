using System.Diagnostics;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Shared.Middlewares;

public class ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
            
            //TODO: Możemy tutaj zwracać różne kody odpowiedzi w zależności od rodzaju błędu
            logger.LogError(ex, "Error occurred. TraceId: {TraceId}, Message: {Message}", traceId, ex.Message);
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = 500;

            var error = new ApiErrorResponse
            {
                Error = new ApiError
                {
                    Code = "INTERNAL_SERVER_ERROR",
                    Message = ex.Message,
                    TraceId = traceId
                }
            };

            await context.Response.WriteAsJsonAsync(error);
        }
    }
}
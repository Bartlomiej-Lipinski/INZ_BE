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
            //TODO: Możemy tutaj zwracać różne kody odpowiedzi w zależności od rodzaju błędu
            logger.LogError(ex, ex.Message);
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = 500;

            var error = new ApiErrorResponse
            {
                Error = new ApiError
                {
                    Code = "INTERNAL_SERVER_ERROR",
                    Message = ex.Message
                }
            };

            await context.Response.WriteAsJsonAsync(error);
        }
    }
}
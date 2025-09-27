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
            int statusCode = ex switch
            {
                ArgumentException => 400,
                UnauthorizedAccessException => 401,
                KeyNotFoundException => 404,
                _ => 500
            };
            string errorMessage = statusCode switch
            {
                400 => "Nieprawidłowe żądanie.",
                401 => "Brak autoryzacji.",
                404 => "Nie znaleziono zasobu.",
                500 => "Wystąpił błąd serwera. Skontaktuj się z administratorem.",
                _ => "Wystąpił błąd."
            };
            //TODO: Możemy tutaj zwracać różne kody odpowiedzi w zależności od rodzaju błędu
            logger.LogError(ex, "Error occurred. TraceId: {TraceId}", traceId);
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;
           
            var error = new ApiErrorResponse
            {
                Error = new ApiError
                {
                    Code = statusCode.ToString(),
                    Message = errorMessage,
                    TraceId = traceId
                }
            };

            await context.Response.WriteAsJsonAsync(error);
        }
    }
}
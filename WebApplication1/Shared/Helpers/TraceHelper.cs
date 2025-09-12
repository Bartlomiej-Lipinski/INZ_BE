using System.Diagnostics;

namespace WebApplication1.Shared.Helpers;

public static class TraceHelper
{
    public static string GetTraceId(HttpContext httpContext)
    {
        return Activity.Current?.Id ?? httpContext.TraceIdentifier;
    }
    
    public static void LogWithTrace(ILogger logger, LogLevel level, string message, string traceId, params object[] args)
    {
        var argsWithTrace = args.Concat([traceId]).ToArray();
        logger.Log(level, message + ". TraceId: {TraceId}", argsWithTrace);
    }
}

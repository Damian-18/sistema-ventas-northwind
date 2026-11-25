namespace Equipo5.Api.Infrastructure;

public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(static state =>
        {
            var response = ((HttpContext)state).Response;
            response.Headers["X-Frame-Options"] = "DENY";
            response.Headers["Content-Security-Policy"] = "frame-ancestors 'none'";
            response.Headers["X-Content-Type-Options"] = "nosniff";
            response.Headers["Referrer-Policy"] = "no-referrer";
            return Task.CompletedTask;
        }, context);

        await next(context);
    }
}

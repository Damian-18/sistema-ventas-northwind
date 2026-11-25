using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;

namespace Equipo5.Api.Infrastructure;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var mysql = exception as MySqlException ?? exception.InnerException as MySqlException;
        var status = exception is DbUpdateConcurrencyException || mysql?.Number is 1062 or 1451 or 1452 ? 409
            : mysql is not null ? 503 : 500;
        var title = status switch { 409 => "Conflicto con el estado actual de los datos", 503 => "Base de datos no disponible", _ => "Error interno" };
        logger.LogError(exception, "Error HTTP {Status}", status);
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status, Title = title,
            Detail = status == 409 ? "El registro cambió, está relacionado con otros datos o contiene un valor duplicado. Vuelve a consultar antes de reintentar." : "Revisa el registro del servidor.",
            Extensions = { ["traceId"] = context.TraceIdentifier }
        }, ct);
        return true;
    }
}

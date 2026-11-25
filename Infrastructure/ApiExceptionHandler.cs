using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;

namespace Equipo5.Api.Infrastructure;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        MySqlException? mysql = null;
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is MySqlException mysqlException)
            {
                mysql = mysqlException;
                break;
            }
        }

        var status = exception is DbUpdateConcurrencyException || mysql?.Number is 1062 or 1451 or 1452 ? 409
            : mysql is not null ? 503 : 500;
        var title = status switch
        {
            409 => "Conflicto con el estado actual de los datos",
            503 => "Base de datos no disponible",
            _ => "Error interno"
        };
        var detail = status switch
        {
            409 => "La solicitud entra en conflicto con los datos actuales. Revisa la información e inténtalo de nuevo.",
            503 => "El servicio de datos no está disponible. Inténtalo más tarde.",
            _ => "Ocurrió un error inesperado. Inténtalo más tarde."
        };
        logger.LogError(exception, "Error HTTP {Status}", status);
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status, Title = title,
            Detail = detail,
            Extensions = { ["traceId"] = context.TraceIdentifier }
        }, ct);
        return true;
    }
}

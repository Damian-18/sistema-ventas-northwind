using System.Collections;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Equipo5.Api.Infrastructure;

public static class InputSanitizer
{
    private static readonly Regex UnsafeText = new(
        @"<\s*/?\s*[a-z][^>]*>|<\s*(?:script|iframe|object|embed)\b|\bon[a-z][a-z0-9_-]*\s*=|javascript\s*:",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static bool TrySanitizeText(string value, out string sanitized)
    {
        sanitized = value.Trim();
        return IsSafeText(sanitized);
    }

    public static bool IsSafeText(string value) =>
        !UnsafeText.IsMatch(value) && !UnsafeText.IsMatch(WebUtility.HtmlDecode(value));

    public static bool TrySanitize(object? value, out string? error)
    {
        error = null;
        return SanitizeValue(value, new HashSet<object>(ReferenceEqualityComparer.Instance), out error);
    }

    private static bool SanitizeValue(object? value, HashSet<object> visited, out string? error)
    {
        error = null;
        if (value is null || value is byte[])
            return true;

        if (value is string)
            return true;

        var type = value.GetType();
        if (type.IsValueType || !visited.Add(value))
            return true;

        if (value is IList list)
        {
            for (var index = 0; index < list.Count; index++)
            {
                if (list[index] is string item)
                {
                    if (!TrySanitizeText(item, out var sanitized))
                    {
                        error = "El texto contiene etiquetas HTML o código activo no permitido.";
                        return false;
                    }

                    if (!list.IsReadOnly)
                        list[index] = sanitized;
                }
                else if (!SanitizeValue(list[index], visited, out error))
                {
                    return false;
                }
            }

            return true;
        }

        foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!property.CanRead || property.GetIndexParameters().Length != 0)
                continue;

            var propertyValue = property.GetValue(value);
            if (propertyValue is string text)
            {
                if (property.Name.EndsWith("Password", StringComparison.OrdinalIgnoreCase))
                {
                    // Preserve credential whitespace while still rejecting active markup.
                    if (!IsSafeText(text))
                    {
                        error = "El texto contiene etiquetas HTML o código activo no permitido.";
                        return false;
                    }

                    continue;
                }

                if (!TrySanitizeText(text, out var sanitized))
                {
                    error = "El texto contiene etiquetas HTML o código activo no permitido.";
                    return false;
                }

                if (property.SetMethod?.IsPublic == true)
                    property.SetValue(value, sanitized);
            }
            else if (!SanitizeValue(propertyValue, visited, out error))
            {
                return false;
            }
        }

        return true;
    }
}

public sealed class InputSanitizerFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        foreach (var key in context.ActionArguments.Keys.ToArray())
        {
            var value = context.ActionArguments[key];
            if (value is string text)
            {
                if (!InputSanitizer.TrySanitizeText(text, out var sanitized))
                {
                    Reject(context);
                    return;
                }

                context.ActionArguments[key] = sanitized;
            }
            else if (!InputSanitizer.TrySanitize(value, out _))
            {
                Reject(context);
                return;
            }
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }

    private static void Reject(ActionExecutingContext context)
    {
        var problem = new ValidationProblemDetails(new Dictionary<string, string[]>
        {
            ["input"] = ["El texto contiene etiquetas HTML o código activo no permitido."]
        })
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "La solicitud contiene texto no permitido.",
            Instance = context.HttpContext.Request.Path
        };
        context.Result = new BadRequestObjectResult(problem);
    }
}

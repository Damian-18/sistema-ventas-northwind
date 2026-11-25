namespace Equipo5.Api.Infrastructure;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Empleado = "Empleado";
    public const string Cliente = "Cliente";

    public static bool IsValid(string? role) => role is Admin or Empleado or Cliente;
}

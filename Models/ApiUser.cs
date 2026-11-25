using System.Text.Json.Serialization;

namespace Equipo5.Api.Models;

// Entidad propia de la API; no es un empleado ni un cliente de Northwind.
public class ApiUser
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    [JsonIgnore] public string NormalizedUsername { get; set; } = "";
    [JsonIgnore] public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "Lector";
    public bool IsActive { get; set; } = true;
    [JsonIgnore] public string SecurityStamp { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockoutEnd { get; set; }
    public string? CustomerId { get; set; }
    public int? EmployeeId { get; set; }
}

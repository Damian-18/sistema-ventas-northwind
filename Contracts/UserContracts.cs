using System.ComponentModel.DataAnnotations;

namespace Equipo5.Api.Contracts;

public class LoginRequest
{
    [Required, StringLength(50)] public string Username { get; set; } = "";
    [Required, StringLength(128)] public string Password { get; set; } = "";
}
public class RegisterRequest
{
    [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
    [RegularExpression(@"^[a-zA-Z0-9_.-]{3,50}$", ErrorMessage = "El nombre de usuario debe tener entre 3 y 50 caracteres válidos.")]
    public string Username { get; set; } = "";

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [StringLength(128, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 128 caracteres.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)[\s\S]*$", ErrorMessage = "La contraseña debe incluir mayúscula, minúscula y número.")]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "El nombre de la empresa es obligatorio.")]
    [StringLength(40, ErrorMessage = "El nombre de la empresa no puede exceder 40 caracteres.")]
    public string CompanyName { get; set; } = "";

    [StringLength(30, ErrorMessage = "El nombre de contacto no puede exceder 30 caracteres.")]
    public string? ContactName { get; set; }

    [StringLength(24, ErrorMessage = "El teléfono no puede exceder 24 caracteres.")]
    public string? Phone { get; set; }

    [StringLength(60, ErrorMessage = "La dirección no puede exceder 60 caracteres.")]
    public string? Address { get; set; }

    [StringLength(15, ErrorMessage = "La ciudad no puede exceder 15 caracteres.")]
    public string? City { get; set; }

    [StringLength(15, ErrorMessage = "El país no puede exceder 15 caracteres.")]
    public string? Country { get; set; }
}
public record UserView(
    int Id,
    string Username,
    string Role,
    bool IsActive,
    DateTime CreatedAtUtc,
    string? CustomerId,
    int? EmployeeId,
    int FailedLoginAttempts,
    DateTime? LockoutEnd,
    bool IsLockedOut);
public record LoginResponse(string AccessToken, string TokenType, DateTime ExpiresAtUtc, UserView User);

public class CreateUserRequest
{
    [Required, RegularExpression(@"^[a-zA-Z0-9_.-]{3,50}$")]
    public string Username { get; set; } = "";
    [Required, StringLength(128, MinimumLength = 12)] public string Password { get; set; } = "";
    [Required] public string Role { get; set; } = "";
}
public class UpdateUserRequest
{
    [Required, RegularExpression(@"^[a-zA-Z0-9_.-]{3,50}$")]
    public string Username { get; set; } = "";
    [Required] public string Role { get; set; } = "";
    [Required] public bool? IsActive { get; set; }
}
public class ChangePasswordRequest
{
    [Required, StringLength(128)] public string CurrentPassword { get; set; } = "";
    [Required, StringLength(128, MinimumLength = 12)] public string NewPassword { get; set; } = "";
}
public class ResetPasswordRequest
{
    [Required, StringLength(128, MinimumLength = 12)] public string NewPassword { get; set; } = "";
}

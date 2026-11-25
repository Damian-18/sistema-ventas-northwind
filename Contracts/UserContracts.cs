using System.ComponentModel.DataAnnotations;

namespace Equipo5.Api.Contracts;

public class LoginRequest
{
    [Required, StringLength(50)] public string Username { get; set; } = "";
    [Required, StringLength(128)] public string Password { get; set; } = "";
}
public record UserView(int Id, string Username, string Role, bool IsActive, DateTime CreatedAtUtc);
public record LoginResponse(string AccessToken, string TokenType, DateTime ExpiresAtUtc, UserView User);

public class CreateUserRequest
{
    [Required, RegularExpression(@"^[a-zA-Z0-9_.-]{3,50}$")]
    public string Username { get; set; } = "";
    [Required, StringLength(128, MinimumLength = 12)] public string Password { get; set; } = "";
    [Required, RegularExpression("^(Admin|Editor|Lector)$")] public string Role { get; set; } = "Lector";
}
public class UpdateUserRequest
{
    [Required, RegularExpression(@"^[a-zA-Z0-9_.-]{3,50}$")]
    public string Username { get; set; } = "";
    [Required, RegularExpression("^(Admin|Editor|Lector)$")] public string Role { get; set; } = "Lector";
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

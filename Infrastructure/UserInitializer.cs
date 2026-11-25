using System.ComponentModel.DataAnnotations;
using Equipo5.Api.Contracts;
using Equipo5.Api.Data;
using Equipo5.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Equipo5.Api.Infrastructure;

public static class UserInitializer
{
    // Comando explícito; el arranque normal no necesita permisos CREATE ni crea usuarios por defecto.
    public static async Task RunAsync(IServiceProvider services, IConfiguration config)
    {
        var request = new CreateUserRequest
        {
            Username = config["Bootstrap:Username"] ?? "", Password = config["Bootstrap:Password"] ?? "", Role = "Admin"
        };
        Validator.ValidateObject(request, new ValidationContext(request), validateAllProperties: true);
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UsuariosDB>();
        var sqlPath = Path.Combine(AppContext.BaseDirectory, "Database", "001_api_users.sql");
        await db.Database.ExecuteSqlRawAsync(await File.ReadAllTextAsync(sqlPath));
        if (await db.Users.AnyAsync()) throw new InvalidOperationException("Ya hay usuarios. El inicializador no reemplaza ni restablece cuentas existentes.");
        var user = new ApiUser { Username = request.Username, NormalizedUsername = request.Username.ToUpperInvariant(), Role = "Admin" };
        user.PasswordHash = scope.ServiceProvider.GetRequiredService<IPasswordHasher<ApiUser>>().HashPassword(user, request.Password);
        db.Users.Add(user); await db.SaveChangesAsync();
        Console.WriteLine("Tabla api_users e inicialización del administrador completadas. La contraseña no se imprime.");
    }
}

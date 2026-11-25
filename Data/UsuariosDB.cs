using Equipo5.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Equipo5.Api.Data;

// Contexto separado para no alterar ni regenerar el scaffold de Northwind.
public class UsuariosDB(DbContextOptions<UsuariosDB> options) : DbContext(options)
{
    public DbSet<ApiUser> Users => Set<ApiUser>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var user = modelBuilder.Entity<ApiUser>();
        user.ToTable("api_users");
        user.HasKey(u => u.Id);
        user.Property(u => u.Username).HasMaxLength(50).IsRequired();
        user.Property(u => u.NormalizedUsername).HasMaxLength(50).IsRequired();
        user.HasIndex(u => u.NormalizedUsername).IsUnique();
        user.Property(u => u.PasswordHash).HasMaxLength(512).IsRequired();
        user.Property(u => u.Role).HasMaxLength(16).IsRequired();
        user.Property(u => u.SecurityStamp).HasMaxLength(32).IsConcurrencyToken().IsRequired();
        user.Property(u => u.CreatedAtUtc).HasColumnType("datetime(6)");
    }
}

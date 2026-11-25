using System.Text;
using System.Threading.RateLimiting;
using Equipo5.Api.Data;
using Equipo5.Api.Infrastructure;
using Equipo5.Api.Models;
using Equipo5.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args.Where(a => a != "--init-users").ToArray());
var config = builder.Configuration;
var key = config["Jwt:Key"] ?? "";
if (Encoding.UTF8.GetByteCount(key) < 32 || string.IsNullOrWhiteSpace(config["Jwt:Issuer"])
    || string.IsNullOrWhiteSpace(config["Jwt:Audience"]) || config.GetValue<int>("Jwt:ExpirationMinutes") is < 1 or > 1440)
    throw new InvalidOperationException("Revisa Jwt: Key debe tener al menos 32 bytes; Issuer, Audience y ExpirationMinutes son obligatorios.");

builder.Services.AddDbContext<ContextoDB>(o => o.UseMySQL(config.GetConnectionString("Northwind")!));
builder.Services.AddDbContext<UsuariosDB>(o => o.UseMySQL(config.GetConnectionString("Northwind")!));
builder.Services.AddScoped<IPasswordHasher<ApiUser>, PasswordHasher<ApiUser>>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<NorthwindService>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidIssuer = config["Jwt:Issuer"],
        ValidateAudience = true, ValidAudience = config["Jwt:Audience"],
        ValidateLifetime = true, RequireExpirationTime = true,
        ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
        ValidAlgorithms = [SecurityAlgorithms.HmacSha256], ClockSkew = TimeSpan.Zero,
        NameClaimType = "name", RoleClaimType = "role"
    };
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            if (!int.TryParse(context.Principal?.FindFirst("sub")?.Value, out var id)) { context.Fail("Token inválido."); return; }
            var db = context.HttpContext.RequestServices.GetRequiredService<UsuariosDB>();
            var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == id, context.HttpContext.RequestAborted);
            if (user is null || !user.IsActive || user.SecurityStamp != context.Principal?.FindFirst("stamp")?.Value
                || user.Role != context.Principal?.FindFirst("role")?.Value)
                context.Fail("Sesión revocada.");
        }
    };
});
builder.Services.AddAuthorization(o => o.AddPolicy("WriteCatalog", p => p.RequireRole("Admin", "Editor")));
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Equipo 5 — Northwind y usuarios", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT",
        Description = "Pega solamente accessToken."
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference("Bearer", document)] = [] });
    options.OperationFilter<PublicEndpointFilter>();
});

var app = builder.Build();
if (args.Contains("--init-users")) { await UserInitializer.RunAsync(app.Services, config); return; }
app.UseExceptionHandler();
app.UseRouting();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
// IIS/Production no publica Swagger. Development conserva las pruebas locales.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(o => o.SwaggerEndpoint("v1/swagger.json", "Equipo 5 v1"));
}
app.MapControllers();
app.Run();

public class PublicEndpointFilter : Swashbuckle.AspNetCore.SwaggerGen.IOperationFilter
{
    public void Apply(OpenApiOperation operation, Swashbuckle.AspNetCore.SwaggerGen.OperationFilterContext context)
    {
        if (context.ApiDescription.ActionDescriptor.EndpointMetadata.OfType<Microsoft.AspNetCore.Authorization.IAllowAnonymous>().Any())
            operation.Security = [];
    }
}

# Equipo 5 — CRUD y usuarios

Proyecto actualizado directamente en `C:\Users\Damian\Documents\API\Equipo5.Api`.

## Configuración local y secretos

`appsettings.json` contiene placeholders para la conexión MySQL y `Jwt:Key`; no ejecutes la API con esos valores. Para desarrollo local, inicializa User Secrets una sola vez desde la carpeta del proyecto y carga tus valores:

```powershell
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:Northwind" "Server=localhost;Port=3306;Database=nwind;User ID=TU_USUARIO;Password=TU_PASSWORD;"
dotnet user-secrets set "Jwt:Key" "TU_CLAVE_ALEATORIA_DE_AL_MENOS_32_BYTES"
dotnet user-secrets set "Cors:AllowedOrigins:0" "http://localhost:5500"
```

En producción, configura los mismos valores como variables de entorno: `ConnectionStrings__Northwind`, `Jwt__Key` y `Cors__AllowedOrigins__0` (agrega índices consecutivos para más orígenes). Para un proxy inverso, configura también `ForwardedHeaders__KnownProxies__0` con la IP del proxy confiable (índices consecutivos para varios). En Azure, usa Application settings/Connection strings del servicio. No guardes credenciales ni claves reales en `appsettings.json` ni en el repositorio. Las listas de orígenes CORS y proxies confiables están vacías de forma predeterminada; agrega los valores exactos de tu entorno.

## Qué cambió

- La API está orientada al cliente web Northwind y gestiona su propio catálogo y autenticación.
- Productos, categorías y proveedores tienen listado, consulta por ID, creación, actualización y eliminación.
- Se conserva GET /api/clientes, solo de consulta.
- Los usuarios ahora se guardan en la tabla `api_users` de la misma base configurada. Los modelos y el contexto de Northwind se conservan.
- El contexto `UsuariosDB` está separado para evitar que un scaffolding de Northwind reemplace la configuración de autenticación.
- Si vuelves a generar el scaffold, selecciona explícitamente las tablas originales de Northwind con `--table` y excluye `api_users`. No ejecutes el antiguo scaffold global con `--force` sobre Models: podría sobrescribir la clase ApiUser propia de la API.
- JWT usa una clave aleatoria nueva de esta instalación. Los tokens antiguos y el antiguo login DemoUser dejaron de ser válidos.
- Swagger está disponible en Development y deshabilitado en Production/IIS.

## Primer acceso en esta computadora

La tabla y el administrador inicial **ya fueron creados**. No repitas la inicialización.

El usuario y la contraseña inicial están en el archivo privado `ADMINISTRADOR_LOCAL.md` entregado por separado. No está en el proyecto ni debe entregarse al Equipo 2.

1. Abre Equipo5.Api.csproj o Equipo5.Api.slnx en Visual Studio.
2. Compila y ejecuta el perfil http.
3. En http://localhost:5055/swagger, haz POST /api/auth/login con el administrador.
4. Pega accessToken en Authorize (solo el token).
5. Cambia tu contraseña con PUT /api/auth/password. Esto revoca el token actual; vuelve a iniciar sesión.
6. Crea una cuenta para el Equipo 2 con POST /api/usuarios. Usa rol Editor si su frontend debe realizar CRUD, o Lector si solo consultará.

Ejemplo de creación (sustituye la contraseña de ejemplo por una nueva):

```json
{
  "username": "equipo2",
  "password": "ElegirUnaClaveNueva!2026",
  "role": "Editor"
}
```

No hay registro público: solo Admin crea usuarios. Los nombres tienen de 3 a 50 caracteres ASCII, con letras, números, punto, guion o guion bajo. Son únicos sin distinguir mayúsculas. Las contraseñas nuevas tienen entre 12 y 128 caracteres; se guardan con PasswordHasher de ASP.NET Core, con salt y hash, nunca en texto plano.

## Roles

| Función | Lector | Editor | Admin |
|---|---|---|---|
| Consultar catálogos y clientes | Sí | Sí | Sí |
| Crear, actualizar y borrar catálogos | No | Sí | Sí |
| Ver su perfil, cambiar su contraseña y cerrar sesión | Sí | Sí | Sí |
| Listar, crear y administrar usuarios | No | No | Sí |

Se valida la firma y vigencia del JWT, y en cada petición protegida se verifica en MySQL que el usuario siga activo y que su versión de sesión y rol coincidan. Cambiar un usuario, su contraseña o su rol, desactivarlo o cerrar sesión invalida sus tokens anteriores. Logout cierra todas las sesiones de esa cuenta. No hay refresh token.

El login y el cambio de contraseña tienen un límite compartido de 10 solicitudes por minuto por IP, en memoria por proceso. Al excederlo devuelve 429; espera un minuto. Es protección básica para esta práctica, no un sistema distribuido de prevención de abuso.

## Endpoints

Para cada base `/api/productos`, `/api/categorias`, `/api/proveedores`:

| Método y ruta | Acción | Respuesta correcta |
|---|---|---|
| GET base?limit=20&offset=0 | Listar | 200 con arreglo |
| GET base/{id} | Consultar | 200 con objeto |
| POST base | Crear | 201 con objeto y Location |
| PUT base/{id} | Reemplazar campos editables | 204 sin cuerpo |
| DELETE base/{id} | Eliminar | 204 sin cuerpo |

Todos requieren JWT. POST, PUT y DELETE requieren Editor o Admin. Los ID son generados por la base. En PUT se envía el formulario completo: campos opcionales omitidos se restablecen a null o al valor predeterminado, no se conservan como en PATCH.

No se borran productos que tengan detalles de pedidos; puedes marcarlos discontinued. No se borran categorías ni proveedores con productos. Se devuelve 409 y no se ejecutan borrados en cascada desde la API. Referencias a categorías/proveedores inexistentes se rechazan con 400.

| Método y ruta | Función |
|---|---|
| POST /api/auth/login | Login público |
| GET /api/auth/me | Perfil del usuario autenticado |
| PUT /api/auth/password | Cambiar contraseña propia: currentPassword, newPassword |
| POST /api/auth/logout | Revocar todas las sesiones propias |
| GET /api/usuarios | Listar usuarios, solo Admin |
| GET /api/usuarios/{id} | Consultar usuario, solo Admin |
| POST /api/usuarios | Crear usuario, solo Admin |
| PUT /api/usuarios/{id} | Cambiar username, role, isActive, solo Admin |
| DELETE /api/usuarios/{id} | Desactivar usuario, solo Admin |
| PUT /api/usuarios/{id}/password | Restablecer contraseña: newPassword, solo Admin |

DELETE de usuario es baja lógica; conserva la fila y reserva el nombre de usuario. Un Admin no puede desactivar su propia cuenta ni quitarse su propio rol. Puede reactivar otras cuentas con PUT. Las respuestas nunca incluyen hashes ni SecurityStamp. Los cambios simultáneos sobre usuarios se detectan mediante un token de concurrencia y devuelven 409.

## Estructura

```text
Controllers/    HTTP, validación de peticiones y autorización
Contracts/      Formularios de entrada y respuestas JSON (DTO)
Data/
  ContextoDB.cs Contexto original de Northwind
  UsuariosDB.cs Contexto de la tabla api_users
Models/        Entidades de Northwind y ApiUser
Services/
  TokenService.cs          Emite JWT
Infrastructure/
  UserInitializer.cs       Inicialización explícita del primer Admin
  ApiExceptionHandler.cs   Respuestas de errores sin detalles internos
Database/
  001_api_users.sql        Creación aditiva de la tabla de usuarios
Program.cs     Configura EF, JWT, roles, CORS, límite de peticiones y Swagger
web.config     IIS, deshabilita WebDAV para permitir PUT y DELETE
```

## Nueva instalación en otra base

No aplica si publicas sobre la misma nwind ya inicializada. Configura la conexión y una clave Jwt:Key aleatoria de al menos 32 bytes. Luego, desde PowerShell en la carpeta del proyecto:

```powershell
$env:Bootstrap__Username = 'admin'
$secret = Read-Host 'Contraseña inicial (12 a 128 caracteres)' -AsSecureString
$env:Bootstrap__Password = [System.Net.NetworkCredential]::new('', $secret).Password
try {
  dotnet run --no-launch-profile -- --init-users
} finally {
  Remove-Item Env:\Bootstrap__Username, Env:\Bootstrap__Password
}
```

El comando crea únicamente api_users si falta y el primer administrador. Se niega a reemplazar cuentas si ya existen usuarios. No usa EnsureCreated ni migraciones sobre toda Northwind. No ejecuta DROP ni altera tablas de negocio. El arranque normal no crea tablas ni usuarios. El script SQL se copia al publicar; para una publicación usa `dotnet Equipo5.Api.dll --init-users` con las mismas variables.

## Volver a publicar en IIS

1. Detén el sitio y su grupo de aplicaciones para reemplazar archivos.
2. Publica Release desde Visual Studio a una carpeta.
3. Copia todos los archivos publicados a la carpeta física de IIS, incluido web.config y Database.
4. Conserva la conexión y la misma clave JWT de esta instalación. No pongas credenciales de administrador en appsettings.
5. Inicia el grupo y el sitio. Conserva Autenticación anónima habilitada; JWT controla el acceso en la aplicación.
6. Prueba el login con Postman o el frontend: Swagger no está disponible en Production.

La actualización de código no modifica automáticamente una publicación IIS existente; hay que volver a publicar. Si IIS devuelve 405 en PUT/DELETE, comprueba que se haya copiado web.config y que WebDAV esté deshabilitado para el sitio. Si una directiva bloqueada provoca 500.19, el administrador de IIS debe permitir esa configuración o deshabilitar WebDAV desde IIS.

La API aplica redirección HTTPS, HSTS fuera de Development, cabeceras de seguridad y CORS limitado a `Cors:AllowedOrigins`. `ForwardedHeaders:KnownProxies` debe contener solo IPs de proxies de confianza; no expongas la aplicación directamente a clientes que puedan falsificar esas cabeceras. El usuario MySQL de ejecución necesita SELECT/INSERT/UPDATE/DELETE, pero no CREATE después de inicializar.

## Verificación y límites

Consulta VERIFICACION.md para los resultados. Las pruebas de catálogo crean y eliminan sus propias filas. Quedaron dos cuentas de prueba desactivadas, con prefijos test_reader_ y test_editor_. Los identificadores autoincrementales consumidos no se reutilizan.

La API no incluye frontend, refresh tokens, recuperación por correo, auditoría completa ni gestión de pedidos. Los PUT de catálogo usan el último cambio recibido; no incluyen control de versiones de inventario para uso concurrente intensivo. La administración de usuarios es intencionalmente pequeña y utiliza PasswordHasher, DTO y permisos explícitos.

Referencias de implementación: [PasswordHasher de ASP.NET Core](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.identity.passwordhasher-1.verifyhashedpassword?view=aspnetcore-10.0), [limitación de peticiones](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit?view=aspnetcore-10.0).

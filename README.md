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

- La API administra autenticación, clientes, empleados, ventas y catálogo Northwind.
- Los usuarios se guardan en `api_users`; las entidades de negocio conservan el esquema Northwind.
- Los roles vigentes son `Admin`, `Empleado` y `Cliente`.
- Los clientes pueden registrarse y consultar productos activos, paqueterías, su perfil y sus propias ventas.
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
6. Crea las cuentas administrativas necesarias con `POST /api/usuarios` (`Admin`, `Empleado` o `Cliente`) o un empleado vinculado con `POST /api/empleados`.

Ejemplo de creación (sustituye la contraseña de ejemplo por una nueva):

```json
{
  "username": "equipo2",
  "password": "ElegirUnaClaveNueva!2026",
  "role": "Empleado"
}
```

También existe `POST /api/auth/register` para que un cliente cree su cuenta y registro Northwind vinculados. Los nombres de usuario tienen de 3 a 50 caracteres ASCII, con letras, números, punto, guion o guion bajo, y son únicos sin distinguir mayúsculas. Las contraseñas se guardan con `PasswordHasher` de ASP.NET Core, nunca en texto plano.

## Roles

| Función | Admin | Empleado | Cliente |
|---|---:|---:|---:|
| Administrar usuarios, empleados, desbloqueos y reactivar clientes | Sí | No | No |
| Escribir productos, categorías y proveedores | Sí | Sí | No |
| Consultar clientes y administrar clientes | Sí | Sí | Solo su perfil |
| Crear y consultar ventas | Todas | Todas | Solo las propias |
| Consultar catálogo activo y paqueterías | Sí | Sí | Sí |

Se valida la firma y vigencia del JWT, y en cada petición protegida se verifica en MySQL que el usuario siga activo y que su versión de sesión y rol coincidan. Cambiar un usuario, su contraseña o su rol, desactivarlo o cerrar sesión invalida sus tokens anteriores. Logout cierra todas las sesiones de esa cuenta. El inicio de sesión bloquea la cuenta tras 3 intentos fallidos consecutivos (15 minutos por defecto); un Admin puede desbloquearla. No hay refresh token.

Login, registro y cambio de contraseña comparten un límite de 10 solicitudes por minuto por IP, en memoria por proceso. Al excederlo devuelve 429; espera un minuto. Es protección básica para esta práctica, no un sistema distribuido de prevención de abuso.

## Endpoints

Todas las rutas salvo registro y login requieren JWT. `WriteCatalog` permite escritura a `Admin` y `Empleado`. Los parámetros de paginación de listados son `limit` (1–100) y `offset` (>= 0), salvo productos paginados, que usa `p` y `pageSize`.

| Método y ruta | Acceso | Función / respuesta correcta |
|---|---|---|
| `POST /api/auth/register` | Anónimo | Registro de cliente vinculado; `201` |
| `POST /api/auth/login` | Anónimo | Login; `200` con JWT |
| `GET /api/auth/me` | Autenticado | Perfil del usuario actual; `200` |
| `PUT /api/auth/password` | Autenticado | Cambiar contraseña propia; `204` |
| `POST /api/auth/logout` | Autenticado | Revocar sesiones propias; `204` |
| `GET /api/usuarios[?limit&offset]`, `GET /api/usuarios/{id}` | Admin | Consultar cuentas; `200` |
| `POST /api/usuarios`, `PUT /api/usuarios/{id}` | Admin | Crear/actualizar cuenta; `201` / `204` |
| `DELETE /api/usuarios/{id}` | Admin | Desactivar cuenta; `204` |
| `PUT /api/usuarios/{id}/password` | Admin | Restablecer contraseña; `204` |
| `PUT /api/usuarios/{id}/desbloquear` | Admin | Limpiar bloqueo; `204` |
| `GET /api/empleados[?limit&offset&includeInactive]`, `GET /api/empleados/{id}` | Admin | Consultar empleados y estado de cuenta; `200` |
| `POST /api/empleados`, `PUT /api/empleados/{id}` | Admin | Alta transaccional con cuenta / actualizar; `201` / `204` |
| `DELETE /api/empleados/{id}` | Admin | Baja lógica y revocación; `204` |
| `GET /api/clientes[?limit&offset&includeInactive&search]` | Admin, Empleado | Listar clientes; `200` |
| `GET /api/clientes/{id}` | Admin, Empleado, propietario Cliente | Detalle; `200` |
| `POST /api/clientes`, `PUT /api/clientes/{id}` | Admin, Empleado; Cliente propio para PUT | Crear/actualizar contacto; `201` / `204` |
| `DELETE /api/clientes/{id}` | Admin, Empleado | Baja lógica y revocación; `204` |
| `PUT /api/clientes/{id}/reactivar` | Admin | Reactivar cliente y cuenta vinculada; `204` |
| `GET /api/ventas[?limit&offset&customerId&from&to]`, `GET /api/ventas/{id}` | Autenticado | Admin/Empleado ven todas; Cliente solo las propias; `200` |
| `POST /api/ventas` | Admin, Empleado, Cliente | Crear venta con precios/stock del servidor; `201` |
| `GET /api/productos`, `GET /api/productos/paginados`, `GET /api/productos/{id}` | Autenticado | Consultar catálogo; Cliente solo ve productos activos; `200` |
| `POST /api/productos`, `PUT /api/productos/{id}`, `DELETE /api/productos/{id}` | Admin, Empleado | Crear/actualizar/marcar descontinuado; `201` / `204` |
| `GET /api/categorias`, `GET /api/categorias/{id}`, `GET /api/proveedores`, `GET /api/proveedores/{id}` | Autenticado | Consultar catálogo; `200` |
| `POST`, `PUT /{id}`, `DELETE /{id}` en `/api/categorias` y `/api/proveedores` | Admin, Empleado | Escribir catálogo; `201` / `204` |
| `GET /api/shippers` | Autenticado | Lista ligera de paqueterías; `200` |

`DELETE` de clientes y empleados es baja lógica; en usuarios se desactiva la cuenta y se revoca su token. `DELETE /api/productos/{id}` marca el producto como discontinuado. Categorías y proveedores relacionados con productos no se pueden eliminar. No existe endpoint de borrado de ventas. En una venta, `CustomerId` de un Cliente se obtiene del JWT; no se aceptan precios ni descuentos aportados por el cliente y el stock se descuenta en transacción.

### Errores HTTP

Las respuestas de error usan `ProblemDetails` RFC 7807 (incluidos errores de autorización, ruta inexistente y límite de solicitudes); las validaciones de modelo usan `ValidationProblemDetails`.

| Código | Uso principal |
|---|---|
| `400` | Datos inválidos, cantidades/ítems no válidos, texto no permitido |
| `401` | Credenciales incorrectas o token faltante, expirado o revocado |
| `403` | Rol insuficiente o intento IDOR de un Cliente |
| `404` | Recurso inexistente |
| `409` | Duplicado, cliente inactivo, producto descontinuado o stock insuficiente |
| `423` | Cuenta bloqueada por intentos fallidos |
| `429` | Rate limit excedido |
| `503` | Base de datos no disponible |

### Pruebas manuales locales pendientes

Ejecuta el script `Database/002_seguridad_y_baja_logica.sql` sobre `nwind`, configura User Secrets y `Cors:AllowedOrigins` siguiendo la sección de configuración, y ejecuta la API. En Development, Swagger está en `/swagger`.

1. Registra dos clientes; confirma `201`, identificadores de 5 caracteres y `409` para usuario duplicado. Inicia sesión como cada uno.
2. Prueba tres contraseñas incorrectas: `401` las primeras, `423` al umbral y mientras dure el bloqueo. Como Admin consulta el contador, usa `PUT /api/usuarios/{id}/desbloquear` y comprueba login y reinicio del contador.
3. Prueba rutas de Admin, Empleado y Cliente: solo Admin administra usuarios/empleados; Admin y Empleado escriben catálogo; las demás rutas respetan la tabla de roles. Comprueba además `401` sin token y `403` con rol insuficiente.
4. Como Cliente A solicita y actualiza su propio perfil y registra una venta indicando también el ID del Cliente B en el cuerpo: debe quedar vinculada a A. Solicita el perfil y las ventas de B; espera `403`.
5. Prueba ventas con lista vacía, cantidades no positivas, productos repetidos, producto inexistente, discontinuado, sin stock, con precio/stock nulo, paquetería inexistente y cliente inactivo. Confirma los códigos `400`/`404`/`409` esperados y que stock/órdenes no cambian al fallar.
6. Prueba baja lógica y reactivación de clientes/empleados, incluyendo que su token anterior quede revocado; confirma que el cliente inactivo no pueda comprar.
7. Prueba duplicados de producto/categoría/proveedor, baja lógica de producto e intento de eliminar categoría/proveedor relacionado (`409`).
8. Comprueba sanitización (texto con etiquetas/script, atributos `onerror` y `javascript:`), cabeceras de seguridad, rechazo de origen CORS no configurado y `429` con más de 10 peticiones por minuto. La comprobación de `X-Forwarded-*` requiere probar detrás de un proxy confiable configurado.

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
  ApiExceptionHandler.cs   Respuestas ProblemDetails sin detalles internos
Database/
  001_api_users.sql        Creación aditiva de la tabla de usuarios
  002_seguridad_y_baja_logica.sql  Bloqueo, vínculos e indicadores de baja lógica
Services/
  OrderService.cs          Validación transaccional de ventas e inventario
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

La API no incluye frontend, refresh tokens, recuperación por correo ni auditoría completa. Las ventas controlan concurrencia de inventario mediante transacción serializable; los PUT de catálogo no incluyen control de versiones para uso concurrente intensivo. La administración de usuarios es intencionalmente pequeña y utiliza PasswordHasher, DTO y permisos explícitos.

Referencias de implementación: [PasswordHasher de ASP.NET Core](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.identity.passwordhasher-1.verifyhashedpassword?view=aspnetcore-10.0), [limitación de peticiones](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit?view=aspnetcore-10.0).

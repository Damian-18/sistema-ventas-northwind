# Plan de Tareas — Backend `Equipo5.Api`

> **Instrucciones para el Agente:**
> 1. Lee `Specs_backend.md` completamente antes de iniciar cualquier tarea.
> 2. Trabaja una fase a la vez, en orden secuencial.
> 3. Al terminar cada subtarea, cambia `[ ]` por `[x]`.
> 4. Al finalizar cada fase, ejecuta `dotnet build` para verificar que no haya errores de compilación y registra tu avance en la **Bitácora de Progreso** al final de este archivo.
> 5. **No modifiques ni construyas el Frontend todavía** (se hará en una etapa posterior con HTML/CSS/JS puro).

## Fase 0: Preparación y Seguridad de Configuración
* [x] Revisar `appsettings.json` y reemplazar credenciales reales (MySQL y `Jwt:Key`) por placeholders seguros.
* [x] Agregar secciones de configuración en `appsettings.json`: `Security:LockoutMinutes` (15), `Security:MaxFailedLoginAttempts` (3) y `Cors:AllowedOrigins`.
* [x] Documentar brevemente en `README.md` (o sección de configuración) cómo configurar User Secrets / variables de entorno.

## Fase 1: Base de Datos y Modelos
* [x] Crear `Database/002_seguridad_y_baja_logica.sql` (idempotente, respetando mayúsculas/minúsculas exactas de las tablas en `nwind`) agregando:
  * `failed_login_attempts`, `lockout_end`, `customer_id` (`CHAR(5) NULL`), `employee_id` (`INT NULL`) en `api_users`.
  * `is_active` en `customers`.
  * `is_active` en `employees`.
* [x] Actualizar modelo `ApiUser` y configuración en `UsuariosDB.cs` (`CustomerId` mapeado a `CHAR(5)`).
* [x] Actualizar modelos `Customer` y `Employee` y su configuración en `ContextoDB.cs`.
* [x] Crear clase de constantes `AppRoles` (`Admin`, `Empleado`, `Cliente`) y actualizar la política `WriteCatalog` en `Program.cs` para usar `Admin` y `Empleado` (en lugar de `Editor`).

## Fase 2: Seguridad Transversal (Middlewares y Sanitización)
* [x] Configurar `UseForwardedHeaders()` en `Program.cs` (para `X-Forwarded-For` y `X-Forwarded-Proto` en Azure/hosting).
* [x] Implementar middleware de cabeceras de seguridad HTTP (`X-Frame-Options: DENY`, `Content-Security-Policy: frame-ancestors 'none'`, `X-Content-Type-Options: nosniff`, `Referrer-Policy: no-referrer`) y registrarlo en `Program.cs`.
* [x] Implementar helper/filtro `InputSanitizer` para prevenir XSS en todos los DTOs de entrada.
* [x] Restringir CORS en `Program.cs` usando `Cors:AllowedOrigins`.
* [x] Habilitar `UseHttpsRedirection()` y `UseHsts()` (en entornos no-Development).
* [x] Revisar `ApiExceptionHandler.cs` para garantizar mensajes claros en español sin filtrar detalles internos de MySQL.

## Fase 3: Autenticación, Bloqueo (3 Intentos) y Registro de Clientes
* [ ] Actualizar `TokenService.cs` para incluir claims `customer_id` y `employee_id` cuando correspondan.
* [ ] Implementar lógica de bloqueo tras 3 intentos fallidos (`423 Locked`) en `POST /api/auth/login` y reinicio al iniciar sesión correctamente.
* [ ] Implementar `POST /api/auth/register` (registro autónomo de clientes con creación transaccional en `customers` + `api_users`, validación de duplicados y generación de `CustomerId` único de 5 caracteres `CHAR(5)`).
* [ ] Agregar Rate Limiting a `/api/auth/register` (manteniendo el de `/api/auth/login` y `/api/auth/password`).
* [ ] Actualizar `GET /api/auth/me` para devolver `CustomerId` y `EmployeeId` si aplica.
* [ ] Actualizar `UserView` y `UsersController.cs`:
  * Validar roles contra `AppRoles`.
  * Exponer estado de bloqueo (`FailedLoginAttempts`, `LockoutEnd`, `IsLockedOut`) y `CustomerId`/`EmployeeId` en `UserView`.
  * Agregar `PUT /api/usuarios/{id}/desbloquear` (solo `Admin`).

## Fase 4: Gestión de Empleados y Clientes (Baja Lógica)
* [ ] Crear DTOs de empleados (`Contracts/EmployeeContracts.cs`) sin exponer datos binarios ni contraseñas.
* [ ] Crear `EmployeesController.cs` (solo `Admin`): `GET`, `GET /{id}`, `POST` (transaccional `Employee` + `ApiUser`), `PUT /{id}`, `DELETE /{id}` (baja lógica en ambas tablas + rotación de `SecurityStamp`).
* [ ] Completar DTOs de clientes (`Contracts/CustomerContracts.cs`).
* [ ] Completar `CustomersController.cs`:
  * `GET` con filtro de activos/inactivos (`Admin`/`Empleado`).
  * `GET /{id}` con validación de ownership para `Cliente`.
  * `POST` con validación de duplicados (`Admin`/`Empleado`).
  * `PUT /{id}` (`Admin`/`Empleado` o el propio `Cliente`, impidiendo que un `Cliente` altere `is_active`).
  * `DELETE /{id}` con **baja lógica** (`is_active = false` + desactivación de `ApiUser` y rotación de `SecurityStamp` en transacción).
  * `PUT /{id}/reactivar` (solo `Admin`, reactiva cliente y su `ApiUser` vinculado en transacción).

## Fase 5: Módulo de Ventas (`OrdersController`), Catálogo y Paqueterías
* [ ] Crear DTOs de ventas (`Contracts/OrderContracts.cs`): `CreateOrderRequest` (sin `UnitPrice`, `Discount` ni `Total`), `OrderSummaryDto`, `OrderDetailViewDto`.
* [ ] Crear servicio de negocio `OrderService.cs` (o lógica encapsulada) con transacción que valide:
  * Cliente existente y activo.
  * `EmployeeId` tomado del JWT para `Admin`/`Empleado` y `null` para compras autónomas de `Cliente`.
  * Si se envía `ShipVia`, validar que exista en `shippers`.
  * Items no vacíos, `Quantity > 0`, sin `ProductId` duplicados.
  * Producto existente y `Discontinued == false`.
  * Stock suficiente (`UnitsInStock >= Quantity`) y descuento de stock.
  * Cálculo de `UnitPrice` **únicamente desde la base de datos** y `Discount = 0`.
* [ ] Crear `OrdersController.cs` (`/api/ventas`):
  * `POST /api/ventas` (asignando `CustomerId` desde JWT si es `Cliente`).
  * `GET /api/ventas` (filtrado por propietario si es `Cliente`).
  * `GET /api/ventas/{id}` (con validación de ownership `403`).
  * Confirmar que no exista ningún endpoint `DELETE` en ventas.
* [ ] Ajustar catálogo (`ProductsController`, `CategoriesController`, `SuppliersController`):
  * Convertir `DELETE /api/productos/{id}` a **baja lógica** (`Discontinued = true` en lugar de eliminar físicamente).
  * Agregar validación de nombres duplicados (`409 Conflict`) en productos, categorías y proveedores.
* [ ] Crear `ShippersController.cs` con `GET /api/shippers` (autenticado, devuelve `ShipperId` y `CompanyName`).

## Fase 6: Verificación del Backend
* [ ] Ejecutar `dotnet build` en configuración `Release` sin errores.
* [ ] Verificar que todos los endpoints estén protegidos con el rol correcto (`Admin`, `Empleado`, `Cliente`) y usen DTOs estrictos.
* [ ] Documentar los nuevos endpoints y códigos de respuesta en `README.md` o un archivo de pruebas HTTP para facilitar el desarrollo del frontend.

## Fase 7: Pendiente (NO ejecutar todavía)
* [ ] Frontend en HTML, CSS y JavaScript puro (Login, Registro, Catálogo/Carrito, Gestión de Clientes, Gestión de Empleados, Guards por URL y Logout real).
* [ ] Despliegue en Azure / Hosting, configuración de dominio propio y certificado HTTPS.

## Bitácora de Progreso (Actualizar al terminar cada sesión)

| Fecha | Fase | Estado | Notas / Archivos modificados / Bloqueos |
| :--- | :--- | :--- | :--- |
| 2026-11-24 | Fase 0 | Completa | Actualizados `appsettings.json`, `README.md`, `Equipo5.Api.csproj` (UserSecretsId) y este archivo. Credenciales reemplazadas por placeholders; agregados valores de seguridad y lista de orígenes CORS vacía. `dotnet build` correcto (0 advertencias, 0 errores). Antes de ejecutar localmente, configurar conexión MySQL, clave JWT y origen frontend con User Secrets; en producción, usar variables de entorno. Los archivos reales se llaman `Specs_backend.md` y `tasks.md`. |
| 2026-11-24 | Fase 1 | Completa | Creado `Database/002_seguridad_y_baja_logica.sql` con alteraciones e índices condicionales; actualizados `ApiUser`, `Customer`, `Employee`, ambos DbContext, nueva clase `AppRoles`, política `WriteCatalog` y este archivo. Pendiente del usuario: ejecutar el script sobre `nwind` local y hacer commit. `dotnet build` correcto (0 advertencias, 0 errores). |
| 2026-11-24 | Fase 2 | Completa | Configurados `ForwardedHeaders`, CORS limitado, HTTPS/HSTS y cabeceras globales; añadido filtro de sanitización global y revisado `ApiExceptionHandler`. Modificados `Program.cs`, `appsettings.json`, `ApiExceptionHandler.cs`, `README.md` y este archivo; creados `SecurityHeadersMiddleware.cs` e `InputSanitizer.cs`. `dotnet build` correcto (0 advertencias, 0 errores); comprobaciones HTTP locales confirmaron redirección HTTPS, cabeceras, CORS/HSTS detrás de proxy y rechazo XSS. Configurar `Cors:AllowedOrigins` y las IPs de proxy confiables antes del despliegue. |
# Especificaciones Técnicas — Backend `Equipo5.Api` (Northwind)

## 1. Contexto y Arquitectura Base
* **Framework:** .NET 10 (`Equipo5.Api.csproj`), `nullable` habilitado.
* **Base de datos:** MySQL (`nwind`) mediante `MySql.EntityFrameworkCore`.
* **Contextos actuales:**
  * `ContextoDB`: Entidades Northwind (`Customer`, `Employee`, `Product`, `Category`, `Supplier`, `Shipper`, `Order`, `OrderDetail`, etc.).
  * `UsuariosDB`: Entidad `ApiUser` mapeada a `api_users`.
* **Convención de cambios en BD:** El proyecto no utiliza migraciones automáticas de EF Core. Todo cambio de esquema debe documentarse en scripts SQL idempotentes dentro de `Database/` (siguiendo el patrón de `Database/001_api_users.sql`).
* **Compatibilidad Windows/Linux:** Al escribir scripts SQL y revisar mapeos en `ContextoDB`, respetar exactamente las mayúsculas y minúsculas de las tablas reales en `nwind` para evitar errores al desplegar en servidores Linux.
* **Frontend (Fase posterior):** Se construirá aparte en **HTML, CSS y JavaScript puro**. El backend debe quedar 100% preparado para ser consumido por este frontend (DTOs limpios, CORS configurable, respuestas JSON claras y soporte para autorización por rol).

---

## 2. Cambios en Base de Datos y Modelos

Crear el script `Database/002_seguridad_y_baja_logica.sql` (idempotente) y actualizar los modelos y `DbContext` correspondientes:

### 2.1 Tabla `api_users` (`ApiUser` en `UsuariosDB`)
Agregar las siguientes columnas:
* `failed_login_attempts` (`INT NOT NULL DEFAULT 0`): Contador de intentos fallidos consecutivos.
* `lockout_end` (`DATETIME NULL`): Fecha/hora UTC hasta la cual la cuenta permanece bloqueada.
* `customer_id` (`CHAR(5) NULL`): Referencia exacta al `CustomerId` (`CHAR(5)`) en `customers` (para usuarios con rol `Cliente`). Índice único cuando no es nulo.
* `employee_id` (`INT NULL`): Referencia al `EmployeeId` (`INT`) en `employees` (para usuarios con rol `Empleado` o `Admin`). Índice único cuando no es nulo.

### 2.2 Tabla `customers` (`Customer` en `ContextoDB`)
* `is_active` (`TINYINT(1) NOT NULL DEFAULT 1`): Control de baja lógica.
* *Referencia confirmada:* `CustomerId` es `string` no nullable mapeado a `CHAR(5)` y `CompanyName` es no nullable; los demás campos escalares son nullable.

### 2.3 Tabla `employees` (`Employee` en `ContextoDB`)
* `is_active` (`TINYINT(1) NOT NULL DEFAULT 1`): Control de baja lógica para empleados.
* *Referencia confirmada:* `EmployeeId` es `int` no nullable; `FirstName` y `LastName` son no nullable; los demás campos escalares (incluido `ReportsTo`) son nullable.

### 2.4 Transacciones entre `ContextoDB` y `UsuariosDB`
Cuando una operación escriba en ambos contextos (registro autónomo de cliente, alta de empleado, baja lógica o reactivación de cliente/empleado), debe ejecutarse de forma atómica (compartiendo la conexión/transacción de base de datos o usando un patrón de rollback explícito) para evitar registros huérfanos.

---

## 3. Roles y Matriz de Autorización

Estandarizar los roles del sistema en constantes (`AppRoles`):
* **`Admin`**: Control total (usuarios, empleados, clientes, catálogo, ventas, desbloqueo de cuentas y reactivación de clientes).
* **`Empleado`**: Gestión de catálogo (productos, categorías, proveedores), consulta/gestión de clientes y registro/consulta de ventas. No puede crear/editar/eliminar empleados ni administrar cuentas de usuario.
* **`Cliente`**: Consulta de catálogo de productos activos y paqueterías, registro de sus propias ventas, consulta de su propio historial de ventas y consulta/actualización de su propio perfil (sin poder modificar `is_active`).

### Alineación de Políticas Existentes
* Actualizar la política `WriteCatalog` en `Program.cs` para autorizar los roles **`Admin` y `Empleado`** (reemplazando el rol previo `Editor`), asegurando que los empleados dados de alta puedan administrar productos, categorías y proveedores sin recibir `403 Forbidden`.

### Reglas de Propiedad (Ownership)
* Un `Cliente` **nunca** puede ver, editar ni crear ventas a nombre de otro `CustomerId`. Su `CustomerId` siempre se obtiene de los claims del JWT.
* Si un `Cliente` intenta acceder a `/api/ventas/{id}` o `/api/clientes/{id}` de otro usuario, responder `403 Forbidden`.

---

## 4. Seguridad y Endurecimiento (Hardening)

### 4.1 Bloqueo por 3 Intentos Fallidos (Sin Captcha)
* Al intentar iniciar sesión en `POST /api/auth/login`:
  1. Verificar si `LockoutEnd != null && LockoutEnd > DateTime.UtcNow`. Si está bloqueado, rechazar inmediatamente con `423 Locked` indicando el tiempo restante aproximado.
  2. Si la contraseña falla: incrementar `FailedLoginAttempts` en 1.
  3. Si `FailedLoginAttempts >= 3`: asignar `LockoutEnd = DateTime.UtcNow.AddMinutes(15)` (valor configurable en `appsettings.json` bajo `Security:LockoutMinutes`), guardar y responder `423 Locked` con mensaje claro.
  4. Si el login es exitoso: reiniciar `FailedLoginAttempts = 0` y `LockoutEnd = null`.
* Agregar endpoint exclusivo de `Admin` (`PUT /api/usuarios/{id}/desbloquear`) para limpiar `FailedLoginAttempts` y `LockoutEnd` manualmente.
* Mantener el Rate Limiting existente en `/api/auth/login` y `/api/auth/password`, y extenderlo a `/api/auth/register`.

### 4.2 Anti-Clickjacking y Cabeceras HTTP de Seguridad
Agregar un middleware global en `Program.cs` que inyecte en todas las respuestas:
* `X-Frame-Options: DENY`
* `Content-Security-Policy: frame-ancestors 'none'`
* `X-Content-Type-Options: nosniff`
* `Referrer-Policy: no-referrer`

### 4.3 Sanitización de Entradas contra XSS
* Implementar un servicio/helper de sanitización (`InputSanitizer`) y aplicarlo (mediante filtro global de acción, converter de JSON o validación en capa de servicio) a todas las cadenas de entrada en los DTOs.
* Reglas:
  * Hacer `Trim()` a los textos.
  * Eliminar o rechazar contenido peligroso: etiquetas `<script>`, `<iframe>`, `<object>`, `<embed>`, atributos de eventos (`onclick=`, `onerror=`, `onload=`, etc.) y protocolos `javascript:`.
  * En campos donde no se permite HTML (nombres, direcciones, usuarios, categorías, etc.), rechazar o codificar/limpiar etiquetas HTML para impedir XSS almacenado y reflejado.

### 4.4 CORS, HTTPS, Proxy Inverso y Secretos
* **CORS:** Eliminar `AllowAnyOrigin()`. Leer los orígenes permitidos desde `appsettings.json` (`Cors:AllowedOrigins`) para soportar desarrollo local y el dominio final en producción.
* **Proxy Inverso (`ForwardedHeaders`):** Configurar `UseForwardedHeaders()` (procesando `X-Forwarded-For` y `X-Forwarded-Proto`) antes de redirección HTTPS y Rate Limiting para que en Azure/hosting no se cicle HTTPS y el Rate Limiter identifique la IP real del cliente.
* **HTTPS:** Habilitar `app.UseHttpsRedirection()` y, fuera de desarrollo, `app.UseHsts()`.
* **Secretos:** Retirar contraseñas reales de MySQL y claves reales de `Jwt:Key` de `appsettings.json`. Dejar placeholders claros y documentar el uso de `dotnet user-secrets` (local) y variables de entorno (producción/Azure).

---

## 5. Endpoints y Reglas de Negocio

**Principio de mínima información:** Todos los endpoints deben recibir y devolver exclusivamente DTOs específicos. Nunca exponer entidades de EF Core, `PasswordHash`, `SecurityStamp`, ni campos sensibles innecesarios.

**Nota sobre verbos HTTP:** Para operaciones de acción (`desbloquear`, `reactivar`), usar `PUT` en lugar de `PATCH` para mantener compatibilidad directa con la configuración actual de IIS (`web.config`).

### 5.1 Autenticación (`AuthController` — `/api/auth`)
* `POST /api/auth/register` *(Anónimo, con Rate Limit)*:
  * Permite el registro autónomo de clientes.
  * Recibe: `Username`, `Password` (mínimo 8 caracteres, mayúscula, minúscula y número), `CompanyName` (obligatorio), y opcionales `ContactName`, `Phone`, `Address`, `City`, `Country`.
  * Valida que el `Username` no esté duplicado (`409 Conflict`).
  * Genera un `CustomerId` único de exactamente 5 caracteres alfanuméricos en mayúsculas (`CHAR(5)`), verificando que no exista en `customers`.
  * Crea en una misma transacción el registro en `customers` (`is_active = true`) y en `api_users` (`Role = "Cliente"`, `CustomerId = ...`, `IsActive = true`).
  * Devuelve `201 Created` con un DTO resumido del usuario creado (sin datos sensibles).
* `POST /api/auth/login` *(Anónimo, con Rate Limit)*:
  * Aplica la lógica de bloqueo de 3 intentos.
  * Incluye en el JWT los claims: `sub` (UserId), `unique_name` (Username), `role`, `security_stamp`, y opcionalmente `customer_id` o `employee_id`.
* `GET /api/auth/me` *(Autenticado)*:
  * Devuelve perfil básico del usuario autenticado (`Id`, `Username`, `Role`, `CustomerId`, `EmployeeId`) para control de acceso por URL en el frontend.
* `PUT /api/auth/password` *(Autenticado, con Rate Limit)*:
  * Mantener funcionalidad existente de cambio de contraseña propia.
* `POST /api/auth/logout` *(Autenticado)*:
  * Rota `SecurityStamp` para invalidar el token actual en el servidor.

### 5.2 Usuarios (`UsersController` — `/api/usuarios`)
* **Acceso:** Exclusivo `[Authorize(Roles = AppRoles.Admin)]`.
* `GET /api/usuarios`, `GET /api/usuarios/{id}`, `POST /api/usuarios`, `PUT /api/usuarios/{id}`, `DELETE /api/usuarios/{id}` (desactiva), `PUT /api/usuarios/{id}/password`:
  * Mantener la administración de cuentas (en especial alta y gestión de administradores), validando que los roles permitidos sean únicamente los definidos en `AppRoles`.
  * Actualizar `UserView` para incluir `CustomerId`, `EmployeeId`, `FailedLoginAttempts`, `LockoutEnd` e indicador `IsLockedOut` (sin exponer `PasswordHash` ni `SecurityStamp`).
* `PUT /api/usuarios/{id}/desbloquear` *(Solo Admin)*:
  * Reinicia `FailedLoginAttempts = 0` y `LockoutEnd = null` para desbloquear manualmente una cuenta bloqueada por intentos fallidos.

### 5.3 Empleados (`EmployeesController` — `/api/empleados`)
* **Acceso:** Exclusivo `[Authorize(Roles = AppRoles.Admin)]`.
* `GET /api/empleados`: Lista paginada de empleados (permite filtrar `includeInactive=true/false`, por defecto solo activos) incluyendo si tienen cuenta de usuario vinculada y su estado.
* `GET /api/empleados/{id}`: Detalle de empleado (DTO sin campos binarios como `Photo`).
* `POST /api/empleados`:
  * Alta de empleado **y** su cuenta de usuario en `api_users` en una sola transacción.
  * Recibe datos del empleado (`FirstName`, `LastName` obligatorios; `Title`, `BirthDate`, `HireDate`, `Address`, `City`, `Country`, `HomePhone`, `ReportsTo` opcionales) y credenciales (`Username`, `Password`, `Role` permitido solo `Admin` o `Empleado`).
  * Valida que no exista duplicado de `Username` ni duplicado exacto de empleado (`FirstName` + `LastName` + `BirthDate`).
* `PUT /api/empleados/{id}`: Actualiza datos del empleado.
* `DELETE /api/empleados/{id}`: **Baja lógica** (`employees.is_active = false` y desactiva su `api_users.is_active = false`, rotando `SecurityStamp`). Nunca eliminación física.

### 5.4 Clientes (`CustomersController` — `/api/clientes`)
* `GET /api/clientes` *(Admin, Empleado)*: Listado paginado y búsqueda; por defecto devuelve solo `is_active == true` (parámetro opcional `includeInactive`).
* `GET /api/clientes/{id}` *(Admin, Empleado, o el propio Cliente)*: Devuelve `CustomerDetailDto` validando ownership (`403` si un cliente intenta ver otro ID).
* `POST /api/clientes` *(Admin, Empleado)*: Crea un cliente validando duplicados (`CustomerId` de 5 caracteres si se envía o generado automáticamente, y validación de `CompanyName` duplicado).
* `PUT /api/clientes/{id}` *(Admin, Empleado, o el propio Cliente)*: Actualiza datos de contacto/dirección. Un `Cliente` solo puede editar su propio perfil y nunca puede alterar `is_active`.
* `DELETE /api/clientes/{id}` *(Admin, Empleado)*: **Baja lógica** (`customers.is_active = false`). Si tiene un `ApiUser` asociado, también desactiva `api_users.is_active = false` y rota `SecurityStamp` dentro de la misma transacción. **Prohibido el borrado físico.**
* `PUT /api/clientes/{id}/reactivar` *(Solo Admin)*: Revierte la baja lógica (`customers.is_active = true`) y reactiva el `ApiUser` vinculado (si existe) dentro de la misma transacción.

### 5.5 Ventas / Órdenes (`OrdersController` — `/api/ventas`)
* *Referencia confirmada:* En `Order`, `CustomerId` (`string?`) y `EmployeeId` (`int?`) son nullable.
* `GET /api/ventas` *(Autenticado)*:
  * `Admin` y `Empleado`: ven todas las ventas (paginadas, con filtros opcionales por cliente o rango de fechas).
  * `Cliente`: ve **únicamente** las ventas donde `Order.CustomerId == claim.CustomerId`.
* `GET /api/ventas/{id}` *(Autenticado)*:
  * Devuelve cabecera de venta + lista de detalles (`OrderViewDto`).
  * Valida ownership (`403 Forbidden`) si el rol es `Cliente`.
* `POST /api/ventas` *(Autenticado: Admin, Empleado, Cliente)*:
  * **DTO de entrada (`CreateOrderRequest`):**
    * `CustomerId` (opcional para `Cliente`, pues se ignora y se toma del JWT; obligatorio para `Admin`/`Empleado`).
    * `ShipAddress`, `ShipCity`, `ShipCountry`, `ShipVia` (opcionales; si se envía `ShipVia`, validar que exista en `shippers`).
    * `Items`: lista no vacía de `{ ProductId, Quantity }`. **NO aceptar `UnitPrice`, `Discount` ni `Total` desde el cliente.**
  * **Reglas de negocio obligatorias (validadas en servidor):**
    1. Verificar que el cliente exista y esté activo (`is_active == true`).
    2. Si el usuario es `Empleado` o `Admin`, asignar `EmployeeId` desde su claim (si tiene). Si es `Cliente`, dejar `EmployeeId = null`.
    3. Verificar que `Items` tenga al menos 1 producto y no contenga `ProductId` duplicados (`400 Bad Request`, evitando conflictos en la clave compuesta `OrderID` + `ProductID`).
    4. Por cada ítem:
       * `Quantity` debe ser mayor a 0 (`400 Bad Request`).
       * Consultar el producto real en BD. Si no existe -> `404 Not Found`.
       * **No comprar descontinuados:** Si `Product.Discontinued == true` -> rechazar toda la venta (`409 Conflict` con mensaje claro).
       * **Control de inventario:** Si `Product.UnitsInStock < item.Quantity` -> rechazar la venta indicando stock insuficiente (`409 Conflict`).
       * Si `Product.UnitPrice` o `Product.UnitsInStock` es `NULL`, rechazar la venta completa (`409 Conflict`); no sustituirlos por valores enviados por el cliente ni por cero.
       * **Anti-falsificación de precios y descuentos:** Tomar `UnitPrice` exclusivamente de `Product.UnitPrice` en la base de datos y asignar `Discount = 0`.
       * Descontar `Product.UnitsInStock -= item.Quantity`.
    5. Ejecutar la inserción de `Order`, `OrderDetails` y actualización de stock dentro de una **transacción**.
  * Los importes de respuesta se calculan a partir de los detalles persistidos: subtotal de línea = `UnitPrice * Quantity * (1 - Discount)`, subtotal de la orden = suma de líneas y total = subtotal + `Freight`.
* **Sin eliminación de ventas:** No exponer `DELETE /api/ventas/{id}`. Las ventas son registros históricos inmutables.

### 5.6 Catálogo (`Products`, `Categories`, `Suppliers`) y Paqueterías (`Shippers`)
* **Política de escritura:** `POST`, `PUT` y `DELETE` en `ProductsController`, `CategoriesController` y `SuppliersController` usan la política `WriteCatalog` (`Admin` y `Empleado`).
* **Baja lógica en productos (`DELETE /api/productos/{id}`):** En lugar de eliminar físicamente el producto de la tabla `products`, marcar `Discontinued = true` y guardar. Esto garantiza la regla de evitar eliminaciones físicas y facilita probar la restricción de compra de productos descontinuados.
* **Evitar duplicados:** Validar antes de insertar/actualizar que no exista otro producto con el mismo `ProductName`, categoría con el mismo `CategoryName` o proveedor con el mismo `CompanyName` (`409 Conflict`).
* **Protección de integridad:** Mantener el bloqueo contra eliminación física en categorías y proveedores que tengan productos relacionados (`409 Conflict`).
* **Paqueterías (`ShippersController` — `/api/shippers`):**
  * `GET /api/shippers` *(Autenticado)*: Devuelve una lista ligera de `ShipperView` (`ShipperId`, `CompanyName`) para poblar el selector opcional de envío (`ShipVia`) en el formulario de registro de ventas.

---

## 6. Mensajes Adecuados y Manejo de Errores
* Mantener `ApiExceptionHandler` devolviendo `ProblemDetails` (RFC 7807).
* Todos los mensajes de error deben estar en **español claro**, orientados al usuario/cliente de la API, sin exponer trazas de pila, nombres de columnas internos ni mensajes crudos de MySQL.
* Códigos HTTP consistentes:
  * `400 Bad Request`: Errores de validación, cantidades inválidas, entradas maliciosas.
  * `401 Unauthorized`: Token faltante, expirado, revocado o credenciales inválidas.
  * `403 Forbidden`: Rol insuficiente o intento de acceder a recursos de otro cliente.
  * `404 Not Found`: Recurso inexistente.
  * `409 Conflict`: Duplicados, producto descontinuado, stock insuficiente, cliente inactivo.
  * `423 Locked`: Cuenta bloqueada por 3 intentos fallidos.
  * `429 Too Many Requests`: Rate limit excedido.
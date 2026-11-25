# Equipo5.Api — Notas heredadas de integración del cliente web Northwind

> **Aviso:** este documento conserva ejemplos de una versión anterior del contrato (incluidos los roles Editor/Lector y rutas detalladas). No debe usarse como contrato vigente. La fuente de verdad para el backend actual es `Specs_backend.md`; los endpoints no se modifican en esta limpieza.

La URL real de IIS la proporciona el Equipo 5: `http://IP_DEL_EQUIPO5:5055` o su URL HTTPS. No se necesita Swagger ni el proyecto C#. Las credenciales de prueba antiguas ya no aplican: el Equipo 5 debe crear y entregar una cuenta **Editor** para realizar CRUD, o **Lector** para consultar.

## Autenticación

POST `/api/auth/login`, Content-Type: application/json:

```json
{"username":"USUARIO_ENTREGADO","password":"CONTRASEÑA_ENTREGADA"}
```

Respuesta 200:

```json
{
  "accessToken":"JWT",
  "tokenType":"Bearer",
  "expiresAtUtc":"2026-10-23T23:00:00Z",
  "user":{"id":1,"username":"usuario","role":"Editor","isActive":true,"createdAtUtc":"2026-10-23T20:00:00Z"}
}
```

Fecha y valores ilustrativos. El token vence en 60 minutos. Para todas las demás peticiones:

```http
Authorization: Bearer JWT
Accept: application/json
```

Agrega Content-Type: application/json cuando envíes un cuerpo JSON. GET `/api/auth/me` devuelve el perfil. POST `/api/auth/logout` devuelve 204 y revoca todas las sesiones de la cuenta. PUT `/api/auth/password` recibe `currentPassword` y `newPassword`, devuelve 204 y requiere nuevo login. La nueva contraseña debe tener 12 a 128 caracteres. No hay refresh ni registro público.

## Rutas CRUD

| Recurso | URL base |
|---|---|
| Productos | /api/productos |
| Categorías | /api/categorias |
| Proveedores | /api/proveedores |

En cada URL base:

- GET `?limit=20&offset=0`: 200, arreglo directo.
- GET `/{id}`: 200, objeto o 404.
- POST: 201, objeto creado (incluye ID) y header Location.
- PUT `/{id}`: 204 sin cuerpo.
- DELETE `/{id}`: 204 sin cuerpo.

Lectura: cualquier usuario activo. Escritura: Editor o Admin. IDs enteros generados por el servidor. limit: 1–100 (predeterminado 20). offset: >=0 (predeterminado 0). Orden por ID; no se devuelve total. PUT reemplaza todos los campos editables: envía el formulario completo, incluyendo los valores opcionales que quieras conservar. No hay PATCH.

### Producto: cuerpo POST/PUT

```json
{
  "productName":"Producto de prueba",
  "supplierId":1,
  "categoryId":1,
  "quantityPerUnit":"Caja de 10",
  "unitPrice":25.50,
  "unitsInStock":10,
  "unitsOnOrder":0,
  "reorderLevel":2,
  "discontinued":false
}
```

La respuesta GET/POST contiene esos mismos campos más `productId`. Los IDs relacionados deben existir; no asumas que el ejemplo 1 exista en cualquier base. Los GET de categorías/proveedores sirven para llenar los selectores del formulario.

- productName: obligatorio, no blanco, máximo 40 caracteres.
- supplierId/categoryId: entero positivo o null.
- quantityPerUnit: string de máximo 20 caracteres o null.
- unitPrice: número entre 0 y 999999999999999.9999, hasta 4 decimales, o null.
- unitsInStock/unitsOnOrder/reorderLevel: entero entre 0 y 32767, o null.
- discontinued: booleano, false si se omite.

No se elimina un producto con pedidos: devuelve 409. Puede marcarse discontinued=true.

### Categoría: cuerpo POST/PUT

```json
{"categoryName":"Pruebas","description":"Descripción","picture":null}
```

Respuesta GET/POST: mismos campos más `categoryId`.

- categoryName: obligatorio, no blanco, máximo 15 caracteres.
- description: string de máximo 10000 caracteres o null.
- picture: string base64 o null; máximo 262144 bytes decodificados para nuevas escrituras. No es una URL ni un objeto File. Las imágenes históricas de Northwind pueden ser datos OLE y no necesariamente una imagen lista para `<img>`; el frontend puede omitir su visualización.

No se elimina una categoría con productos: devuelve 409.

### Proveedor: cuerpo POST/PUT

```json
{
  "companyName":"Proveedor de prueba",
  "contactName":"Ana",
  "contactTitle":"Ventas",
  "address":"Calle 1",
  "city":"Puebla",
  "region":null,
  "postalCode":"72000",
  "country":"México",
  "phone":"2221234567",
  "fax":null,
  "homePage":null
}
```

Respuesta GET/POST: mismos campos más `supplierId`.

companyName es obligatorio, no blanco y de máximo 40 caracteres. Los demás campos son string o null. Límites: contactName/contactTitle 30, address 60, city/region/country 15, postalCode 10, phone/fax 24, homePage 2000. No se elimina un proveedor con productos: devuelve 409.

## Consulta de clientes conservada

GET `/api/clientes?limit=20&offset=0`, con JWT: arreglo de objetos `customerId` (string), `companyName` (string), `contactName`, `city`, `country` (string o null). No se implementaron escrituras de clientes.

## Administración de usuarios (solo para frontend Admin)

- GET `/api/usuarios?limit=20&offset=0`: arreglo de perfiles sin secretos.
- GET `/api/usuarios/{id}`: perfil.
- POST `/api/usuarios`: `{ "username":"nuevo", "password":"UnaClaveNueva!2026", "role":"Editor" }`, 201.
- PUT `/api/usuarios/{id}`: `{ "username":"nuevo", "role":"Lector", "isActive":true }`, 204.
- DELETE `/api/usuarios/{id}`: desactiva, 204; no borra físicamente.
- PUT `/api/usuarios/{id}/password`: `{ "newPassword":"OtraClaveNueva!2026" }`, 204.

Roles válidos exactos: Admin, Editor, Lector. Usuario: 3–50 caracteres, letras ASCII, números, punto, guion y guion bajo; único sin distinguir mayúsculas. Contraseña: 12–128 caracteres. Cambiar un usuario revoca sus tokens. Para reactivar usa PUT con isActive=true. Un Admin no puede desactivarse ni quitarse el rol a sí mismo. El frontend debe ocultar acciones no autorizadas, pero la API también las bloquea.

## Errores y frontend

| Estado | Manejo |
|---|---|
| 400 | Mostrar errores de validación / revisar JSON o relaciones |
| 401 | Credenciales incorrectas o sesión vencida/revocada: volver al login |
| 403 | Usuario autenticado sin permiso |
| 404 | Registro/ruta inexistente |
| 409 | Relaciones impiden borrar, nombre duplicado o conflicto de actualización |
| 429 | Límite de login/cambio de contraseña: esperar un minuto |
| 503 | MySQL no disponible |
| 500 | Error interno: reportar al Equipo 5 |

Un error puede contener message, Problem Details (title/detail/errors) o no tener JSON. No hagas response.json() sobre un 204. Ejemplo:

```javascript
async function llamar(base, ruta, token, method = 'GET', body) {
  const response = await fetch(base + ruta, {
    method,
    headers: {
      Authorization: `Bearer ${token}`,
      ...(body !== undefined ? { 'Content-Type': 'application/json' } : {})
    },
    body: body === undefined ? undefined : JSON.stringify(body)
  });
  if (response.status === 204) return null;
  const text = await response.text();
  let data = null;
  try { data = text ? JSON.parse(text) : null; } catch {}
  if (!response.ok) {
    const error = new Error(data?.detail || data?.message || data?.title || `HTTP ${response.status}`);
    error.status = response.status;
    error.details = data;
    throw error;
  }
  return data;
}
```

CORS está abierto y no usa cookies; no envíes credentials: include ni mode: no-cors. Usa protocolos compatibles: un frontend HTTPS necesita API HTTPS para evitar contenido mixto. La API no consume servicios externos y no necesita API Key.

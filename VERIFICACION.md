# Verificación de CRUD y usuarios

- Proyecto: C:\Users\Damian\Documents\API\Equipo5.Api.
- Framework net10.0. Compilación Release: 0 errores y 0 advertencias.
- Publicación Release generada y ejecutada en modo Production; no se modificó el sitio IIS instalado.
- 78 comprobaciones HTTP de CRUD, validación, roles, usuarios, revocación y CORS aprobadas.
- 11 comprobaciones adicionales de publicación y JWT aprobadas.
- Se retiró el cliente y controlador de consumo externo.
- Catálogos de prueba eliminados; dos cuentas de prueba quedaron desactivadas.
- Contraseña inicial del administrador entregada en un archivo privado separado.

## Pruebas de integración
```text
PASS Swagger (200)
PASS No external API route
PASS Unauthorized /api/productos (401)
PASS Unauthorized /api/categorias (401)
PASS Unauthorized /api/proveedores (401)
PASS Unauthorized /api/usuarios (401)
PASS Invalid login (401)
PASS Admin login (200)
PASS Current user (200)
PASS Admin role
PASS Self deactivation refused (409)
PASS Self demotion refused (409)
PASS Create reader (201)
PASS No password/hash/stamp exposed
PASS Duplicate normalized username (409)
PASS Reject short password (400)
PASS Reject unknown role (400)
PASS Reader login (200)
PASS Reader can list products (200)
PASS Reader cannot manage users (403)
PASS Reader cannot create catalog (403)
PASS Create editor (201)
PASS Editor login (200)
PASS Editor cannot manage users (403)
PASS Create category (201)
PASS Create supplier (201)
PASS Create product with foreign keys (201)
PASS Product foreign keys returned
PASS Read /api/categorias/17 (200)
PASS Read /api/proveedores/30 (200)
PASS Read /api/productos/92 (200)
PASS Category in use cannot be deleted (409)
PASS Supplier in use cannot be deleted (409)
PASS Ordered product cannot be deleted (409)
PASS Unknown category refused (400)
PASS Unknown supplier refused (400)
PASS Negative price refused (400)
PASS Price precision refused (400)
PASS Whitespace name refused (400)
PASS Category length refused (400)
PASS Supplier length refused (400)
PASS Missing product (404)
PASS Invalid pagination (400)
PASS Negative offset (400)
PASS Update category (204)
PASS Read changed category (200)
PASS Category update persisted
PASS Update supplier (204)
PASS Read changed supplier (200)
PASS Supplier update persisted
PASS Update product (204)
PASS Read changed product (200)
PASS Product update persisted
PASS Delete /api/productos/92 (204)
PASS Deleted /api/productos/92 absent (404)
PASS Delete /api/proveedores/30 (204)
PASS Deleted /api/proveedores/30 absent (404)
PASS Delete /api/categorias/17 (204)
PASS Deleted /api/categorias/17 absent (404)
PASS Role change (204)
PASS Old token revoked after role change (401)
PASS Login after role change (200)
PASS Changed role cannot write (403)
PASS Logout (204)
PASS Logout revokes JWT (401)
PASS Wrong current password (400)
PASS Password change (204)
PASS Old token revoked after password change (401)
PASS Login with new password (200)
PASS Admin password reset (204)
PASS Reset revokes JWT (401)
PASS Login with reset password (200)
PASS Deactivate user (204)
PASS Inactive user token revoked (401)
PASS Inactive user cannot login (401)
PASS Login rate limit (429)
PASS CORS PUT preflight (204)
PASS CORS wildcard origin
```

## Publicación
```text
PASS Production Swagger disabled (404)
PASS Production OpenAPI disabled (404)
PASS External endpoint removed (404)
PASS Published admin login (200)
PASS Published catalog query (200)
PASS Old demo login rejected (401)
PASS JWT rejects expired (401)
PASS JWT rejects issuer (401)
PASS JWT rejects audience (401)
PASS JWT rejects signature (401)
PASS published SQL and IIS web.config present
```

Pendiente del usuario: volver a publicar en IIS y entregar al Equipo 2 una cuenta nueva con el rol adecuado. El comportamiento de IIS y el acceso desde otra computadora no se verificaron en estas pruebas.

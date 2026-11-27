# NetDesk API

API REST en .NET 9 con arquitectura por capas, al estilo onion.

```
src/NetDesk.Domain            Entidades, estados y reglas de caja, turno y contrato
src/NetDesk.Application       Casos de uso, validadores y DTOs
src/NetDesk.Infrastructure    EF Core, cifrado, JWT, Excel y semilla
src/NetDesk.Api               Controladores, Swagger y Serilog
scripts/stored-procedures.sql Función y procedimientos
```

## Persistencia

Entity Framework Core sobre SQL Server. La migración inicial está en `Persistence/Migrations`. Al arrancar, la API ejecuta `Migrate()`, luego el script de `scripts/stored-procedures.sql` y la semilla.

Objetos de base de datos:

- `dbo.fn_TotalTurnosRango`: total del día para la bienvenida, según el rol.
- `dbo.sp_GenerarCodigoTurno`: siguiente código `AC0001`, `PS0001`, etc., con bloqueo para no repetir.
- `dbo.sp_DashboardIndicadores`: sesiones, cajas, cajeros y gestores en un rango de fechas.

## Seguridad

- Contraseñas con `PasswordHasher` (PBKDF2). No se pueden recuperar; el flujo de recuperación emite un código de un solo uso.
- Identificación y correo con AES-256-CBC e IV aleatorio. El índice de búsqueda es HMAC-SHA256 del valor normalizado.
- JWT cifrado (JWE, `A256KW` + `A128CBC-HS256`) además de la firma HMAC. Swagger pide ese token en `Authorization: Bearer`.
- Las rutas están autorizadas por rol.

En desarrollo, `Recuperacion:ExponerCodigo` devuelve el código de recuperación en la respuesta para poder probar sin servidor SMTP. En producción debe quedar en `false`.

## Serilog

Escribe en consola y en `logs/netdesk-.log`. Quedan registrados los inicios y cierres de sesión, los intentos fallidos, las alertas de negocio y cada creación, actualización y eliminación. Los errores no controlados se registran con la ruta y la excepción.

## Ejecutar

```bash
dotnet run --project src/NetDesk.Api --launch-profile http
```

Swagger: http://localhost:5080/swagger

La cadena local apunta a `(localdb)\MSSQLLocalDB`. Docker Compose la reemplaza por el contenedor de SQL Server.

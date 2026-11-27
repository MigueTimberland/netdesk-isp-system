# NetDesk ISP

Sistema de caja para un proveedor de internet. Cubre usuarios por rol, turnos, contratos y pagos, con API .NET 9 y cliente Angular 19.

## Estructura

```
Backend/     API, dominio, aplicación e infraestructura
Frontend/    Angular modular
postman/     Colección de los CRUD y de login
Backend/scripts/stored-procedures.sql
AI/          Decisiones y prompt usados en el desarrollo
```

## Arranque con Docker

Requiere Docker Desktop. El frontend queda en el puerto 8088 y la API en el 8080.

```bash
docker compose up --build
```

- Aplicación: http://localhost:8088
- Swagger: http://localhost:8080/swagger
- SQL Server: `localhost,1433`, usuario `sa`, clave `NetDesk_Sql2026!`, base `NetDeskIsp`

La API reintenta la conexión mientras SQL Server termina de iniciar, aplica migraciones, crea la función y los procedimientos, y carga la semilla.

## Arranque local

Esta máquina tiene LocalDB y no tiene Docker. El perfil local usa `(localdb)\MSSQLLocalDB`.

```bash
dotnet run --project Backend/src/NetDesk.Api --launch-profile http
cd Frontend
npm install
npx ng serve
```

- Aplicación: http://localhost:4200
- API: http://localhost:5080/swagger

## Usuarios de prueba

| Usuario | Clave | Rol |
| --- | --- | --- |
| admin123 | Admin1234 | Administrador |
| gestor123 | Gestor1234 | Gestor |
| cajero123 | Cajero1234 | Cajero, asignado a CJ01 |
| cajero234 | Cajero1234 | Cajero sin caja al inicio |
| gestor234 | Gestor1234 | Gestor pendiente de aprobación |

Cliente de demostración: identificación `0923456781`, contrato vigente de Fibra 100. El cajero `cajero123` tiene el turno `PS0001` para cobrarlo. Si ya se atendió en una prueba anterior, el gestor puede generar otro turno del mismo tipo.

## Reglas que implementa

- Un administrador crea gestores y cajeros activos. Un gestor también puede crearlos, pero quedan pendientes hasta que un administrador los apruebe.
- Usuario de 8 a 20 caracteres, letras y al menos un número, sin símbolos, único.
- Contraseña de 8 a 30 caracteres, con una mayúscula y un número. Se guarda con hash PBKDF2. Tres intentos fallidos bloquean la cuenta.
- Identificación y correo se guardan cifrados con AES-256-CBC. La búsqueda usa un índice HMAC, así que el valor en claro no queda en la base.
- El token es un JWE: va firmado y las claims viajan cifradas.
- Cada caja tiene como máximo dos cajeros. Esos dos no pueden operarla al mismo tiempo.
- El turno se numera con dos letras y cuatro dígitos según el trámite: `AC` contratación, `PS` pago, `CS` cambio de servicio, `CP` cambio de forma de pago, `CC` cancelación.
- Cambiar a un plan de otra velocidad marca el contrato anterior como `SUS` y abre uno `REN` con la misma fecha fin.
- Cancelar actualiza el estado a `CAN` y cierra la fecha fin.
- Las bajas son lógicas.
- El menú sale de la base según el rol.

## Documentación de cada proyecto

- [Backend](Backend/README.md)
- [Frontend](Frontend/README.md)

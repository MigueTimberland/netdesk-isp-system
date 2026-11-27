# Desarrollo asistido

## Pedido

Construir la prueba .NET Core + Angular de gestión de caja para un ISP, a partir de `PRUEBA.pdf`, dentro del repositorio `netdesk-isp-system`. El esquema de base del PDF se tomó como referencia y se ajustó donde la regla de negocio lo pedía.

## Decisiones que quedaron en el código

- Arquitectura por capas: Domain, Application, Infrastructure y Api.
- SQL Server con EF Core, migraciones y tres objetos T-SQL: una función para la bienvenida y dos procedimientos (código de turno y dashboard).
- Contraseña con hash de un solo sentido. Identificación y correo con AES y un índice HMAC para poder buscarlos sin guardar el texto en claro.
- El token es un JWE. Las claims no se leen sin la llave de cifrado.
- Un cajero por caja en la operación, y máximo dos asignados. La apertura de caja es condicional en base de datos para que el segundo cajero no entre si el primero ya la tiene abierta.
- El cambio de velocidad no reescribe el contrato: el vigente pasa a `SUS` y se crea uno `REN` con la misma fecha fin.
- Angular con NgModules y rutas perezosas, porque la prueba pide módulos y protección de rutas.
- Serilog en archivo y consola para altas, cambios, bajas, errores, alertas y sesiones.
- La recuperación de clave muestra el código solo cuando `Recuperacion:ExponerCodigo` está activo, para poder demostrar el flujo sin SMTP.

## Qué se verificó en local

Contra LocalDB, con la API en marcha:

- Login de administrador, token JWE de 5 segmentos, menú por rol y dashboard.
- Apertura de CJ01, atención de `PS0001`, búsqueda del cliente `0923456781`, cobro y cierre del turno.
- Generación de `AC0001` por el procedimiento.
- Cambio de plan: `CT-000001` quedó `SUS` y `CT-000002` quedó `REN` con la misma fecha fin.
- El segundo cajero asignado no puede abrir la caja mientras el primero la opera.

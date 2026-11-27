# NetDesk Frontend

Angular 19 organizado en módulos. Las rutas cargan módulos con `loadChildren`. El menú no está fijo en el código: llega de `/api/menus/mios` según el rol del token.

## Módulos

```
core        HTTP, interceptores, guards
shared      layout, Material y mensajes de formulario
features/auth         login y recuperación
features/bienvenida   datos del usuario y turnos del día
features/dashboard    indicadores del administrador
features/usuarios     mantenimiento y carga Excel
features/servicios    catálogo de planes
features/turnos       asignación de cajas y turnos del gestor
features/caja         procesos del cajero
features/clientes     mantenimiento y carga Excel
```

## Protección

- `authGuard` exige sesión.
- `roleGuard` lee `data.roles` de la ruta.
- `JwtInterceptor` adjunta el JWE.
- `LoadingInterceptor` muestra la barra superior mientras hay peticiones.
- `ErrorInterceptor` muestra el mensaje de error del API. Un 401 fuera del login cierra la sesión.

Los formularios se validan en el cliente antes de llamar al servicio. Las mismas reglas se vuelven a validar en la API.

## Ejecutar

```bash
npm install
npx ng serve
```

El proxy de desarrollo envía `/api` a http://localhost:5080. En Docker, nginx hace ese mismo reenvío hacia el servicio `backend`.

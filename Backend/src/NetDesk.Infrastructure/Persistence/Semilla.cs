using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetDesk.Application.Abstracciones;
using NetDesk.Domain.Entities;
using NetDesk.Domain.Enums;

namespace NetDesk.Infrastructure.Persistence;

public static class Semilla
{
    public static async Task AplicarAsync(IServiceProvider servicios)
    {
        var db = servicios.GetRequiredService<NetDeskDbContext>();
        var cifrado = servicios.GetRequiredService<ICifradoService>();
        var passwords = servicios.GetRequiredService<IPasswordService>();
        var logger = servicios.GetRequiredService<ILogger<NetDeskDbContext>>();

        if (!await db.Roles.AnyAsync())
        {
            db.Roles.AddRange(
                new Rol { Nombre = "Administrador", Descripcion = "Administra usuarios, indicadores y catálogo." },
                new Rol { Nombre = "Gestor", Descripcion = "Asigna cajeros, cajas y turnos." },
                new Rol { Nombre = "Cajero", Descripcion = "Atiende turnos y contratos en caja." });
            await db.SaveChangesAsync();
        }

        if (!await db.TiposAtencion.AnyAsync())
        {
            db.TiposAtencion.AddRange(
                new TipoAtencion { Codigo = "AC", Nombre = "Atención al cliente", Descripcion = "Contratación de servicio de internet." },
                new TipoAtencion { Codigo = "PS", Nombre = "Pago de servicio", Descripcion = "Cobro del servicio contratado." },
                new TipoAtencion { Codigo = "CS", Nombre = "Cambio de servicio", Descripcion = "Cambio a un plan de otra velocidad." },
                new TipoAtencion { Codigo = "CP", Nombre = "Cambio de forma de pago", Descripcion = "Actualiza la forma de pago del contrato." },
                new TipoAtencion { Codigo = "CC", Nombre = "Cancelación de contrato", Descripcion = "Cancela el servicio y cierra la fecha fin." });
            await db.SaveChangesAsync();
        }

        if (!await db.FormasPago.AnyAsync())
        {
            db.FormasPago.AddRange(
                new FormaPago { Nombre = "Efectivo" },
                new FormaPago { Nombre = "Transferencia" },
                new FormaPago { Nombre = "Tarjeta" });
            await db.SaveChangesAsync();
        }

        if (!await db.Servicios.AnyAsync())
        {
            db.Servicios.AddRange(
                new Servicio { Nombre = "Fibra Hogar 50", Descripcion = "Internet residencial para navegación y streaming.", Equipos = "ONT fibra + router wifi", VelocidadMbps = 50, Precio = 24.99m },
                new Servicio { Nombre = "Fibra Hogar 100", Descripcion = "Internet residencial para hogar conectado.", Equipos = "ONT fibra + router wifi dual", VelocidadMbps = 100, Precio = 34.99m },
                new Servicio { Nombre = "Fibra Hogar 300", Descripcion = "Internet de alta velocidad para teletrabajo.", Equipos = "ONT fibra + router wifi 6", VelocidadMbps = 300, Precio = 49.99m },
                new Servicio { Nombre = "Fibra Pro 600", Descripcion = "Internet simétrico para negocio pequeño.", Equipos = "ONT fibra + router wifi 6E", VelocidadMbps = 600, Precio = 69.99m });
            await db.SaveChangesAsync();
        }

        var roles = await db.Roles.ToDictionaryAsync(r => r.Nombre);
        if (!await db.Menus.AnyAsync())
        {
            db.Menus.AddRange(
                Menu(roles["Administrador"].Id, "Bienvenida", "/bienvenida", "home", 1),
                Menu(roles["Administrador"].Id, "Dashboard", "/dashboard", "dashboard", 2),
                Menu(roles["Administrador"].Id, "Usuarios", "/usuarios", "group", 3),
                Menu(roles["Administrador"].Id, "Servicios", "/servicios", "router", 4),
                Menu(roles["Gestor"].Id, "Bienvenida", "/bienvenida", "home", 1),
                Menu(roles["Gestor"].Id, "Turnos y cajas", "/turnos", "schedule", 2),
                Menu(roles["Cajero"].Id, "Bienvenida", "/bienvenida", "home", 1),
                Menu(roles["Cajero"].Id, "Procesos de caja", "/caja", "point_of_sale", 2),
                Menu(roles["Cajero"].Id, "Clientes", "/clientes", "badge", 3));
            await db.SaveChangesAsync();
        }

        async Task<Usuario> AsegurarUsuario(string usuario, string password, string nombres, string apellidos, string identificacion, string correo, string telefono, string rol, EstadoUsuario estado)
        {
            var existente = await db.Usuarios.FirstOrDefaultAsync(u => u.NombreUsuario == usuario);
            if (existente is not null)
            {
                return existente;
            }

            var nuevo = new Usuario
            {
                NombreUsuario = usuario,
                PasswordHash = passwords.Hash(password),
                Nombres = nombres,
                Apellidos = apellidos,
                IdentificacionCifrada = cifrado.Cifrar(identificacion),
                IdentificacionIndice = cifrado.Indice(identificacion),
                CorreoCifrado = cifrado.Cifrar(correo),
                CorreoIndice = cifrado.Indice(correo),
                Telefono = telefono,
                RolId = roles[rol].Id,
                Estado = estado,
                FechaAprobacion = estado == EstadoUsuario.Activo ? DateTime.UtcNow : null
            };
            db.Usuarios.Add(nuevo);
            await db.SaveChangesAsync();
            return nuevo;
        }

        var admin = await AsegurarUsuario("admin123", "Admin1234", "Miguel", "Administrador", "1710034065", "admin@netdesk.com", "0991000001", "Administrador", EstadoUsuario.Activo);
        var gestor = await AsegurarUsuario("gestor123", "Gestor1234", "Ana", "Grijalva", "0926684412", "gestor@netdesk.com", "0991000002", "Gestor", EstadoUsuario.Activo);
        var cajero = await AsegurarUsuario("cajero123", "Cajero1234", "Carlos", "Benitez", "0102030405", "cajero@netdesk.com", "0991000003", "Cajero", EstadoUsuario.Activo);
        await AsegurarUsuario("cajero234", "Cajero1234", "Diana", "Salazar", "0102030406", "cajero2@netdesk.com", "0991000004", "Cajero", EstadoUsuario.Activo);
        var pendiente = await db.Usuarios.FirstOrDefaultAsync(u => u.NombreUsuario == "gestor234");
        if (pendiente is null)
        {
            pendiente = new Usuario
            {
                NombreUsuario = "gestor234",
                PasswordHash = passwords.Hash("Gestor1234"),
                Nombres = "Pedro",
                Apellidos = "Navarrete",
                IdentificacionCifrada = cifrado.Cifrar("0102030407"),
                IdentificacionIndice = cifrado.Indice("0102030407"),
                CorreoCifrado = cifrado.Cifrar("gestor2@netdesk.com"),
                CorreoIndice = cifrado.Indice("gestor2@netdesk.com"),
                Telefono = "0991000005",
                RolId = roles["Gestor"].Id,
                Estado = EstadoUsuario.PendienteAprobacion,
                CreadoPorId = gestor.Id
            };
            db.Usuarios.Add(pendiente);
            await db.SaveChangesAsync();
        }

        if (admin.AprobadoPorId is null)
        {
            admin.AprobadoPorId = admin.Id;
            gestor.AprobadoPorId = admin.Id;
            cajero.AprobadoPorId = admin.Id;
            await db.SaveChangesAsync();
        }

        if (!await db.Cajas.AnyAsync())
        {
            db.Cajas.AddRange(
                new Caja { Codigo = "CJ01", Nombre = "Caja Norte", Descripcion = "Atención de contratos y pagos, planta baja." },
                new Caja { Codigo = "CJ02", Nombre = "Caja Sur", Descripcion = "Atención de contratos y pagos, ventanilla 2." });
            await db.SaveChangesAsync();
        }

        var caja = await db.Cajas.FirstAsync(c => c.Codigo == "CJ01");
        if (!await db.AsignacionesCaja.AnyAsync(a => a.CajaId == caja.Id && a.UsuarioId == cajero.Id))
        {
            db.AsignacionesCaja.Add(new AsignacionCaja
            {
                CajaId = caja.Id,
                UsuarioId = cajero.Id,
                AsignadoPorId = gestor.Id,
                Activa = true
            });
            await db.SaveChangesAsync();
        }

        if (!await db.Clientes.AnyAsync())
        {
            var cliente = new Cliente
            {
                Nombres = "María Elena",
                Apellidos = "Vásquez",
                IdentificacionCifrada = cifrado.Cifrar("0923456781"),
                IdentificacionIndice = cifrado.Indice("0923456781"),
                CorreoCifrado = cifrado.Cifrar("maria.vasquez@correo.com"),
                CorreoIndice = cifrado.Indice("maria.vasquez@correo.com"),
                Telefono = "0987654321",
                Direccion = "Av. Francisco de Orellana y Calle 12, edificio 4",
                ReferenciaDireccion = "Frente al parque Kennedy, casa de fachada blanca",
                Estado = EstadoCliente.Activo
            };
            db.Clientes.Add(cliente);
            await db.SaveChangesAsync();

            var servicio = await db.Servicios.FirstAsync(s => s.VelocidadMbps == 100);
            var efectivo = await db.FormasPago.FirstAsync(f => f.Nombre == "Efectivo");
            var tipoPago = await db.TiposAtencion.FirstAsync(t => t.Codigo == "PS");
            var turno = new Turno
            {
                Descripcion = "PS0001",
                TipoAtencionId = tipoPago.Id,
                CajaId = caja.Id,
                CajeroId = cajero.Id,
                GestorAsignadorId = gestor.Id,
                Estado = EstadoTurno.Asignado,
                FechaAsignacion = DateTime.UtcNow
            };
            db.Turnos.Add(turno);
            await db.SaveChangesAsync();

            db.Contratos.Add(new Contrato
            {
                Numero = "CT-000001",
                ClienteId = cliente.Id,
                ServicioId = servicio.Id,
                FormaPagoId = efectivo.Id,
                Estado = EstadoContrato.Vigente,
                FechaInicio = DateTime.UtcNow.AddMonths(-1),
                FechaFin = DateTime.UtcNow.AddMonths(11),
                CajeroId = cajero.Id,
                Observacion = "Contrato de demostración."
            });
            await db.SaveChangesAsync();
        }

        logger.LogInformation("Semilla de NetDesk verificada.");
    }

    private static MenuOpcion Menu(int rolId, string titulo, string ruta, string icono, int orden) => new()
    {
        RolId = rolId,
        Titulo = titulo,
        Ruta = ruta,
        Icono = icono,
        Orden = orden,
        Activo = true
    };
}

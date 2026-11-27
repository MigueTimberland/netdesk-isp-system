using NetDesk.Domain.Entities;

namespace NetDesk.Application.Abstracciones;

public interface ICifradoService
{
    string Cifrar(string textoPlano);
    string Descifrar(string textoCifrado);
    string Indice(string textoPlano);
}

public interface IPasswordService
{
    string Hash(string password);
    bool Verificar(string hash, string password);
}

public interface ITokenJwtService
{
    (string Token, DateTime ExpiraEn) Emitir(Usuario usuario);
}

public interface IUsuarioActual
{
    bool Autenticado { get; }
    int Id { get; }
    string NombreUsuario { get; }
    string Rol { get; }
}

public interface IReloj
{
    DateTime UtcAhora { get; }
    DateOnly HoyOperacion { get; }
    (DateTime InicioUtc, DateTime FinUtc) RangoDia(DateOnly fecha);
}

public interface IOpcionesAplicacion
{
    bool ExponerCodigoRecuperacion { get; }
}

public interface IUnidadDeTrabajo
{
    Task<int> GuardarAsync(CancellationToken ct = default);
    Task EjecutarEnTransaccionAsync(Func<CancellationToken, Task> accion, CancellationToken ct = default);
    void LimpiarSeguimiento();
}

public interface IUsuarioRepositorio
{
    Task<Usuario?> ObtenerPorIdAsync(int id, CancellationToken ct = default);
    Task<Usuario?> ObtenerPorNombreAsync(string nombreUsuario, CancellationToken ct = default);
    Task<Usuario?> ObtenerPorCorreoIndiceAsync(string indice, CancellationToken ct = default);
    Task<bool> ExisteNombreAsync(string nombreUsuario, int? exceptoId, CancellationToken ct = default);
    Task<bool> ExisteIdentificacionAsync(string indice, int? exceptoId, CancellationToken ct = default);
    Task<bool> ExisteCorreoAsync(string indice, int? exceptoId, CancellationToken ct = default);
    Task<IReadOnlyList<Usuario>> ListarAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Usuario>> ListarPendientesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Usuario>> ListarPorRolAsync(string rol, CancellationToken ct = default);
    Task AgregarAsync(Usuario usuario, CancellationToken ct = default);
    Task<int> ContarAdministradoresActivosAsync(CancellationToken ct = default);
}

public interface ITokenRecuperacionRepositorio
{
    Task AgregarAsync(TokenRecuperacion token, CancellationToken ct = default);
    Task<TokenRecuperacion?> ObtenerVigentePorHashAsync(string tokenHash, CancellationToken ct = default);
    Task InvalidarPendientesAsync(int usuarioId, CancellationToken ct = default);
}

public interface IClienteRepositorio
{
    Task<Cliente?> ObtenerPorIdAsync(int id, CancellationToken ct = default);
    Task<Cliente?> ObtenerPorIndiceAsync(string indice, CancellationToken ct = default);
    Task<bool> ExisteIdentificacionAsync(string indice, int? exceptoId, CancellationToken ct = default);
    Task<bool> ExisteCorreoAsync(string indice, int? exceptoId, CancellationToken ct = default);
    Task<IReadOnlyList<Cliente>> ListarAsync(string? texto, string? indiceIdentificacion, CancellationToken ct = default);
    Task AgregarAsync(Cliente cliente, CancellationToken ct = default);
}

public interface ICajaRepositorio
{
    Task<IReadOnlyList<Caja>> ListarAsync(CancellationToken ct = default);
    Task<Caja?> ObtenerPorIdAsync(int id, CancellationToken ct = default);
    Task<Caja?> ObtenerAbiertaPorUsuarioAsync(int usuarioId, CancellationToken ct = default);
    Task<bool> CajeroYaAsignadoAsync(int cajeroId, CancellationToken ct = default);
    Task AgregarAsync(Caja caja, CancellationToken ct = default);
    Task<bool> IntentarAbrirAsync(int cajaId, int usuarioId, CancellationToken ct = default);
    Task<bool> IntentarCerrarAsync(int cajaId, int usuarioId, CancellationToken ct = default);
}

public interface ITurnoRepositorio
{
    Task<Turno?> ObtenerPorIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Turno>> ListarAsync(DateTime inicioUtc, DateTime finUtc, int? cajaId, int? cajeroId, int? gestorId, CancellationToken ct = default);
    Task<bool> TieneEnAtencionAsync(int cajeroId, CancellationToken ct = default);
    Task AgregarAsync(Turno turno, CancellationToken ct = default);
    Task<string> GenerarCodigoAsync(string prefijo, CancellationToken ct = default);
}

public interface IContratoRepositorio
{
    Task<Contrato?> ObtenerPorIdAsync(int id, CancellationToken ct = default);
    Task<Contrato?> ObtenerOperablePorClienteAsync(int clienteId, CancellationToken ct = default);
    Task<IReadOnlyList<Contrato>> ListarPorClienteAsync(int clienteId, CancellationToken ct = default);
    Task<string> SiguienteNumeroAsync(CancellationToken ct = default);
    Task AgregarAsync(Contrato contrato, CancellationToken ct = default);
}

public interface IPagoRepositorio
{
    Task<IReadOnlyList<Pago>> ListarPorClienteAsync(int clienteId, CancellationToken ct = default);
    Task AgregarAsync(Pago pago, CancellationToken ct = default);
}

public interface ICatalogoRepositorio
{
    Task<Rol?> ObtenerRolAsync(int id, CancellationToken ct = default);
    Task<Rol?> ObtenerRolPorNombreAsync(string nombre, CancellationToken ct = default);
    Task<IReadOnlyList<Rol>> ListarRolesOperativosAsync(CancellationToken ct = default);
    Task<TipoAtencion?> ObtenerTipoAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<TipoAtencion>> ListarTiposAsync(CancellationToken ct = default);
    Task<FormaPago?> ObtenerFormaPagoAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<FormaPago>> ListarFormasPagoAsync(CancellationToken ct = default);
    Task<Servicio?> ObtenerServicioAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Servicio>> ListarServiciosAsync(bool soloActivos, CancellationToken ct = default);
    Task AgregarServicioAsync(Servicio servicio, CancellationToken ct = default);
    Task<IReadOnlyList<MenuOpcion>> ListarMenusPorRolAsync(int rolId, CancellationToken ct = default);
}

public interface IDashboardRepositorio
{
    Task<int> TotalTurnosAsync(int usuarioId, string rol, DateTime inicioUtc, DateTime finUtc, CancellationToken ct = default);
    Task<DashboardCrudo> IndicadoresAsync(DateTime inicioUtc, DateTime finUtc, CancellationToken ct = default);
}

public sealed class DashboardCrudo
{
    public int SesionActiva { get; set; }
    public int SesionInactiva { get; set; }
    public int Bloqueados { get; set; }
    public int Pendientes { get; set; }
    public int Activos { get; set; }
    public int Inactivos { get; set; }
    public List<FilaCaja> Cajas { get; set; } = [];
    public List<FilaPersona> Cajeros { get; set; } = [];
    public List<FilaPersona> Gestores { get; set; } = [];
}

public sealed class FilaCaja
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public int TurnosAsignados { get; set; }
    public int TurnosAtendidos { get; set; }
}

public sealed class FilaPersona
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Usuario { get; set; } = string.Empty;
    public int Total { get; set; }
}

public interface ILectorArchivoMasivo
{
    IReadOnlyList<FilaUsuarioArchivo> LeerUsuarios(Stream contenido, string nombreArchivo);
    IReadOnlyList<FilaClienteArchivo> LeerClientes(Stream contenido, string nombreArchivo);
    byte[] PlantillaUsuarios();
    byte[] PlantillaClientes();
}

public sealed record FilaUsuarioArchivo(
    int Fila,
    string NombreUsuario,
    string Password,
    string Nombres,
    string Apellidos,
    string Identificacion,
    string Correo,
    string Telefono,
    string Rol);

public sealed record FilaClienteArchivo(
    int Fila,
    string Nombres,
    string Apellidos,
    string Identificacion,
    string Correo,
    string Telefono,
    string Direccion,
    string ReferenciaDireccion);

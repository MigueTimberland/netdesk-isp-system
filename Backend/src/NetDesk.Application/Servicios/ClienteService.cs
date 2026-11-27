using FluentValidation;
using Microsoft.Extensions.Logging;
using NetDesk.Application.Abstracciones;
using NetDesk.Application.Comun;
using NetDesk.Application.Dtos;
using NetDesk.Domain.Common;
using NetDesk.Domain.Entities;

namespace NetDesk.Application.Servicios;

public sealed class ClienteService(
    IClienteRepositorio clientes,
    ICifradoService cifrado,
    IUnidadDeTrabajo unidad,
    IUsuarioActual actual,
    ILectorArchivoMasivo lector,
    Mapeador mapeador,
    IValidator<GuardarClienteRequest> validator,
    IValidator<CambiarEstadoRequest> estadoValidator,
    ILogger<ClienteService> logger)
{
    public async Task<IReadOnlyList<ClienteDto>> ListarAsync(string? texto, CancellationToken ct)
    {
        string? indice = null;
        var criterio = texto?.Trim();
        if (!string.IsNullOrWhiteSpace(criterio) && criterio.All(char.IsDigit) && criterio.Length is >= 10 and <= 13)
        {
            indice = cifrado.Indice(criterio);
            criterio = null;
        }

        var lista = await clientes.ListarAsync(criterio, indice, ct);
        return lista.Select(mapeador.Cliente).ToList();
    }

    public async Task<ClienteDto> ObtenerAsync(int id, CancellationToken ct)
    {
        var cliente = await clientes.ObtenerPorIdAsync(id, ct)
            ?? throw new NoEncontradoException("Cliente no encontrado.");
        return mapeador.Cliente(cliente);
    }

    public async Task<ClienteDto?> BuscarPorIdentificacionAsync(string identificacion, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(identificacion) || !System.Text.RegularExpressions.Regex.IsMatch(identificacion.Trim(), Reglas.Identificacion))
        {
            throw new ReglaNegocioException(Reglas.MensajeIdentificacion);
        }

        var cliente = await clientes.ObtenerPorIndiceAsync(cifrado.Indice(identificacion.Trim()), ct);
        return cliente is null ? null : mapeador.Cliente(cliente);
    }

    public async Task<ClienteDto> CrearAsync(GuardarClienteRequest request, CancellationToken ct)
    {
        var cliente = await CrearInternoAsync(request, ct);
        unidad.LimpiarSeguimiento();
        return mapeador.Cliente(cliente);
    }

    public async Task<ClienteDto> ActualizarAsync(int id, GuardarClienteRequest request, CancellationToken ct)
    {
        await validator.ValidarAsync(request, ct);
        var cliente = await clientes.ObtenerPorIdAsync(id, ct)
            ?? throw new NoEncontradoException("Cliente no encontrado.");
        await AsegurarUnicosAsync(request.Identificacion, request.Correo, cliente.Id, ct);
        Aplicar(cliente, request);
        cliente.Tocar();
        await unidad.GuardarAsync(ct);
        logger.LogInformation("Actualización de cliente {ClienteId} por {Actor}.", cliente.Id, actual.NombreUsuario);
        return mapeador.Cliente(cliente);
    }

    public async Task<ClienteDto> CambiarEstadoAsync(int id, CambiarEstadoRequest request, CancellationToken ct)
    {
        await estadoValidator.ValidarAsync(request, ct);
        var cliente = await clientes.ObtenerPorIdAsync(id, ct)
            ?? throw new NoEncontradoException("Cliente no encontrado.");
        cliente.CambiarEstado(NombresEstado.ParseCliente(request.Estado));
        await unidad.GuardarAsync(ct);
        logger.LogInformation("Actualización de estado del cliente {ClienteId} a {Estado} por {Actor}.", cliente.Id, cliente.Estado, actual.NombreUsuario);
        return mapeador.Cliente(cliente);
    }

    public async Task EliminarAsync(int id, CancellationToken ct)
    {
        var cliente = await clientes.ObtenerPorIdAsync(id, ct)
            ?? throw new NoEncontradoException("Cliente no encontrado.");
        cliente.MarcarEliminado();
        await unidad.GuardarAsync(ct);
        logger.LogInformation("Eliminación lógica del cliente {ClienteId} por {Actor}.", cliente.Id, actual.NombreUsuario);
    }

    public async Task<CargaMasivaResultadoDto> CargarMasivoAsync(Stream archivo, string nombreArchivo, CancellationToken ct)
    {
        var filas = lector.LeerClientes(archivo, nombreArchivo);
        if (filas.Count == 0)
        {
            throw new ReglaNegocioException("El archivo no contiene filas para procesar.");
        }

        var errores = new List<string>();
        var creados = 0;
        foreach (var fila in filas)
        {
            try
            {
                await CrearInternoAsync(new GuardarClienteRequest(
                    fila.Nombres, fila.Apellidos, fila.Identificacion, fila.Correo, fila.Telefono, fila.Direccion, fila.ReferenciaDireccion), ct);
                creados++;
            }
            catch (Exception ex) when (ex is ValidacionException or ReglaNegocioException)
            {
                var detalle = ex is ValidacionException validacion
                    ? string.Join(" ", validacion.Errores.SelectMany(e => e.Value))
                    : ex.Message;
                errores.Add($"Fila {fila.Fila}: {detalle}");
            }
            finally
            {
                unidad.LimpiarSeguimiento();
            }
        }

        logger.LogInformation("Carga masiva de clientes. Creados {Creados}. Fallidos {Fallidos}. Actor {Actor}.", creados, errores.Count, actual.NombreUsuario);
        return new CargaMasivaResultadoDto(creados, errores.Count, errores);
    }

    public byte[] Plantilla() => lector.PlantillaClientes();

    private async Task<Cliente> CrearInternoAsync(GuardarClienteRequest request, CancellationToken ct)
    {
        await validator.ValidarAsync(request, ct);
        await AsegurarUnicosAsync(request.Identificacion, request.Correo, null, ct);
        var cliente = new Cliente();
        Aplicar(cliente, request);
        await clientes.AgregarAsync(cliente, ct);
        await unidad.GuardarAsync(ct);
        logger.LogInformation("Creación de cliente {Identificacion} por {Actor}.", request.Identificacion, actual.NombreUsuario);
        return cliente;
    }

    private void Aplicar(Cliente cliente, GuardarClienteRequest request)
    {
        var id = Normalizador.Identificacion(request.Identificacion);
        var correo = Normalizador.Correo(request.Correo);
        cliente.Nombres = Normalizador.Nombre(request.Nombres);
        cliente.Apellidos = Normalizador.Nombre(request.Apellidos);
        cliente.IdentificacionCifrada = cifrado.Cifrar(id);
        cliente.IdentificacionIndice = cifrado.Indice(id);
        cliente.CorreoCifrado = cifrado.Cifrar(correo);
        cliente.CorreoIndice = cifrado.Indice(correo);
        cliente.Telefono = Normalizador.Telefono(request.Telefono);
        cliente.Direccion = Normalizador.Texto(request.Direccion);
        cliente.ReferenciaDireccion = Normalizador.Texto(request.ReferenciaDireccion);
    }

    private async Task AsegurarUnicosAsync(string identificacion, string correo, int? exceptoId, CancellationToken ct)
    {
        if (await clientes.ExisteIdentificacionAsync(cifrado.Indice(Normalizador.Identificacion(identificacion)), exceptoId, ct))
        {
            throw new ReglaNegocioException("Ya existe un cliente con esa identificación.");
        }

        if (await clientes.ExisteCorreoAsync(cifrado.Indice(Normalizador.Correo(correo)), exceptoId, ct))
        {
            throw new ReglaNegocioException("Ya existe un cliente con ese correo.");
        }
    }
}

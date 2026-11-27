using NetDesk.Domain.Common;
using NetDesk.Domain.Enums;

namespace NetDesk.Domain.Entities;

public class Contrato : EntidadAuditable
{
    public string Numero { get; set; } = string.Empty;
    public int ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;
    public int ServicioId { get; set; }
    public Servicio Servicio { get; set; } = null!;
    public int FormaPagoId { get; set; }
    public FormaPago FormaPago { get; set; } = null!;
    public EstadoContrato Estado { get; set; } = EstadoContrato.Vigente;
    public int? ContratoOrigenId { get; set; }
    public Contrato? ContratoOrigen { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public int? TurnoId { get; set; }
    public Turno? Turno { get; set; }
    public int CajeroId { get; set; }
    public Usuario Cajero { get; set; } = null!;
    public string? Observacion { get; set; }
    public ICollection<Pago> Pagos { get; set; } = new List<Pago>();

    public bool EsOperable =>
        !Eliminado && Estado is EstadoContrato.Vigente or EstadoContrato.Renovacion;

    public static Contrato Nuevo(
        int clienteId,
        int servicioId,
        int formaPagoId,
        string numero,
        int cajeroId,
        int? turnoId,
        DateTime fechaInicio,
        int meses)
    {
        return new Contrato
        {
            ClienteId = clienteId,
            ServicioId = servicioId,
            FormaPagoId = formaPagoId,
            Numero = numero,
            Estado = EstadoContrato.Vigente,
            FechaInicio = fechaInicio,
            FechaFin = fechaInicio.AddMonths(meses),
            TurnoId = turnoId,
            CajeroId = cajeroId
        };
    }

    public Contrato Sustituir(int nuevoServicioId, string nuevoNumero, int cajeroId, int? turnoId)
    {
        if (!EsOperable)
        {
            throw new ReglaNegocioException("Solo se puede cambiar el servicio de un contrato vigente.");
        }

        if (nuevoServicioId == ServicioId)
        {
            throw new ReglaNegocioException("El nuevo servicio debe tener una velocidad distinta.");
        }

        var fechaFinOriginal = FechaFin;
        Estado = EstadoContrato.Sustituido;
        Tocar();

        return new Contrato
        {
            ClienteId = ClienteId,
            ServicioId = nuevoServicioId,
            FormaPagoId = FormaPagoId,
            Numero = nuevoNumero,
            Estado = EstadoContrato.Renovacion,
            ContratoOrigenId = Id,
            FechaInicio = DateTime.UtcNow,
            FechaFin = fechaFinOriginal,
            TurnoId = turnoId,
            CajeroId = cajeroId,
            Observacion = $"Renovación por cambio de servicio. Origen {Numero}."
        };
    }

    public void CambiarFormaPago(int formaPagoId)
    {
        if (!EsOperable)
        {
            throw new ReglaNegocioException("Solo se puede cambiar la forma de pago de un contrato vigente.");
        }

        if (FormaPagoId == formaPagoId)
        {
            throw new ReglaNegocioException("El contrato ya usa esa forma de pago.");
        }

        FormaPagoId = formaPagoId;
        Tocar();
    }

    public void Cancelar(string? observacion)
    {
        if (!EsOperable)
        {
            throw new ReglaNegocioException("Solo se puede cancelar un contrato vigente.");
        }

        Estado = EstadoContrato.Cancelado;
        FechaFin = DateTime.UtcNow;
        Observacion = observacion;
        Tocar();
    }
}

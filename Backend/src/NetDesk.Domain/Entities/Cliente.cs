using NetDesk.Domain.Common;
using NetDesk.Domain.Enums;

namespace NetDesk.Domain.Entities;

public class Cliente : EntidadAuditable
{
    public string Nombres { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public string IdentificacionCifrada { get; set; } = string.Empty;
    public string IdentificacionIndice { get; set; } = string.Empty;
    public string CorreoCifrado { get; set; } = string.Empty;
    public string CorreoIndice { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public string ReferenciaDireccion { get; set; } = string.Empty;
    public EstadoCliente Estado { get; set; } = EstadoCliente.Activo;
    public ICollection<Contrato> Contratos { get; set; } = new List<Contrato>();

    public string NombreCompleto => $"{Nombres} {Apellidos}".Trim();

    public void CambiarEstado(EstadoCliente estado)
    {
        Estado = estado;
        Tocar();
    }

    public void AsegurarActivo()
    {
        if (Eliminado || Estado != EstadoCliente.Activo)
        {
            throw new ReglaNegocioException("El cliente no está activo.");
        }
    }
}

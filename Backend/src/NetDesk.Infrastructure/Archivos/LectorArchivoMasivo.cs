using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using NetDesk.Application.Abstracciones;
using NetDesk.Domain.Common;

namespace NetDesk.Infrastructure.Archivos;

public class LectorArchivoMasivo : ILectorArchivoMasivo
{
    private static readonly string[] ColumnasUsuario =
    [
        "nombreusuario", "password", "nombres", "apellidos", "identificacion", "correo", "telefono", "rol"
    ];

    private static readonly string[] ColumnasCliente =
    [
        "nombres", "apellidos", "identificacion", "correo", "telefono", "direccion", "referenciadireccion"
    ];

    public IReadOnlyList<FilaUsuarioArchivo> LeerUsuarios(Stream contenido, string nombreArchivo) =>
        Leer(contenido, nombreArchivo, ColumnasUsuario, (fila, celdas) => new FilaUsuarioArchivo(
            fila,
            celdas["nombreusuario"],
            celdas["password"],
            celdas["nombres"],
            celdas["apellidos"],
            celdas["identificacion"],
            celdas["correo"],
            celdas["telefono"],
            celdas["rol"]));

    public IReadOnlyList<FilaClienteArchivo> LeerClientes(Stream contenido, string nombreArchivo) =>
        Leer(contenido, nombreArchivo, ColumnasCliente, (fila, celdas) => new FilaClienteArchivo(
            fila,
            celdas["nombres"],
            celdas["apellidos"],
            celdas["identificacion"],
            celdas["correo"],
            celdas["telefono"],
            celdas["direccion"],
            celdas["referenciadireccion"]));

    public byte[] PlantillaUsuarios() => Plantilla(
        "Usuarios",
        ["NombreUsuario", "Password", "Nombres", "Apellidos", "Identificacion", "Correo", "Telefono", "Rol"],
        ["cajero890", "Cajero1234", "Andres", "Paredes", "0102030408", "andres.paredes@netdesk.com", "0991112233", "Cajero"]);

    public byte[] PlantillaClientes() => Plantilla(
        "Clientes",
        ["Nombres", "Apellidos", "Identificacion", "Correo", "Telefono", "Direccion", "ReferenciaDireccion"],
        ["Lucia", "Mendoza", "1712345678", "lucia.mendoza@correo.com", "0987001122", "Av. Amazonas N34-120 y Pereira, edificio central", "Junto a la farmacia de la esquina norte"]);

    private static List<T> Leer<T>(Stream contenido, string nombreArchivo, string[] columnas, Func<int, Dictionary<string, string>, T> mapear)
    {
        var extension = Path.GetExtension(nombreArchivo).ToLowerInvariant();
        var filas = extension switch
        {
            ".csv" => LeerCsv(contenido),
            ".xlsx" => LeerExcel(contenido),
            _ => throw new ReglaNegocioException("El archivo debe ser .xlsx o .csv.")
        };

        if (filas.Count == 0)
        {
            return [];
        }

        var encabezados = filas[0].Select((valor, indice) => new { Clave = Normalizar(valor), indice })
            .Where(x => !string.IsNullOrWhiteSpace(x.Clave))
            .GroupBy(x => x.Clave)
            .ToDictionary(g => g.Key, g => g.First().indice);

        var faltantes = columnas.Where(c => !encabezados.ContainsKey(c)).ToList();
        if (faltantes.Count > 0)
        {
            throw new ReglaNegocioException("Faltan columnas: " + string.Join(", ", faltantes));
        }

        var resultado = new List<T>();
        for (var i = 1; i < filas.Count; i++)
        {
            var celdas = columnas.ToDictionary(c => c, c => Valor(filas[i], encabezados[c]));
            if (celdas.Values.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            resultado.Add(mapear(i + 1, celdas));
        }

        return resultado;
    }

    private static List<List<string>> LeerExcel(Stream contenido)
    {
        using var libro = new XLWorkbook(contenido);
        var hoja = libro.Worksheets.First();
        var usadas = hoja.RangeUsed();
        if (usadas is null)
        {
            return [];
        }

        var filas = new List<List<string>>();
        foreach (var fila in usadas.Rows())
        {
            var celdas = new List<string>();
            for (var columna = 1; columna <= usadas.ColumnCount(); columna++)
            {
                celdas.Add(TextoCelda(fila.Cell(columna)));
            }

            filas.Add(celdas);
        }

        return filas;
    }

    private static string TextoCelda(IXLCell celda)
    {
        if (celda.IsEmpty())
        {
            return string.Empty;
        }

        if (celda.DataType == XLDataType.Number)
        {
            return celda.GetDouble().ToString("0", CultureInfo.InvariantCulture);
        }

        return celda.GetString().Trim();
    }

    private static List<List<string>> LeerCsv(Stream contenido)
    {
        using var lector = new StreamReader(contenido, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var texto = lector.ReadToEnd().TrimStart('\uFEFF');
        var filas = new List<List<string>>();
        var fila = new List<string>();
        var campo = new StringBuilder();
        var entreComillas = false;
        for (var i = 0; i < texto.Length; i++)
        {
            var caracter = texto[i];
            if (entreComillas)
            {
                if (caracter == '"')
                {
                    if (i + 1 < texto.Length && texto[i + 1] == '"')
                    {
                        campo.Append('"');
                        i++;
                    }
                    else
                    {
                        entreComillas = false;
                    }
                }
                else
                {
                    campo.Append(caracter);
                }

                continue;
            }

            switch (caracter)
            {
                case '"':
                    entreComillas = true;
                    break;
                case ',':
                    fila.Add(campo.ToString().Trim());
                    campo.Clear();
                    break;
                case '\n':
                    fila.Add(campo.ToString().Trim());
                    campo.Clear();
                    filas.Add(fila);
                    fila = [];
                    break;
                case '\r':
                    break;
                default:
                    campo.Append(caracter);
                    break;
            }
        }

        if (campo.Length > 0 || fila.Count > 0)
        {
            fila.Add(campo.ToString().Trim());
            filas.Add(fila);
        }

        return filas;
    }

    private static byte[] Plantilla(string nombre, string[] columnas, string[] ejemplo)
    {
        using var libro = new XLWorkbook();
        var hoja = libro.AddWorksheet(nombre);
        for (var i = 0; i < columnas.Length; i++)
        {
            hoja.Cell(1, i + 1).Value = columnas[i];
            hoja.Cell(2, i + 1).Value = ejemplo[i];
            hoja.Cell(1, i + 1).Style.Font.Bold = true;
        }

        hoja.Columns().AdjustToContents();
        using var memoria = new MemoryStream();
        libro.SaveAs(memoria);
        return memoria.ToArray();
    }

    private static string Normalizar(string valor) =>
        new string(valor.Trim().ToLowerInvariant().Where(c => c != ' ' && c != '_' ).ToArray());

    private static string Valor(List<string> fila, int indice) => indice < fila.Count ? fila[indice].Trim() : string.Empty;
}

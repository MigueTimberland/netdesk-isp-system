using FluentValidation;
using NetDesk.Application.Comun;

namespace NetDesk.Application.Servicios;

internal static class ValidacionExt
{
    public static async Task ValidarAsync<T>(this IValidator<T> validador, T modelo, CancellationToken ct)
    {
        var resultado = await validador.ValidateAsync(modelo, ct);
        if (!resultado.IsValid)
        {
            throw new ValidacionException(resultado);
        }
    }
}

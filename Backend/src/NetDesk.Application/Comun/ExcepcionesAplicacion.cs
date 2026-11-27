using FluentValidation.Results;

namespace NetDesk.Application.Comun;

public class ValidacionException : Exception
{
    public IReadOnlyDictionary<string, string[]> Errores { get; }

    public ValidacionException(ValidationResult resultado) : base("Revise los datos ingresados.")
    {
        Errores = resultado.Errors
            .GroupBy(e => string.IsNullOrWhiteSpace(e.PropertyName) ? "general" : e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray());
    }
}

public class NoEncontradoException : Exception
{
    public NoEncontradoException(string mensaje) : base(mensaje)
    {
    }
}

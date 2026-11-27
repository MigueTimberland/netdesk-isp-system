using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using NetDesk.Application.Abstracciones;
using NetDesk.Domain.Entities;

namespace NetDesk.Infrastructure.Seguridad;

public class CifradoService : ICifradoService
{
    private readonly byte[] _llave;

    public CifradoService(IConfiguration configuracion)
    {
        var valor = configuracion["Cifrado:Llave"] ?? throw new InvalidOperationException("Falta Cifrado:Llave.");
        _llave = SHA256.HashData(Encoding.UTF8.GetBytes(valor));
    }

    public string Cifrar(string textoPlano)
    {
        using var aes = Aes.Create();
        aes.Key = _llave;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.GenerateIV();
        using var cifrador = aes.CreateEncryptor();
        var plano = Encoding.UTF8.GetBytes(textoPlano);
        var cifrado = cifrador.TransformFinalBlock(plano, 0, plano.Length);
        var resultado = new byte[aes.IV.Length + cifrado.Length];
        Buffer.BlockCopy(aes.IV, 0, resultado, 0, aes.IV.Length);
        Buffer.BlockCopy(cifrado, 0, resultado, aes.IV.Length, cifrado.Length);
        return Convert.ToBase64String(resultado);
    }

    public string Descifrar(string textoCifrado)
    {
        var data = Convert.FromBase64String(textoCifrado);
        using var aes = Aes.Create();
        aes.Key = _llave;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        var iv = new byte[aes.BlockSize / 8];
        var cifrado = new byte[data.Length - iv.Length];
        Buffer.BlockCopy(data, 0, iv, 0, iv.Length);
        Buffer.BlockCopy(data, iv.Length, cifrado, 0, cifrado.Length);
        aes.IV = iv;
        using var descifrador = aes.CreateDecryptor();
        var plano = descifrador.TransformFinalBlock(cifrado, 0, cifrado.Length);
        return Encoding.UTF8.GetString(plano);
    }

    public string Indice(string textoPlano)
    {
        using var hmac = new HMACSHA256(_llave);
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(textoPlano.Trim().ToUpperInvariant())));
    }
}

public class PasswordService : IPasswordService
{
    private readonly PasswordHasher<Usuario> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(null!, password);

    public bool Verificar(string hash, string password) =>
        _hasher.VerifyHashedPassword(null!, hash, password) is not PasswordVerificationResult.Failed;
}

public class TokenJwtService : ITokenJwtService
{
    private readonly string _emisor;
    private readonly string _audiencia;
    private readonly int _minutos;
    private readonly SymmetricSecurityKey _firma;
    private readonly SymmetricSecurityKey _cifrado;

    public TokenJwtService(IConfiguration configuracion)
    {
        _emisor = configuracion["Jwt:Emisor"] ?? "NetDesk";
        _audiencia = configuracion["Jwt:Audiencia"] ?? "NetDesk.Client";
        _minutos = int.TryParse(configuracion["Jwt:ExpiraMinutos"], out var minutos) ? minutos : 480;
        _firma = new SymmetricSecurityKey(SHA256.HashData(Encoding.UTF8.GetBytes(configuracion["Jwt:LlaveFirma"] ?? "netdesk-firma-desarrollo")));
        _cifrado = new SymmetricSecurityKey(SHA256.HashData(Encoding.UTF8.GetBytes(configuracion["Jwt:LlaveCifrado"] ?? "netdesk-cifrado-desarrollo")));
    }

    public (string Token, DateTime ExpiraEn) Emitir(Usuario usuario)
    {
        var expira = DateTime.UtcNow.AddMinutes(_minutos);
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _emisor,
            Audience = _audiencia,
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = usuario.Id.ToString(),
                [JwtRegisteredClaimNames.UniqueName] = usuario.NombreUsuario,
                [ClaimTypes.Role] = usuario.Rol.Nombre,
                ["nombre"] = usuario.NombreCompleto
            },
            Expires = expira,
            IssuedAt = DateTime.UtcNow,
            SigningCredentials = new SigningCredentials(_firma, SecurityAlgorithms.HmacSha256),
            EncryptingCredentials = new EncryptingCredentials(_cifrado, SecurityAlgorithms.Aes256KW, SecurityAlgorithms.Aes128CbcHmacSha256)
        };

        var manejador = new Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler();
        return (manejador.CreateToken(descriptor), expira);
    }

    public TokenValidationParameters ParametrosValidacion() => new()
    {
        ValidateIssuer = true,
        ValidIssuer = _emisor,
        ValidateAudience = true,
        ValidAudience = _audiencia,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = _firma,
        TokenDecryptionKey = _cifrado,
        ClockSkew = TimeSpan.FromMinutes(1),
        NameClaimType = JwtRegisteredClaimNames.UniqueName,
        RoleClaimType = ClaimTypes.Role
    };
}

public class UsuarioActual(IHttpContextAccessor accessor) : IUsuarioActual
{
    public bool Autenticado => accessor.HttpContext?.User.Identity?.IsAuthenticated == true;

    public int Id
    {
        get
        {
            var valor = Claim("sub") ?? Claim(ClaimTypes.NameIdentifier);
            return int.TryParse(valor, out var id) ? id : throw new UnauthorizedAccessException("Sesión inválida.");
        }
    }

    public string NombreUsuario => Claim("unique_name") ?? Claim(ClaimTypes.Name) ?? string.Empty;

    public string Rol => Claim(ClaimTypes.Role) ?? Claim("role") ?? string.Empty;

    private string? Claim(string tipo) => accessor.HttpContext?.User.FindFirst(tipo)?.Value;
}

public class Reloj : IReloj
{
    private static readonly TimeZoneInfo Zona = Resolver();

    public DateTime UtcAhora => DateTime.UtcNow;

    public DateOnly HoyOperacion => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zona));

    public (DateTime InicioUtc, DateTime FinUtc) RangoDia(DateOnly fecha)
    {
        var inicio = DateTime.SpecifyKind(fecha.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        var fin = DateTime.SpecifyKind(fecha.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        return (TimeZoneInfo.ConvertTimeToUtc(inicio, Zona), TimeZoneInfo.ConvertTimeToUtc(fin, Zona));
    }

    private static TimeZoneInfo Resolver()
    {
        foreach (var id in new[] { "America/Guayaquil", "SA Pacific Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.CreateCustomTimeZone("EC", TimeSpan.FromHours(-5), "Ecuador", "Ecuador");
    }
}

public class OpcionesAplicacion(IConfiguration configuracion) : IOpcionesAplicacion
{
    public bool ExponerCodigoRecuperacion => configuracion.GetValue("Recuperacion:ExponerCodigo", true);
}

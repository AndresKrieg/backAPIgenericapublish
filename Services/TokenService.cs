using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace csharpapigenerica.Services
{
    public class TokenService
    {
        private readonly IConfiguration _configuracion;

        public TokenService(IConfiguration configuracion)
        {
            _configuracion = configuracion ?? throw new ArgumentNullException(nameof(configuracion));
        }

        public string GenerarToken(string usuario, List<string> roles)
        {
            var claveJwt = _configuracion["Jwt:Key"]
                ?? throw new InvalidOperationException("La clave JWT no está configurada correctamente.");

            var claveSecreta = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(claveJwt));
            var credenciales = new SigningCredentials(claveSecreta, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, usuario),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("nombreUsuario", usuario) // (opcional) otro claim personalizado
            };

            // Agregar cada rol como un claim
            foreach (var rol in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, rol));
            }

            var tokenDescriptor = new JwtSecurityToken(
                issuer: _configuracion["Jwt:Issuer"],
                audience: _configuracion["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: credenciales
            );

            return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
        }
    }
}

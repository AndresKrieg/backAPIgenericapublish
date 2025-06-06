using Microsoft.AspNetCore.Mvc;
using csharpapigenerica.Services;
using System.Data.SqlClient;
using Microsoft.OpenApi.Models;

namespace csharpapigenerica.Controllers
{
    [ApiController]
    [Route("api/pry/usuario")]
    public class LoginController : ControllerBase
    {
        private readonly TokenService _tokenService;
        private readonly string connectionString = "Server=(localdb)\\MSSQLLocalDB;Database=bd_indicadores_1330_SQLSERVER;Integrated Security=True";

        public LoginController(TokenService tokenService)
        {
            _tokenService = tokenService;
        }

        public class LoginRequest
        {
            public string Email { get; set; }
            public string Contrasena { get; set; }
        }
 
[HttpPost("verificar-contrasena")]
public IActionResult VerificarContrasena([FromBody] LoginRequest request)
{
    if (request == null || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Contrasena))
    {
        return BadRequest(new { mensaje = "Email y contraseña son requeridos." });
    }

    List<string> rolesUsuario = new List<string>();
    bool usuarioValido = false;

    using (var connection = new SqlConnection(connectionString))
    {
        connection.Open();

        string obtenerHashSql = "SELECT Contrasena FROM Usuario WHERE Email = @Email";
        string hashAlmacenado = null;

        using (var cmd = new SqlCommand(obtenerHashSql, connection))
        {
            cmd.Parameters.AddWithValue("@Email", request.Email);
            hashAlmacenado = cmd.ExecuteScalar() as string;
        }

        if (hashAlmacenado != null)
        {
            if (BCrypt.Net.BCrypt.Verify(request.Contrasena, hashAlmacenado))
            {
                usuarioValido = true;
            }
        }

        if (usuarioValido)
        {
            string rolesSql = @"
                SELECT r.Nombre
                FROM rol_usuario ru
                INNER JOIN rol r ON ru.fkidrol = r.id
                WHERE ru.fkemail = @Email";

            using (var rolesCmd = new SqlCommand(rolesSql, connection))
            {
                rolesCmd.Parameters.AddWithValue("@Email", request.Email);
                using (var reader = rolesCmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rolesUsuario.Add(reader.GetString(0));
                    }
                }
            }
        }
    }

    if (usuarioValido)
    {
        var token = _tokenService.GenerarToken(request.Email, rolesUsuario);

        return Ok(new
        {
            mensaje = "Usuario autenticado correctamente",
            token = token,
            roles = rolesUsuario
        });
    }
    else
    {
        return Unauthorized(new { mensaje = "Credenciales incorrectas." });
    }
}


    }
}

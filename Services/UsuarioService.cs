namespace csharpapigenerica.Services
{
    public class UsuarioService : IUsuarioService
    {
        public bool ValidarCredenciales(string email, string contrasena)
        {
            return email == "admin@empresa.com" && contrasena == "1234567";
        }
    }
}
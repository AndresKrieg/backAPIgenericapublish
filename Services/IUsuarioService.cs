namespace csharpapigenerica.Services
{
    public interface IUsuarioService
    {
        bool ValidarCredenciales(string email, string contrasena);
    }
}
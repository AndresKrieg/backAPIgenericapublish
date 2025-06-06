#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace csharpapigenerica.Services
{
    public class ControlConexion
    {
        private readonly IWebHostEnvironment _entorno;
        private readonly IConfiguration _configuracion;
        private IDbConnection? _conexionBd;

        public ControlConexion(IWebHostEnvironment entorno, IConfiguration configuracion)
        {
            _entorno = entorno ?? throw new ArgumentNullException(nameof(entorno));
            _configuracion = configuracion ?? throw new ArgumentNullException(nameof(configuracion));
            _conexionBd = null;
        }

        public void AbrirBd()
        {
            try
            {
                string proveedor = _configuracion["DatabaseProvider"] 
                    ?? throw new InvalidOperationException("Proveedor de base de datos no configurado.");

                string? cadenaConexion = _configuracion.GetConnectionString(proveedor);

                if (string.IsNullOrEmpty(cadenaConexion))
                    throw new InvalidOperationException("La cadena de conexión es nula o vacía.");

                Console.WriteLine($"Intentando abrir conexión con el proveedor: {proveedor}");
                Console.WriteLine($"Cadena de conexión: {cadenaConexion}");

                switch (proveedor)
                {
                    case "LocalDb":
                        // Para LocalDb con base de datos nombrada
                        _conexionBd = new SqlConnection(cadenaConexion);
                        break;
                    case "SqlServer":
                        _conexionBd = new SqlConnection(cadenaConexion);
                        break;
                    default:
                        throw new InvalidOperationException("Proveedor de base de datos no soportado. Solo se admiten LocalDb y SqlServer.");
                }

                _conexionBd.Open();
                Console.WriteLine("Conexión a la base de datos abierta exitosamente.");
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"Ocurrió una SqlException: {ex.Message}");
                Console.WriteLine($"Número de Error: {ex.Number}");
                Console.WriteLine($"Estado de Error: {ex.State}");
                Console.WriteLine($"Clase de Error: {ex.Class}");
                throw new InvalidOperationException("Error al abrir la conexión a la base de datos debido a un error SQL.", ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ocurrió una excepción: {ex.Message}");
                throw new InvalidOperationException("Error al abrir la conexión a la base de datos.", ex);
            }
        }

        public void AbrirBdLocalDB(string archivoBd)
        {
            try
            {
                string nombreArchivoBd = archivoBd.EndsWith(".mdf") ? archivoBd : archivoBd + ".mdf";
                string rutaAppData = Path.Combine(_entorno.ContentRootPath, "App_Data");
                string rutaArchivo = Path.Combine(rutaAppData, nombreArchivoBd);
                string cadenaConexion = $@"Data Source=(localdb)\MSSQLLocalDB;AttachDbFilename={rutaArchivo};Integrated Security=True;Connect Timeout=30;";

                _conexionBd = new SqlConnection(cadenaConexion);
                _conexionBd.Open();
                Console.WriteLine("Conexión LocalDB con archivo .mdf abierta exitosamente.");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error al abrir la conexión a LocalDB.", ex);
            }
        }

        public void CerrarBd()
        {
            try
            {
                if (_conexionBd != null && _conexionBd.State == ConnectionState.Open)
                {
                    _conexionBd.Close();
                    Console.WriteLine("Conexión cerrada correctamente.");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error al cerrar la conexión a la base de datos.", ex);
            }
        }

        public int EjecutarComandoSql(string consultaSql, DbParameter[] parametros)
        {
            try
            {
                if (_conexionBd == null || _conexionBd.State != ConnectionState.Open)
                    throw new InvalidOperationException("La conexión a la base de datos no está abierta.");

                using (var comando = _conexionBd.CreateCommand())
                {
                    comando.CommandText = consultaSql;
                    foreach (var parametro in parametros)
                    {
                        Console.WriteLine($"Agregando parámetro: {parametro.ParameterName} = {parametro.Value}, DbType: {parametro.DbType}");
                        comando.Parameters.Add(parametro);
                    }
                    int filasAfectadas = comando.ExecuteNonQuery();
                    return filasAfectadas;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ocurrió una excepción: {ex.Message}");
                throw new InvalidOperationException("Error al ejecutar el comando SQL.", ex);
            }
        }

        public DataTable EjecutarConsultaSql(string consultaSql, DbParameter[]? parametros)
        {
            if (_conexionBd == null || _conexionBd.State != ConnectionState.Open)
                throw new InvalidOperationException("La conexión a la base de datos no está abierta.");

            try
            {
                using (var comando = _conexionBd.CreateCommand())
                {
                    comando.CommandText = consultaSql;
                    if (parametros != null)
                    {
                        foreach (var param in parametros)
                        {
                            Console.WriteLine($"Agregando parámetro: {param.ParameterName} = {param.Value}, DbType: {param.DbType}");
                            comando.Parameters.Add(param);
                        }
                    }

                    var resultado = new DataSet();
                    var adaptador = new SqlDataAdapter((SqlCommand)comando);

                    Console.WriteLine($"Ejecutando comando: {comando.CommandText}");
                    adaptador.Fill(resultado);
                    Console.WriteLine("DataSet lleno");

                    if (resultado.Tables.Count == 0)
                    {
                        Console.WriteLine("No se devolvieron tablas en el DataSet");
                        throw new Exception("No se devolvieron tablas en el DataSet");
                    }

                    Console.WriteLine($"Número de tablas en el DataSet: {resultado.Tables.Count}");
                    Console.WriteLine($"Número de filas en la primera tabla: {resultado.Tables[0].Rows.Count}");

                    return resultado.Tables[0];
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ocurrió una excepción: {ex.Message}");
                throw new Exception($"Error al ejecutar la consulta SQL. Error: {ex.Message}", ex);
            }
        }

        public DbParameter CrearParametro(string nombre, object? valor)
        {
            try
            {
                string proveedor = _configuracion["DatabaseProvider"]
                    ?? throw new InvalidOperationException("Proveedor de base de datos no configurado.");

                return proveedor switch
                {
                    "SqlServer" => new SqlParameter(nombre, valor ?? DBNull.Value),
                    "LocalDb" => new SqlParameter(nombre, valor ?? DBNull.Value),
                    _ => throw new InvalidOperationException("Proveedor de base de datos no soportado. Solo se admiten LocalDb y SqlServer."),
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error al crear el parámetro.", ex);
            }
        }

        public IDbConnection? ObtenerConexion()
        {
            return _conexionBd;
        }

        public DataTable EjecutarProcedimientoAlmacenado(string nombreProcedimiento, DbParameter[]? parametros)
        {
            if (_conexionBd == null || _conexionBd.State != ConnectionState.Open)
                throw new InvalidOperationException("La conexión no está abierta.");

            try
            {
                using (var comando = _conexionBd.CreateCommand())
                {
                    comando.CommandText = nombreProcedimiento;
                    comando.CommandType = CommandType.StoredProcedure;

                    if (parametros != null)
                    {
                        foreach (var param in parametros)
                        {
                            comando.Parameters.Add(param);
                        }
                    }

                    var resultado = new DataSet();
                    var adaptador = new SqlDataAdapter((SqlCommand)comando);
                    adaptador.Fill(resultado);

                    return resultado.Tables[0];
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al ejecutar el procedimiento almacenado: {ex.Message}", ex);
            }
        }

        public object? EjecutarFuncion(string nombreFuncion, DbParameter[]? parametros)
        {
            if (_conexionBd == null || _conexionBd.State != ConnectionState.Open)
                throw new InvalidOperationException("La conexión no está abierta.");

            try
            {
                using (var comando = _conexionBd.CreateCommand())
                {
                    comando.CommandText = nombreFuncion;
                    comando.CommandType = CommandType.Text;

                    if (parametros != null)
                    {
                        foreach (var param in parametros)
                        {
                            comando.Parameters.Add(param);
                        }
                    }

                    return comando.ExecuteScalar();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al ejecutar la función SQL: {ex.Message}", ex);
            }
        }
    }
}

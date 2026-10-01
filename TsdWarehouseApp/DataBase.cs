using Microsoft.Data.SqlClient;

namespace TsdWarehouseApp
{
    internal class DataBase
    {
        private readonly string connectionString = @"Data Source=192.168.3.5; Initial Catalog=TsdDb; Integrated Security=false; Encrypt=False; User ID=app_admin; Password=123123";



        // Возвращает новый экземпляр SqlConnection с гарантированно заполненной строкой подключения
        public SqlConnection getConnection()
        {
            return new SqlConnection(connectionString);
        }

        // Вспомогательный метод для быстрого открытия соединения
        public async Task<SqlConnection> OpenConnectionAsync()
        {
            var connection = new SqlConnection(connectionString);
            if (connection.State == System.Data.ConnectionState.Closed)
            {
                await connection.OpenAsync();
            }
            return connection;
        }
    }
}
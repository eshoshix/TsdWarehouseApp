using Microsoft.Data.SqlClient;

namespace TsdWarehouseApp
{
    internal class DataBase
    {
        private readonly string connectionString = @"Data Source=188.120.228.126; Initial Catalog=shift_log; Integrated Security=false; Encrypt=False; User ID=sa; Password=Ik21998123";



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
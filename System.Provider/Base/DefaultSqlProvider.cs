using System;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Configuration;
using System.Reflection;
using System.Linq;

namespace App.Provider
{
    public class DefaultSqlProvider
    {
        private readonly string _connectionString = "Server=your_server_address;Database=your_database_name;User Id=your_username;Password=your_password;";


        #region ProviderBase
        private List<T> ExecuteQuery<T>(string query, List<SqlParameter>? parameters = null) where T : new()
        {
            List<T> resultList = new List<T>();

            using SqlConnection connection = new SqlConnection(_connectionString);
            using SqlCommand command = new SqlCommand(query, connection);
            if (parameters != null)
            {
                command.Parameters.AddRange(parameters.ToArray());
            }
            try
            {
                connection.Open();
                SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    T item = new T();
                    MapDataReaderToModel(reader, item);
                    resultList.Add(item);
                }

                return resultList;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro ao executar a consulta: " + ex.Message);
                if (ex.InnerException != null)
                {
                    Console.WriteLine("Exceção interna: " + ex.InnerException.Message);
                }
                return new List<T>();
            }
        }

        private static void MapDataReaderToModel<T>(SqlDataReader reader, T item)
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var propertyName = reader.GetName(i);
                PropertyInfo property = typeof(T).GetProperty(propertyName);
                if (property != null && !reader.IsDBNull(i))
                {
                    object value = reader.GetValue(i);
                    property.SetValue(item, value);
                }
            }
        }

        private int ExecuteNonQuery(string query, List<SqlParameter> parameters)
        {
            try
            {
                using SqlConnection connection = new SqlConnection(_connectionString);
                using SqlCommand command = new SqlCommand(query, connection);
                if (parameters != null)
                {
                    command.Parameters.AddRange(parameters.ToArray());
                }

                connection.Open();
                return command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao executar o comando SQL: " + ex.Message);
            }
        }

        #endregion

        #region Default
        public int Insert<T>(T item)
        {
            var type = typeof(T);
            var tableName = type.Name;
            var properties = type.GetProperties();
            var columns = string.Join(", ", properties.Select(p => p.Name));
            var values = string.Join(", ", properties.Select(p => $"@{p.Name}"));

            var query = $"INSERT INTO {tableName} ({columns}) VALUES ({values})";

            var parameters = properties.Select(p => new SqlParameter($"@{p.Name}", p.GetValue(item) ?? DBNull.Value)).ToList();
            return ExecuteNonQuery(query, parameters);
        }

        public int Update<T>(T item, string keyProperty)
        {
            var type = typeof(T);
            var tableName = type.Name;
            var properties = type.GetProperties();
            var key = properties.FirstOrDefault(p => p.Name == keyProperty);
            if (key == null) throw new ArgumentException("Key property not found");

            var setClause = string.Join(", ", properties.Where(p => p.Name != keyProperty).Select(p => $"{p.Name} = @{p.Name}"));
            var query = $"UPDATE {tableName} SET {setClause} WHERE {keyProperty} = @{keyProperty}";

            var parameters = properties.Select(p => new SqlParameter($"@{p.Name}", p.GetValue(item) ?? DBNull.Value)).ToList();
            return ExecuteNonQuery(query, parameters);
        }

        public int Delete<T>(object key, string keyProperty)
        {
            var type = typeof(T);
            var tableName = type.Name;
            var query = $"DELETE FROM {tableName} WHERE {keyProperty} = @{keyProperty}";

            var parameter = new SqlParameter($"@{keyProperty}", key);
            return ExecuteNonQuery(query, new List<SqlParameter> { parameter });
        }

        public T? Get<T>(object key, string keyProperty) where T : new()
        {
            var type = typeof(T);
            var tableName = type.Name;
            var query = $"SELECT * FROM {tableName} WHERE {keyProperty} = @{keyProperty}";

            var parameter = new SqlParameter($"@{keyProperty}", key);
            var result = ExecuteQuery<T>(query, new List<SqlParameter> { parameter });
            return result.FirstOrDefault();
        }

        public List<T> GetAll<T>() where T : new()
        {
            var type = typeof(T);
            var tableName = type.Name;
            var query = $"SELECT * FROM {tableName}";
            return ExecuteQuery<T>(query);
        }
        #endregion

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Runtime.ConstrainedExecution;
using System.Diagnostics;

namespace ADO.NET_Demo
{
    internal class UserPassAuth
    {
        private readonly SqlConnection conn = new("Data Source=.\\SQLEXPRESS;Integrated Security=True");

        public string TableName = "UserPass";

        public void CloseConnection()
        {
            conn.Close();
        }

        private void EnsureOpen()
        {
            if (conn.State == System.Data.ConnectionState.Closed || conn.State == System.Data.ConnectionState.Broken)
                conn.Open();
        }

        public bool TableExists()
        {
            bool found = false;
            ExecuteSqlLambda(command =>
            {
                command.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @tbl";
                command.Parameters.Add("@tbl", System.Data.SqlDbType.VarChar);
                command.Parameters["@tbl"].Value = TableName;
                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read() && !found)
                {
                    found = reader.GetInt32(0) > 0;
                }
                reader.Close();
            });
            return found;
        }

        private (SqlCommand, SqlTransaction) CreateCommandAndTransaction()
        {
            EnsureOpen();
            SqlCommand command = conn.CreateCommand();
            SqlTransaction transact = conn.BeginTransaction();

            command.Connection = conn;
            command.Transaction = transact;

            return (command, transact);
        }

        public void ExecuteSqlLambda(Action<SqlCommand> action)
        {
            (SqlCommand command, SqlTransaction transaction) = CreateCommandAndTransaction();

            try
            {
                action(command);
                transaction.Commit();
            }
            catch (SqlException e)
            {
                Console.WriteLine(e.Message);
                Console.Error.WriteLine(e.Message);
                Debug.WriteLine(e.Message);
                transaction.Rollback();
            }
        }
        
        public void CreateTables()
        {
            ExecuteSqlLambda(command =>
            {
                command.CommandText = "CREATE TABLE " + TableName + " (" +
                    "username VARCHAR(256) NOT NULL PRIMARY KEY," +
                    "hash_salt CHAR(32) NOT NULL," +
                    "hash_pass CHAR(64) NOT NULL" +
                    ");";
                command.ExecuteNonQuery();
            });
        }

        public void DropTables()
        {
            ExecuteSqlLambda(command =>
            {
                command.CommandText = "DROP TABLE " + TableName;
                command.ExecuteNonQuery();
            });
        }

        private static (string, string) GenerateHashAndSalt(string password)
        {
            string salt = RandomNumberGenerator.GetHexString(32);
            string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(password + salt)));
            return (hash, salt);
        }

        private static bool CheckPassword(string password, string hash, string salt)
        {
            string ours = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(password + salt)));
            return hash.Equals(ours);
        }

        public bool CreateUser(string username, string password)
        {
            bool success = false;
            (string hash, string salt) = GenerateHashAndSalt(password);
            ExecuteSqlLambda(command =>
            {
                command.CommandText = "INSERT INTO " + TableName + "(username, hash_salt, hash_pass) VALUES (@username, @hash_salt, @hash_pass)";
                command.Parameters.Add("@username", System.Data.SqlDbType.VarChar);
                command.Parameters["@username"].Value = username;
                command.Parameters.AddWithValue("@hash_salt", salt);
                command.Parameters.AddWithValue("@hash_pass", hash);
                success = command.ExecuteNonQuery() > 0;
            });
            return success;
        }

        public bool ChangeUserPassword(string username, string password)
        {
            bool success = false;
            (string hash, string salt) = GenerateHashAndSalt(password);
            ExecuteSqlLambda(command =>
            {
                command.CommandText = "UPDATE " + TableName + " SET hash_salt = @hash_salt, hash_pass = @hash_pass WHERE username = @username";
                command.Parameters.Add("@username", System.Data.SqlDbType.VarChar);
                command.Parameters["@username"].Value = username;
                command.Parameters.AddWithValue("@hash_salt", salt);
                command.Parameters.AddWithValue("@hash_pass", hash);
                success = command.ExecuteNonQuery() > 0;
            });
            return success;
        }

        public bool TryLoginUser(string username, string password)
        {
            bool found = false;
            ExecuteSqlLambda(command =>
            {
                command.CommandText = "SELECT hash_salt, hash_pass FROM " + TableName + " WHERE username = @username";
                command.Parameters.Add("@username", System.Data.SqlDbType.VarChar);
                command.Parameters["@username"].Value = username;
                SqlDataReader reader = command.ExecuteReader();
                while(reader.Read() && !found)
                {
                    string salt = reader.GetString(0);
                    string hash = reader.GetString(1);
                    found = found || CheckPassword(password, hash, salt);
                }
                reader.Close();
            });
            return found;
        }

        public bool DeleteUser(string username)
        {
            bool success = false;
            ExecuteSqlLambda(command =>
            {
                command.CommandText = "DELETE FROM " + TableName + " WHERE username = @username";
                command.Parameters.Add("@username", System.Data.SqlDbType.VarChar);
                command.Parameters["@username"].Value = username;
                success = command.ExecuteNonQuery() > 0;
            });
            return success;
        }
    }
}

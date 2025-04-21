using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server_MultipleClientsChatTest
{
    internal static class DatabaseTools
    {
        public static bool Connect()
        {
            string connectionString = $"server={DatabaseConfiguration.Address};database={DatabaseConfiguration.DBName};user={DatabaseConfiguration.DBUsername};password={DatabaseConfiguration.DBPassword}";

            // Create a MySqlConnection object
            using (var connection = new MySql.Data.MySqlClient.MySqlConnection(connectionString))
            {
                try
                {
                    // Open the connection
                    connection.Open();
                    Server.logger.LogInformation("Connection to the database established successfully.");
                    return true;
                }
                catch (MySql.Data.MySqlClient.MySqlException ex)
                {
                    Server.logger.LogError($"Error: {ex.Message}");
                    return false;
                }
            }
        }

        public static bool RunQuery(string query)
        {
            string connectionString = $"server={DatabaseConfiguration.Address};database={DatabaseConfiguration.DBName};user={DatabaseConfiguration.DBUsername};password={DatabaseConfiguration.DBPassword}";
            MySqlConnection connection = new MySqlConnection(connectionString);

            MySqlCommand command = new MySqlCommand(query, connection);
            command.CommandTimeout = 60;

            try
            {
                connection.Open();

                MySqlDataReader reader = command.ExecuteReader();

                Server.logger.LogInformation("Query executed!");

                return true;

            }
            catch (Exception ex)
            {
                Server.logger.LogError("Error: " + ex.Message);

                return false;
            }
        }

    }
}

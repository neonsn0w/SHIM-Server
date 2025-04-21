using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server_MultipleClientsChatTest
{
    internal static class DatabaseConfiguration
    {
        private static string address, dbName, dbUsername, dbPassword;
        public static string Address { get => address; set => address = value; }
        public static string DBName { get => dbName; set => dbName = value; }
        public static string DBUsername { get => dbUsername; set => dbUsername = value; }
        public static string DBPassword { get => dbPassword; set => dbPassword = value; }
    }
}
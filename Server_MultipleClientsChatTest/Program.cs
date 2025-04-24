using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

using Microsoft.Extensions.Logging;

namespace Server_MultipleClientsChatTest
{
    class Server
    {
        // the ConcurrentDictionary class is thread-safe
        private static ConcurrentDictionary<int, UserClient> clients = new ConcurrentDictionary<int, UserClient>();
        private static int clientIdCounter = 0;

        public static ILogger logger;

        private static void Main(string[] args)
        {
            // initializing the logger
            using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddConsole());
            logger = factory.CreateLogger("SHIM-Server");

            setConfiguration();
            
            if (DatabaseTools.Connect())
            {
                logger.LogInformation("Connected to the database!");
                ExecuteServer();
            }
            else
            {
                logger.LogError("Failed to connect to the database!");
            }
        }

        public static void setConfiguration()
        {
            DatabaseConfiguration.Address = "127.0.0.1";
            DatabaseConfiguration.DBName = "shim";
            DatabaseConfiguration.DBUsername = "root";
            DatabaseConfiguration.DBPassword = "pietpras";
        }

        private static int searchUserByPublicKey(string publickey)
        {
            foreach (var client in clients)
            {
                if (client.Value.PublicKey == publickey.Trim())
                {
                    logger.LogInformation($"User found: Nickname: {client.Value.Nickname}, PublicKey: {client.Value.PublicKey}, RemoteEndPoint: {client.Value.Socket.RemoteEndPoint}");
                    return client.Key;
                }
            }
            logger.LogWarning("User with the specified public key not found.");
            return -1;
        }

        private static void ExecuteServer()
        {
            logger.LogInformation("Server starting...");

            IPHostEntry ipHost = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddr = ipHost.AddressList[0];
            IPEndPoint localEndPoint = new IPEndPoint(ipAddr, 11111);

            Socket listener = new Socket(ipAddr.AddressFamily,
                        SocketType.Stream, ProtocolType.Tcp);

            try
            {
                listener.Bind(localEndPoint);

                listener.Listen();

                while (true)
                {

                    logger.LogInformation("Waiting connection ... ");

                    Socket clientSocket = listener.Accept(); // Suspends execution

                    int clientID = clientIdCounter++;   
                    clients.TryAdd(clientID, new UserClient(clientSocket));

                    logger.LogInformation("Accepted connection from " + clientSocket.RemoteEndPoint);

                    Thread clientThread = new Thread(() => HandleClient(clientID, new UserClient(clientSocket)));
                    clientThread.Start();

                }
            }

            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void HandleClient(int clientID, UserClient userClient)
        {
            try
            {
                while (true)
                {
                    // Data buffer
                    byte[] bytes = new Byte[65536];
                    string data = null;

                    int numByte = userClient.Socket.Receive(bytes);

                    // Console.WriteLine(numByte);

                    data += Encoding.UTF8.GetString(bytes,
                                                0, numByte);

                    logger.LogInformation("Text received -> {0} ", data);

                    byte[] message = new Byte[65536];

                    data = data.Remove(data.Length);

                    logger.LogInformation("Message: {0}", data);

                    switch (data.ToLower())
                    {
                        case "ping":
                            message = Encoding.UTF8.GetBytes("Pong");
                            userClient.Socket.Send(message);
                            break;

                        case "exit":
                            message = Encoding.UTF8.GetBytes("Goodbye");
                            clients.TryRemove(clientID, out _);
                            // clientSocket.Send(message);
                            break;

                        case "list":
                            message = Encoding.UTF8.GetBytes("List of clients: ");
                            userClient.Socket.Send(message);
                            foreach (var client in clients)
                            {
                                message = Encoding.UTF8.GetBytes("Client " + client.Key + ": " + client.Value.Socket.RemoteEndPoint
                                    + "\n" + "pubkey: " + client.Value.PublicKey + "\n" + "nickname: " + client.Value.Nickname);
                                userClient.Socket.Send(message);
                            }
                            break;

                        case "updatedb":
                            if (!DatabaseTools.RunQuery($"INSERT INTO shim.users(public_key, username) VALUES ('{userClient.PublicKey}', '{userClient.Nickname}')"))
                            {
                                DatabaseTools.RunQuery($"UPDATE shim.users SET username = '{userClient.Nickname}' WHERE public_key = '{userClient.PublicKey}'");
                            }
                            message = Encoding.UTF8.GetBytes("OK");
                            userClient.Socket.Send(message);
                            break;

                        case "getusers":
                            StringBuilder userListBuilder = new StringBuilder();
                            userListBuilder.Append("§§§USERLIST§§§");
                            foreach (var client in clients)
                            {
                                // Format: Nickname§PublicKey
                                userListBuilder.AppendLine($"{client.Value.Nickname}§{client.Value.PublicKey}");
                            }
                            message = Encoding.UTF8.GetBytes(userListBuilder.ToString());
                            userClient.Socket.Send(message);
                            break;

                        default:
                            if (data.StartsWith("msg ") && data.Contains(" "))
                            {
                                string[] splitData = data.Split(" ");
                                int receiverId = int.Parse(splitData[1]);
                                string messageToSend = splitData[2];
                                message = Encoding.UTF8.GetBytes("Client " + clientID + " says: " + messageToSend);
                                clients[receiverId].Socket.Send(message);
                            }
                            else if (data.StartsWith("broadcast ") && data.Contains(" "))
                            {
                                BroadcastMessage(clientID, $"brd {data.Substring(10)}§{clients[clientID].Nickname}");
                            }
                            else if (data.StartsWith("setpubkey ") && data.Contains(" "))
                            {
                                string[] splitData = data.Split(" ");
                                userClient.PublicKey = splitData[1];
                                clients[clientID] = userClient;
                                message = Encoding.UTF8.GetBytes("OK");
                                userClient.Socket.Send(message);
                            }
                            else if (data.StartsWith("setnick ") && data.Contains(" "))
                            {
                                userClient.Nickname = data.Substring(8);
                                clients[clientID] = userClient;
                                message = Encoding.UTF8.GetBytes("OK");
                                userClient.Socket.Send(message);
                            }
                            else if (data.StartsWith("dm ") && data.Contains("§"))
                            {
                                data = data.Substring(3);
                                string[] splitData = data.Split("§");
                                int receiverId = searchUserByPublicKey(splitData[0]);
                                if (receiverId != -1)
                                {
                                    message = Encoding.UTF8.GetBytes($"md {clients[clientID].PublicKey}§{splitData[1]}§{clients[clientID].Nickname}");
                                    clients[receiverId].Socket.Send(message);
                                }
                                else
                                {
                                    message = Encoding.UTF8.GetBytes("errusrnotfound");
                                    userClient.Socket.Send(message);
                                }
                            }
                            else
                            {
                                message = Encoding.UTF8.GetBytes("Invalid command");
                                userClient.Socket.Send(message);
                            }
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                logger.LogWarning(e.ToString());
                logger.LogWarning("REMOVING THE SOCKET FROM THE INTERNAL DICTIONARY IMMEDIATELY!");
                clients.TryRemove(clientID, out _);
            }
            finally
            {
                userClient.Socket.Shutdown(SocketShutdown.Both);
                userClient.Socket.Close();
            }
        }

        private static void BroadcastMessage(int senderId, string message)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(message);

            foreach (var client in clients)
            {
                if (client.Key != senderId)
                {
                    client.Value.Socket.Send(buffer); // .Value is used because it's in a Dictionary!!!
                }
            }
        }
    }
}

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

        private static ILogger logger;

        private static void Main(string[] args)
        {
            // initializing the logger
            using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddConsole());
            logger = factory.CreateLogger("SHIM-Server");
            
            ExecuteServer();
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

                    Thread clientThread = new Thread(() => HandleClient(clientID, clientSocket));
                    clientThread.Start();

                }
            }

            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void HandleClient(int clientID, Socket clientSocket)
        {
            try
            {
                while (true)
                {
                    // Data buffer
                    byte[] bytes = new Byte[1024];
                    string data = null;

                    int numByte = clientSocket.Receive(bytes);

                    // Console.WriteLine(numByte);

                    data += Encoding.UTF8.GetString(bytes,
                                                0, numByte);

                    logger.LogInformation("Text received -> {0} ", data);

                    byte[] message;

                    data = data.Remove(data.Length - 5);

                    logger.LogInformation("Message: {0}", data);

                    switch (data.ToLower())
                    {
                        case "ping":
                            message = Encoding.UTF8.GetBytes("Pong");
                            clientSocket.Send(message);
                            break;

                        case "exit":
                            message = Encoding.UTF8.GetBytes("Goodbye");
                            clients.TryRemove(clientID, out _);
                            // clientSocket.Send(message);
                            break;

                        case "list":
                            message = Encoding.UTF8.GetBytes("List of clients: ");
                            clientSocket.Send(message);
                            foreach (var client in clients)
                            {
                                message = Encoding.UTF8.GetBytes("Client " + client.Key + ": " + client.Value.Socket.RemoteEndPoint + "\n");
                                clientSocket.Send(message);
                            }
                            break;

                        default:
                            if (data.StartsWith("msg") && data.Contains(" "))
                            {
                                string[] splitData = data.Split(" ");
                                int receiverId = int.Parse(splitData[1]);
                                string messageToSend = splitData[2];
                                message = Encoding.UTF8.GetBytes("Client " + clientID + " says: " + messageToSend);
                                clients[receiverId].Socket.Send(message);
                            }
                            else if (data.StartsWith("broadcast") && data.Contains(" "))
                            {
                                string[] splitData = data.Split(" ");
                                BroadcastMessage(clientID, splitData[1]);
                            }
                            else if (data.StartsWith("register") && data.Contains(" "))
                            {
                                string[] splitData = data.Split(" ");
                                BroadcastMessage(clientID, splitData[1]);
                            }
                            else
                            {
                                message = Encoding.UTF8.GetBytes("Invalid command");
                                clientSocket.Send(message);
                            }
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                logger.LogError(e.ToString());
                logger.LogWarning("REMOVING THE SOCKET FROM THE INTERNAL DICTIONARY IMMEDIATELY!");
                clients.TryRemove(clientID, out _);
            }
            finally
            {
                clientSocket.Shutdown(SocketShutdown.Both);
                clientSocket.Close();
            }
        }

        private static void BroadcastMessage(int senderId, string message)
        {
            byte[] buffer = Encoding.ASCII.GetBytes("Client " + senderId + ": " + message);

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

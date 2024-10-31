using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
namespace ChatServerApplication
{
    class UserSession
    {
        protected internal string UniqueId { get; } = Guid.NewGuid().ToString();
        private readonly Thread _sessionThread;
        private Socket _socketChannel;
        private string _username;
        private ChatServer _serverInstance;
        public UserSession(string username, Socket socketChannel, ChatServer serverInstance)
        {
            _sessionThread = new Thread(ProcessUserInput);
            _socketChannel = socketChannel;
            _username = username;
            _serverInstance = serverInstance;
            _sessionThread.Start();
        }
        public Socket GetSocketChannel()
        {
            return _socketChannel;
        }
        private void ProcessUserInput()
        {
            try
            {
                var welcomeMessage = $"{_username} has joined the chat.";
                _serverInstance.BroadcastMessageToAllExcept(Encoding.Unicode.GetBytes(welcomeMessage), UniqueId);
                Console.WriteLine(welcomeMessage);
                while (true)
                {
                    var receivedData = new StringBuilder();
                    int bytesRead = 0;
                    byte[] buffer = new byte[256];
                    do
                    {
                        bytesRead = _socketChannel.Receive(buffer);
                        receivedData.Append(Encoding.Unicode.GetString(buffer, 0, bytesRead));
                    } while (_socketChannel.Available > 0);
                    if (receivedData.ToString() == "12345")
                    {
                        var departureMessage = $"{_username} has left the chat.";
                        Console.WriteLine(departureMessage);
                        _serverInstance.BroadcastMessageToAllExcept(Encoding.Unicode.GetBytes(departureMessage), UniqueId);
                        break;
                    }
                    var formattedMessage = $"{_username} [{DateTime.Now:HH:mm:ss}]: {receivedData}";
                    Console.WriteLine(formattedMessage);
                    _serverInstance.BroadcastMessageToAllExcept(Encoding.Unicode.GetBytes(formattedMessage), UniqueId);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing user session: {ex.Message}");
            }
            finally
            {
                _socketChannel.Shutdown(SocketShutdown.Both);
                _socketChannel.Close();
                _serverInstance.RemoveUserSession(UniqueId);
            }
        }
    }
    class ChatServer
    {
        private readonly List<UserSession> _activeSessions = new List<UserSession>();
        public void StartListening()
        {
            const int portNumber = 3821;
            var hostName = Dns.GetHostName();
            Console.WriteLine($"Computer Name: {hostName}");
            foreach (var ipAddress in Dns.GetHostAddresses(hostName))
            {
                Console.WriteLine(ipAddress);
            }
            var localEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), portNumber);
            using var listenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listenerSocket.Bind(localEndPoint);
            listenerSocket.Listen(10);
            Console.WriteLine("\nChat Server Started. Waiting for connections...");
            while (true)
            {
                Socket clientSocket = listenerSocket.Accept();
                var userNameBytes = new byte[256];
                int userNameLength = clientSocket.Receive(userNameBytes);
                var userName = Encoding.Unicode.GetString(userNameBytes, 0, userNameLength);
                var userSession = new UserSession(userName, clientSocket, this);
                _activeSessions.Add(userSession);
            }
        }
        public void BroadcastMessageToAllExcept(byte[] message, string senderId)
        {
            foreach (var session in _activeSessions)
            {
                if (session.UniqueId != senderId)
                {
                    session.GetSocketChannel().Send(message);
                }
            }
        }
        public void RemoveUserSession(string sessionId)
        {
            var sessionToRemove = _activeSessions.FirstOrDefault(s => s.UniqueId == sessionId);
            if (sessionToRemove != null)
            {
                _activeSessions.Remove(sessionToRemove);
            }
        }
    }
    class Program
    {
        static void Main()
        {
            var chatServer = new ChatServer();
            chatServer.StartListening();
        }
    }
}

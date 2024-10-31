using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
namespace ChatClientApp
{
    class MessageReceiverThread
    {
        private Thread _thread;
        public MessageReceiverThread(Socket socket)
        {
            _thread = new Thread(ReceiveMessages);
            _thread.Start(socket); // Passing parameter to the thread
        }
        // Method for receiving messages
        void ReceiveMessages(object socketObj)
        {
            Socket socket = (Socket)socketObj;
            while (true)
            {
                try
                {
                    StringBuilder receivedMessage = new StringBuilder();
                    byte[] buffer = new byte[256]; // Buffer for incoming data
                    int bytesRead = 0;
                    // Receiving messages
                    do
                    {
                        bytesRead = socket.Receive(buffer, buffer.Length, 0);
                        receivedMessage.Append(Encoding.Unicode.GetString(buffer, 0, bytesRead));
                    }
                    while (socket.Available > 0);
                    Console.WriteLine(receivedMessage.ToString());
                }
                catch
                {
                    break;
                }
            }
        }
    }
    class Program
    {
        static int serverPort = 3821; // Server port
        static string serverAddress = "127.0.0.1"; // Server address
        static void Main(string[] args)
        {
            Console.Write("Enter your name: ");
            string userName = Console.ReadLine();
            Console.WriteLine($"Welcome, {userName}");
            try
            {
                // Creating endpoint
                IPEndPoint ipEndpoint = new IPEndPoint(IPAddress.Parse(serverAddress), serverPort);
                // Creating socket
                Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                // Connecting to remote host
                clientSocket.Connect(ipEndpoint);
                // Starting thread for receiving data
                MessageReceiverThread receiverThread = new MessageReceiverThread(clientSocket);
                // Sending initial message
                clientSocket.Send(Encoding.Unicode.GetBytes(userName));
                Console.Write("Enter your message and press Enter to send:\n");
                while (true)
                {
                    string inputMessage = Console.ReadLine();
                    byte[] messageToSend = Encoding.Unicode.GetBytes(inputMessage);
                    // Sending message
                    clientSocket.Send(messageToSend);
                    if (inputMessage.ToLower() == "12345") break;
                }
                Console.WriteLine("All messages sent.");
                // Closing socket
                clientSocket.Shutdown(SocketShutdown.Both);
                clientSocket.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            Console.ReadKey();
        }
    }
}

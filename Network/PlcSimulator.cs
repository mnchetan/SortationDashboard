using System;
using System.Net.Sockets;
using System.Text;

namespace SortationDashboard.Network
{
    public class PlcSimulator
    {
        public void SendCartonEvent(string serverIp, int port, string message)
        {
            try
            {
                // 1. Connect to the Server
                using (TcpClient client = new TcpClient(serverIp, port))
                {
                    Console.WriteLine($"[Client] Connected to Server at {serverIp}:{port}");

                    // 2. Translate the string message into a raw byte array
                    byte[] dataToSend = Encoding.UTF8.GetBytes(message);

                    // 3. Get the stream and write the bytes over the network
                    using (NetworkStream stream = client.GetStream())
                    {
                        stream.Write(dataToSend, 0, dataToSend.Length);
                        Console.WriteLine($"[Client] Sent: {message}");

                        // 4. Wait for the Acknowledgment from the server
                        byte[] buffer = new byte[256];
                        int bytesRead = stream.Read(buffer, 0, buffer.Length);
                        string responseData = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                        Console.WriteLine($"[Client] Received Acknowledgment: {responseData}");
                    }
                }
            }
            catch (ArgumentNullException e)
            {
                Console.WriteLine($"[Client] ArgumentNullException: {e.Message}");
            }
            catch (SocketException e)
            {
                Console.WriteLine($"[Client] SocketException: {e.Message}");
            }
        }
    }
}
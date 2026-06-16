using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SortationDashboard.Network
{
    public class SimplePlcServer
    {
        public void StartListening(int port)
        {
            // 1. Define the Endpoint. IPAddress.Any means listen on all available network interfaces.
            IPAddress ipAddress = IPAddress.Any;
            TcpListener listener = new TcpListener(ipAddress, port);

            try
            {
                // 2. Start the listener
                listener.Start();
                Console.WriteLine($"[Server] Listening for PLC connections on Port {port}...");

                // 3. Accept a pending connection request (This is a BLOCKING call for this basic example)
                using (TcpClient client = listener.AcceptTcpClient())
                {
                    Console.WriteLine("[Server] PLC Connected!");

                    // 4. Get the raw network stream for reading and writing
                    using (NetworkStream stream = client.GetStream())
                    {
                        // 5. Create a buffer to hold the incoming bytes
                        byte[] buffer = new byte[1024];
                        int bytesRead = stream.Read(buffer, 0, buffer.Length);

                        // 6. Convert the raw bytes back into a readable string (assuming UTF-8 encoding)
                        string receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        Console.WriteLine($"[Server] Received Event: {receivedData}");

                        // 7. Send an Acknowledgment back to the hardware
                        byte[] ackBytes = Encoding.UTF8.GetBytes("ACK_RECEIVED");
                        stream.Write(ackBytes, 0, ackBytes.Length);
                        Console.WriteLine("[Server] Acknowledgment sent to PLC.");
                    }
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine($"[Server] SocketException: {e.Message}");
            }
            finally
            {
                // 8. Always stop the listener to free up the port
                listener.Stop();
                Console.WriteLine("[Server] Listener shut down.");
            }
        }
    }
}
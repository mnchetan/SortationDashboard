using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SortationDashboard.Network
{
    public class AsyncPlcServer
    {
        private TcpListener _listener;
        private bool _isRunning;

        public async Task StartListeningAsync(int port)
        {
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
            _isRunning = true;
            Console.WriteLine($"[AsyncServer] Listening on Port {port}...");

            try
            {
                while (_isRunning)
                {
                    // 1. Await a connection. The thread is freed up while waiting!
                    TcpClient client = await _listener.AcceptTcpClientAsync();
                    Console.WriteLine($"[AsyncServer] PLC Connected from {((IPEndPoint)client.Client.RemoteEndPoint).Address}");

                    // 2. FIRE AND FORGET: Hand the client off to a background task.
                    // The '_' discard operator tells the compiler we intentionally aren't waiting for this to finish.
                    // The loop immediately goes back to waiting for the NEXT connection.
                    _ = HandleClientConnectionAsync(client);
                }
            }
            catch (ObjectDisposedException)
            {
                // This exception is expected when we call listener.Stop() while it's awaiting a connection
                Console.WriteLine("[AsyncServer] Listener has been shut down.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AsyncServer] Fatal Exception: {ex.Message}");
            }
        }

        private async Task HandleClientConnectionAsync(TcpClient client)
        {
            // The 'using' statements ensure the socket is gracefully closed and memory is freed
            // even if the PLC abruptly loses power or drops the connection.
            using (client)
            using (NetworkStream stream = client.GetStream())
            {
                // A 4KB buffer is standard for hardware messaging
                byte[] buffer = new byte[4096];

                try
                {
                    // Loop continuously to read data from this specific PLC
                    while (client.Connected)
                    {
                        // 3. Await incoming bytes. Again, the thread is freed while waiting for network I/O.
                        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

                        // If bytesRead is 0, the client initiated a graceful disconnect
                        if (bytesRead == 0)
                        {
                            Console.WriteLine("[AsyncServer] PLC disconnected gracefully.");
                            break;
                        }

                        // Decode the raw bytes
                        string receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        Console.WriteLine($"[AsyncServer] Processed Event: {receivedData}");

                        // Simulate a quick database write or business logic check
                        await Task.Delay(50);

                        // Send Acknowledgment back to the PLC
                        byte[] ackBytes = Encoding.UTF8.GetBytes("ACK_OK");
                        await stream.WriteAsync(ackBytes, 0, ackBytes.Length);
                    }
                }
                catch (Exception ex)
                {
                    // Catch network drops, timeouts, and hardware failures for this specific connection
                    Console.WriteLine($"[AsyncServer] Connection Error with PLC: {ex.Message}");
                }
            }
        }

        public void Stop()
        {
            _isRunning = false;
            _listener?.Stop(); // Triggers the ObjectDisposedException in the accept loop to break it cleanly
        }
    }
}
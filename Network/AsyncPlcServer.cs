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

        // 1. Define events for the UI to subscribe to
        public event Action<string> OnClientConnected;
        public event Action<string> OnMessageReceived;
        public event Action<string> OnServerError;

        public async Task StartListeningAsync(int port)
        {
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
            _isRunning = true;

            // Replaced Console.WriteLine
            OnClientConnected?.Invoke($"Listening on Port {port}...");

            try
            {
                while (_isRunning)
                {
                    TcpClient client = await _listener.AcceptTcpClientAsync();

                    // Trigger event when a PLC connects
                    string clientIp = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
                    OnClientConnected?.Invoke($"PLC Connected from {clientIp}");

                    _ = HandleClientConnectionAsync(client);
                }
            }
            catch (Exception ex)
            {
                OnServerError?.Invoke($"Fatal Exception: {ex.Message}");
            }
        }

        private async Task HandleClientConnectionAsync(TcpClient client)
        {
            using (client)
            using (NetworkStream stream = client.GetStream())
            {
                byte[] buffer = new byte[4096];
                try
                {
                    while (client.Connected)
                    {
                        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

                        if (bytesRead == 0)
                        {
                            OnClientConnected?.Invoke("PLC disconnected gracefully.");
                            break;
                        }

                        string receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                        // 2. Trigger event when data arrives
                        OnMessageReceived?.Invoke(receivedData);

                        byte[] ackBytes = Encoding.UTF8.GetBytes("ACK_OK");
                        await stream.WriteAsync(ackBytes, 0, ackBytes.Length);
                    }
                }
                catch (Exception ex)
                {
                    OnServerError?.Invoke($"Connection Error: {ex.Message}");
                }
            }
        }

        public void Stop()
        {
            _isRunning = false;
            _listener?.Stop();
        }
    }
}
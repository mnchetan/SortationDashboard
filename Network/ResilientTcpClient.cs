using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SortationDashboard.Network
{
    public class ResilientTcpClient
    {
        private readonly string _ipAddress;
        private readonly int _port;
        private TcpClient _client;
        private NetworkStream _stream;
        private bool _isShuttingDown;

        // Configuration for retries
        private readonly int _maxRetries = 5;
        private readonly int _baseDelayMilliseconds = 2000;

        public ResilientTcpClient(string ipAddress, int port)
        {
            _ipAddress = ipAddress;
            _port = port;
        }

        public async Task ConnectAndListenAsync()
        {
            _isShuttingDown = false;
            int attempt = 0;

            while (!_isShuttingDown)
            {
                try
                {
                    Console.WriteLine($"[ResilientClient] Attempting to connect to {_ipAddress}:{_port} (Attempt {attempt + 1})...");

                    _client = new TcpClient();
                    await _client.ConnectAsync(_ipAddress, _port);
                    _stream = _client.GetStream();

                    Console.WriteLine("[ResilientClient] Connection Established successfully!");

                    // Reset attempt counter on successful connection
                    attempt = 0;

                    // Start reading data continuously
                    await ReadStreamAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ResilientClient] Connection failed or dropped: {ex.Message}");

                    attempt++;
                    if (attempt > _maxRetries)
                    {
                        Console.WriteLine("[ResilientClient] Max retries reached. Triggering critical alert...");
                        // In a real system, you would send an email/SMS or log to Splunk/ELK here
                        attempt = _maxRetries; // Cap the attempt to prevent integer overflow on the delay
                    }

                    // Exponential Backoff Math: 2s, 4s, 8s, 16s...
                    int delay = (int)(_baseDelayMilliseconds * Math.Pow(2, attempt - 1));
                    Console.WriteLine($"[ResilientClient] Waiting {delay / 1000} seconds before next attempt...");

                    await Task.Delay(delay);
                }
                finally
                {
                    CleanupConnection();
                }
            }
        }

        private async Task ReadStreamAsync()
        {
            byte[] buffer = new byte[4096];

            while (_client.Connected && !_isShuttingDown)
            {
                // This will throw an exception if the physical connection is severed
                int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);

                if (bytesRead == 0)
                {
                    Console.WriteLine("[ResilientClient] Server closed the connection gracefully.");
                    break; // Break the read loop to trigger the reconnect loop
                }

                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine($"[ResilientClient] Received: {message}");
            }
        }

        private void CleanupConnection()
        {
            _stream?.Dispose();
            _client?.Dispose();
            _stream = null;
            _client = null;
        }

        public void Disconnect()
        {
            _isShuttingDown = true;
            CleanupConnection();
            Console.WriteLine("[ResilientClient] Manual shutdown executed.");
        }
    }
}
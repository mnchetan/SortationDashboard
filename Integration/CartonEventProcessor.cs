using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace SortationDashboard.Integration
{
    // Keeping the data structure perfectly flat for high-speed database inserts
    // and avoiding the overhead of nested JSON serialization in high-throughput loops.
    public class ParsedCartonEvent
    {
        public string MessageId { get; set; }
        public string LabelNumber { get; set; }
        public string TargetChute { get; set; }
        public string EventType { get; set; }
        public DateTime ProcessedTimestamp { get; set; }
    }

    public class CartonEventProcessor
    {
        // Thread-safe dictionary to act as our Idempotency Cache.
        // Stores the MessageId and the time it was processed.
        private readonly ConcurrentDictionary<string, DateTime> _processedMessagesCache;

        // In a real production system, this cache needs a cleanup routine so it doesn't 
        // grow infinitely and cause an OutOfMemoryException.
        private readonly TimeSpan _cacheRetention = TimeSpan.FromMinutes(10);

        public CartonEventProcessor()
        {
            _processedMessagesCache = new ConcurrentDictionary<string, DateTime>();
        }

        public async Task ProcessRawTcpMessageAsync(string rawTcpPayload)
        {
            // Expected payload format: "MSG_ID|EVENT_TYPE|LABEL_NUMBER|CHUTE"
            // Example: "MSG99120|SCAN|LBL-4492|CHUTE_4"

            try
            {
                // 1. Message Parsing
                ParsedCartonEvent parsedEvent = ParseProtocol(rawTcpPayload);

                // 2. Idempotency Check
                if (IsDuplicateMessage(parsedEvent.MessageId))
                {
                    Console.WriteLine($"[Processor] DUPLICATE DETECTED: Message {parsedEvent.MessageId} was already processed. Dropping event.");
                    return; // Exit early, ensuring consistent state
                }

                // 3. Mark as processed immediately to prevent race conditions from multi-threaded PLC reads
                _processedMessagesCache.TryAdd(parsedEvent.MessageId, DateTime.UtcNow);

                // 4. Execute Business Logic (e.g., Save to SQL Server / Trigger ZPL Print)
                Console.WriteLine($"[Processor] Valid Event: Routing {parsedEvent.LabelNumber} to {parsedEvent.TargetChute}.");
                await PersistToDatabaseAsync(parsedEvent);

                // 5. Fire event to update the WPF UI Dispatcher (connecting to Phase 1)
                OnCartonProcessed(parsedEvent);
            }
            catch (FormatException ex)
            {
                Console.WriteLine($"[Processor] Protocol Parsing Error: {ex.Message} | Payload: {rawTcpPayload}");
            }
        }

        private ParsedCartonEvent ParseProtocol(string payload)
        {
            string[] parts = payload.Split('|');

            return parts.Length != 4
                ? throw new FormatException("Payload does not match the required 4-part protocol.")
                : new ParsedCartonEvent
                {
                    MessageId = parts[0],
                    EventType = parts[1],
                    LabelNumber = parts[2],
                    TargetChute = parts[3],
                    ProcessedTimestamp = DateTime.UtcNow
                };
        }

        private bool IsDuplicateMessage(string messageId)
        {
            // Check if the message exists in the cache
            if (_processedMessagesCache.TryGetValue(messageId, out DateTime processedTime))
            {
                // If it's within our retention window, it's a duplicate
                if (DateTime.UtcNow - processedTime < _cacheRetention)
                {
                    return true;
                }
            }
            return false;
        }

        private async Task PersistToDatabaseAsync(ParsedCartonEvent cartonEvent)
        {
            // Simulate a fast, flat insert via ADO.NET to SQL Server or Oracle
            await Task.Delay(50);
        }

        // Standard .NET Event pattern to broadcast the update to the WPF ViewModel
        public event EventHandler<ParsedCartonEvent> CartonProcessed;
        protected virtual void OnCartonProcessed(ParsedCartonEvent e)
        {
            CartonProcessed?.Invoke(this, e);
        }
    }
}
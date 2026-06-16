using System;

namespace SortationDashboard.Models
{
    public class ParsedCartonEvent
    {
        public string MessageId { get; set; }
        public string EventType { get; set; }
        public string LabelNumber { get; set; }
        public string TargetChute { get; set; }
        public DateTime ProcessedTimestamp { get; set; }
    }
}
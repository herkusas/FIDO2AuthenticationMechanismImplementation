using System;

namespace FidoBack.V1.Models
{
    public class CommonEvent
    {
        public Nest.Id Id { get; set; }
        public string ControllerName { get; set; }
        public string FunctionName { get; set; }
        public string UserId { get; set; }
        public string Timestamp => DateTimeOffset.UtcNow.ToString("O");
        public virtual string EventType { get; set; }
    }
}
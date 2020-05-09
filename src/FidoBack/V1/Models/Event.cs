using System;

namespace FidoBack.V1.Models
{
    public class Event : CommonEvent
    {
        public override string EventType => nameof(Event);

        public string Message { get; set; }

        public Event(string userId, string message, string controllerName, string functionName)
        {
            Id = Guid.NewGuid();
            UserId = userId;
            Message = message;
            ControllerName = controllerName;
            FunctionName = functionName;
        }
    }
}
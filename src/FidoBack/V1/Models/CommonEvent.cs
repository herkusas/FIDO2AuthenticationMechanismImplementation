using System;
using FidoBack.V1.Models;

namespace FidoBack.V1.Models
{
    public class CommonEvent
    {
        public Nest.Id Id { get; set; }
        public string Message { get; set; }
        public string ControllerName { get; set; }
        public string FunctionName { get; set; }
        public string UserId { get; set; }
        public string Timestamp => DateTimeOffset.UtcNow.ToString("O");
        public virtual string EventType { get; set; }
    }
}

public class Event : CommonEvent
{
    public override string EventType => nameof(Event);
}

public class ErrorEvent : CommonEvent
{
    public override string EventType => nameof(ErrorEvent);

    public Exception FailingException { get; set; }

    public ErrorEvent(Exception failingException, string userId, string controllerName, string functionName)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        FailingException = failingException;
        ControllerName = controllerName;
        FunctionName = functionName;
    }
}

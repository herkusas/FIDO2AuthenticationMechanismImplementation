using System;
using FidoBack.V1.Models;

namespace FidoBack.V1.Models
{
    public class CommonEvent
    {
        public Nest.Id Id { get; set; }
        public string Message { get; set; }
        public virtual string EventType { get; set; }
        public string UserId { get; set; }
    }
}

public class Event : CommonEvent
{
    public new string EventType => nameof(ErrorEvent);
}

public class ErrorEvent : CommonEvent
{
    public override string EventType => nameof(ErrorEvent);

    public Exception FailingException { get; set; }

    public ErrorEvent(Exception failingException, string userId)
    {
        Id = Guid.NewGuid();
        Message = failingException.Message;
        UserId = userId;
        FailingException = failingException;
    }
}

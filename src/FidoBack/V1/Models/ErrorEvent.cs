using System;

namespace FidoBack.V1.Models
{
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
}
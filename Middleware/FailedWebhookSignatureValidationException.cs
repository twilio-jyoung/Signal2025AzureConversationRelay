using System;

public class FailedWebhookSignatureValidationException : Exception
{
    public FailedWebhookSignatureValidationException() : base() { }
    public FailedWebhookSignatureValidationException(string message) : base(message) { }
    public FailedWebhookSignatureValidationException(string message, Exception inner) : base(message, inner) { }
}

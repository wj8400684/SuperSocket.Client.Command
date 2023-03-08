using System.Runtime.Serialization;

namespace SuperSocket.Client.Command;

public sealed class ConnectException : Exception
{
    public ConnectException()
    {
    }

    public ConnectException(string? message) : base(message)
    {
    }

    public ConnectException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }

    public ConnectException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}

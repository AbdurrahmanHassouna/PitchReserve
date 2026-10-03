namespace PitchReserve.Application.Common.Exceptions;

public class SynchronousAccessException : Exception
{
    public SynchronousAccessException()
        : base("multi Access to this resource is forbidden.")
    {
    }

    public SynchronousAccessException(string message)
        : base(message)
    {
    }
}

namespace PitchReserve.Application.Common.Exceptions;

public class UnreleasedLockException:Exception
{
    public UnreleasedLockException() : base("a lock wasn't released")
    {
    }

    public UnreleasedLockException(string message)
        : base(message)
    {

    }
}
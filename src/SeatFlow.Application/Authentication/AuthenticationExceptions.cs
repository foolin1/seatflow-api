namespace SeatFlow.Application.Authentication;

public abstract class AuthenticationException : Exception
{
    protected AuthenticationException(string message)
        : base(message)
    {
    }
}

public sealed class UserAlreadyExistsException
    : AuthenticationException
{
    public UserAlreadyExistsException(string email)
        : base(
            $"A user with email '{email}' already exists.")
    {
        Email = email;
    }

    public string Email { get; }
}

public sealed class InvalidCredentialsException
    : AuthenticationException
{
    public InvalidCredentialsException()
        : base("The email or password is incorrect.")
    {
    }
}

public sealed class InvalidRefreshTokenException
    : AuthenticationException
{
    public InvalidRefreshTokenException()
        : base(
            "The refresh token is invalid, expired or revoked.")
    {
    }
}

public sealed class InactiveUserException
    : AuthenticationException
{
    public InactiveUserException(Guid userId)
        : base(
            $"User '{userId}' is inactive.")
    {
        UserId = userId;
    }

    public Guid UserId { get; }
}
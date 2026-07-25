using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using SeatFlow.Application.Authentication;
using SeatFlow.Domain.Entities;
using SeatFlow.Domain.Enums;
using SeatFlow.Domain.Exceptions;
using SeatFlow.Infrastructure.Persistence;

namespace SeatFlow.Infrastructure.Authentication;

public sealed class AuthenticationService
    : IAuthenticationService
{
    private const string TemporaryPasswordHash =
        "temporary-password-hash";

    private readonly SeatFlowDbContext _dbContext;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly JwtOptions _jwtOptions;
    private readonly TimeProvider _timeProvider;
    private readonly JwtSecurityTokenHandler _tokenHandler;

    public AuthenticationService(
        SeatFlowDbContext dbContext,
        IPasswordHasher<User> passwordHasher,
        JwtOptions jwtOptions,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _jwtOptions = jwtOptions;
        _timeProvider = timeProvider;
        _tokenHandler = new JwtSecurityTokenHandler();
    }

    public async Task<AuthenticationResult> RegisterAsync(
        string email,
        string password,
        string fullName,
        CancellationToken cancellationToken)
    {
        var normalizedEmail =
            User.NormalizeEmail(email);

        PasswordPolicy.Validate(password);

        var userAlreadyExists =
            await _dbContext.Users.AnyAsync(
                user => user.Email == normalizedEmail,
                cancellationToken);

        if (userAlreadyExists)
        {
            throw new UserAlreadyExistsException(
                normalizedEmail);
        }

        var currentTimeUtc =
            _timeProvider.GetUtcNow();

        var user = new User(
            Guid.NewGuid(),
            normalizedEmail,
            TemporaryPasswordHash,
            fullName,
            UserRole.User,
            currentTimeUtc);

        var passwordHash =
            _passwordHasher.HashPassword(
                user,
                password);

        user.ChangePasswordHash(passwordHash);

        var issuedRefreshToken =
            CreateRefreshToken(
                user.Id,
                currentTimeUtc);

        _dbContext.Users.Add(user);

        _dbContext.RefreshTokens.Add(
            issuedRefreshToken.Entity);

        try
        {
            await _dbContext.SaveChangesAsync(
                cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException
                  is PostgresException
            {
                SqlState:
                          PostgresErrorCodes.UniqueViolation
            })
        {
            throw new UserAlreadyExistsException(
                normalizedEmail);
        }

        return CreateAuthenticationResult(
            user,
            issuedRefreshToken,
            currentTimeUtc);
    }

    public async Task<AuthenticationResult> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidCredentialsException();
        }

        string normalizedEmail;

        try
        {
            normalizedEmail =
                User.NormalizeEmail(email);
        }
        catch (DomainValidationException)
        {
            throw new InvalidCredentialsException();
        }

        var user =
            await _dbContext.Users.SingleOrDefaultAsync(
                currentUser =>
                    currentUser.Email == normalizedEmail,
                cancellationToken);

        if (user is null)
        {
            throw new InvalidCredentialsException();
        }

        if (!user.IsActive)
        {
            throw new InactiveUserException(user.Id);
        }

        var verificationResult =
            _passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                password);

        if (verificationResult ==
            PasswordVerificationResult.Failed)
        {
            throw new InvalidCredentialsException();
        }

        if (verificationResult ==
            PasswordVerificationResult.SuccessRehashNeeded)
        {
            var updatedPasswordHash =
                _passwordHasher.HashPassword(
                    user,
                    password);

            user.ChangePasswordHash(
                updatedPasswordHash);
        }

        var currentTimeUtc =
            _timeProvider.GetUtcNow();

        var issuedRefreshToken =
            CreateRefreshToken(
                user.Id,
                currentTimeUtc);

        _dbContext.RefreshTokens.Add(
            issuedRefreshToken.Entity);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return CreateAuthenticationResult(
            user,
            issuedRefreshToken,
            currentTimeUtc);
    }

    public async Task<AuthenticationResult> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken)
    {
        var tokenHash =
            HashRefreshToken(refreshToken);

        var storedToken =
            await _dbContext.RefreshTokens
                .SingleOrDefaultAsync(
                    token =>
                        token.TokenHash == tokenHash,
                    cancellationToken);

        var currentTimeUtc =
            _timeProvider.GetUtcNow();

        if (storedToken is null ||
            !storedToken.IsActiveAt(currentTimeUtc))
        {
            throw new InvalidRefreshTokenException();
        }

        var user =
            await _dbContext.Users.SingleOrDefaultAsync(
                currentUser =>
                    currentUser.Id == storedToken.UserId,
                cancellationToken);

        if (user is null)
        {
            throw new InvalidRefreshTokenException();
        }

        if (!user.IsActive)
        {
            throw new InactiveUserException(user.Id);
        }

        var replacementToken =
            CreateRefreshToken(
                user.Id,
                currentTimeUtc);

        storedToken.Revoke(
            currentTimeUtc,
            replacementToken.Entity.Id);

        _dbContext.RefreshTokens.Add(
            replacementToken.Entity);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return CreateAuthenticationResult(
            user,
            replacementToken,
            currentTimeUtc);
    }

    public async Task RevokeAsync(
        string refreshToken,
        CancellationToken cancellationToken)
    {
        var tokenHash =
            HashRefreshToken(refreshToken);

        var storedToken =
            await _dbContext.RefreshTokens
                .SingleOrDefaultAsync(
                    token =>
                        token.TokenHash == tokenHash,
                    cancellationToken);

        var currentTimeUtc =
            _timeProvider.GetUtcNow();

        if (storedToken is null ||
            !storedToken.IsActiveAt(currentTimeUtc))
        {
            throw new InvalidRefreshTokenException();
        }

        storedToken.Revoke(currentTimeUtc);

        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }

    public async Task<AuthenticatedUser?> GetUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .Where(
                user =>
                    user.Id == userId &&
                    user.IsActive)
            .Select(
                user => new AuthenticatedUser(
                    user.Id,
                    user.Email,
                    user.FullName,
                    user.Role))
            .SingleOrDefaultAsync(
                cancellationToken);
    }

    private AuthenticationResult CreateAuthenticationResult(
        User user,
        IssuedRefreshToken issuedRefreshToken,
        DateTimeOffset currentTimeUtc)
    {
        var accessTokenExpiresAtUtc =
            currentTimeUtc.AddMinutes(
                _jwtOptions.AccessTokenMinutes);

        var accessToken =
            CreateAccessToken(
                user,
                currentTimeUtc,
                accessTokenExpiresAtUtc);

        return new AuthenticationResult(
            accessToken,
            accessTokenExpiresAtUtc,
            issuedRefreshToken.RawValue,
            issuedRefreshToken.Entity.ExpiresAtUtc,
            new AuthenticatedUser(
                user.Id,
                user.Email,
                user.FullName,
                user.Role));
    }

    private string CreateAccessToken(
        User user,
        DateTimeOffset issuedAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        var claims = new[]
        {
            new Claim(
                JwtRegisteredClaimNames.Sub,
                user.Id.ToString()),

            new Claim(
                JwtRegisteredClaimNames.Name,
                user.FullName),

            new Claim(
                JwtRegisteredClaimNames.Email,
                user.Email),

            new Claim(
                "role",
                user.Role.ToString()),

            new Claim(
                JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString())
        };

        var signingKey =
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    _jwtOptions.SigningKey));

        var tokenDescriptor =
            new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Issuer = _jwtOptions.Issuer,
                Audience = _jwtOptions.Audience,
                IssuedAt = issuedAtUtc.UtcDateTime,
                NotBefore = issuedAtUtc.UtcDateTime,
                Expires = expiresAtUtc.UtcDateTime,
                SigningCredentials =
                    new SigningCredentials(
                        signingKey,
                        SecurityAlgorithms.HmacSha256)
            };

        var securityToken =
            _tokenHandler.CreateToken(
                tokenDescriptor);

        return _tokenHandler.WriteToken(
            securityToken);
    }

    private IssuedRefreshToken CreateRefreshToken(
        Guid userId,
        DateTimeOffset createdAtUtc)
    {
        var rawToken =
            CreateRefreshTokenValue();

        var refreshToken =
            new RefreshToken(
                Guid.NewGuid(),
                userId,
                HashRefreshToken(rawToken),
                createdAtUtc,
                createdAtUtc.AddDays(
                    _jwtOptions.RefreshTokenDays));

        return new IssuedRefreshToken(
            rawToken,
            refreshToken);
    }

    private static string CreateRefreshTokenValue()
    {
        var randomBytes =
            RandomNumberGenerator.GetBytes(64);

        return Convert
            .ToBase64String(randomBytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string HashRefreshToken(
        string? refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new InvalidRefreshTokenException();
        }

        var tokenBytes =
            Encoding.UTF8.GetBytes(refreshToken);

        var hashBytes =
            SHA256.HashData(tokenBytes);

        return Convert.ToHexString(hashBytes);
    }

    private sealed record IssuedRefreshToken(
        string RawValue,
        RefreshToken Entity);
}
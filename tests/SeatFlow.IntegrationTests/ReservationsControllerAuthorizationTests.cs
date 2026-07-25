using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using SeatFlow.Api.Controllers;

namespace SeatFlow.IntegrationTests;

public sealed class
    ReservationsControllerAuthorizationTests
{
    [Fact]
    public void Controller_RequiresAuthentication()
    {
        var authorizeAttribute =
            typeof(ReservationsController)
                .GetCustomAttribute<
                    AuthorizeAttribute>();

        Assert.NotNull(
            authorizeAttribute);
    }
}
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using SeatFlow.Api.Controllers;
using SeatFlow.Domain.Enums;

namespace SeatFlow.IntegrationTests;

public sealed class AdminControllerAuthorizationTests
{
    public static TheoryData<Type> AdminControllers =>
        new()
        {
            typeof(VenuesController),
            typeof(HallsController),
            typeof(SeatsController),
            typeof(EventsController),
            typeof(EventSessionsController)
        };

    [Theory]
    [MemberData(nameof(AdminControllers))]
    public void Controller_RequiresAdminRole(
        Type controllerType)
    {
        var authorizeAttribute =
            controllerType.GetCustomAttribute<
                AuthorizeAttribute>();

        Assert.NotNull(authorizeAttribute);

        Assert.Equal(
            nameof(UserRole.Admin),
            authorizeAttribute.Roles);
    }
}
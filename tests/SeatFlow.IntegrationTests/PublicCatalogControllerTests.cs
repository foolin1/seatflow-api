using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using SeatFlow.Api.Controllers;

namespace SeatFlow.IntegrationTests;

public sealed class PublicCatalogControllerTests
{
    public static TheoryData<Type> PublicControllers =>
        new()
        {
            typeof(PublicEventsController),
            typeof(PublicSessionsController)
        };

    [Theory]
    [MemberData(nameof(PublicControllers))]
    public void Controller_AllowsAnonymousAccess(
        Type controllerType)
    {
        var allowAnonymousAttribute =
            controllerType.GetCustomAttribute<
                AllowAnonymousAttribute>();

        Assert.NotNull(allowAnonymousAttribute);
    }

    [Fact]
    public void PublicEventsController_HasExpectedBaseRoute()
    {
        var routeAttribute =
            typeof(PublicEventsController)
                .GetCustomAttribute<RouteAttribute>();

        Assert.NotNull(routeAttribute);

        Assert.Equal(
            "api/events",
            routeAttribute.Template);
    }

    [Fact]
    public void PublicSessionsController_HasExpectedBaseRoute()
    {
        var routeAttribute =
            typeof(PublicSessionsController)
                .GetCustomAttribute<RouteAttribute>();

        Assert.NotNull(routeAttribute);

        Assert.Equal(
            "api/sessions",
            routeAttribute.Template);
    }

    [Fact]
    public void PublicEventsController_ExposesExpectedGetRoutes()
    {
        var routeTemplates =
            typeof(PublicEventsController)
                .GetMethods(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.DeclaredOnly)
                .SelectMany(
                    method =>
                        method.GetCustomAttributes<
                            HttpMethodAttribute>())
                .Select(
                    attribute =>
                        attribute.Template ??
                        string.Empty)
                .OrderBy(
                    template => template)
                .ToArray();

        Assert.Equal(
            new[]
            {
                string.Empty,
                "{eventId:guid}",
                "{eventId:guid}/sessions"
            },
            routeTemplates);
    }

    [Fact]
    public void PublicSessionsController_ExposesSeatMapRoute()
    {
        var routeTemplates =
            typeof(PublicSessionsController)
                .GetMethods(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.DeclaredOnly)
                .SelectMany(
                    method =>
                        method.GetCustomAttributes<
                            HttpMethodAttribute>())
                .Select(
                    attribute =>
                        attribute.Template ??
                        string.Empty)
                .ToArray();

        Assert.Equal(
            new[]
            {
                "{sessionId:guid}/seats"
            },
            routeTemplates);
    }
}
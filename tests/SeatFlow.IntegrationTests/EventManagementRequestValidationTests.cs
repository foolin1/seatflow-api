using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using SeatFlow.Api.Contracts.Catalog;
using Xunit;

namespace SeatFlow.IntegrationTests;

public sealed class EventManagementRequestValidationTests
{
    [Fact]
    public void CreateEventSessionRequest_WithDecimalPrice_ValidatesInRussianCulture()
    {
        var originalCulture =
            CultureInfo.CurrentCulture;

        var originalUiCulture =
            CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentCulture =
                CultureInfo.GetCultureInfo("ru-RU");

            CultureInfo.CurrentUICulture =
                CultureInfo.GetCultureInfo("ru-RU");

            var currentTimeUtc =
                DateTimeOffset.UtcNow;

            var startsAtUtc =
                currentTimeUtc.AddDays(30);

            var request =
                new CreateEventSessionRequest
                {
                    EventId = Guid.NewGuid(),
                    HallId = Guid.NewGuid(),
                    StartsAtUtc = startsAtUtc,
                    BookingOpensAtUtc =
                        currentTimeUtc.AddMinutes(1),
                    BookingClosesAtUtc =
                        startsAtUtc.AddHours(-1),
                    DefaultPrice = 65.50m
                };

            var validationContext =
                new ValidationContext(request);

            var validationResults =
                new List<ValidationResult>();

            var isValid =
                Validator.TryValidateObject(
                    request,
                    validationContext,
                    validationResults,
                    validateAllProperties: true);

            var validationMessage =
                string.Join(
                    Environment.NewLine,
                    validationResults.Select(
                        result =>
                            result.ErrorMessage ??
                            "Unknown validation error."));

            Assert.True(
                isValid,
                validationMessage);
        }
        finally
        {
            CultureInfo.CurrentCulture =
                originalCulture;

            CultureInfo.CurrentUICulture =
                originalUiCulture;
        }
    }
}
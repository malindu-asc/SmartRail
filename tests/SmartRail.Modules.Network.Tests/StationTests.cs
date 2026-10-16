using SmartRail.Modules.Network.Domain.Stations;

namespace SmartRail.Modules.Network.Tests;

public sealed class StationTests
{
    [Theory]
    [InlineData("CEN")]
    [InlineData("OAKPK")]
    public void Valid_codes_are_accepted(string code)
    {
        var result = Station.Create(code, "Central", []);
        Assert.True(result.IsSuccess);
        Assert.Equal(code, result.Value.Code);
    }

    [Theory]
    [InlineData("ce")]
    [InlineData("cen")]
    [InlineData("TOOLONG")]
    [InlineData("C3N")]
    [InlineData("")]
    public void Invalid_codes_are_rejected(string code)
    {
        var result = Station.Create(code, "Central", []);
        Assert.True(result.IsFailure);
        Assert.Equal("Network.InvalidStationCode", result.Error.Code);
    }

    [Fact]
    public void Name_is_required()
    {
        Assert.Equal("Network.InvalidName", Station.Create("CEN", "  ", []).Error.Code);
    }

    [Fact]
    public void Unknown_facility_is_rejected()
    {
        var result = Station.Create("CEN", "Central", ["Catering"]);
        Assert.Equal("Network.UnknownFacility", result.Error.Code);
    }

    [Fact]
    public void Facilities_are_deduplicated()
    {
        var station = Station.Create("CEN", "Central", [FacilityCodes.Lifts, FacilityCodes.Lifts, FacilityCodes.StepFree]).Value;
        Assert.Equal([FacilityCodes.Lifts, FacilityCodes.StepFree], station.Facilities);
    }

    [Fact]
    public void Platform_code_must_be_unique_within_the_station()
    {
        var station = Station.Create("CEN", "Central", []).Value;
        Assert.True(station.AddPlatform("1").IsSuccess);
        Assert.Equal("Network.DuplicatePlatform", station.AddPlatform("1").Error.Code);
    }

    [Fact]
    public void Translation_for_the_default_language_is_rejected()
    {
        var station = Station.Create("CEN", "Central", []).Value;
        var result = station.AddTranslation("en", "Central", defaultLanguageCode: "en");
        Assert.Equal("Network.DefaultLanguageTranslation", result.Error.Code);
    }

    [Fact]
    public void Translation_is_added_once_per_language()
    {
        var station = Station.Create("CEN", "Central", []).Value;
        Assert.True(station.AddTranslation("si", "මධ්‍යම", "en").IsSuccess);
        Assert.Equal("Network.DuplicateTranslation", station.AddTranslation("si", "මධ්‍යම", "en").Error.Code);
    }
}

using clypse.portal.setup.Services.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace clypse.portal.setup.UnitTests.Services.Json;

public class NewtonsoftJsonMergerServiceTests
{
    [Theory]
    [InlineData("Base1.json", "Override1.json", "Expected1.json")]
    public async Task GivenBaseJsonString_AndOverrideJsonString_WhenMerged_ThenReturnsExpectedResult(
        string baseJsonPath,
        string overrideJsonPath,
        string expectedJsonPath)
    {
        // Arrange
        baseJsonPath = Path.Combine("Data/Json/", baseJsonPath);
        overrideJsonPath = Path.Combine("Data/Json/", overrideJsonPath);
        expectedJsonPath = Path.Combine("Data/Json/", expectedJsonPath);
        var baseJson = await File.ReadAllTextAsync(baseJsonPath);
        var overrideJson = await File.ReadAllTextAsync(overrideJsonPath);
        var expectedMergedJson = await File.ReadAllTextAsync(expectedJsonPath);
        var expectedMergedJsonReparsed = JObject.Parse(expectedMergedJson).ToString(Newtonsoft.Json.Formatting.Indented);
        var sut = new NewtonsoftJsonMergerService();

        // Act
        var mergedJson = sut.MergeJsonStrings(baseJson, overrideJson);

        // Assert
        Assert.Equal(expectedMergedJsonReparsed, mergedJson);
    }
}

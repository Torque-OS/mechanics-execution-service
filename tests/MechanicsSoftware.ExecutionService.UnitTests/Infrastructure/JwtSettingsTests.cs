using FluentAssertions;
using MechanicsSoftware.ExecutionService.Infrastructure.Security;
using Microsoft.Extensions.Configuration;

namespace MechanicsSoftware.ExecutionService.UnitTests.Infrastructure;

public class JwtSettingsTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FromConfiguration_WithoutSecret_Throws(string? secret)
    {
        var configuration = Build(("JWT_SECRET", secret));

        var act = () => JwtSettings.FromConfiguration(configuration);

        act.Should().Throw<InvalidOperationException>().WithMessage("*JWT_SECRET*");
    }

    [Fact]
    public void FromConfiguration_WithoutIssuerAndAudience_UsesThePlatformDefaults()
    {
        var settings = JwtSettings.FromConfiguration(Build(("JWT_SECRET", "secret")));

        settings.Issuer.Should().Be("torque-os");
        settings.Audience.Should().Be("mechanics-software-api");
    }

    [Fact]
    public void FromConfiguration_WithIssuerAndAudience_UsesTheConfiguredValues()
    {
        var settings = JwtSettings.FromConfiguration(Build(
            ("JWT_SECRET", "secret"), ("JWT_ISSUER", "issuer"), ("JWT_AUDIENCE", "audience")));

        settings.Issuer.Should().Be("issuer");
        settings.Audience.Should().Be("audience");
    }

    [Fact]
    public void ToValidationParameters_ValidatesSignatureIssuerAudienceAndLifetime()
    {
        var parameters = JwtSettings.FromConfiguration(Build(("JWT_SECRET", "secret"))).ToValidationParameters();

        parameters.ValidateIssuerSigningKey.Should().BeTrue();
        parameters.ValidateIssuer.Should().BeTrue();
        parameters.ValidateAudience.Should().BeTrue();
        parameters.ValidateLifetime.Should().BeTrue();
        parameters.ClockSkew.Should().Be(TimeSpan.Zero);
        parameters.RoleClaimType.Should().Be(JwtSettings.RoleClaimType);
    }

    private static IConfiguration Build(params (string Key, string? Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value)))
            .Build();
}

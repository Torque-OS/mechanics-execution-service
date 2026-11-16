using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using MechanicsSoftware.ExecutionService.Infrastructure.Security;
using Microsoft.IdentityModel.Tokens;

namespace MechanicsSoftware.ExecutionService.UnitTests.Helpers;

public static class TestTokens
{
    public const string Secret = "test-secret-for-execution-endpoint-tests-32+";

    public static string Staff(string role = "MECHANIC") => Create(role, Guid.NewGuid());

    public static string Customer() => Create("CUSTOMER", Guid.NewGuid());

    public static string Create(string role, Guid subject, string secret = Secret, DateTime? expires = null)
    {
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)), SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: JwtSettings.DefaultIssuer,
            audience: JwtSettings.DefaultAudience,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, subject.ToString()),
                new Claim(JwtSettings.RoleClaimType, role),
            ],
            notBefore: (expires ?? DateTime.UtcNow.AddHours(1)).AddHours(-2),
            expires: expires ?? DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static HttpClient WithToken(this HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}

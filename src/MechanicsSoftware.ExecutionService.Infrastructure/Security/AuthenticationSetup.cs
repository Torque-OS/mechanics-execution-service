using MechanicsSoftware.ExecutionService.Domain.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MechanicsSoftware.ExecutionService.Infrastructure.Security;

public static class AuthenticationSetup
{
    public static IServiceCollection AddPlatformAuthentication(
        this IServiceCollection services, IConfiguration configuration)
    {
        var jwt = JwtSettings.FromConfiguration(configuration);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = jwt.ToValidationParameters();
            });

        services.AddAuthorization(options =>
            options.AddPolicy(Policies.Staff, policy => policy.RequireRole(Policies.StaffRoles)));

        return services;
    }
}

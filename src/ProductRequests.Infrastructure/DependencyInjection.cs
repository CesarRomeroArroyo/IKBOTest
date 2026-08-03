using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProductRequests.Application.Abstractions;
using ProductRequests.Application.Auth;
using ProductRequests.Domain.Users;
using ProductRequests.Infrastructure.Authentication;
using ProductRequests.Infrastructure.Persistence;
using ProductRequests.Infrastructure.Repositories;
using ProductRequests.Infrastructure.Seeding;
using Microsoft.IdentityModel.Tokens;

namespace ProductRequests.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        JwtOptions jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? new JwtOptions();
        byte[] validationKey = jwtOptions.SigningKey.Length >= 32
            ? Encoding.UTF8.GetBytes(jwtOptions.SigningKey)
            : RandomNumberGenerator.GetBytes(32);
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(validationKey),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    NameClaimType = "email",
                    RoleClaimType = "role"
                };
            });
        services.AddAuthorization();
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<IPasswordVerifier, PasswordVerifier>();
        services.AddScoped<ITokenGenerator, JwtTokenGenerator>();

        string? connectionString = configuration.GetConnectionString("ProductRequests");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddDbContext<ProductRequestsDbContext>(options =>
                options.UseMySQL(connectionString, mysql => mysql.MaxBatchSize(1)));
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IProductRequestRepository, ProductRequestRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<DemoUserSeeder>();
        }
        return services;
    }
}

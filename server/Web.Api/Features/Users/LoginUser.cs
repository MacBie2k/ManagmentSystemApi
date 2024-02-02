using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Carter;
using FluentValidation;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Web.Api.Contracts;
using Web.Api.Database;
using Web.Api.Entities;
using Web.Api.Shared;

namespace Web.Api.Features.Users;

public class LoginUser
{
    public class Command : IRequest<Result<string>>
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
    
    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Email).EmailAddress();
            RuleFor(c => c.Password).NotEmpty();
        }
    }

    internal sealed class Handler : IRequestHandler<Command, Result<string>>

    {
        private readonly ApplicationDBContext _dbContext;
        private readonly IValidator<Command> _validator;
        private readonly IConfiguration _configuration;
        public Handler(ApplicationDBContext dbContext, IValidator<Command> validator, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _validator = validator;
            _configuration = configuration;
        }

        public async Task<Result<string>> Handle(Command request, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);

            if (!validationResult.IsValid)
                return Result.Failure<string>(new Error("LoginUser.Validation", validationResult.ToString()));

            var hashedPassword = QuickHash(request.Password);

            var user = await _dbContext.Users
                .FirstOrDefaultAsync(x => x.Email == request.Email && x.Password == hashedPassword,
                    cancellationToken: cancellationToken);
            if(user is null)
                return Result.Failure<string>(new Error("LoginUser.UserNotFound", "User has not be found"));

            return GenerateJwt(user);
        }
        string QuickHash(string input)
        {
            var inputBytes = Encoding.UTF8.GetBytes(input);
            var inputHash = SHA256.HashData(inputBytes);
            return Convert.ToHexString(inputHash);
        }

        private string GenerateJwt(User user)
        {
            var claims = new Claim[]
            {
                new (JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new (JwtRegisteredClaimNames.Email, user.Email),
            };
    

            var securityKey =
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? string.Empty));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(Convert.ToDouble(_configuration["Jwt:ExpiryMinutes"])),
                signingCredentials: credentials
            );

            string tokenValue = new JwtSecurityTokenHandler()
                .WriteToken(token);

            return tokenValue;
        }
    }
}

public class LoginUserEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("api/login", async (LoginUserRequest request, ISender sender) =>
        {
            var command = request.Adapt<LoginUser.Command>();
            
            var result = await sender.Send(command);
            if (result.IsFailure)
                return Results.BadRequest(result.Error);
                
            return Results.Ok(result.Value);
        }).WithTags("Users");;
    }
}
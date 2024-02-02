using System.Security.Cryptography;
using System.Text;
using Carter;
using FluentValidation;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Web.Api.Contracts;
using Web.Api.Database;
using Web.Api.Entities;
using Web.Api.Shared;

namespace Web.Api.Features.Users;

public class CreateUser
{
    public class Command : IRequest<Result<Guid>>
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.FullName).NotEmpty();
            RuleFor(c => c.Password).NotEmpty();
            RuleFor(c => c.Email).EmailAddress();
        }
    }

    internal sealed class Handler : IRequestHandler<Command, Result<Guid>>
    {
        private readonly ApplicationDBContext _dbContext;
        private readonly IValidator<Command> _validator;

        public Handler(ApplicationDBContext dbContext, IValidator<Command> validator)
        {
            _dbContext = dbContext;
            _validator = validator;
        }

        public async Task<Result<Guid>> Handle(Command request, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);

            if (!validationResult.IsValid)
                return Result.Failure<Guid>(new Error("CreateUser.Validation", validationResult.ToString()));

            if(await _dbContext.Users.AnyAsync(x=>x.Email == request.Email, cancellationToken: cancellationToken))
                return Result.Failure<Guid>(new Error("CreateUser.Validation", "User already exists"));
            
            var user = new User()
            {
                Id = Guid.NewGuid(),
                Password = QuickHash(request.Password),
                FullName = request.FullName,
                Email = request.Email,
            };
            await _dbContext.Users.AddAsync(user, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return user.Id;
        }


        string QuickHash(string input)
        {
            var inputBytes = Encoding.UTF8.GetBytes(input);
            var inputHash = SHA256.HashData(inputBytes);
            return Convert.ToHexString(inputHash);
        }
    }
    
}

public class CreateUserEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("api/users", async (CreateUserRequest request, ISender sender) =>
        {
            var command = request.Adapt<CreateUser.Command>();
            
            var result = await sender.Send(command);
            if (result.IsFailure)
                return Results.BadRequest(result.Error);
                
            return Results.Ok(result.Value);
        }).WithTags("Users");
    }
}
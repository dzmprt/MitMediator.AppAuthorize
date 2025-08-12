using Books.Domain;
using FluentValidation;

namespace Books.Application.UseCase.Users.Command.CreateUser;

/// <summary>
/// Validator for <see cref="CreateUserCommand"/>.
/// </summary>
public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateUserCommandValidator"/>.
    /// </summary>
    public CreateUserCommandValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(User.MaxNameLength);
        RuleFor(request => request.Password).NotEmpty().MaximumLength(1024);
    }
}
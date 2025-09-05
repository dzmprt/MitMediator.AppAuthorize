using Books.Domain;
using MitMediator;
using MitMediator.AppAuthorize;

namespace Books.Application.UseCase.Users.Command.CreateUser;

/// <summary>
/// Create new user.
/// </summary>
[AppAllowAnonymous]
public class CreateUserCommand : IRequest<User>
{
    /// <summary>
    /// New username.
    /// </summary>
    public string Name { get; init; }
    
    /// <summary>
    /// New user password.
    /// </summary>
    public string Password { get; init; }
}
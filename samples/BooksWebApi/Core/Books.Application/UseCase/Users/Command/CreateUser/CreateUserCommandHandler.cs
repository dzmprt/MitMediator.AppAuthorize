using Books.Application.Abstractions.Infrastructure;
using Books.Domain;
using Books.Domain.ValueObjects;
using MitMediator;

namespace Books.Application.UseCase.Users.Command.CreateUser;

/// <summary>
/// Create user handler.
/// </summary>
public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, User>
{
    private readonly IBaseRepository<User> _usersRepository;
    private readonly IBaseProvider<Tenant> _tenantsProvider;
    private readonly IBaseProvider<UserRole> _userRoleProvider;
    private readonly IPasswordHasher _passwordHasher;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateUserCommandHandler"/>.
    /// </summary>
    /// <param name="usersRepository">Users repository.</param>
    /// <param name="userRoleProvider">User roles repository.</param>
    /// <param name="passwordHasher"><see cref="IPasswordHasher"/>.</param>
    /// <param name="tenantsProvider">Tenants repository.</param>
    public CreateUserCommandHandler(
        IBaseRepository<User> usersRepository,
        IBaseProvider<Tenant> tenantsProvider,
        IBaseProvider<UserRole> userRoleProvider,
        IPasswordHasher passwordHasher)
    {
        _usersRepository = usersRepository;
        _tenantsProvider = tenantsProvider;
        _userRoleProvider = userRoleProvider;
        _passwordHasher = passwordHasher;
    }
    
    /// <inheritdoc/>
    public async ValueTask<User> HandleAsync(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _tenantsProvider.FirstOrDefaultAsync(null, cancellationToken);
        var userRole = await _userRoleProvider.FirstOrDefaultAsync(null, cancellationToken);
        var newUser = new User(request.Name, _passwordHasher.GetHash(request.Password), userRole!, tenant);
        await _usersRepository.AddAsync(newUser, cancellationToken);
        return newUser;
    }
}
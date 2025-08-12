using Books.Application.Abstractions.Infrastructure;
using Books.Domain;
using MitMediator;
using MitMediator.AppAuthorize;

namespace Books.Application.UseCase.Users.Queries.GetCurrentUser;

/// <summary>
/// Get current user query handler.
/// </summary>
public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, User>
{
    private readonly IBaseProvider<User> _usersProvider;
    private readonly IAuthenticationContext _authenticationContext;

    
    /// <summary>
    /// Initializes a new instance of the <see cref="GetCurrentUserQueryHandler"/>.
    /// </summary>
    /// <param name="usersProvider">Users provider.</param>
    /// <param name="authenticationContext"><see cref="IAuthenticationContext"/>.</param>
    public GetCurrentUserQueryHandler(IBaseProvider<User> usersProvider, IAuthenticationContext authenticationContext)
    {
        _usersProvider = usersProvider;
        _authenticationContext = authenticationContext;
    }

    /// <inheritdoc/>
    public async ValueTask<User> HandleAsync(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        return await _usersProvider.SingleAsync(u => u.UserId == int.Parse(_authenticationContext.UserId!), cancellationToken);
    }
}
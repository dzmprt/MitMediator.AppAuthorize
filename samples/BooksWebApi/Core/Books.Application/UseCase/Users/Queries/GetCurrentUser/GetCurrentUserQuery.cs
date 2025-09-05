using Books.Domain;
using MitMediator;

namespace Books.Application.UseCase.Users.Queries.GetCurrentUser;

/// <summary>
/// Get current user query.
/// </summary>
public class GetCurrentUserQuery : IRequest<User>;
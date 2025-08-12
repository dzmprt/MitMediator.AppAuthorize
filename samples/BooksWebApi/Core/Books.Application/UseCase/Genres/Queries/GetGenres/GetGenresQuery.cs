using Books.Domain;
using Books.Domain.ValueObjects;
using MitMediator;

namespace Books.Application.UseCase.Genres.Queries.GetGenres;

/// <summary>
/// Get genres query.
/// </summary>
public struct GetGenresQuery : IRequest<Genre[]>
{
    
}
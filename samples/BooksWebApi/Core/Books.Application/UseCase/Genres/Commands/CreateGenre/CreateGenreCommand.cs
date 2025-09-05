using Books.Domain;
using Books.Domain.ValueObjects;
using MitMediator;

namespace Books.Application.UseCase.Genres.Commands.CreateGenre;

/// <summary>
/// Create genre command.
/// </summary>
public struct CreateGenreCommand : IRequest<Genre>
{
    /// <summary>
    /// Genre name.
    /// </summary>
    public string GenreName { get; init; }
}
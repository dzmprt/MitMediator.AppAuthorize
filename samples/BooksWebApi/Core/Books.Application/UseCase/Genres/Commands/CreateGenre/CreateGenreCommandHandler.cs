using Books.Application.Abstractions.Infrastructure;
using Books.Application.Exceptions;
using Books.Application.UseCase.Authors.Commands.CreateAuthor;
using Books.Domain;
using Books.Domain.ValueObjects;
using MitMediator;

namespace Books.Application.UseCase.Genres.Commands.CreateGenre;

/// <summary>
/// Handler for <see cref="CreateGenreCommand"/>.
/// </summary>
internal sealed class CreateGenreCommandHandler : IRequestHandler<CreateGenreCommand, Genre>
{
    private readonly IBaseRepository<Genre> _genreRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateAuthorCommandHandler"/>.
    /// </summary>
    /// <param name="genreRepository">Genre repository.</param>
    public CreateGenreCommandHandler(IBaseRepository<Genre> genreRepository)
    {
        _genreRepository = genreRepository;
    }
    
    /// <inheritdoc/>
    public async ValueTask<Genre> HandleAsync(CreateGenreCommand command, CancellationToken cancellationToken)
    {
        var isGenreExists = await _genreRepository.AnyAsync(g => g.GenreName == command.GenreName.Trim().ToUpperInvariant(), cancellationToken);
        if (isGenreExists)
        {
            throw new BadOperationException($"Genre '{command.GenreName}' already exists.");
        }
        var genre = new Genre(command.GenreName);
        await _genreRepository.AddAsync(genre, cancellationToken);
        return genre;
    }
}
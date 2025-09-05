using Books.Application.UseCase.Genres.Queries.GetGenres;
using Books.Application.UseCase.Users.Command;
using Books.Application.UseCase.Users.Command.CreateUser;
using Books.Application.UseCase.Users.Queries.GetCurrentUser;
using Books.Domain;
using Books.Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc;
using MitMediator;

namespace Books.WebApi.BasicAuthorize.Endpoints;

/// <summary>
/// Users api endpoints.
/// </summary>
public static class UsersApi
{
    private const string Tag = "users";
    
    private const string ApiUrl = "api";
    
    private const string Version = "v1";
    
    /// <summary>
    /// Use genres api endpoints.
    /// </summary>
    /// <param name="app"></param>
    /// <returns></returns>
    public static WebApplication UseUsersApi(this WebApplication app)
    {
        #region Queries
        
        app.MapGet($"{ApiUrl}/{Version}/{Tag}/current", GetCurrentUserAsync)
            .WithTags(Tag)
            .WithName("Get current user.")
            .WithGroupName(Version)
            .Produces<Genre[]>();
        
        #endregion

        #region Commands

        app.MapPost($"{ApiUrl}/{Version}/{Tag}", CreateUser)
            .WithTags(Tag)
            .WithName("Create user.")
            .WithGroupName(Version)
            .Produces<User>();
        
        #endregion

        return app;
    }
    
    private static ValueTask<User> CreateUser([FromBody] CreateUserCommand createUserCommand, [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        return mediator.SendAsync<CreateUserCommand, User>(createUserCommand, cancellationToken);
    }
    
    private static ValueTask<User> GetCurrentUserAsync([FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        return mediator.SendAsync<GetCurrentUserQuery, User>(new GetCurrentUserQuery(), cancellationToken);
    }
}
using Microsoft.Extensions.DependencyInjection;
using MitMediator.AppAuthorize.ClientMediator;
using MitMediator.AutoApi.HttpMediator;
using SimpleHttpMediatorConsoleApp;

var httpClientName = "baseHttpClient";
var serviceCollection = new ServiceCollection();
serviceCollection.AddHttpClient(httpClientName, client => { client.BaseAddress = new Uri("http://localhost:5114/"); });
serviceCollection.AddScoped(typeof(IHttpHeaderInjector<,>), typeof(AuthorizationHeaderInjection<,>));
serviceCollection.AddScoped<TokenProvider>();
serviceCollection.AddScoped<IClientMediator, HttpMediator>(c => new HttpMediator(c, null, httpClientName));

var provider = serviceCollection.BuildServiceProvider();
var httpMediator = provider.GetRequiredService<IClientMediator>();

var token = await httpMediator.GetJwtByPasswordAsync("test", "test", CancellationToken.None);
var tokenProvider = provider.GetRequiredService<TokenProvider>();
tokenProvider.Token = token;
Console.WriteLine($"UserId: {token.UserId}");
Console.WriteLine($"Access token: {token.JwtToken}");
Console.WriteLine($"Access token expires: {token.JwtTokenExpires}");
Console.WriteLine($"Refresh token: {token.RefreshToken}");
Console.WriteLine($"Refresh token expires: {token.RefreshTokenExpires}");
var isAuth = await httpMediator.SendAsync<GetAuthStatusQuery, bool>(new GetAuthStatusQuery(), CancellationToken.None);
Console.WriteLine($"Auth status: {isAuth}");

var refreshedToken = await httpMediator.GetJwtByRefreshAsync(
    token.UserId,
    token.RefreshToken ??
    throw new InvalidOperationException(
        "The server did not issue a refresh token."),
    CancellationToken.None);
Console.WriteLine($"New refresh token: {refreshedToken.RefreshToken}");
Console.WriteLine($"New refresh token expires: {refreshedToken.RefreshTokenExpires}");

using Microsoft.Extensions.DependencyInjection;
using MitMediator.AppAuthorize.ClientMediator;
using MitMediator.AutoApi.HttpMediator;
using SimpleHttpMediatorConsoleApp;

var httpClientName = "baseHttpClient";
var serviceCollection = new ServiceCollection();
serviceCollection.AddHttpClient(httpClientName, client => { client.BaseAddress = new Uri("http://localhost:5114/"); });
serviceCollection.AddScoped(typeof(IHttpHeaderInjector<,>), typeof(AuthorizationHeaderInjection<,>));
serviceCollection.AddScoped<JwtTokenProvider>();
serviceCollection.AddScoped<IClientMediator, HttpMediator>(c => new HttpMediator(c, null, httpClientName));

var provider = serviceCollection.BuildServiceProvider();
var httpMediator = provider.GetRequiredService<IClientMediator>();

var jwtToken = await httpMediator.GetJwtBearerTokenAsync("test", "test", CancellationToken.None);
var jwtTokenProvider = provider.GetRequiredService<JwtTokenProvider>();
jwtTokenProvider.JwtTokenModel = jwtToken;
Console.WriteLine($"UserId: {jwtToken.UserId}");
Console.WriteLine($"Jwt token: {jwtToken.JwtToken}");
Console.WriteLine($"Jwt token expires: {jwtToken.JwtTokenExpires}");
Console.WriteLine($"Refresh token: {jwtToken.RefreshToken}");
Console.WriteLine($"Refresh token expires: {jwtToken.RefreshTokenExpires}");
var isAuth = await httpMediator.SendAsync<GetAuthStatusQuery, bool>(new GetAuthStatusQuery(), CancellationToken.None);
Console.WriteLine($"Auth status: {isAuth}");

var jwtByRefreshToken = await httpMediator.GetJwtBearerTokenByRefreshAsync(jwtToken.UserId, jwtToken.RefreshToken, CancellationToken.None);
Console.WriteLine($"New refresh token: {jwtByRefreshToken.RefreshToken}");
Console.WriteLine($"New refresh token expires: {jwtByRefreshToken.RefreshTokenExpires}");

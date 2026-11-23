using MitMediator;
using MitMediator.AppAuthorize.ClientMediator;
using MitMediator.AutoApi.HttpMediator;

namespace SimpleHttpMediatorConsoleApp;

public class AuthorizationHeaderInjection<TRequest, TResponse>(TokenProvider tokenProvider)
    : IHttpHeaderInjector<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public ValueTask<(string, string)?> GetHeadersAsync(CancellationToken cancellationToken)
    {
        if (tokenProvider.Token is null)
        {
            return ValueTask.FromResult<(string, string)?>(null);
        }
        var result = ("Authorization", $"Bearer {tokenProvider.Token.JwtToken}");
        return ValueTask.FromResult<(string, string)?>(result);
    }
}
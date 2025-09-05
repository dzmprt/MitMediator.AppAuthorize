using MitMediator;
using MitMediator.AppAuthorize.ClientMediator;
using MitMediator.AutoApi.HttpMediator;

namespace SimpleHttpMediatorConsoleApp;

public class AuthorizationHeaderInjection<TRequest, TResponse>(JwtTokenProvider jwtTokenProvider)
    : IHttpHeaderInjector<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public ValueTask<(string, string)?> GetHeadersAsync(CancellationToken cancellationToken)
    {
        if (jwtTokenProvider.JwtTokenModel is null)
        {
            return ValueTask.FromResult<(string, string)?>(null);
        }
        var result = ("Authorization", $"Bearer {jwtTokenProvider.JwtTokenModel.JwtToken}");
        return ValueTask.FromResult<(string, string)?>(result);
    }
}
using MitMediator;
using MitMediator.AutoApi.Abstractions.Attributes;

namespace SimpleHttpMediatorConsoleApp;

[Pattern("auth-status")]
public class GetAuthStatusQuery : IRequest<bool>;
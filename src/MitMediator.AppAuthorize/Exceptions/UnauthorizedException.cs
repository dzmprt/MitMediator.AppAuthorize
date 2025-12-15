namespace MitMediator.AppAuthorize.Exceptions;

public class UnauthorizedException(string message = "Unauthorized") : Exception(message);
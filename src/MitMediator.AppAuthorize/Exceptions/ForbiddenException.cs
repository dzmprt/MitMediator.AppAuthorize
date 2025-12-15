namespace MitMediator.AppAuthorize.Exceptions;

public class ForbiddenException(string message = "Forbidden") : Exception(message);
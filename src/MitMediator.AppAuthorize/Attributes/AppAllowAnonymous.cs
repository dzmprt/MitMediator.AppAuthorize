namespace MitMediator;

/// <summary>
/// Allows anonymous access to request execution.
/// Use this attribute to mark classes or structs that
/// can be accessed without authentication.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public  class AppAllowAnonymousAttribute : Attribute;
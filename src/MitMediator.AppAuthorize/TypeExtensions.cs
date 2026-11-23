namespace MitMediator.AppAuthorize;

internal static class TypeExtensions
{
    public static IEnumerable<Type> EnumerateTypeHierarchy(this Type type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            yield return current;
        }
    }
}

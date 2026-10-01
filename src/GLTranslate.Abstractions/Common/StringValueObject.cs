namespace GLTranslate.Abstractions.Common;

/// <summary>
/// Represents a value object based on a normalized string value.
/// </summary>
public abstract class StringValueObject(string value) : ValueObject<string>(Normalize(value))
{
    private static string Normalize(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return value.Trim();
    }
}
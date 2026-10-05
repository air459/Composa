namespace Composa.Editing;

/// <summary>Names assigned to newly created content. Imported and user-entered names are never translated.</summary>
public static class DefaultNames
{
    public static Func<string, string> Translate { get; set; } = value => value;
    public static string Text(string value) => Translate(value);
}

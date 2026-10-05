using System.Reflection;
using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace Composa.App;

/// <summary>Chinese presentation strings. Model values, shortcut IDs and user text remain unchanged.</summary>
public static class L10n
{
    private sealed record Template(Regex Pattern, string Translation);
    private static readonly Dictionary<string, string> Words = new(StringComparer.Ordinal);
    private static readonly List<Template> Templates = [];
    private static readonly Regex Slot = new(@"\{(\d+)(:t)?\}");
    public static readonly HashSet<string> Missing = new(StringComparer.Ordinal);
    public static bool Audit { get; set; }
    // The upstream regression suite uses English labels. Chinese is the production default.
    public static bool Enabled { get; set; } = true;

    static L10n()
    {
        var assembly = typeof(L10n).Assembly;
        foreach (var resource in new[] { "Composa.ZhCN.Terms", "Composa.ZhCN.Prose", "Composa.ZhCN.Errors" })
        {
            using var stream = assembly.GetManifestResourceStream(resource);
            if (stream == null) throw new InvalidOperationException("Missing Chinese resource: " + resource);
            using var reader = new StreamReader(stream);
            while (reader.ReadLine() is { } line)
            {
                var tab = line.IndexOf('\t');
                if (tab < 0) continue;
                var original = line[..tab].Replace("\\n", "\n");
                var chinese = line[(tab + 1)..].Replace("\\n", "\n");
                Words[original] = chinese;
            }
        }
        foreach (var (original, translation) in Words.Where(p => Slot.IsMatch(p.Key)).OrderByDescending(p => Slot.Replace(p.Key, "").Length))
        {
            var pattern = ""; var start = 0;
            foreach (Match match in Slot.Matches(original))
            {
                pattern += Regex.Escape(original[start..match.Index]) + "(?<v" + match.Groups[1].Value + ">.*?)";
                start = match.Index + match.Length;
            }
            pattern += Regex.Escape(original[start..]);
            Templates.Add(new(new Regex("\\A" + pattern + "\\z", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100)), translation));
        }
    }

    public static string T(string? text) => Enabled ? Translate(text ?? "", 0) : text ?? "";
    public static string T(string text, string context) => Enabled && Words.TryGetValue(context + "|" + text, out var translated) ? translated : T(text);
    public static object? T(object? value) => value is string text ? T(text) : value;

    private static string Translate(string text, int depth)
    {
        if (text.Length == 0 || depth > 8) return text;
        if (Words.TryGetValue(text, out var translated)) return translated;
        if (text.EndsWith('…')) return Translate(text[..^1], depth + 1) + "…";
        var trimmed = text.Trim();
        if (trimmed != text && Words.ContainsKey(trimmed)) return text[..(text.Length - text.TrimStart().Length)] + Translate(trimmed, depth + 1) + text[text.TrimEnd().Length..];
        // Lists and operating hints are composed of independent sentences.
        if (text.Contains(" · ")) return string.Join(" · ", text.Split(" · ").Select(part => Translate(part, depth + 1)));
        foreach (var template in Templates)
        {
            var match = template.Pattern.Match(text);
            if (!match.Success) continue;
            return Slot.Replace(template.Translation, slot =>
            {
                var value = match.Groups["v" + slot.Groups[1].Value].Value;
                return slot.Groups[2].Success ? Translate(value, depth + 1) : value;
            });
        }
        if (text.Contains('\n')) return string.Join("\n", text.Split('\n').Select(part => Translate(part, depth + 1)));
        if (text.Contains(", ") && text.Split(", ").All(part => Words.ContainsKey(part)))
            return string.Join("、", text.Split(", ").Select(part => Translate(part, depth + 1)));
        if (Audit && Regex.IsMatch(text, "[A-Za-z]{3}")) Missing.Add(text);
        return text;
    }

    /// <summary>ColorView's template belongs to Avalonia. Translate labels, never editable fields or color values.</summary>
    public static void ColorLabels(ColorView view)
    {
        view.LayoutUpdated += (_, _) =>
        {
            foreach (var control in view.GetVisualDescendants())
            {
                if (control is TextBlock block && !block.GetVisualAncestors().Any(a => a is TextBox or NumericUpDown)) block.Text = T(block.Text);
                if (control is TabItem tab && tab.Header is string header) tab.Header = T(header);
                if (control is Control labeled && ToolTip.GetTip(labeled) is string tip) ToolTip.SetTip(labeled, T(tip));
            }
        };
    }
}

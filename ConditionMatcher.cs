using System.Text.RegularExpressions;

namespace WindowWatcher;

public static class ConditionMatcher
{
    public static bool IsMatch(RuleConfig rule, string ocrText)
    {
        var text = ocrText ?? "";
        var haystack = rule.IgnoreWhitespace ? RemoveWhitespace(text) : text;

        if (rule.ConditionMode == ConditionMode.Regex)
        {
            var pattern = rule.ConditionText ?? "";
            if (pattern.Length == 0)
            {
                return false;
            }

            var options = rule.IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;
            return Regex.IsMatch(haystack, pattern, options, TimeSpan.FromSeconds(2));
        }

        var terms = (rule.ConditionText ?? "")
            .Split('\n')
            .Select(t => t.Trim())
            .Where(t => t.Length > 0)
            .Select(t => rule.IgnoreWhitespace ? RemoveWhitespace(t) : t)
            .ToList();

        if (terms.Count == 0)
        {
            return false;
        }

        var comparison = rule.IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        return rule.ConditionMode switch
        {
            ConditionMode.ContainsAny => terms.Any(t => haystack.Contains(t, comparison)),
            ConditionMode.ContainsAll => terms.All(t => haystack.Contains(t, comparison)),
            ConditionMode.NotContainsAny => !terms.Any(t => haystack.Contains(t, comparison)),
            _ => false
        };
    }

    private static string RemoveWhitespace(string value) =>
        new(value.Where(c => !char.IsWhiteSpace(c)).ToArray());
}

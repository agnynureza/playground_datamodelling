using System.Text.Json;

namespace TenantService.Api.Common;

public static class RequestAuditSanitizer
{
    private const int DefaultMaxLength = 8000;
    private static readonly HashSet<string> SensitivePropertyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "token",
        "accessToken",
        "refreshToken",
        "apiKey",
        "secret",
        "authorization",
        "ssn",
        "email",
        "phone",
        "pii"
    };

    public static string? SanitizeAndTruncate(string? content, int? maxLength = null)
    {
        if (string.IsNullOrWhiteSpace(content))
            return content;

        var sanitized = TrySanitizeJson(content) ?? RedactCommonPatterns(content);
        return Truncate(sanitized, maxLength ?? DefaultMaxLength);
    }

    private static string? TrySanitizeJson(string content)
    {
        try
        {
            using var document = JsonDocument.Parse(content);
            var sanitized = SanitizeElement(document.RootElement);
            return JsonSerializer.Serialize(sanitized);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static object? SanitizeElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(
                property => property.Name,
                property => SensitivePropertyNames.Contains(property.Name)
                    ? (object?)"[REDACTED]"
                    : SanitizeElement(property.Value),
                StringComparer.OrdinalIgnoreCase),
            JsonValueKind.Array => element.EnumerateArray().Select(SanitizeElement).ToArray(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.ToString(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            _ => element.ToString()
        };
    }

    private static string RedactCommonPatterns(string content){
        var result = content;

        foreach (var propertyName in SensitivePropertyNames){
        result = System.Text.RegularExpressions.Regex.Replace(
            result,
            $@"(?i)(""?{System.Text.RegularExpressions.Regex.Escape(propertyName)}""?\s*[:=]\s*)(""[^""]*""|[^&,\s]+)",
            "$1\"[REDACTED]\"");
        }

        return result;
    }

    private static string Truncate(string content, int maxLength)
    {
        if (content.Length <= maxLength)
            return content;

        return content[..maxLength] + "...[truncated]";
    }
}
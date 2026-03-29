using System.Text.Json;
using System.Text.Json.Serialization;

namespace Accord.Web.Domain.Parser;

public class TemplateConfig
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = default!;

    [JsonPropertyName("type")]
    public string Type { get; set; } = default!;

    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    [JsonPropertyName("version")]
    public string Version { get; set; } = default!;

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("rules")]
    public Dictionary<string, RuleDefinition> Rules { get; set; } = [];

    [JsonPropertyName("variants")]
    public Dictionary<string, VariantDefinition> Variants { get; set; } = [];
}

public class RuleDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    [JsonPropertyName("type")]
    public string Type { get; set; } = default!;

    [JsonPropertyName("hint")]
    public string? Hint { get; set; }

    [JsonPropertyName("explanation")]
    public string? Explanation { get; set; }

    [JsonPropertyName("value")]
    public JsonElement? Value { get; set; }

    [JsonPropertyName("size")]
    public string Size { get; set; } = "medium";

    [JsonPropertyName("optional")]
    public bool Optional { get; set; }

    [JsonPropertyName("ignoreMissingWhenOptional")]
    public bool IgnoreMissingWhenOptional { get; set; }

    [JsonPropertyName("extra")]
    public RuleExtra? Extra { get; set; }

    public string[] GetValues() => Value?.ValueKind switch
    {
        JsonValueKind.String => [Value.Value.GetString()!],
        JsonValueKind.Array => [.. Value.Value.EnumerateArray().Select(e => e.GetString()!)],
        _ => []
    };
}

public class RuleExtra
{
    [JsonPropertyName("before")]
    public string Before { get; set; } = " ";

    [JsonPropertyName("after")]
    public string After { get; set; } = " ";
}

public class VariantDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("example")]
    public string? Example { get; set; }

    [JsonPropertyName("rules")]
    public string[] Rules { get; set; } = [];
}

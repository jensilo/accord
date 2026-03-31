namespace Accord.Web.Domain.Parser;

public record ParserMessages(
    string FieldEmpty,        // {0} = field name
    string ExpectedValue,     // {0} = expected, {1} = actual
    string ExpectedOneOf      // {0} = values list, {1} = actual
)
{
    public static readonly ParserMessages De = new(
        "Pflichtfeld \u201e{0}\u201c ist leer.",
        "Erwartet \u201e{0}\u201c, erhalten \u201e{1}\u201c.",
        "Erwartet eines von: {0}. Erhalten: \u201e{1}\u201c."
    );

    public static readonly ParserMessages En = new(
        "Required field '{0}' is empty.",
        "Expected '{0}', got '{1}'.",
        "Expected one of: {0}. Got '{1}'."
    );
}

public static class ParisParser
{
    public static ParsingResult Parse(TemplateConfig config, string variantKey, Dictionary<string, string> segments,
        ParserMessages? messages = null)
    {
        messages ??= ParserMessages.En;

        if (!config.Variants.TryGetValue(variantKey, out var variant))
            throw new InvalidOperationException($"Variant '{variantKey}' does not exist in template '{config.Id}'.");

        var result = new ParsingResult();

        foreach (var ruleKey in variant.Rules)
        {
            var rule = config.Rules[ruleKey];
            var isMissing = !segments.TryGetValue(ruleKey, out var input) || string.IsNullOrWhiteSpace(input);

            if (isMissing && rule.Optional && rule.IgnoreMissingWhenOptional)
                continue;

            if (isMissing)
            {
                var message = string.Format(messages.FieldEmpty, rule.Name);
                if (rule.Optional)
                    result.Notices.Add(new ParsingLog(ruleKey, rule.Name, ParsingLogLevel.Notice, message, Downgrade: true));
                else
                    result.Errors.Add(new ParsingLog(ruleKey, rule.Name, ParsingLogLevel.Error, message));
                continue;
            }

            var trimmed = input!.Trim();
            ParsingLog? validationError = null;

            if (rule.Type == "equals")
            {
                var expected = rule.GetValues()[0];
                if (!string.Equals(trimmed, expected, StringComparison.OrdinalIgnoreCase))
                    validationError = new ParsingLog(ruleKey, rule.Name, ParsingLogLevel.Error,
                        string.Format(messages.ExpectedValue, expected, trimmed));
            }
            else if (rule.Type == "equalsAny")
            {
                var values = rule.GetValues();
                var lower = trimmed.ToLowerInvariant();
                if (!values.Any(v => v.ToLowerInvariant() == lower))
                    validationError = new ParsingLog(ruleKey, rule.Name, ParsingLogLevel.Error,
                        string.Format(messages.ExpectedOneOf, string.Join(", ", values), trimmed));
            }

            if (validationError is not null)
            {
                if (rule.Optional)
                    result.Notices.Add(validationError with { Level = ParsingLogLevel.Notice, Downgrade = true });
                else
                    result.Errors.Add(validationError);
            }

            var before = rule.Extra?.Before ?? " ";
            var after = rule.Extra?.After ?? "";
            result.Requirement += before + trimmed + after;
        }

        result.Requirement = result.Requirement.Trim();

        return result;
    }
}

public static class DisplayType
{
    public static string For(RuleDefinition rule) => rule.Type switch
    {
        "equals" => "readonly",
        "equalsAny" => "datalist",
        _ => rule.Size is "large" or "full" ? "textarea" : "text"
    };
}

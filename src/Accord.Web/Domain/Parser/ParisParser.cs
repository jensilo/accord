namespace Accord.Web.Domain.Parser;

public static class ParisParser
{
    public static ParsingResult Parse(TemplateConfig config, string variantKey, Dictionary<string, string> segments)
    {
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
                var message = $"Required field '{rule.Name}' is empty.";
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
                        $"Expected '{expected}', got '{trimmed}'.");
            }
            else if (rule.Type == "equalsAny")
            {
                var values = rule.GetValues();
                var lower = trimmed.ToLowerInvariant();
                if (!values.Any(v => v.ToLowerInvariant() == lower))
                    validationError = new ParsingLog(ruleKey, rule.Name, ParsingLogLevel.Error,
                        $"Expected one of: {string.Join(", ", values)}. Got '{trimmed}'.");
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
        "equals" => "text",
        "equalsAny" => "select",
        _ => rule.Size is "large" or "full" ? "textarea" : "text"
    };
}

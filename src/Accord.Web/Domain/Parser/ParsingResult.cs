namespace Accord.Web.Domain.Parser;

public enum ParsingLogLevel { Error, Warning, Notice }

public record ParsingLog(string RuleName, string RuleDisplayName, ParsingLogLevel Level, string Message, bool Downgrade = false);

public class ParsingResult
{
    public List<ParsingLog> Errors { get; } = [];
    public List<ParsingLog> Warnings { get; } = [];
    public List<ParsingLog> Notices { get; } = [];
    public string Requirement { get; set; } = "";

    public bool Ok() => Errors.Count == 0;
    public bool Flawless() => Errors.Count == 0 && Warnings.Count == 0 && Notices.Count == 0;
}

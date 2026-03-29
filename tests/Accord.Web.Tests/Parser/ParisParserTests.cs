using System.Text.Json;
using Accord.Web.Domain.Parser;
using FluentAssertions;

namespace Accord.Web.Tests.Parser;

public class ParisParserTests
{
    // ---------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------

    private static TemplateConfig BasicTemplate() => new()
    {
        Id = "test-template",
        Type = "ebt",
        Name = "Test Template",
        Version = "1.0.0",
        Rules = new Dictionary<string, RuleDefinition>
        {
            ["stateVerbRule"] = new()
            {
                Name = "State Verb Rule",
                Type = "equalsAny",
                Value = JsonSerializer.SerializeToElement(new[] { "was", "will", "is" })
            },
            ["fooRule"] = new()
            {
                Name = "Foo Rule",
                Type = "equals",
                Value = JsonSerializer.SerializeToElement("foo")
            },
            ["fooPostfixRule"] = new()
            {
                Name = "Foo Postfix Rule",
                Type = "placeholder",
                Optional = true
            },
            ["optionalMissingTestRule"] = new()
            {
                Name = "Optional Missing Test Rule",
                Type = "placeholder",
                Optional = true,
                IgnoreMissingWhenOptional = true
            },
            ["optionalErrorTestRule"] = new()
            {
                Name = "Optional Empty Error Test Rule",
                Type = "equals",
                Value = JsonSerializer.SerializeToElement("foo"),
                Optional = true
            }
        },
        Variants = new Dictionary<string, VariantDefinition>
        {
            ["basicVariant"] = new()
            {
                Name = "Basic Variant",
                Description = "Matches foo",
                Rules = ["stateVerbRule", "fooRule", "fooPostfixRule", "optionalMissingTestRule", "optionalErrorTestRule"]
            }
        }
    };

    // ---------------------------------------------------------------------------
    // Equals rule
    // ---------------------------------------------------------------------------

    [Fact]
    public void EqualsRule_MatchingValue_NoErrors()
    {
        var config = BasicTemplate();
        var result = ParisParser.Parse(config, "basicVariant", new Dictionary<string, string>
        {
            ["stateVerbRule"] = "is",
            ["fooRule"] = "foo",
            ["fooPostfixRule"] = "example",
            ["optionalErrorTestRule"] = "foo"
        });

        result.Ok().Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void EqualsRule_CaseInsensitive_NoErrors()
    {
        var config = BasicTemplate();
        var result = ParisParser.Parse(config, "basicVariant", new Dictionary<string, string>
        {
            ["stateVerbRule"] = "is",
            ["fooRule"] = "FOO",
            ["fooPostfixRule"] = "example",
            ["optionalErrorTestRule"] = "FOO"
        });

        result.Ok().Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void EqualsRule_WrongValue_AddsError()
    {
        var config = BasicTemplate();
        var result = ParisParser.Parse(config, "basicVariant", new Dictionary<string, string>
        {
            ["stateVerbRule"] = "is",
            ["fooRule"] = "bar",
            ["fooPostfixRule"] = "example"
        });

        result.Ok().Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.RuleName == "fooRule");
    }

    // ---------------------------------------------------------------------------
    // EqualsAny rule
    // ---------------------------------------------------------------------------

    [Theory]
    [InlineData("was")]
    [InlineData("will")]
    [InlineData("is")]
    [InlineData("IS")]
    [InlineData("Was")]
    public void EqualsAnyRule_MatchingValue_NoErrors(string value)
    {
        var config = BasicTemplate();
        var result = ParisParser.Parse(config, "basicVariant", new Dictionary<string, string>
        {
            ["stateVerbRule"] = value,
            ["fooRule"] = "foo",
            ["fooPostfixRule"] = "example",
            ["optionalErrorTestRule"] = "foo"
        });

        result.Ok().Should().BeTrue();
    }

    [Fact]
    public void EqualsAnyRule_NoMatch_AddsError()
    {
        var config = BasicTemplate();
        var result = ParisParser.Parse(config, "basicVariant", new Dictionary<string, string>
        {
            ["stateVerbRule"] = "wrong-verb",
            ["fooRule"] = "foo",
            ["fooPostfixRule"] = "example"
        });

        result.Ok().Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.RuleName == "stateVerbRule");
    }

    // ---------------------------------------------------------------------------
    // Placeholder rule
    // ---------------------------------------------------------------------------

    [Fact]
    public void PlaceholderRule_AnyInput_NoErrors()
    {
        var config = BasicTemplate();
        var result = ParisParser.Parse(config, "basicVariant", new Dictionary<string, string>
        {
            ["stateVerbRule"] = "is",
            ["fooRule"] = "foo",
            ["fooPostfixRule"] = "anything goes here"
        });

        result.Ok().Should().BeTrue();
    }

    // ---------------------------------------------------------------------------
    // Missing fields
    // ---------------------------------------------------------------------------

    [Fact]
    public void MissingRequiredField_AddsError()
    {
        var config = BasicTemplate();
        // omit fooRule (required)
        var result = ParisParser.Parse(config, "basicVariant", new Dictionary<string, string>
        {
            ["stateVerbRule"] = "is",
            ["fooPostfixRule"] = "example"
        });

        result.Ok().Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.RuleName == "fooRule");
    }

    [Fact]
    public void MissingOptionalField_WithoutIgnoreMissing_AddsNoticeWithDowngrade()
    {
        var config = BasicTemplate();
        // fooPostfixRule is optional but IgnoreMissingWhenOptional = false
        var result = ParisParser.Parse(config, "basicVariant", new Dictionary<string, string>
        {
            ["stateVerbRule"] = "is",
            ["fooRule"] = "foo"
            // fooPostfixRule omitted
        });

        result.Ok().Should().BeTrue();
        result.Notices.Should().ContainSingle(n => n.RuleName == "fooPostfixRule" && n.Downgrade);
    }

    [Fact]
    public void MissingOptionalField_WithIgnoreMissing_ProducesNoNotice()
    {
        var config = BasicTemplate();
        // optionalMissingTestRule has IgnoreMissingWhenOptional = true
        var result = ParisParser.Parse(config, "basicVariant", new Dictionary<string, string>
        {
            ["stateVerbRule"] = "is",
            ["fooRule"] = "foo",
            ["fooPostfixRule"] = "example",
            ["optionalErrorTestRule"] = "foo"
            // optionalMissingTestRule intentionally omitted
        });

        result.Notices.Should().NotContain(n => n.RuleName == "optionalMissingTestRule");
    }

    // ---------------------------------------------------------------------------
    // Optional rule with wrong value
    // ---------------------------------------------------------------------------

    [Fact]
    public void OptionalRule_WrongValue_DowngradedToNotice()
    {
        var config = BasicTemplate();
        var result = ParisParser.Parse(config, "basicVariant", new Dictionary<string, string>
        {
            ["stateVerbRule"] = "is",
            ["fooRule"] = "foo",
            ["fooPostfixRule"] = "example",
            ["optionalErrorTestRule"] = "bar" // wrong — expects "foo"
        });

        result.Ok().Should().BeTrue();
        result.Notices.Should().ContainSingle(n => n.RuleName == "optionalErrorTestRule" && n.Downgrade);
    }

    // ---------------------------------------------------------------------------
    // Requirement string building
    // ---------------------------------------------------------------------------

    [Fact]
    public void RequirementBuilding_DefaultSpaceSeparators_CorrectString()
    {
        var config = new TemplateConfig
        {
            Id = "req-test",
            Type = "ebt",
            Name = "Req Test",
            Version = "1.0",
            Rules = new Dictionary<string, RuleDefinition>
            {
                ["word1"] = new() { Name = "Word 1", Type = "placeholder" },
                ["word2"] = new() { Name = "Word 2", Type = "placeholder" }
            },
            Variants = new Dictionary<string, VariantDefinition>
            {
                ["v"] = new() { Name = "V", Rules = ["word1", "word2"] }
            }
        };

        var result = ParisParser.Parse(config, "v", new Dictionary<string, string>
        {
            ["word1"] = "hello",
            ["word2"] = "world"
        });

        // default before=" ", after="" → " hello world", trimmed → "hello world"
        result.Requirement.Should().Be("hello world");
    }

    [Fact]
    public void RequirementBuilding_CustomExtraBeforeAfter_NoSpaces()
    {
        var config = new TemplateConfig
        {
            Id = "extra-test",
            Type = "ebt",
            Name = "Extra Test",
            Version = "1.0",
            Rules = new Dictionary<string, RuleDefinition>
            {
                ["prefix"] = new() { Name = "Prefix", Type = "placeholder" },
                ["punct"] = new()
                {
                    Name = "Punctuation",
                    Type = "equals",
                    Value = JsonSerializer.SerializeToElement("."),
                    Extra = new RuleExtra { Before = "", After = "" }
                }
            },
            Variants = new Dictionary<string, VariantDefinition>
            {
                ["v"] = new() { Name = "V", Rules = ["prefix", "punct"] }
            }
        };

        var result = ParisParser.Parse(config, "v", new Dictionary<string, string>
        {
            ["prefix"] = "Hello",
            ["punct"] = "."
        });

        result.Requirement.Should().Be("Hello.");
    }

    // ---------------------------------------------------------------------------
    // Invalid variant
    // ---------------------------------------------------------------------------

    [Fact]
    public void InvalidVariant_ThrowsInvalidOperationException()
    {
        var config = BasicTemplate();

        var act = () => ParisParser.Parse(config, "nonExistentVariant", new Dictionary<string, string>());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*nonExistentVariant*");
    }

    // ---------------------------------------------------------------------------
    // Ok() and Flawless()
    // ---------------------------------------------------------------------------

    [Fact]
    public void Ok_ReturnsFalse_WhenErrorsExist()
    {
        var config = BasicTemplate();
        var result = ParisParser.Parse(config, "basicVariant", new Dictionary<string, string>
        {
            ["stateVerbRule"] = "is",
            ["fooRule"] = "wrong"
        });

        result.Ok().Should().BeFalse();
    }

    [Fact]
    public void Flawless_ReturnsFalse_WhenNoticesExist()
    {
        var config = BasicTemplate();
        // optional error rule receives wrong value → notice
        var result = ParisParser.Parse(config, "basicVariant", new Dictionary<string, string>
        {
            ["stateVerbRule"] = "is",
            ["fooRule"] = "foo",
            ["fooPostfixRule"] = "example",
            ["optionalErrorTestRule"] = "bar"
        });

        result.Ok().Should().BeTrue();
        result.Flawless().Should().BeFalse();
    }

    [Fact]
    public void Flawless_ReturnsTrue_WhenNoErrorsOrNotices()
    {
        var config = BasicTemplate();
        var result = ParisParser.Parse(config, "basicVariant", new Dictionary<string, string>
        {
            ["stateVerbRule"] = "is",
            ["fooRule"] = "foo",
            ["fooPostfixRule"] = "example",
            ["optionalErrorTestRule"] = "foo"
        });

        result.Ok().Should().BeTrue();
        result.Flawless().Should().BeTrue();
    }

    // ---------------------------------------------------------------------------
    // Multiple errors
    // ---------------------------------------------------------------------------

    [Fact]
    public void MultipleRequiredFieldsWrong_AddsMultipleErrors()
    {
        var config = BasicTemplate();
        var result = ParisParser.Parse(config, "basicVariant", new Dictionary<string, string>
        {
            ["stateVerbRule"] = "wrong-verb",
            ["fooRule"] = "not-foo",
            ["fooPostfixRule"] = "example"
        });

        result.Ok().Should().BeFalse();
        result.Errors.Should().HaveCount(2);
    }

    // ---------------------------------------------------------------------------
    // Full ESFA integration test
    // ---------------------------------------------------------------------------

    [Fact]
    public void EsfaTemplate_BenutzeranforderungOhneBedingung_BuildsCorrectRequirement()
    {
        var json = File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..",
                "docs", "templates", "paris", "v0.6.2", "esfa.json"));

        var config = System.Text.Json.JsonSerializer.Deserialize<TemplateConfig>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

        // "Das System Libri muss dem Administrator ermöglichen, eine Auswertung der Verbrauchsdaten zu erzeugen."
        var result = ParisParser.Parse(config, "benutzeranforderung-ohne-bedingung", new Dictionary<string, string>
        {
            ["system"] = "Das System Libri",
            ["modalitaet"] = "muss",
            ["benutzer"] = "dem Administrator",
            ["ermoeglichen"] = "ermöglichen,",
            ["objektbeschreibung"] = "eine Auswertung der Verbrauchsdaten",
            ["prozessbeschreibung"] = "zu erzeugen",
            ["punkt"] = "."
            // begruendung omitted — has IgnoreMissingWhenOptional = true
        });

        result.Ok().Should().BeTrue();
        result.Requirement.Should().Be(
            "Das System Libri muss dem Administrator ermöglichen, eine Auswertung der Verbrauchsdaten zu erzeugen.");
    }

    [Fact]
    public void EsfaTemplate_ModalitaetWrong_AddsError()
    {
        var json = File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..",
                "docs", "templates", "paris", "v0.6.2", "esfa.json"));

        var config = System.Text.Json.JsonSerializer.Deserialize<TemplateConfig>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

        var result = ParisParser.Parse(config, "benutzeranforderung-ohne-bedingung", new Dictionary<string, string>
        {
            ["system"] = "Das System Libri",
            ["modalitaet"] = "könnte",   // invalid — not in equalsAny list
            ["benutzer"] = "dem Administrator",
            ["ermoeglichen"] = "ermöglichen,",
            ["prozessbeschreibung"] = "zu erzeugen",
            ["punkt"] = "."
        });

        result.Ok().Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.RuleName == "modalitaet");
    }
}

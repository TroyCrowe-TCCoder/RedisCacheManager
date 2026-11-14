using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using RedisCacheManager.Options;
using Xunit;

namespace RedisCacheManager.Tests;

/// <summary>
/// Keeps CONFIGURATION.md in step with the options class. A library may carry defaults only when they can be
/// found without reading code, so a listed default that differs from the code fails the build.
/// </summary>
public sealed class ConfigurationReferenceTests
{
    private const string ReferenceFileName = "CONFIGURATION.md";
    private const string LibraryFolder = "RedisCacheManager";
    private const string SolutionFileName = "RedisCacheManager.slnx";
    private const string ProjectFileName = "RedisCacheManager.csproj";
    private const string NoValue = "none";
    private const string Yes = "Yes";
    private const string No = "No";
    private const string Conditional = "Conditional";

    // Names that end in a secret word. A name such as AccessKeySecretName holds the name of a secret, not its value.
    private static readonly Regex SecretName = new("ConnectionString|Salt|Password|Token|ApiKey|AccessKey$|Secret$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex SecretValue = new(@"(Password|Pwd|AccountKey|SharedAccessKey|Server|Data Source)\s*=", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly string[] RequiredValues = [Yes, No, Conditional];

    private static readonly string[] SecretValues = [Yes, No];

    private sealed record OptionRow(string Default, string Required, string Secret);

    [Fact]
    public void WhenOptionNamesDifferFromCodeThenEveryDifferenceIsReported()
    {
        var documented = ReadSection();

        var inCode = ReadProperties().Select(property => property.Name).ToHashSet(StringComparer.Ordinal);
        var problems = new List<string>();
        problems.AddRange(inCode.Except(documented.Keys).Select(name => $"RedisCacheOptions.{name} is in the code but not in {ReferenceFileName}."));
        problems.AddRange(documented.Keys.Except(inCode).Select(name => $"RedisCacheOptions.{name} is in {ReferenceFileName} but not in the code."));

        Assert.Empty(problems);
    }

    [Fact]
    public void WhenDefaultDiffersFromCodeThenEveryDifferenceIsReported()
    {
        var documented = ReadSection();
        var instance = new RedisCacheOptions();

        var problems = new List<string>();
        foreach (var property in ReadProperties())
        {
            if (!documented.TryGetValue(property.Name, out var row))
            {
                continue;
            }

            var value = property.GetValue(instance);
            if (row.Required != No)
            {
                if (!IsClrDefault(value))
                {
                    problems.Add($"RedisCacheOptions.{property.Name}: a required option has the code default '{FormatDefault(value)}', but it must have none.");
                }

                continue;
            }

            var actual = FormatDefault(value);
            if (!string.Equals(row.Default, actual, StringComparison.Ordinal))
            {
                problems.Add($"RedisCacheOptions.{property.Name}: {ReferenceFileName} says '{row.Default}' but the code default is '{actual}'.");
            }
        }

        Assert.Empty(problems);
    }

    [Fact]
    public void WhenRequiredOrSecretMarkingIsInvalidThenEveryProblemIsReported()
    {
        var documented = ReadSection();

        var problems = new List<string>();
        foreach (var (name, row) in documented)
        {
            if (!RequiredValues.Contains(row.Required))
            {
                problems.Add($"RedisCacheOptions.{name}: Required must be Yes, No or Conditional.");
            }
            else if (row.Required != No && row.Default != NoValue)
            {
                problems.Add($"RedisCacheOptions.{name}: a required option cannot have a default.");
            }

            if (!SecretValues.Contains(row.Secret))
            {
                problems.Add($"RedisCacheOptions.{name}: Secret must be Yes or No.");
            }
            else if (SecretName.IsMatch(name) && row.Secret != Yes)
            {
                problems.Add($"RedisCacheOptions.{name}: the name suggests a secret, so Secret must be Yes.");
            }
        }

        Assert.Empty(problems);
    }

    [Fact]
    public void WhenProjectIsPackedThenItIncludesTheReference()
    {
        var project = File.ReadAllText(Path.Combine(RepositoryRoot(), LibraryFolder, ProjectFileName));

        Assert.Contains(ReferenceFileName, project, StringComparison.Ordinal);
    }

    [Fact]
    public void WhenReferenceContainsCredentialTextThenItIsReported()
    {
        var text = File.ReadAllText(ReferencePath());

        var match = SecretValue.Match(text);

        Assert.False(match.Success, $"{ReferenceFileName} contains '{match.Value}', which looks like a connection string or credential.");
    }

    private static List<PropertyInfo> ReadProperties() => typeof(RedisCacheOptions)
        .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
        .Where(property => property.GetMethod is not null)
        .ToList();

    private static bool IsClrDefault(object? value) => value switch
    {
        null => true,
        string text => text.Length == 0,
        _ => value.Equals(Activator.CreateInstance(value.GetType())),
    };

    private static string FormatDefault(object? value) => value switch
    {
        null => NoValue,
        string text => text.Length == 0 ? NoValue : text,
        bool flag => flag ? "true" : "false",
        TimeSpan span => FormatSpan(span),
        IFormattable number => number.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? NoValue,
    };

    private static string FormatSpan(TimeSpan span)
    {
        if (span == TimeSpan.Zero)
        {
            return "0 s";
        }

        return span < TimeSpan.FromSeconds(1)
            ? span.TotalMilliseconds.ToString(CultureInfo.InvariantCulture) + " ms"
            : span.TotalSeconds.ToString(CultureInfo.InvariantCulture) + " s";
    }

    private static Dictionary<string, OptionRow> ReadSection()
    {
        var path = ReferencePath();
        Assert.True(File.Exists(path), $"{path} does not exist.");

        var heading = $"## `{nameof(RedisCacheOptions)}`";
        var rows = new Dictionary<string, OptionRow>(StringComparer.Ordinal);
        var inSection = false;
        var found = false;

        foreach (var line in File.ReadAllLines(path))
        {
            if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                inSection = string.Equals(line.Trim(), heading, StringComparison.Ordinal);
                found |= inSection;
                continue;
            }

            if (!inSection || !line.StartsWith('|'))
            {
                continue;
            }

            var cells = line.Trim().Trim('|').Split('|').Select(cell => cell.Trim()).ToArray();
            if (cells[0] == "Option" || cells[0].StartsWith("---", StringComparison.Ordinal))
            {
                continue;
            }

            Assert.True(cells.Length == 5, $"{ReferenceFileName} row '{line}' must have 5 columns: Option, Default, Required, Secret, Meaning.");
            rows[cells[0].Trim('`')] = new OptionRow(cells[1].Trim('`'), cells[2], cells[3]);
        }

        Assert.True(found, $"{path} has no section {heading}.");
        return rows;
    }

    private static string ReferencePath() => Path.Combine(RepositoryRoot(), LibraryFolder, ReferenceFileName);

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, SolutionFileName)))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException($"{SolutionFileName} was not found above the test output folder.");
    }
}

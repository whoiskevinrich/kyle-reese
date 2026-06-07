using KyleReese.Core;
using Xunit;

namespace KyleReese.Core.Tests;

public sealed class KillListConfigTests : IDisposable
{
    private readonly string _tempPath = Path.Combine(Path.GetTempPath(), $"killlist-{Guid.NewGuid():N}.json");

    public void Dispose()
    {
        if (File.Exists(_tempPath))
        {
            File.Delete(_tempPath);
        }
    }

    [Fact]
    public void DefaultProcessNames_AreClaudeBashGitSh()
    {
        Assert.Equal(new[] { "claude", "bash", "git", "sh" }, KillListConfig.DefaultProcessNames);
    }

    [Fact]
    public void Load_MissingFile_ReturnsDefaults()
    {
        var config = KillListConfig.Load(_tempPath);

        Assert.Equal(KillListConfig.DefaultProcessNames, config.ProcessNames);
    }

    [Fact]
    public void Load_ValidFile_ReturnsConfiguredNames()
    {
        File.WriteAllText(_tempPath, """{ "processNames": ["node", "python"] }""");

        var config = KillListConfig.Load(_tempPath);

        Assert.Equal(new[] { "node", "python" }, config.ProcessNames);
    }

    [Fact]
    public void Load_MalformedJson_ReturnsDefaults()
    {
        File.WriteAllText(_tempPath, "{ this is not json ");

        var config = KillListConfig.Load(_tempPath);

        Assert.Equal(KillListConfig.DefaultProcessNames, config.ProcessNames);
    }

    [Fact]
    public void Load_EmptyList_ReturnsDefaults()
    {
        File.WriteAllText(_tempPath, """{ "processNames": [] }""");

        var config = KillListConfig.Load(_tempPath);

        Assert.Equal(KillListConfig.DefaultProcessNames, config.ProcessNames);
    }

    [Fact]
    public void Load_TrimsWhitespace_DropsEmpty_AndDedupesCaseInsensitively()
    {
        File.WriteAllText(_tempPath, """{ "processNames": ["  bash ", "", "BASH", "git"] }""");

        var config = KillListConfig.Load(_tempPath);

        Assert.Equal(new[] { "bash", "git" }, config.ProcessNames);
    }

    [Fact]
    public void SaveThenLoad_RoundTrips()
    {
        var original = new KillListConfig { ProcessNames = new List<string> { "claude", "pwsh" } };
        original.Save(_tempPath);

        var reloaded = KillListConfig.Load(_tempPath);

        Assert.Equal(new[] { "claude", "pwsh" }, reloaded.ProcessNames);
    }
}

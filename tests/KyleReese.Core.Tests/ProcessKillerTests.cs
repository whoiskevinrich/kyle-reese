using KyleReese.Core;
using Xunit;

namespace KyleReese.Core.Tests;

public sealed class ProcessKillerTests
{
    private static readonly TargetProcess[] TwoProcesses =
    {
        new(101, "claude"),
        new(202, "bash"),
    };

    [Fact]
    public void Constructor_NullProvider_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new ProcessKiller(null!));
    }

    [Fact]
    public void Find_DelegatesToProvider_AndPassesNames()
    {
        var provider = new FakeProcessProvider(TwoProcesses);
        var killer = new ProcessKiller(provider);

        var result = killer.Find(new[] { "claude", "bash" });

        Assert.Equal(TwoProcesses, result);
        Assert.Single(provider.FindCalls);
        Assert.Equal(new[] { "claude", "bash" }, provider.FindCalls[0]);
    }

    [Fact]
    public void Kill_AllSucceed_AllReportedAsKilled()
    {
        var provider = new FakeProcessProvider(TwoProcesses);
        var killer = new ProcessKiller(provider);

        var result = killer.Kill(TwoProcesses);

        Assert.Equal(2, result.KilledCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Equal(new[] { 101, 202 }, provider.KilledPids);
    }

    [Fact]
    public void Kill_SomeFail_SplitBetweenKilledAndFailed()
    {
        // Fail only PID 101.
        var provider = new FakeProcessProvider(TwoProcesses, pid => pid != 101);
        var killer = new ProcessKiller(provider);

        var result = killer.Kill(TwoProcesses);

        Assert.Equal(1, result.KilledCount);
        Assert.Equal(1, result.FailedCount);
        Assert.Equal("bash", Assert.Single(result.Killed).Name);
        Assert.Equal("claude", Assert.Single(result.Failed).Name);
    }

    [Fact]
    public void Kill_EmptyTargets_ReturnsEmptyResult()
    {
        var provider = new FakeProcessProvider(Array.Empty<TargetProcess>());
        var killer = new ProcessKiller(provider);

        var result = killer.Kill(Array.Empty<TargetProcess>());

        Assert.Equal(0, result.KilledCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Empty(provider.KilledPids);
    }
}

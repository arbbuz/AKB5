using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

public class KnowledgeBaseNaturalStringComparerTests
{
    [Fact]
    public void Compare_UsesNaturalNumericOrder()
    {
        int result = KnowledgeBaseNaturalStringComparer.Instance.Compare("Участок 2", "Участок 10");

        Assert.True(result < 0);
    }

    [Fact]
    public void Compare_IgnoresCaseForEquivalentNames()
    {
        int result = KnowledgeBaseNaturalStringComparer.Instance.Compare("насос", "НАСОС");

        Assert.Equal(0, result);
    }

    [Fact]
    public void OrderBy_SortsNamesLikeExplorer()
    {
        var names = new[] { "Участок 10", "участок 1", "Участок 2" };

        var ordered = names
            .OrderBy(static name => name, KnowledgeBaseNaturalStringComparer.Instance)
            .ToArray();

        Assert.Equal(new[] { "участок 1", "Участок 2", "Участок 10" }, ordered);
    }
}

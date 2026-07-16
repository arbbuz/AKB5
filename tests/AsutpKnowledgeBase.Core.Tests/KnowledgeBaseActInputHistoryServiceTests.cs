using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

public class KnowledgeBaseActInputHistoryServiceTests
{
    private readonly KnowledgeBaseActInputHistoryService _service = new();

    [Fact]
    public void NormalizeEntries_CollapsesWhitespaceAndKeepsMostRecentDuplicate()
    {
        List<KbActInputHistoryEntry> result = _service.NormalizeEntries(
        [
            Entry(" Цех 1 ", KbActInputHistoryField.ExecutorName, " Иванов   И.И. ", 2),
            Entry("цех 1", KbActInputHistoryField.ExecutorName, "иванов И.И.", 5),
            Entry("Цех 1", (KbActInputHistoryField)999, "Неизвестное поле", 10),
            Entry("Цех 1", KbActInputHistoryField.ExecutorPosition, "  ", 11)
        ]);

        KbActInputHistoryEntry entry = Assert.Single(result);
        Assert.Equal("цех 1", entry.WorkshopName);
        Assert.Equal("иванов И.И.", entry.DisplayValue);
        Assert.Equal("ИВАНОВ И.И.", entry.NormalizedValue);
        Assert.Equal(5, entry.UseOrder);
    }

    [Fact]
    public void AddOrTouch_MovesValueToTopWithoutCreatingDuplicate()
    {
        List<KbActInputHistoryEntry> entries =
        [
            Entry("Цех 1", KbActInputHistoryField.ExecutorPosition, "Инженер", 1),
            Entry("Цех 1", KbActInputHistoryField.ExecutorPosition, "Мастер", 2)
        ];

        List<KbActInputHistoryEntry> result = _service.AddOrTouch(
            entries,
            "Цех 1",
            KbActInputHistoryField.ExecutorPosition,
            " инженер ");

        Assert.Equal(2, result.Count);
        Assert.Equal(
            ["инженер", "Мастер"],
            _service.GetSuggestions(result, "Цех 1", KbActInputHistoryField.ExecutorPosition));
    }

    [Fact]
    public void Delete_RemovesOnlyMatchingWorkshopAndField()
    {
        List<KbActInputHistoryEntry> entries =
        [
            Entry("Цех 1", KbActInputHistoryField.ExecutorName, "Иванов", 1),
            Entry("Цех 1", KbActInputHistoryField.CustomerName, "Иванов", 2),
            Entry("Цех 2", KbActInputHistoryField.ExecutorName, "Иванов", 3)
        ];

        List<KbActInputHistoryEntry> result = _service.Delete(
            entries,
            "цех 1",
            KbActInputHistoryField.ExecutorName,
            "иванов");

        Assert.Equal(2, result.Count);
        Assert.Contains(result, entry => entry.Field == KbActInputHistoryField.CustomerName);
        Assert.Contains(result, entry => entry.WorkshopName == "Цех 2");
    }

    [Fact]
    public void RenameWorkshop_MovesOnlyMatchingWorkshopHistory()
    {
        List<KbActInputHistoryEntry> entries =
        [
            Entry("Цех 1", KbActInputHistoryField.ExecutorName, "Иванов", 1),
            Entry("Цех 1", KbActInputHistoryField.ExecutorPosition, "Инженер", 2),
            Entry("Цех 2", KbActInputHistoryField.ExecutorName, "Петров", 3)
        ];

        List<KbActInputHistoryEntry> result = _service.RenameWorkshop(
            entries,
            " цех 1 ",
            " Новый цех ");

        Assert.Equal(2, result.Count(entry => entry.WorkshopName == "Новый цех"));
        Assert.Single(result, entry => entry.WorkshopName == "Цех 2");
        Assert.DoesNotContain(result, entry =>
            string.Equals(entry.WorkshopName, "Цех 1", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DeleteWorkshop_RemovesAllFieldsOnlyForMatchingWorkshop()
    {
        List<KbActInputHistoryEntry> entries =
        [
            Entry("Цех 1", KbActInputHistoryField.ExecutorName, "Иванов", 1),
            Entry("Цех 1", KbActInputHistoryField.CustomerPosition, "Мастер", 2),
            Entry("Цех 2", KbActInputHistoryField.ExecutorName, "Иванов", 3)
        ];

        List<KbActInputHistoryEntry> result = _service.DeleteWorkshop(entries, "ЦЕХ 1");

        KbActInputHistoryEntry remaining = Assert.Single(result);
        Assert.Equal("Цех 2", remaining.WorkshopName);
    }

    [Fact]
    public void AddOrTouch_PreservesLongValueAndIgnoresEmptyValue()
    {
        string longValue = string.Join(" ", Enumerable.Repeat("длинное значение", 40));

        List<KbActInputHistoryEntry> result = _service.AddOrTouch(
            Array.Empty<KbActInputHistoryEntry>(),
            "Цех 1",
            KbActInputHistoryField.ApproverPosition,
            longValue);
        result = _service.AddOrTouch(
            result,
            "Цех 1",
            KbActInputHistoryField.ApproverPosition,
            "   ");

        Assert.Equal(longValue, Assert.Single(result).DisplayValue);
    }

    [Fact]
    public void GetSuggestions_ReturnsOnlyRequestedWorkshopAndFieldInRecentOrder()
    {
        List<KbActInputHistoryEntry> entries =
        [
            Entry("Цех 1", KbActInputHistoryField.ApproverName, "Первый", 3),
            Entry("Цех 1", KbActInputHistoryField.ApproverName, "Второй", 8),
            Entry("Цех 1", KbActInputHistoryField.ApproverPosition, "Начальник", 9),
            Entry("Цех 2", KbActInputHistoryField.ApproverName, "Третий", 10)
        ];

        IReadOnlyList<string> result = _service.GetSuggestions(
            entries,
            "ЦЕХ 1",
            KbActInputHistoryField.ApproverName);

        Assert.Equal(["Второй", "Первый"], result);
    }

    [Fact]
    public void RecordActValues_AddsExactlySixApprovedFields()
    {
        var act = new KbAct
        {
            CustomerName = "Петров П.П.",
            CustomerPosition = "Начальник участка",
            ApproverName = "Сидоров С.С.",
            ApproverPosition = "Главный инженер"
        };
        KbActExecutor[] executors =
        [
            new()
            {
                LastName = "Иванов",
                FirstName = "Иван",
                MiddleName = "Иванович",
                Position = "Инженер"
            }
        ];

        List<KbActInputHistoryEntry> result = _service.RecordActValues(
            Array.Empty<KbActInputHistoryEntry>(),
            "Цех 1",
            act,
            executors);

        Assert.Equal(6, result.Count);
        Assert.Equal(
            "Иванов Иван Иванович",
            Assert.Single(result, entry => entry.Field == KbActInputHistoryField.ExecutorName).DisplayValue);
        Assert.Equal(
            Enum.GetValues<KbActInputHistoryField>().Order(),
            result.Select(static entry => entry.Field).Order());
    }

    private static KbActInputHistoryEntry Entry(
        string workshopName,
        KbActInputHistoryField field,
        string displayValue,
        long useOrder) =>
        new()
        {
            WorkshopName = workshopName,
            Field = field,
            DisplayValue = displayValue,
            NormalizedValue = displayValue.ToUpperInvariant(),
            UseOrder = useOrder
        };
}

using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

public class KnowledgeBaseActJournalFilterServiceTests
{
    [Fact]
    public void Apply_FiltersBySingleColumn()
    {
        var service = new KnowledgeBaseActJournalFilterService();
        var filterState = new KnowledgeBaseActJournalFilterState();
        filterState.SetSelectedValues(
            KnowledgeBaseActJournalFilterColumns.Status,
            new[] { "Черновик" });

        IReadOnlyList<KnowledgeBaseActJournalRow> rows = service.Apply(
            new[]
            {
                CreateRow("act-1", status: "Черновик", documentState: "Нет"),
                CreateRow("act-2", status: "Сформирован", documentState: "Есть")
            },
            filterState);

        KnowledgeBaseActJournalRow row = Assert.Single(rows);
        Assert.Equal("act-1", row.ActId);
    }

    [Fact]
    public void Apply_CombinesMultipleColumnFilters()
    {
        var service = new KnowledgeBaseActJournalFilterService();
        var filterState = new KnowledgeBaseActJournalFilterState();
        filterState.SetSelectedValues(
            KnowledgeBaseActJournalFilterColumns.Workshop,
            new[] { "Купоросный цех" });
        filterState.SetSelectedValues(
            KnowledgeBaseActJournalFilterColumns.DocumentState,
            new[] { "Есть" });

        IReadOnlyList<KnowledgeBaseActJournalRow> rows = service.Apply(
            new[]
            {
                CreateRow("act-1", workshop: "Купоросный цех", documentState: "Нет"),
                CreateRow("act-2", workshop: "Купоросный цех", documentState: "Есть"),
                CreateRow("act-3", workshop: "Медное отделение", documentState: "Есть")
            },
            filterState);

        KnowledgeBaseActJournalRow row = Assert.Single(rows);
        Assert.Equal("act-2", row.ActId);
    }

    [Fact]
    public void Apply_WhenSelectedValuesAreEmpty_ReturnsNoRows()
    {
        var service = new KnowledgeBaseActJournalFilterService();
        var filterState = new KnowledgeBaseActJournalFilterState();
        filterState.SetSelectedValues(
            KnowledgeBaseActJournalFilterColumns.DocumentState,
            Array.Empty<string>());

        IReadOnlyList<KnowledgeBaseActJournalRow> rows = service.Apply(
            new[]
            {
                CreateRow("act-1", documentState: "Нет"),
                CreateRow("act-2", documentState: "Есть")
            },
            filterState);

        Assert.Empty(rows);
    }

    [Fact]
    public void Apply_CanExcludeCurrentColumnFilterForFilterOptions()
    {
        var service = new KnowledgeBaseActJournalFilterService();
        var filterState = new KnowledgeBaseActJournalFilterState();
        filterState.SetSelectedValues(
            KnowledgeBaseActJournalFilterColumns.Status,
            new[] { "Черновик" });
        filterState.SetSelectedValues(
            KnowledgeBaseActJournalFilterColumns.DocumentState,
            new[] { "Есть" });

        IReadOnlyList<KnowledgeBaseActJournalRow> rows = service.Apply(
            new[]
            {
                CreateRow("act-1", status: "Черновик", documentState: "Нет"),
                CreateRow("act-2", status: "Черновик", documentState: "Есть"),
                CreateRow("act-3", status: "Сформирован", documentState: "Есть")
            },
            filterState,
            excludedColumnName: KnowledgeBaseActJournalFilterColumns.DocumentState);

        Assert.Equal(new[] { "act-1", "act-2" }, rows.Select(static row => row.ActId));
    }

    [Fact]
    public void GetDistinctValues_ReturnsColumnValuesWithoutDocumentPath()
    {
        var service = new KnowledgeBaseActJournalFilterService();

        IReadOnlyList<string> values = service.GetDistinctValues(
            new[]
            {
                CreateRow("act-1", documentState: "Нет"),
                CreateRow("act-2", documentState: "Есть"),
                CreateRow("act-3", documentState: "Есть")
            },
            KnowledgeBaseActJournalFilterColumns.DocumentState);

        Assert.Equal(new[] { "Есть", "Нет" }, values);
    }

    private static KnowledgeBaseActJournalRow CreateRow(
        string actId,
        string status = "Черновик",
        string workshop = "Купоросный цех",
        string documentState = "Нет") =>
        new()
        {
            ActId = actId,
            ActDateText = "25.06.2026",
            ActNumberText = "2026-0001",
            StatusText = status,
            ActTypeText = "Отказ оборудования",
            WorkshopName = workshop,
            ObjectName = "АСУ использования конденсатов",
            EquipmentName = "SIMATIC S7-300",
            OrderNumber = "6ES7",
            DocumentPath = string.Empty,
            DocumentStateText = documentState
        };
}

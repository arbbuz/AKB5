using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

public class KnowledgeBaseActEditorServiceTests
{
    [Fact]
    public void PrepareForSave_TrimsEditableFieldsAndKeepsOrderNumberSeparateFromSerialNumber()
    {
        var service = new KnowledgeBaseActEditorService(
            clock: () => new DateTime(2026, 6, 25, 12, 30, 0));

        var result = service.PrepareForSave(CreateValidFailureAct(orderNumber: " 6ES7307-1BA00-0AA0 "));

        Assert.True(result.IsSuccess, result.ErrorMessage);
        KbAct act = Assert.IsType<KbAct>(result.Act);
        Assert.Equal("Купоросный цех (КЦ)", act.WorkshopName);
        Assert.Equal("ШКМ1", act.ObjectNameSnapshot);
        Assert.Equal("SIMATIC S7-300, PS 307, БЛОК ПИТАНИЯ", act.EquipmentName);
        Assert.Equal("6ES7307-1BA00-0AA0", act.EquipmentSnapshot.OrderNumber);
        Assert.Equal("SN-42", act.EquipmentSnapshot.SerialNumber);
        Assert.Equal("Начальник отдела", act.ApproverName);
        Assert.Equal("Начальник отдела автоматизации", act.ApproverPosition);
        Assert.Equal(new DateTime(2026, 6, 25), act.ActDate);
        Assert.Equal(new DateTime(2026, 6, 25, 12, 30, 0), act.UpdatedAt);
    }

    [Fact]
    public void PrepareForSave_WhenSerialNumberBlank_DoesNotCopyOrderNumber()
    {
        var service = new KnowledgeBaseActEditorService(
            clock: () => new DateTime(2026, 6, 25, 12, 30, 0));
        KbAct draft = CreateValidFailureAct(orderNumber: "6ES7307-1BA00-0AA0");
        draft.EquipmentSnapshot.SerialNumber = string.Empty;

        var result = service.PrepareForSave(draft);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal("6ES7307-1BA00-0AA0", result.Act!.EquipmentSnapshot.OrderNumber);
        Assert.Equal(string.Empty, result.Act.EquipmentSnapshot.SerialNumber);
    }

    [Fact]
    public void PrepareForSave_RequiresOrderNumber()
    {
        var service = new KnowledgeBaseActEditorService();
        KbAct draft = CreateValidFailureAct(orderNumber: " ");

        var result = service.PrepareForSave(draft);

        Assert.False(result.IsSuccess);
        Assert.Equal("Укажите заказной номер.", result.ErrorMessage);
    }

    [Fact]
    public void PrepareForSave_RequiresFailureDescriptionForFailureAct()
    {
        var service = new KnowledgeBaseActEditorService();
        KbAct draft = CreateValidFailureAct(orderNumber: "6ES7307-1BA00-0AA0");
        draft.FaultDescription = " ";

        var result = service.PrepareForSave(draft);

        Assert.False(result.IsSuccess);
        Assert.Equal("Укажите описание неисправности.", result.ErrorMessage);
    }

    [Fact]
    public void PrepareForSave_RequiresRequestDocument()
    {
        var service = new KnowledgeBaseActEditorService();
        KbAct draft = CreateValidFailureAct(orderNumber: "6ES7307-1BA00-0AA0");
        draft.RequestDocument = " ";

        var result = service.PrepareForSave(draft);

        Assert.False(result.IsSuccess);
        Assert.Equal("Укажите заявку или документ-основание.", result.ErrorMessage);
    }

    [Fact]
    public void PrepareForSave_RequiresFailureReasonForFailureAct()
    {
        var service = new KnowledgeBaseActEditorService();
        KbAct draft = CreateValidFailureAct(orderNumber: "6ES7307-1BA00-0AA0");
        draft.FailureReason = " ";

        var result = service.PrepareForSave(draft);

        Assert.False(result.IsSuccess);
        Assert.Equal("Укажите причину отказа.", result.ErrorMessage);
    }

    [Fact]
    public void PrepareForSave_RequiresFaultCriterionForFailureAct()
    {
        var service = new KnowledgeBaseActEditorService();
        KbAct draft = CreateValidFailureAct(orderNumber: "6ES7307-1BA00-0AA0");
        draft.FaultCriterion = " ";

        var result = service.PrepareForSave(draft);

        Assert.False(result.IsSuccess);
        Assert.Equal("Укажите критерий неисправности.", result.ErrorMessage);
    }

    [Fact]
    public void PrepareForSave_DoesNotRequireInspectionResultForFailureAct()
    {
        var service = new KnowledgeBaseActEditorService();
        KbAct draft = CreateValidFailureAct(orderNumber: "6ES7307-1BA00-0AA0");
        draft.InspectionResult = " ";

        var result = service.PrepareForSave(draft);

        Assert.True(result.IsSuccess, result.ErrorMessage);
    }

    [Fact]
    public void PrepareForSave_RequiresCustomerPosition()
    {
        var service = new KnowledgeBaseActEditorService();
        KbAct draft = CreateValidFailureAct(orderNumber: "6ES7307-1BA00-0AA0");
        draft.CustomerPosition = " ";

        var result = service.PrepareForSave(draft);

        Assert.False(result.IsSuccess);
        Assert.Equal("Укажите должность представителя цеха.", result.ErrorMessage);
    }

    [Fact]
    public void PrepareForSave_RequiresApproverName()
    {
        var service = new KnowledgeBaseActEditorService();
        KbAct draft = CreateValidFailureAct(orderNumber: "6ES7307-1BA00-0AA0");
        draft.ApproverName = " ";

        var result = service.PrepareForSave(draft);

        Assert.False(result.IsSuccess);
        Assert.Equal("Укажите утверждающего.", result.ErrorMessage);
    }

    [Fact]
    public void PrepareForSave_RequiresApproverPosition()
    {
        var service = new KnowledgeBaseActEditorService();
        KbAct draft = CreateValidFailureAct(orderNumber: "6ES7307-1BA00-0AA0");
        draft.ApproverPosition = " ";

        var result = service.PrepareForSave(draft);

        Assert.False(result.IsSuccess);
        Assert.Equal("Укажите должность утверждающего.", result.ErrorMessage);
    }

    [Fact]
    public void ValidateExecutorsForSave_RequiresExecutor()
    {
        string? result = KnowledgeBaseActEditorService.ValidateExecutorsForSave(Array.Empty<KbActExecutor>());

        Assert.Equal("Укажите исполнителя.", result);
    }

    [Fact]
    public void ValidateExecutorsForSave_RequiresExecutorPosition()
    {
        string? result = KnowledgeBaseActEditorService.ValidateExecutorsForSave(
            new[]
            {
                new KbActExecutor
                {
                    ActId = "act-1",
                    LastName = "Иванов",
                    Position = " "
                }
            });

        Assert.Equal("Укажите должность исполнителя.", result);
    }

    [Fact]
    public void ValidateExecutorsForSave_AcceptsNamedExecutorWithPosition()
    {
        string? result = KnowledgeBaseActEditorService.ValidateExecutorsForSave(
            new[]
            {
                new KbActExecutor
                {
                    ActId = "act-1",
                    LastName = "Иванов",
                    Position = "инженер-электроник ОА БСО АСУ ТП УИТиА"
                }
            });

        Assert.Null(result);
    }

    [Fact]
    public void PrepareForSave_RequiresInspectionResultForInspectionAct()
    {
        var service = new KnowledgeBaseActEditorService();
        KbAct draft = CreateValidFailureAct(orderNumber: "6ES7307-1BA00-0AA0");
        draft.ActType = KbActType.InspectionWork;
        draft.FaultDescription = string.Empty;
        draft.InspectionResult = " ";

        var result = service.PrepareForSave(draft);

        Assert.False(result.IsSuccess);
        Assert.Equal("Укажите результат осмотра.", result.ErrorMessage);
    }

    [Fact]
    public void PrepareForSave_DoesNotRequireFailureFieldsForInspectionAct()
    {
        var service = new KnowledgeBaseActEditorService();
        KbAct draft = CreateValidFailureAct(orderNumber: "6ES7307-1BA00-0AA0");
        draft.ActType = KbActType.InspectionWork;
        draft.FaultDescription = " ";
        draft.FailureReason = " ";

        var result = service.PrepareForSave(draft);

        Assert.True(result.IsSuccess, result.ErrorMessage);
    }

    private static KbAct CreateValidFailureAct(string orderNumber) =>
        new()
        {
            ActId = "act-1",
            ActType = KbActType.EquipmentFailure,
            Status = KbActStatus.Draft,
            ActDate = new DateTime(2026, 6, 25),
            WorkshopName = " Купоросный цех (КЦ) ",
            ObjectNameSnapshot = " ШКМ1 ",
            Lvl3NodeId = "lvl3-1",
            Lvl3NameSnapshot = "Шкаф",
            ObjectPathSnapshot = "КЦ / ШКМ1 / Шкаф",
            CompositionEntryId = "entry-1",
            EquipmentName = " SIMATIC S7-300, PS 307, БЛОК ПИТАНИЯ ",
            EquipmentSnapshot = new KbActEquipmentSnapshot
            {
                CompositionEntryId = "entry-1",
                OrderNumber = orderNumber,
                SerialNumber = " SN-42 "
            },
            FailureDate = new DateTime(2026, 6, 25),
            FaultDescription = " Нет питания ",
            FailureReason = " Причина уточняется ",
            InspectionResult = " Осмотр выполнен ",
            FaultCriterion = " Невозможность эксплуатации ",
            RequestDocument = " Заявка N 1 ",
            ActualLaborHours = " 2 ",
            CustomerName = " Представитель цеха ",
            CustomerPosition = " Начальник участка ЭнЦ ",
            ApproverName = " Начальник отдела ",
            ApproverPosition = " Начальник отдела автоматизации ",
            CreatedAt = new DateTime(2026, 6, 25, 10, 0, 0)
        };
}

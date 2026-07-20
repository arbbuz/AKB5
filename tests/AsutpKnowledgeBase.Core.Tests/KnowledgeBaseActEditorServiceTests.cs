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
        Assert.Equal("Нет питания", act.FaultDescription);
        Assert.Equal("Причина уточняется", act.FailureReason);
        Assert.Equal("Невозможность эксплуатации", act.FaultCriterion);
        Assert.Equal(string.Empty, act.ApproverName);
        Assert.Equal(string.Empty, act.ApproverPosition);
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
    public void PrepareForSave_InspectionActClearsEquipmentAndOrderNumber()
    {
        var service = new KnowledgeBaseActEditorService();
        KbAct draft = CreateValidFailureAct(orderNumber: "6ES7307-1BA00-0AA0");
        draft.ActType = KbActType.InspectionWork;
        draft.FailureDate = null;
        draft.FaultDescription = string.Empty;
        draft.FailureReason = string.Empty;
        draft.FaultCriterion = string.Empty;
        draft.InspectionResult = "Работы выполнены в полном объеме.";

        var result = service.PrepareForSave(draft);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal(string.Empty, result.Act!.EquipmentName);
        Assert.Equal(string.Empty, result.Act.EquipmentSnapshot.OrderNumber);
    }

    [Fact]
    public void PrepareForSave_InspectionActDoesNotRequireCompositionEntry()
    {
        var service = new KnowledgeBaseActEditorService();
        KbAct draft = CreateValidFailureAct(orderNumber: "6ES7307-1BA00-0AA0");
        draft.ActType = KbActType.InspectionWork;
        draft.CompositionEntryId = string.Empty;
        draft.EquipmentSnapshot.CompositionEntryId = string.Empty;

        var result = service.PrepareForSave(draft);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal(string.Empty, result.Act!.CompositionEntryId);
    }

    [Fact]
    public void PrepareForSave_FailureActRequiresCompositionEntry()
    {
        var service = new KnowledgeBaseActEditorService();
        KbAct draft = CreateValidFailureAct(orderNumber: "6ES7307-1BA00-0AA0");
        draft.CompositionEntryId = string.Empty;

        var result = service.PrepareForSave(draft);

        Assert.False(result.IsSuccess);
        Assert.Equal("Не удалось связать акт со строкой состава.", result.ErrorMessage);
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
    public void PrepareForSave_RequiresRequestDocumentForInspectionAct()
    {
        var service = new KnowledgeBaseActEditorService();
        KbAct draft = CreateValidFailureAct(orderNumber: "6ES7307-1BA00-0AA0");
        draft.ActType = KbActType.InspectionWork;
        draft.RequestDocument = " ";

        var result = service.PrepareForSave(draft);

        Assert.False(result.IsSuccess);
        Assert.Equal("Укажите заявку или документ-основание.", result.ErrorMessage);
    }

    [Fact]
    public void PrepareForSave_RequiresActualLaborHoursForInspectionAct()
    {
        var service = new KnowledgeBaseActEditorService();
        KbAct draft = CreateValidFailureAct(orderNumber: "6ES7307-1BA00-0AA0");
        draft.ActType = KbActType.InspectionWork;
        draft.ActualLaborHours = " ";

        var result = service.PrepareForSave(draft);

        Assert.False(result.IsSuccess);
        Assert.Equal("Укажите трудозатраты.", result.ErrorMessage);
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
    public void PrepareForSave_DoesNotRequireInspectionOnlyFieldsForFailureAct()
    {
        var service = new KnowledgeBaseActEditorService();
        KbAct draft = CreateValidFailureAct(orderNumber: "6ES7307-1BA00-0AA0");

        var result = service.PrepareForSave(draft);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal(string.Empty, result.Act!.InspectionResult);
        Assert.Equal(string.Empty, result.Act.RequestDocument);
        Assert.Equal(string.Empty, result.Act.ActualLaborHours);
        Assert.Equal(string.Empty, result.Act.CustomerName);
        Assert.Equal(string.Empty, result.Act.CustomerPosition);
        Assert.Equal(string.Empty, result.Act.ApproverName);
        Assert.Equal(string.Empty, result.Act.ApproverPosition);
    }

    [Fact]
    public void PrepareForSave_RequiresCustomerNameForInspectionAct()
    {
        var service = new KnowledgeBaseActEditorService();
        KbAct draft = CreateValidFailureAct(orderNumber: "6ES7307-1BA00-0AA0");
        draft.ActType = KbActType.InspectionWork;
        draft.CustomerName = " ";

        var result = service.PrepareForSave(draft);

        Assert.False(result.IsSuccess);
        Assert.Equal("Укажите представителя цеха.", result.ErrorMessage);
    }

    [Fact]
    public void PrepareForSave_RequiresCustomerPositionForInspectionAct()
    {
        var service = new KnowledgeBaseActEditorService();
        KbAct draft = CreateValidFailureAct(orderNumber: "6ES7307-1BA00-0AA0");
        draft.ActType = KbActType.InspectionWork;
        draft.CustomerPosition = " ";

        var result = service.PrepareForSave(draft);

        Assert.False(result.IsSuccess);
        Assert.Equal("Укажите должность представителя цеха.", result.ErrorMessage);
    }

    [Fact]
    public void PrepareForSave_RequiresApproverNameForInspectionAct()
    {
        var service = new KnowledgeBaseActEditorService();
        KbAct draft = CreateValidFailureAct(orderNumber: "6ES7307-1BA00-0AA0");
        draft.ActType = KbActType.InspectionWork;
        draft.ApproverName = " ";

        var result = service.PrepareForSave(draft);

        Assert.False(result.IsSuccess);
        Assert.Equal("Укажите утверждающего.", result.ErrorMessage);
    }

    [Fact]
    public void PrepareForSave_RequiresApproverPositionForInspectionAct()
    {
        var service = new KnowledgeBaseActEditorService();
        KbAct draft = CreateValidFailureAct(orderNumber: "6ES7307-1BA00-0AA0");
        draft.ActType = KbActType.InspectionWork;
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

        var result = service.PrepareForSave(draft);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Null(result.Act!.FailureDate);
        Assert.Equal(string.Empty, result.Act.FaultDescription);
        Assert.Equal(string.Empty, result.Act.FailureReason);
        Assert.Equal(string.Empty, result.Act.FaultCriterion);
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

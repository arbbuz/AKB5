using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public sealed class KnowledgeBaseActEditorSaveResult
    {
        public bool IsSuccess { get; init; }

        public string ErrorMessage { get; init; } = string.Empty;

        public KbAct? Act { get; init; }
    }

    public sealed class KnowledgeBaseActEditorService
    {
        private readonly Func<DateTime> _clock;

        public KnowledgeBaseActEditorService(Func<DateTime>? clock = null)
        {
            _clock = clock ?? (() => DateTime.Now);
        }

        public KnowledgeBaseActEditorSaveResult PrepareForSave(KbAct? act)
        {
            if (act == null)
                return Failure("Не переданы данные акта.");

            KbAct candidate = CloneAct(act);
            if (candidate.ActDate.HasValue)
            {
                candidate.ActDate = candidate.ActDate.Value.Date;
                candidate.ActYear = candidate.ActDate.Value.Year;
            }

            if (candidate.FailureDate.HasValue)
                candidate.FailureDate = candidate.FailureDate.Value.Date;

            DateTime now = _clock();
            candidate.CreatedAt ??= now;
            candidate.UpdatedAt = now;

            KbAct normalized = KnowledgeBaseDataService.NormalizeActs(new[] { candidate }).Single();
            string? validationError = ValidateForSave(normalized);
            if (validationError != null)
                return Failure(validationError);

            return new KnowledgeBaseActEditorSaveResult
            {
                IsSuccess = true,
                Act = normalized
            };
        }

        public static string? ValidateForSave(KbAct? act)
        {
            if (act == null)
                return "Не переданы данные акта.";

            if (!act.ActDate.HasValue)
                return "Укажите дату акта.";

            if (string.IsNullOrWhiteSpace(act.WorkshopName))
                return "Укажите цех.";

            if (string.IsNullOrWhiteSpace(act.ObjectNameSnapshot))
                return "Укажите объект.";

            if (string.IsNullOrWhiteSpace(act.EquipmentName))
                return "Укажите оборудование.";

            if (act.EquipmentSnapshot == null ||
                string.IsNullOrWhiteSpace(act.EquipmentSnapshot.OrderNumber))
            {
                return "Укажите заказной номер.";
            }

            if (string.IsNullOrWhiteSpace(act.CompositionEntryId))
                return "Не удалось связать акт со строкой состава.";

            if (string.IsNullOrWhiteSpace(act.RequestDocument))
                return "Укажите заявку или документ-основание.";

            if (string.IsNullOrWhiteSpace(act.ActualLaborHours))
                return "Укажите трудозатраты.";

            if (string.IsNullOrWhiteSpace(act.CustomerName))
                return "Укажите представителя цеха.";

            if (string.IsNullOrWhiteSpace(act.CustomerPosition))
                return "Укажите должность представителя цеха.";

            if (string.IsNullOrWhiteSpace(act.ApproverName))
                return "Укажите утверждающего.";

            if (string.IsNullOrWhiteSpace(act.ApproverPosition))
                return "Укажите должность утверждающего.";

            if (act.ActType == KbActType.EquipmentFailure &&
                !act.FailureDate.HasValue)
            {
                return "Укажите дату отказа.";
            }

            if (act.ActType == KbActType.EquipmentFailure)
            {
                if (string.IsNullOrWhiteSpace(act.FaultDescription))
                    return "Укажите описание неисправности.";

                if (string.IsNullOrWhiteSpace(act.FailureReason))
                    return "Укажите причину отказа.";

                if (string.IsNullOrWhiteSpace(act.FaultCriterion))
                    return "Укажите критерий неисправности.";
            }

            if (act.ActType == KbActType.InspectionWork &&
                string.IsNullOrWhiteSpace(act.InspectionResult))
            {
                return "Укажите результат осмотра.";
            }

            return null;
        }

        public static string? ValidateExecutorsForSave(IEnumerable<KbActExecutor>? executors)
        {
            List<KbActExecutor> normalized = KnowledgeBaseDataService.NormalizeActExecutors(executors);
            if (normalized.Count == 0)
                return "Укажите исполнителя.";

            foreach (KbActExecutor executor in normalized)
            {
                bool hasName = !string.IsNullOrWhiteSpace(executor.LastName) ||
                    !string.IsNullOrWhiteSpace(executor.FirstName) ||
                    !string.IsNullOrWhiteSpace(executor.MiddleName);
                if (!hasName)
                    return "Укажите исполнителя.";

                if (string.IsNullOrWhiteSpace(executor.Position))
                    return "Укажите должность исполнителя.";
            }

            return null;
        }

        public static KbAct CloneAct(KbAct act) =>
            new()
            {
                ActId = act.ActId,
                ActYear = act.ActYear,
                ActNumber = act.ActNumber,
                ActType = act.ActType,
                Status = act.Status,
                SignedAt = act.SignedAt,
                StatusHistory = act.StatusHistory
                    ?.Select(change => new KbActStatusChange
                    {
                        ChangeId = change.ChangeId,
                        PreviousStatus = change.PreviousStatus,
                        NewStatus = change.NewStatus,
                        ChangedAt = change.ChangedAt,
                        ChangedBy = change.ChangedBy
                    })
                    .ToList() ?? new List<KbActStatusChange>(),
                ActDate = act.ActDate,
                WorkshopName = act.WorkshopName,
                Lvl3NodeId = act.Lvl3NodeId,
                Lvl3NameSnapshot = act.Lvl3NameSnapshot,
                ObjectNameSnapshot = act.ObjectNameSnapshot,
                ObjectPathSnapshot = act.ObjectPathSnapshot,
                RackId = act.RackId,
                RackNumberSnapshot = act.RackNumberSnapshot,
                RackNameSnapshot = act.RackNameSnapshot,
                CompositionEntryId = act.CompositionEntryId,
                EquipmentName = act.EquipmentName,
                EquipmentSnapshot = CloneEquipmentSnapshot(act.EquipmentSnapshot),
                FailureDate = act.FailureDate,
                FaultDescription = act.FaultDescription,
                FailureReason = act.FailureReason,
                InspectionResult = act.InspectionResult,
                FaultCriterion = act.FaultCriterion,
                RequestDocument = act.RequestDocument,
                ActualLaborHours = act.ActualLaborHours,
                CustomerName = act.CustomerName,
                CustomerPosition = act.CustomerPosition,
                ApproverName = act.ApproverName,
                ApproverPosition = act.ApproverPosition,
                CreatedBy = act.CreatedBy,
                CreatedAt = act.CreatedAt,
                UpdatedAt = act.UpdatedAt
            };

        private static KbActEquipmentSnapshot CloneEquipmentSnapshot(KbActEquipmentSnapshot? snapshot)
        {
            if (snapshot == null)
                return new KbActEquipmentSnapshot();

            return new KbActEquipmentSnapshot
            {
                Lvl3Name = snapshot.Lvl3Name,
                ObjectPath = snapshot.ObjectPath,
                RackId = snapshot.RackId,
                RackNumber = snapshot.RackNumber,
                RackName = snapshot.RackName,
                CompositionEntryId = snapshot.CompositionEntryId,
                ComponentType = snapshot.ComponentType,
                Model = snapshot.Model,
                OrderNumber = snapshot.OrderNumber,
                SerialNumber = snapshot.SerialNumber,
                Notes = snapshot.Notes
            };
        }

        private static KnowledgeBaseActEditorSaveResult Failure(string errorMessage) =>
            new()
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
    }
}

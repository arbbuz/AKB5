using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public sealed class KnowledgeBaseActStatusChangeRequest
    {
        public KbAct? Act { get; init; }

        public KbActStatus NewStatus { get; init; }

        public string ChangedBy { get; init; } = string.Empty;

        public DateTime ChangedAt { get; init; }

        public DateTime? SignedAt { get; init; }

    }

    public sealed class KnowledgeBaseActStatusChangeResult
    {
        public bool IsSuccess { get; init; }

        public string ErrorMessage { get; init; } = string.Empty;

        public KbAct? Act { get; init; }
    }

    public sealed class KnowledgeBaseActStatusService
    {
        public static bool CanEdit(KbActStatus status) =>
            status is KbActStatus.Draft or KbActStatus.Generated;

        public static bool CanGenerateDocument(KbActStatus status) =>
            status is KbActStatus.Draft or KbActStatus.Generated;

        public static bool CanSign(KbActStatus status) => status == KbActStatus.Generated;

        public static bool CanCancel(KbActStatus status) =>
            status is KbActStatus.Draft or KbActStatus.Generated;

        public KnowledgeBaseActStatusChangeResult PrepareStatusChange(
            KnowledgeBaseActStatusChangeRequest? request)
        {
            if (request?.Act == null)
                return Failure("Акт не найден.");

            KbAct current = request.Act;
            if (!CanChangeStatus(current.Status, request.NewStatus))
                return Failure("Для текущего статуса это действие недоступно.");

            string changedBy = request.ChangedBy?.Trim() ?? string.Empty;
            DateTime changedAt = request.ChangedAt == default ? DateTime.Now : request.ChangedAt;
            KbAct updated = KnowledgeBaseActEditorService.CloneAct(current);
            updated.Status = request.NewStatus;
            updated.UpdatedAt = changedAt;

            if (request.NewStatus == KbActStatus.Signed)
            {
                if (!request.SignedAt.HasValue)
                    return Failure("Укажите дату подписания.");

                updated.SignedAt = request.SignedAt.Value.Date;
            }

            updated.StatusHistory.Add(new KbActStatusChange
            {
                ChangeId = Guid.NewGuid().ToString("N"),
                PreviousStatus = current.Status,
                NewStatus = request.NewStatus,
                ChangedAt = changedAt,
                ChangedBy = changedBy
            });

            return new KnowledgeBaseActStatusChangeResult
            {
                IsSuccess = true,
                Act = updated
            };
        }

        private static bool CanChangeStatus(KbActStatus current, KbActStatus next) =>
            (current == KbActStatus.Draft &&
                (next == KbActStatus.Generated || next == KbActStatus.Cancelled)) ||
            (current == KbActStatus.Generated &&
                (next == KbActStatus.Signed || next == KbActStatus.Cancelled));

        private static KnowledgeBaseActStatusChangeResult Failure(string errorMessage) =>
            new()
            {
                ErrorMessage = errorMessage
            };
    }
}

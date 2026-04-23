using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.UiServices
{
    public class KnowledgeBaseWorkshopUiWorkflowContext
    {
        public IWin32Window Owner { get; init; } = null!;

        public Func<List<KbNode>> GetPersistedTreeData { get; init; } = null!;

        public Action<KnowledgeBaseSessionViewState> ApplySessionView { get; init; } = null!;

        public Action RefreshSearchAfterMutation { get; init; } = null!;

        public Action UpdateDirtyState { get; init; } = null!;

        public Action UpdateUi { get; init; } = null!;

        public Action<string> SetStatusText { get; init; } = null!;
    }

    /// <summary>
    /// Координирует WinForms-специфичные screen-level сценарии
    /// по переключению/добавлению/переименованию/удалению цехов.
    /// </summary>
    public class KnowledgeBaseWorkshopUiWorkflowService
    {
        private readonly KnowledgeBaseSessionService _session;
        private readonly KnowledgeBaseSessionWorkflowService _sessionWorkflowService;
        private readonly UndoRedoService _history;

        public KnowledgeBaseWorkshopUiWorkflowService(
            KnowledgeBaseSessionService session,
            KnowledgeBaseSessionWorkflowService sessionWorkflowService,
            UndoRedoService history)
        {
            _session = session;
            _sessionWorkflowService = sessionWorkflowService;
            _history = history;
        }

        public void SelectWorkshop(KnowledgeBaseWorkshopUiWorkflowContext context, string? selectedWorkshop)
        {
            if (string.IsNullOrWhiteSpace(selectedWorkshop))
                return;

            var switchResult = _sessionWorkflowService.SelectWorkshop(
                selectedWorkshop,
                context.GetPersistedTreeData());

            if (!switchResult.IsSuccess)
                return;

            context.ApplySessionView(switchResult.ViewState);
            context.RefreshSearchAfterMutation();
            context.UpdateDirtyState();
            context.UpdateUi();
        }

        public void AddWorkshop(KnowledgeBaseWorkshopUiWorkflowContext context)
        {
            using var dialog = new InputDialog("Введите название нового цеха:");
            if (dialog.ShowDialog(context.Owner) != DialogResult.OK || string.IsNullOrWhiteSpace(dialog.Result))
                return;

            var currentRoots = context.GetPersistedTreeData();
            string historySnapshot = _session.SerializeSnapshot(currentRoots, includeCurrentWorkshop: true);
            var addResult = _sessionWorkflowService.AddWorkshop(dialog.Result.Trim(), currentRoots);

            if (!addResult.IsSuccess)
            {
                ShowWorkshopFailure(context.Owner, addResult, "Ошибка");
                return;
            }

            _history.SaveState(historySnapshot);
            context.ApplySessionView(addResult.ViewState);
            context.RefreshSearchAfterMutation();
            context.UpdateDirtyState();
            context.UpdateUi();
            context.SetStatusText($"🏭 Добавлен цех: {addResult.ViewState.CurrentWorkshop}");
        }

        public void RenameCurrentWorkshop(KnowledgeBaseWorkshopUiWorkflowContext context)
        {
            string currentWorkshop = _session.CurrentWorkshop;
            if (string.IsNullOrWhiteSpace(currentWorkshop))
            {
                MessageBox.Show(
                    context.Owner,
                    "Нет выбранного цеха для переименования.",
                    "Внимание",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            using var dialog = new InputDialog("Введите новое название цеха:", currentWorkshop);
            if (dialog.ShowDialog(context.Owner) != DialogResult.OK || string.IsNullOrWhiteSpace(dialog.Result))
                return;

            string normalizedWorkshop = dialog.Result.Trim();
            if (MessageBox.Show(
                    context.Owner,
                    $"Переименовать цех '{currentWorkshop}' в '{normalizedWorkshop}'?",
                    "Подтверждение",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            var currentRoots = context.GetPersistedTreeData();
            string historySnapshot = _session.SerializeSnapshot(currentRoots, includeCurrentWorkshop: true);
            var renameResult = _sessionWorkflowService.RenameCurrentWorkshop(normalizedWorkshop, currentRoots);
            if (!renameResult.IsSuccess)
            {
                ShowWorkshopFailure(context.Owner, renameResult, "Переименование цеха");
                return;
            }

            _history.SaveState(historySnapshot);
            context.ApplySessionView(renameResult.ViewState);
            context.RefreshSearchAfterMutation();
            context.UpdateDirtyState();
            context.UpdateUi();
            context.SetStatusText(
                $"🏭 Цех переименован: {currentWorkshop} → {renameResult.ViewState.CurrentWorkshop}");
        }

        public void DeleteCurrentWorkshop(KnowledgeBaseWorkshopUiWorkflowContext context)
        {
            string currentWorkshop = _session.CurrentWorkshop;
            if (string.IsNullOrWhiteSpace(currentWorkshop))
            {
                MessageBox.Show(
                    context.Owner,
                    "Нет выбранного цеха для удаления.",
                    "Внимание",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show(
                    context.Owner,
                    $"Удалить цех '{currentWorkshop}' и все его объекты?",
                    "Подтверждение",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }

            var currentRoots = context.GetPersistedTreeData();
            string historySnapshot = _session.SerializeSnapshot(currentRoots, includeCurrentWorkshop: true);
            var deleteResult = _sessionWorkflowService.DeleteCurrentWorkshop(currentRoots);
            if (!deleteResult.IsSuccess)
            {
                ShowWorkshopFailure(context.Owner, deleteResult, "Удаление цеха");
                return;
            }

            _history.SaveState(historySnapshot);
            context.ApplySessionView(deleteResult.ViewState);
            context.RefreshSearchAfterMutation();
            context.UpdateDirtyState();
            context.UpdateUi();
            context.SetStatusText($"🗑 Удален цех: {currentWorkshop}");
        }

        private static void ShowWorkshopFailure(
            IWin32Window owner,
            KnowledgeBaseSessionTransitionResult result,
            string title)
        {
            if (string.IsNullOrWhiteSpace(result.ErrorMessage))
                return;

            MessageBox.Show(
                owner,
                result.ErrorMessage,
                title,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }
}

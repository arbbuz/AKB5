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
    /// по переключению/добавлению цехов и настройке уровней.
    /// </summary>
    public class KnowledgeBaseWorkshopUiWorkflowService
    {
        private readonly KnowledgeBaseSessionService _session;
        private readonly KnowledgeBaseSessionWorkflowService _sessionWorkflowService;
        private readonly KnowledgeBaseConfigurationWorkflowService _configurationWorkflowService;
        private readonly UndoRedoService _history;

        public KnowledgeBaseWorkshopUiWorkflowService(
            KnowledgeBaseSessionService session,
            KnowledgeBaseSessionWorkflowService sessionWorkflowService,
            KnowledgeBaseConfigurationWorkflowService configurationWorkflowService,
            UndoRedoService history)
        {
            _session = session;
            _sessionWorkflowService = sessionWorkflowService;
            _configurationWorkflowService = configurationWorkflowService;
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
                if (!string.IsNullOrWhiteSpace(addResult.ErrorMessage))
                {
                    MessageBox.Show(
                        context.Owner,
                        addResult.ErrorMessage,
                        "Ошибка",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }

                return;
            }

            _history.SaveState(historySnapshot);
            context.ApplySessionView(addResult.ViewState);
            context.RefreshSearchAfterMutation();
            context.UpdateDirtyState();
            context.UpdateUi();
            context.SetStatusText($"🏭 Добавлен цех: {addResult.ViewState.CurrentWorkshop}");
        }

        public void ConfigureLevels(KnowledgeBaseWorkshopUiWorkflowContext context)
        {
            using var setup = new SetupForm(_session.Config);
            if (setup.ShowDialog(context.Owner) != DialogResult.OK)
                return;

            var updateResult = _configurationWorkflowService.ValidateAndNormalize(setup.Config, _session.Workshops);
            if (!updateResult.IsSuccess)
            {
                MessageBox.Show(
                    context.Owner,
                    updateResult.ErrorMessage,
                    "Некорректная конфигурация",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            string historySnapshot = _session.SerializeSnapshot(
                context.GetPersistedTreeData(),
                includeCurrentWorkshop: true);

            _session.UpdateConfig(updateResult.Config);
            _history.SaveState(historySnapshot);
            context.UpdateDirtyState();
            context.UpdateUi();
            context.SetStatusText($"💡 Уровни: {string.Join(" → ", _session.Config.LevelNames)}");
        }
    }
}

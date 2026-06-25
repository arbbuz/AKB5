using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase
{
    public partial class MainForm
    {
        private void CreateActFromSelectedCompositionEntry(object? sender, EventArgs e)
        {
            if (!TryGetCompositionParentNode(out var parentNode))
                return;

            var selectedEntry = FindSelectedCompositionEntry(
                parentNode,
                selectedNodeCompositionScreen.SelectedEntryId,
                requireSlotted: true);
            if (selectedEntry == null)
            {
                MessageBox.Show(
                    this,
                    "Выберите строку оборудования в составе.",
                    "Акт",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            var result = _actDraftService.CreateDraft(new KnowledgeBaseActDraftRequest
            {
                Lvl3Node = parentNode,
                WorkshopRoots = GetVisibleTreeData(),
                WorkshopName = _currentWorkshop,
                VisibleLevel = GetVisibleLevelForNode(parentNode),
                Rack = BuildSelectedRackDraft(parentNode),
                CompositionEntry = selectedEntry
            });

            if (!result.IsSuccess || result.Act == null)
            {
                MessageBox.Show(
                    this,
                    result.ErrorMessage,
                    "Акт",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            ShowActDraftStub(result.Act);
            SetLastActionText("Черновик акта подготовлен.");
        }

        private void ShowActDraftStub(KbAct act)
        {
            var equipment = act.EquipmentSnapshot ?? new KbActEquipmentSnapshot();
            string message =
                "Черновик акта подготовлен." + Environment.NewLine +
                Environment.NewLine +
                $"Цех: {FormatActDraftValue(act.WorkshopName)}" + Environment.NewLine +
                $"Объект: {FormatActDraftValue(act.ObjectNameSnapshot)}" + Environment.NewLine +
                $"Оборудование: {FormatActDraftValue(act.EquipmentName)}" + Environment.NewLine +
                $"Заказной номер: {FormatActDraftValue(equipment.OrderNumber)}";

            MessageBox.Show(
                this,
                message,
                "Акт",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private static string FormatActDraftValue(string? value) =>
            string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
    }
}

using System.Diagnostics;
using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase
{
    public partial class MainForm
    {
        private void AddNetworkFileReference(object? sender, EventArgs e)
        {
            if (!TryGetNetworkOwnerNode(out var ownerNode))
                return;

            EditNetworkFileReferenceCore(
                ownerNode,
                new KbNetworkFileReference
                {
                    OwnerNodeId = ownerNode.NodeId
                },
                "Добавить файл сети",
                "Файл сети добавлен.");
        }

        private void OpenSelectedNetworkFileReference(object? sender, EventArgs e)
        {
            if (!TryGetNetworkOwnerNode(out var ownerNode))
                return;

            var networkFileReference = FindSelectedNetworkFileReference(ownerNode);
            if (networkFileReference == null || string.IsNullOrWhiteSpace(networkFileReference.Path))
            {
                MessageBox.Show(
                    this,
                    "Сначала выберите файл сети с заполненным путем.",
                    "Сеть",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = networkFileReference.Path.Trim(),
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    $"Не удалось открыть файл сети: {ex.Message}",
                    "Сеть",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void EditSelectedNetworkFileReference(object? sender, EventArgs e)
        {
            if (!TryGetNetworkOwnerNode(out var ownerNode))
                return;

            var networkFileReference = FindSelectedNetworkFileReference(ownerNode);
            if (networkFileReference == null)
            {
                MessageBox.Show(
                    this,
                    "Выберите файл сети для изменения.",
                    "Сеть",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            EditNetworkFileReferenceCore(
                ownerNode,
                CloneNetworkFileReference(networkFileReference),
                "Изменить файл сети",
                "Файл сети обновлен.");
        }

        private void DeleteSelectedNetworkFileReference(object? sender, EventArgs e)
        {
            if (!TryGetNetworkOwnerNode(out var ownerNode))
                return;

            var networkFileReference = FindSelectedNetworkFileReference(ownerNode);
            if (networkFileReference == null)
            {
                MessageBox.Show(
                    this,
                    "Выберите файл сети для удаления.",
                    "Сеть",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            var confirmDelete = MessageBox.Show(
                this,
                $"Удалить файл сети \"{networkFileReference.Title}\"?",
                "Сеть",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Warning);
            if (confirmDelete != DialogResult.OK)
                return;

            ApplyNetworkFileReferenceMutation(
                _networkMutationService.DeleteNetworkFileReference(
                    ownerNode,
                    _session.NetworkFileReferences,
                    networkFileReference.NetworkAssetId,
                    GetVisibleLevelForNode(ownerNode)),
                "Файл сети удален.");
        }

        private void EditNetworkFileReferenceCore(
            KbNode ownerNode,
            KbNetworkFileReference draftReference,
            string dialogTitle,
            string successStatusText)
        {
            using var dialog = new KnowledgeBaseNetworkFileReferenceDialog(dialogTitle, draftReference);
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            ApplyNetworkFileReferenceMutation(
                _networkMutationService.UpsertNetworkFileReference(
                    ownerNode,
                    _session.NetworkFileReferences,
                    dialog.Result,
                    GetVisibleLevelForNode(ownerNode)),
                successStatusText);
        }

        private void ApplyNetworkFileReferenceMutation(
            KnowledgeBaseNetworkFileReferenceMutationResult result,
            string successStatusText)
        {
            if (!result.IsSuccess)
            {
                MessageBox.Show(
                    this,
                    result.ErrorMessage,
                    "Сеть",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            _session.ReplaceNetworkFileReferences(result.NetworkFileReferences);
            UpdateDirtyState();
            UpdateUI();
            SetLastActionText(successStatusText);
        }

        private bool TryGetNetworkOwnerNode(out KbNode ownerNode)
        {
            ownerNode = new KbNode();
            if (TryGetSelectedTreeNode(out KbNode selectedNode) &&
                KnowledgeBaseNetworkStateService.SupportsRecords(
                    selectedNode.NodeType,
                    GetVisibleLevelForNode(selectedNode)))
            {
                ownerNode = selectedNode;
                return true;
            }

            MessageBox.Show(
                this,
                "Вкладка \"Сеть\" доступна только для инженерных узлов.",
                "Сеть",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return false;
        }

        private KbNetworkFileReference? FindSelectedNetworkFileReference(KbNode ownerNode)
        {
            string selectedItemId = selectedNodeNetworkScreen.SelectedItemId;
            if (string.IsNullOrWhiteSpace(selectedItemId))
                return null;

            return _session.NetworkFileReferences.FirstOrDefault(reference =>
                string.Equals(reference.NetworkAssetId, selectedItemId, StringComparison.Ordinal) &&
                string.Equals(reference.OwnerNodeId, ownerNode.NodeId, StringComparison.Ordinal));
        }

        private static KbNetworkFileReference CloneNetworkFileReference(KbNetworkFileReference reference) =>
            new()
            {
                NetworkAssetId = reference.NetworkAssetId,
                OwnerNodeId = reference.OwnerNodeId,
                Title = reference.Title,
                Path = reference.Path,
                PreviewKind = reference.PreviewKind
            };
    }
}

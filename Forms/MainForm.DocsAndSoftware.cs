using System.Diagnostics;
using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase
{
    public partial class MainForm
    {
        private void AddSchemeDocumentLink(object? sender, EventArgs e)
        {
            if (!TryGetDocsAndSoftwareOwnerNode(out var ownerNode))
                return;

            EditDocumentLinkCore(
                ownerNode,
                new KbDocumentLink
                {
                    OwnerNodeId = ownerNode.NodeId,
                    Kind = KbDocumentKind.SchemeLink
                },
                kind: KbDocumentKind.SchemeLink,
                dialogTitle: "Добавить схему",
                successStatusText: "Ссылка на схему добавлена.");
        }

        private void AddManualOrInstructionLink(object? sender, EventArgs e)
        {
            if (!TryGetDocsAndSoftwareOwnerNode(out var ownerNode))
                return;

            EditDocumentLinkCore(
                ownerNode,
                new KbDocumentLink
                {
                    OwnerNodeId = ownerNode.NodeId,
                    Kind = KbDocumentKind.Instruction
                },
                kind: KbDocumentKind.Instruction,
                dialogTitle: "Добавить инструкцию",
                successStatusText: "Ссылка на инструкцию добавлена.");
        }

        private void AddSoftwareRecord(object? sender, EventArgs e)
        {
            if (!TryGetDocsAndSoftwareOwnerNode(out var ownerNode))
                return;

            EditSoftwareRecordCore(
                ownerNode,
                new KbSoftwareRecord
                {
                    OwnerNodeId = ownerNode.NodeId
                },
                "Добавить ссылку на ПО",
                "Ссылка на ПО добавлена.");
        }

        private void OpenSelectedDocsAndSoftwareItem(object? sender, EventArgs e)
        {
            if (!TryGetDocsAndSoftwareOwnerNode(out var ownerNode))
                return;

            string path = ResolveSelectedDocsAndSoftwarePath(ownerNode);
            if (string.IsNullOrWhiteSpace(path))
            {
                MessageBox.Show(
                    this,
                    "Сначала выберите ссылку с заполненным путем.",
                    "Документация и ПО",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    $"Не удалось открыть путь: {ex.Message}",
                    "Документация и ПО",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void EditSelectedDocsAndSoftwareItem(object? sender, EventArgs e)
        {
            if (!TryGetDocsAndSoftwareOwnerNode(out var ownerNode))
                return;

            switch (selectedNodeDocsAndSoftwareScreen.SelectedItemKind)
            {
                case KnowledgeBaseDocsAndSoftwareSelectionKind.DocumentLink:
                    {
                        var documentLink = FindSelectedDocumentLink(ownerNode);
                        if (documentLink == null)
                        {
                            MessageBox.Show(
                                this,
                                "Выберите ссылку на схему или инструкцию.",
                                "Документация и ПО",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                            return;
                        }

                        EditDocumentLinkCore(
                            ownerNode,
                            CloneDocumentLink(documentLink),
                            kind: documentLink.Kind,
                            dialogTitle: documentLink.Kind == KbDocumentKind.SchemeLink
                                ? "Изменить ссылку на схему"
                                : "Изменить инструкцию",
                            successStatusText: "Ссылка обновлена.");
                        return;
                    }

                case KnowledgeBaseDocsAndSoftwareSelectionKind.SoftwareRecord:
                    {
                        var softwareRecord = FindSelectedSoftwareRecord(ownerNode);
                        if (softwareRecord == null)
                        {
                            MessageBox.Show(
                                this,
                                "Выберите ссылку на ПО.",
                                "Документация и ПО",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                            return;
                        }

                        EditSoftwareRecordCore(
                            ownerNode,
                            CloneSoftwareRecord(softwareRecord),
                            "Изменить ссылку на ПО",
                            "Ссылка на ПО обновлена.");
                        return;
                    }
            }

            MessageBox.Show(
                this,
                "Выберите ссылку для изменения.",
                "Документация и ПО",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void DeleteSelectedDocsAndSoftwareItem(object? sender, EventArgs e)
        {
            if (!TryGetDocsAndSoftwareOwnerNode(out var ownerNode))
                return;

            switch (selectedNodeDocsAndSoftwareScreen.SelectedItemKind)
            {
                case KnowledgeBaseDocsAndSoftwareSelectionKind.DocumentLink:
                    {
                        var documentLink = FindSelectedDocumentLink(ownerNode);
                        if (documentLink == null)
                        {
                            MessageBox.Show(
                                this,
                                "Выберите ссылку на схему или инструкцию.",
                                "Документация и ПО",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                            return;
                        }

                        var confirmDocumentDelete = MessageBox.Show(
                            this,
                            $"Удалить ссылку \"{documentLink.Title}\"?",
                            "Документация и ПО",
                            MessageBoxButtons.OKCancel,
                            MessageBoxIcon.Warning);
                        if (confirmDocumentDelete != DialogResult.OK)
                            return;

                        ApplyDocumentLinkMutation(
                            _docsAndSoftwareMutationService.DeleteDocumentLink(
                                ownerNode,
                                _session.DocumentLinks,
                                documentLink.DocumentId,
                                GetVisibleLevelForNode(ownerNode)),
                            "Ссылка удалена.");
                        return;
                    }

                case KnowledgeBaseDocsAndSoftwareSelectionKind.SoftwareRecord:
                    {
                        var softwareRecord = FindSelectedSoftwareRecord(ownerNode);
                        if (softwareRecord == null)
                        {
                            MessageBox.Show(
                                this,
                                "Выберите ссылку на ПО.",
                                "Документация и ПО",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                            return;
                        }

                        var confirmSoftwareDelete = MessageBox.Show(
                            this,
                            $"Удалить ссылку на ПО \"{softwareRecord.Title}\"?",
                            "Документация и ПО",
                            MessageBoxButtons.OKCancel,
                            MessageBoxIcon.Warning);
                        if (confirmSoftwareDelete != DialogResult.OK)
                            return;

                        ApplySoftwareRecordMutation(
                            _docsAndSoftwareMutationService.DeleteSoftwareRecord(
                                ownerNode,
                                _session.SoftwareRecords,
                                softwareRecord.SoftwareId,
                                GetVisibleLevelForNode(ownerNode)),
                            "Ссылка на ПО удалена.");
                        return;
                    }
            }

            MessageBox.Show(
                this,
                "Выберите ссылку для удаления.",
                "Документация и ПО",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void EditDocumentLinkCore(
            KbNode ownerNode,
            KbDocumentLink draftLink,
            KbDocumentKind kind,
            string dialogTitle,
            string successStatusText)
        {
            using var dialog = new KnowledgeBaseDocumentLinkDialog(
                dialogTitle,
                kind,
                draftLink);
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            ApplyDocumentLinkMutation(
                _docsAndSoftwareMutationService.UpsertDocumentLink(
                    ownerNode,
                    _session.DocumentLinks,
                    dialog.Result,
                    GetVisibleLevelForNode(ownerNode)),
                successStatusText);
        }

        private void EditSoftwareRecordCore(
            KbNode ownerNode,
            KbSoftwareRecord draftRecord,
            string dialogTitle,
            string successStatusText)
        {
            using var dialog = new KnowledgeBaseSoftwareRecordDialog(dialogTitle, draftRecord);
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            ApplySoftwareRecordMutation(
                _docsAndSoftwareMutationService.UpsertSoftwareRecord(
                    ownerNode,
                    _session.SoftwareRecords,
                    dialog.Result,
                    GetVisibleLevelForNode(ownerNode)),
                successStatusText);
        }

        private void ApplyDocumentLinkMutation(
            KnowledgeBaseDocumentLinkMutationResult result,
            string successStatusText)
        {
            if (!result.IsSuccess)
            {
                MessageBox.Show(
                    this,
                    result.ErrorMessage,
                    "Документация и ПО",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            _session.ReplaceDocumentLinks(result.DocumentLinks);
            UpdateDirtyState();
            UpdateUI();
            SetLastActionText(successStatusText);
        }

        private void ApplySoftwareRecordMutation(
            KnowledgeBaseSoftwareRecordMutationResult result,
            string successStatusText)
        {
            if (!result.IsSuccess)
            {
                MessageBox.Show(
                    this,
                    result.ErrorMessage,
                    "Документация и ПО",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            _session.ReplaceSoftwareRecords(result.SoftwareRecords);
            UpdateDirtyState();
            UpdateUI();
            SetLastActionText(successStatusText);
        }

        private bool TryGetDocsAndSoftwareOwnerNode(out KbNode ownerNode)
        {
            ownerNode = new KbNode();
            if (TryGetSelectedTreeNode(out KbNode selectedNode) &&
                KnowledgeBaseDocsAndSoftwareStateService.SupportsRecords(
                    selectedNode.NodeType,
                    GetVisibleLevelForNode(selectedNode)))
            {
                ownerNode = selectedNode;
                return true;
            }

            MessageBox.Show(
                this,
                "Документация и ПО доступны только для инженерных узлов.",
                "Документация и ПО",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return false;
        }

        private KbDocumentLink? FindSelectedDocumentLink(KbNode ownerNode)
        {
            if (selectedNodeDocsAndSoftwareScreen.SelectedItemKind != KnowledgeBaseDocsAndSoftwareSelectionKind.DocumentLink)
                return null;

            string selectedItemId = selectedNodeDocsAndSoftwareScreen.SelectedItemId;
            if (string.IsNullOrWhiteSpace(selectedItemId))
                return null;

            return _session.DocumentLinks.FirstOrDefault(link =>
                string.Equals(link.DocumentId, selectedItemId, StringComparison.Ordinal) &&
                string.Equals(link.OwnerNodeId, ownerNode.NodeId, StringComparison.Ordinal));
        }

        private KbSoftwareRecord? FindSelectedSoftwareRecord(KbNode ownerNode)
        {
            if (selectedNodeDocsAndSoftwareScreen.SelectedItemKind != KnowledgeBaseDocsAndSoftwareSelectionKind.SoftwareRecord)
                return null;

            string selectedItemId = selectedNodeDocsAndSoftwareScreen.SelectedItemId;
            if (string.IsNullOrWhiteSpace(selectedItemId))
                return null;

            return _session.SoftwareRecords.FirstOrDefault(record =>
                string.Equals(record.SoftwareId, selectedItemId, StringComparison.Ordinal) &&
                string.Equals(record.OwnerNodeId, ownerNode.NodeId, StringComparison.Ordinal));
        }

        private string ResolveSelectedDocsAndSoftwarePath(KbNode ownerNode)
        {
            var documentLink = FindSelectedDocumentLink(ownerNode);
            if (documentLink != null)
                return documentLink.Path.Trim();

            var softwareRecord = FindSelectedSoftwareRecord(ownerNode);
            return softwareRecord?.Path?.Trim() ?? string.Empty;
        }

        private static KbDocumentLink CloneDocumentLink(KbDocumentLink link) =>
            new()
            {
                DocumentId = link.DocumentId,
                OwnerNodeId = link.OwnerNodeId,
                Kind = link.Kind,
                Title = link.Title,
                Path = link.Path,
                UpdatedAt = link.UpdatedAt
            };

        private static KbSoftwareRecord CloneSoftwareRecord(KbSoftwareRecord record) =>
            new()
            {
                SoftwareId = record.SoftwareId,
                OwnerNodeId = record.OwnerNodeId,
                Title = record.Title,
                Path = record.Path,
                AddedAt = record.AddedAt,
                LastChangedAt = record.LastChangedAt,
                LastBackupAt = record.LastBackupAt,
                Notes = record.Notes
            };
    }
}

using System.Diagnostics;
using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase
{
    public partial class MainForm
    {
        private void HandleNodeDetailsChanged(Action<KbNodeDetails> updateDetails)
        {
            if (_isApplyingSelectedNodeState || tvTree.SelectedNode?.Tag is not KbNode selectedNode)
                return;

            selectedNode.Details ??= new KbNodeDetails();
            updateDetails(selectedNode.Details);

            if (!KnowledgeBaseNodeMetadataService.SupportsTechnicalFields(selectedNode.NodeType))
            {
                selectedNode.Details.IpAddress = string.Empty;
                selectedNode.Details.SchemaLink = string.Empty;
            }

            UpdateDirtyState();
            UpdateUI(refreshSelectedNodeState: false);
            UpdatePhotoControlsState(selectedNode.Details.PhotoPath);
        }

        private void BtnBrowsePhoto_Click(object? sender, EventArgs e)
        {
            if (tvTree.SelectedNode?.Tag is not KbNode)
                return;

            using var dialog = new OpenFileDialog
            {
                Title = "Выберите фото объекта",
                CheckFileExists = true,
                Filter = "Изображения (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp|Все файлы (*.*)|*.*"
            };

            if (dialog.ShowDialog(this) == DialogResult.OK)
                txtNodePhotoPath.Text = dialog.FileName;
        }

        private void BtnOpenPhoto_Click(object? sender, EventArgs e)
        {
            string photoPath = txtNodePhotoPath.Text.Trim();
            if (string.IsNullOrWhiteSpace(photoPath) || !File.Exists(photoPath))
            {
                MessageBox.Show(
                    this,
                    "Укажите существующий путь к фото, чтобы открыть файл.",
                    "Фото недоступно",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = photoPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    $"Не удалось открыть фото: {ex.Message}",
                    "Ошибка открытия",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void UpdatePhotoControlsState(string photoPath) =>
            btnOpenPhoto.Enabled =
                !string.IsNullOrWhiteSpace(photoPath) &&
                File.Exists(photoPath.Trim());
    }
}

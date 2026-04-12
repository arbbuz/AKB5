using System.Diagnostics;
using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase
{
    public partial class MainForm
    {
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                DisposePhotoPreviewImage();

            base.Dispose(disposing);
        }

        private void HandleNodeDetailsChanged(Action<KbNodeDetails> updateDetails, bool refreshPhotoPreview = false)
        {
            if (_isApplyingSelectedNodeState || tvTree.SelectedNode?.Tag is not KbNode selectedNode)
                return;

            selectedNode.Details ??= new KbNodeDetails();
            updateDetails(selectedNode.Details);

            if (selectedNode.LevelIndex < 2)
            {
                selectedNode.Details.IpAddress = string.Empty;
                selectedNode.Details.SchemaLink = string.Empty;
            }

            UpdateDirtyState();
            UpdateUI(refreshSelectedNodeState: false);

            if (refreshPhotoPreview)
                UpdatePhotoPreview(selectedNode.Details.PhotoPath, hasSelection: true);
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
                SetLastActionText($"📷 Открыто фото: {Path.GetFileName(photoPath)}");
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

        private void UpdatePhotoPreview(string photoPath, bool hasSelection)
        {
            if (!hasSelection)
            {
                ClearPhotoPreview("Выберите объект, чтобы увидеть превью фото.");
                return;
            }

            string normalizedPath = photoPath.Trim();
            if (string.IsNullOrWhiteSpace(normalizedPath))
            {
                ClearPhotoPreview("Путь к фото не указан.");
                return;
            }

            btnOpenPhoto.Enabled = File.Exists(normalizedPath);
            if (!File.Exists(normalizedPath))
            {
                ClearPhotoPreview("Файл не найден по указанному пути.");
                return;
            }

            try
            {
                using var stream = new FileStream(normalizedPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var sourceImage = Image.FromStream(stream);
                var previewImage = new Bitmap(sourceImage);

                DisposePhotoPreviewImage();
                _photoPreviewImage = previewImage;
                picNodePhotoPreview.Image = _photoPreviewImage;
                lblPhotoPreviewState.Text = $"Файл: {Path.GetFileName(normalizedPath)}";
            }
            catch (Exception ex)
            {
                ClearPhotoPreview($"Не удалось загрузить превью: {ex.Message}");
            }
        }

        private void ClearPhotoPreview(string statusText)
        {
            DisposePhotoPreviewImage();
            picNodePhotoPreview.Image = null;
            lblPhotoPreviewState.Text = statusText;
            btnOpenPhoto.Enabled = !string.IsNullOrWhiteSpace(txtNodePhotoPath.Text) && File.Exists(txtNodePhotoPath.Text.Trim());
        }

        private void DisposePhotoPreviewImage()
        {
            if (_photoPreviewImage == null)
                return;

            picNodePhotoPreview.Image = null;
            _photoPreviewImage.Dispose();
            _photoPreviewImage = null;
        }
    }
}

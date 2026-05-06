using System.Text;
using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase
{
    public sealed class KnowledgeBaseObjectTemplateApplyPreviewDialog : Form
    {
        private readonly Func<string, KnowledgeBaseObjectTemplateApplicationPlan> _buildPreview;
        private readonly string _targetName;

        private ComboBox _cmbTemplates = null!;
        private TextBox _txtTarget = null!;
        private TextBox _txtPreview = null!;
        private Button _btnApply = null!;
        private KnowledgeBaseObjectTemplateApplicationPlan? _currentPlan;

        public KnowledgeBaseObjectTemplateApplyPreviewDialog(
            KbNode targetNode,
            IReadOnlyList<KbObjectTemplate> templates,
            Func<string, KnowledgeBaseObjectTemplateApplicationPlan> buildPreview)
        {
            _buildPreview = buildPreview;
            _targetName = targetNode.Name?.Trim() ?? string.Empty;

            Text = "Применить шаблон к объекту";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(760, 520);
            Size = new Size(880, 640);
            ShowInTaskbar = false;
            AppIconProvider.Apply(this);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                ColumnCount = 2,
                RowCount = 4
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            layout.Controls.Add(CreateLabel("Объект"), 0, 0);
            _txtTarget = new TextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                Text = string.IsNullOrWhiteSpace(_targetName) ? "(без имени)" : _targetName
            };
            layout.Controls.Add(_txtTarget, 1, 0);

            layout.Controls.Add(CreateLabel("Шаблон"), 0, 1);
            _cmbTemplates = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                DisplayMember = nameof(KbObjectTemplate.DisplayName),
                DataSource = templates.ToList()
            };
            _cmbTemplates.SelectedIndexChanged += (_, _) => UpdatePreview();
            layout.Controls.Add(_cmbTemplates, 1, 1);

            layout.Controls.Add(CreateLabel("Предпросмотр"), 0, 2);
            _txtPreview = new TextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                Font = new Font("Consolas", 10F)
            };
            layout.Controls.Add(_txtPreview, 1, 2);

            var buttonsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                AutoSize = true,
                Margin = new Padding(0, 10, 0, 0)
            };

            var btnCancel = new Button
            {
                Text = "Отмена",
                DialogResult = DialogResult.Cancel,
                AutoSize = true
            };
            _btnApply = new Button
            {
                Text = "Применить",
                AutoSize = true
            };
            _btnApply.Click += BtnApply_Click;

            buttonsPanel.Controls.Add(btnCancel);
            buttonsPanel.Controls.Add(_btnApply);
            layout.Controls.Add(buttonsPanel, 0, 3);
            layout.SetColumnSpan(buttonsPanel, 2);

            Controls.Add(layout);
            AcceptButton = _btnApply;
            CancelButton = btnCancel;

            if (_cmbTemplates.Items.Count > 0 && _cmbTemplates.SelectedIndex < 0)
                _cmbTemplates.SelectedIndex = 0;

            UpdatePreview();
            Shown += (_, _) => UpdatePreview();
        }

        public string SelectedTemplateId { get; private set; } = string.Empty;

        private void BtnApply_Click(object? sender, EventArgs e)
        {
            if (_cmbTemplates.SelectedItem is not KbObjectTemplate template)
            {
                MessageBox.Show(
                    this,
                    "Выберите шаблон объекта.",
                    "Шаблоны объектов",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            if (_currentPlan == null || !_currentPlan.IsSuccess || !_currentPlan.HasChanges)
            {
                MessageBox.Show(
                    this,
                    "В выбранном шаблоне нет новых данных для применения к объекту.",
                    "Шаблоны объектов",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            SelectedTemplateId = template.TemplateId;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void UpdatePreview()
        {
            if (_cmbTemplates.SelectedItem is not KbObjectTemplate template)
            {
                _currentPlan = null;
                _txtPreview.Text = "Выберите шаблон объекта для предпросмотра применения.";
                _btnApply.Enabled = false;
                return;
            }

            try
            {
                _currentPlan = _buildPreview(template.TemplateId);
                _txtPreview.Text = BuildPreviewText(_currentPlan);
                _btnApply.Enabled = _currentPlan.IsSuccess && _currentPlan.HasChanges;
            }
            catch (Exception ex)
            {
                _currentPlan = null;
                _txtPreview.Text = $"Не удалось построить предпросмотр применения шаблона: {ex.Message}";
                _btnApply.Enabled = false;
            }
        }

        private string BuildPreviewText(KnowledgeBaseObjectTemplateApplicationPlan plan)
        {
            if (!plan.IsSuccess)
            {
                return string.IsNullOrWhiteSpace(plan.ErrorMessage)
                    ? "Шаблон нельзя применить к выбранному объекту."
                    : plan.ErrorMessage;
            }

            var builder = new StringBuilder();
            builder.AppendLine($"Объект: {_txtTarget.Text}");
            builder.AppendLine($"Шаблон: {plan.TemplateDisplayName}");
            builder.AppendLine($"Будет добавлено: {plan.AddedCount}");
            builder.AppendLine($"Будет пропущено: {plan.SkippedCount}");
            builder.AppendLine($"Без изменений: {plan.UnchangedCount}");
            builder.AppendLine();

            foreach (KnowledgeBaseObjectTemplateApplicationPreviewItem item in plan.PreviewItems)
            {
                builder.Append(GetActionMarker(item.Action));
                builder.Append(' ');
                builder.Append('[');
                builder.Append(item.Area);
                builder.Append("] ");
                builder.Append(item.Target);
                builder.Append(" - ");
                builder.AppendLine(item.Description);
            }

            if (plan.PreviewItems.Count == 0)
            {
                builder.AppendLine(
                    "Предпросмотр не содержит действий. Кнопка \"Применить\" доступна только если шаблон добавляет новые данные.");
            }

            if (!plan.HasChanges)
            {
                builder.AppendLine();
                builder.AppendLine("Новых данных для применения нет.");
            }

            return builder.ToString();
        }

        private static string GetActionMarker(KnowledgeBaseObjectTemplateApplicationAction action) => action switch
        {
            KnowledgeBaseObjectTemplateApplicationAction.Added => "+",
            KnowledgeBaseObjectTemplateApplicationAction.Skipped => "!",
            _ => "="
        };

        private static Label CreateLabel(string text) =>
            new()
            {
                Text = text,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 0, 8, 8)
            };
    }
}

using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase
{
    public sealed class KnowledgeBaseActForm : Form
    {
        private const string DefaultFaultCriterion = "Электрическая часть";
        private const string DefaultCustomerPosition = "Начальник участка ЭнЦ";
        private const string DefaultExecutorPosition = "инженер-электроник ОА БСО АСУ ТП УИТиА";
        private const string DefaultApproverPosition = "Начальник отдела автоматизации";

        private static readonly string[] FaultCriterionOptions =
        [
            "Механическая часть",
            "Электрическая часть",
            "Неисправность АСУТП",
            "Консультация персонала"
        ];

        private static readonly string[] CustomerPositionOptions =
        [
            "Начальник участка ЭнЦ",
            "Заместитель начальника цеха",
            "Мастер по ремонту оборудования"
        ];

        private static readonly string[] ExecutorPositionOptions =
        [
            "инженер-электроник ОА БСО АСУ ТП УИТиА",
            "инженер-электроник ОА БСО УИТиА",
            "Ведущий инженер-электроник",
            "Ведущий инженер-электроник ОА БСО АСУ ТП",
            "Инженер-электроник",
            "ведущий инженер-электроник ОА БСО АСУ ТП УИТиА",
            "ведущий инженер-электроник ОА БСО УИТиА"
        ];

        private static readonly string[] ApproverPositionOptions =
        [
            DefaultApproverPosition
        ];

        private readonly KbAct _draft;
        private readonly List<KbActExecutor> _draftExecutors;
        private readonly KnowledgeBaseActEditorService _editorService = new();

        private ComboBox _cmbActType = null!;
        private TextBox _txtStatus = null!;
        private DateTimePicker _dtpActDate = null!;
        private TextBox _txtWorkshop = null!;
        private TextBox _txtObject = null!;
        private TextBox _txtEquipment = null!;
        private TextBox _txtOrderNumber = null!;
        private TextBox _txtFaultDescription = null!;
        private TextBox _txtFailureReason = null!;
        private TextBox _txtInspectionResult = null!;
        private DateTimePicker _dtpFailureDate = null!;
        private ComboBox _cmbFaultCriterion = null!;
        private TextBox _txtRequestDocument = null!;
        private TextBox _txtActualLaborHours = null!;
        private TextBox _txtCustomerName = null!;
        private ComboBox _cmbCustomerPosition = null!;
        private TextBox _txtApproverName = null!;
        private ComboBox _cmbApproverPosition = null!;
        private TextBox _txtExecutorName = null!;
        private ComboBox _cmbExecutorPosition = null!;

        public KnowledgeBaseActForm(KbAct draft, IEnumerable<KbActExecutor>? executors = null)
        {
            _draft = KnowledgeBaseActEditorService.CloneAct(draft);
            _draftExecutors = executors?.Select(CloneExecutor).ToList() ?? new List<KbActExecutor>();
            Result = KnowledgeBaseActEditorService.CloneAct(draft);
            ResultExecutors = new List<KbActExecutor>();

            Text = "Акт";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimizeBox = false;
            MaximizeBox = true;
            ShowInTaskbar = false;
            MinimumSize = new Size(780, 560);
            ClientSize = new Size(880, 680);
            AppIconProvider.Apply(this);

            var rootLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var tabs = new TabControl
            {
                Dock = DockStyle.Fill,
                Padding = new Point(12, 4)
            };
            tabs.TabPages.Add(CreateGeneralTab());
            tabs.TabPages.Add(CreateDescriptionTab());
            tabs.TabPages.Add(CreateAdditionalTab());

            var buttonsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                AutoSize = true,
                Padding = new Padding(12, 8, 12, 12)
            };

            var btnCancel = new Button
            {
                Text = "Отмена",
                DialogResult = DialogResult.Cancel,
                AutoSize = true
            };
            var btnGenerateDocument = new Button
            {
                Text = "Сформировать DOCX",
                AutoSize = true
            };
            btnGenerateDocument.Click += (_, _) => Submit(requestDocumentGeneration: true);

            var btnSave = new Button
            {
                Text = "Сохранить черновик",
                AutoSize = true
            };
            btnSave.Click += (_, _) => Submit(requestDocumentGeneration: false);

            buttonsPanel.Controls.Add(btnCancel);
            buttonsPanel.Controls.Add(btnGenerateDocument);
            buttonsPanel.Controls.Add(btnSave);

            rootLayout.Controls.Add(tabs, 0, 0);
            rootLayout.Controls.Add(buttonsPanel, 0, 1);
            Controls.Add(rootLayout);

            AcceptButton = btnSave;
            CancelButton = btnCancel;
        }

        public KbAct Result { get; private set; }

        public IReadOnlyList<KbActExecutor> ResultExecutors { get; private set; }

        public bool DocumentGenerationRequested { get; private set; }

        private TabPage CreateGeneralTab()
        {
            var page = new TabPage("Основное");
            var layout = CreateFormLayout(rowCount: 7);

            _cmbActType = new ComboBox
            {
                Dock = DockStyle.Left,
                Width = 260,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cmbActType.Items.Add(new ActTypeOption(KbActType.EquipmentFailure, "Отказ оборудования"));
            _cmbActType.Items.Add(new ActTypeOption(KbActType.InspectionWork, "Осмотр / выполненные работы"));
            SelectActType(_draft.ActType);

            _dtpActDate = CreateDatePicker(_draft.ActDate ?? DateTime.Today);
            _txtStatus = CreateReadOnlyTextBox(FormatStatus(_draft.Status));
            _txtWorkshop = CreateTextBox(_draft.WorkshopName);
            _txtObject = CreateTextBox(_draft.ObjectNameSnapshot);
            _txtEquipment = CreateMultilineTextBox(_draft.EquipmentName, height: 72);

            KbActEquipmentSnapshot equipment = _draft.EquipmentSnapshot ?? new KbActEquipmentSnapshot();
            _txtOrderNumber = CreateTextBox(equipment.OrderNumber);

            AddRow(layout, 0, "Тип акта", _cmbActType);
            AddRow(layout, 1, "Дата акта", _dtpActDate);
            AddRow(layout, 2, "Статус", _txtStatus);
            AddRow(layout, 3, "Цех", _txtWorkshop);
            AddRow(layout, 4, "Объект", _txtObject);
            AddRow(layout, 5, "Оборудование", _txtEquipment);
            AddRow(layout, 6, "Заказной номер", _txtOrderNumber);

            page.Controls.Add(layout);
            return page;
        }

        private TabPage CreateDescriptionTab()
        {
            var page = new TabPage("Описание");
            var layout = CreateFormLayout(rowCount: 3);

            _txtFaultDescription = CreateMultilineTextBox(_draft.FaultDescription, height: 120);
            _txtFailureReason = CreateMultilineTextBox(_draft.FailureReason, height: 120);
            _txtInspectionResult = CreateMultilineTextBox(_draft.InspectionResult, height: 120);

            AddRow(layout, 0, "Неисправность", _txtFaultDescription);
            AddRow(layout, 1, "Причина", _txtFailureReason);
            AddRow(layout, 2, "Результат осмотра", _txtInspectionResult);

            page.Controls.Add(layout);
            return page;
        }

        private TabPage CreateAdditionalTab()
        {
            var page = new TabPage("Дополнительно");
            var layout = CreateFormLayout(rowCount: 10);
            KbActExecutor? firstExecutor = _draftExecutors
                .OrderBy(static executor => executor.SortOrder)
                .FirstOrDefault();

            _dtpFailureDate = CreateOptionalDatePicker(_draft.FailureDate ?? _draft.ActDate ?? DateTime.Today);
            _dtpFailureDate.Checked = _draft.FailureDate.HasValue || _draft.ActDate.HasValue;
            _cmbFaultCriterion = CreateEditableComboBox(
                FaultCriterionOptions,
                SelectDefault(_draft.FaultCriterion, DefaultFaultCriterion));
            _txtRequestDocument = CreateTextBox(_draft.RequestDocument);
            _txtActualLaborHours = CreateTextBox(_draft.ActualLaborHours);
            _txtCustomerName = CreateTextBox(_draft.CustomerName);
            _cmbCustomerPosition = CreateEditableComboBox(
                CustomerPositionOptions,
                SelectDefault(_draft.CustomerPosition, DefaultCustomerPosition));
            _txtApproverName = CreateTextBox(_draft.ApproverName);
            _cmbApproverPosition = CreateEditableComboBox(
                ApproverPositionOptions,
                SelectDefault(_draft.ApproverPosition, DefaultApproverPosition));
            _txtExecutorName = CreateTextBox(FormatExecutorName(firstExecutor));
            _cmbExecutorPosition = CreateEditableComboBox(
                ExecutorPositionOptions,
                SelectDefault(firstExecutor?.Position, DefaultExecutorPosition));

            AddRow(layout, 0, "Дата отказа", _dtpFailureDate);
            AddRow(layout, 1, "Критерий отказа", _cmbFaultCriterion);
            AddRow(layout, 2, "Заявка / основание", _txtRequestDocument);
            AddRow(layout, 3, "Трудозатраты", _txtActualLaborHours);
            AddRow(layout, 4, "Представитель цеха", _txtCustomerName);
            AddRow(layout, 5, "Должность представителя", _cmbCustomerPosition);
            AddRow(layout, 6, "Утверждающий", _txtApproverName);
            AddRow(layout, 7, "Должность утверждающего", _cmbApproverPosition);
            AddRow(layout, 8, "Исполнитель", _txtExecutorName);
            AddRow(layout, 9, "Должность исполнителя", _cmbExecutorPosition);

            page.Controls.Add(layout);
            return page;
        }

        private void Submit(bool requestDocumentGeneration)
        {
            KbAct candidate = BuildResultFromFields();
            KnowledgeBaseActEditorSaveResult result = _editorService.PrepareForSave(candidate);
            if (!result.IsSuccess || result.Act == null)
            {
                ShowValidationMessage(result.ErrorMessage);
                return;
            }

            List<KbActExecutor> executors = BuildExecutors(result.Act.ActId);
            string? executorValidationError = KnowledgeBaseActEditorService.ValidateExecutorsForSave(executors);
            if (executorValidationError != null)
            {
                ShowValidationMessage(executorValidationError);
                return;
            }

            Result = result.Act;
            ResultExecutors = KnowledgeBaseDataService.NormalizeActExecutors(
                executors,
                new[] { Result.ActId });
            DocumentGenerationRequested = requestDocumentGeneration;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void ShowValidationMessage(string? message)
        {
            MessageBox.Show(
                this,
                string.IsNullOrWhiteSpace(message) ? "Проверьте данные акта." : message,
                "Акт",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private KbAct BuildResultFromFields()
        {
            KbAct act = KnowledgeBaseActEditorService.CloneAct(_draft);
            act.ActType = _cmbActType.SelectedItem is ActTypeOption selectedType
                ? selectedType.Value
                : KbActType.EquipmentFailure;
            act.Status = _draft.Status;
            act.ActDate = _dtpActDate.Value.Date;
            act.ActYear = _dtpActDate.Value.Year;
            act.WorkshopName = _txtWorkshop.Text;
            act.ObjectNameSnapshot = _txtObject.Text;
            act.EquipmentName = _txtEquipment.Text;
            act.FailureDate = _dtpFailureDate.Checked
                ? _dtpFailureDate.Value.Date
                : null;
            act.FaultDescription = _txtFaultDescription.Text;
            act.FailureReason = _txtFailureReason.Text;
            act.InspectionResult = _txtInspectionResult.Text;
            act.FaultCriterion = _cmbFaultCriterion.Text;
            act.RequestDocument = _txtRequestDocument.Text;
            act.ActualLaborHours = _txtActualLaborHours.Text;
            act.CustomerName = _txtCustomerName.Text;
            act.CustomerPosition = _cmbCustomerPosition.Text;
            act.ApproverName = _txtApproverName.Text;
            act.ApproverPosition = _cmbApproverPosition.Text;

            act.EquipmentSnapshot ??= new KbActEquipmentSnapshot();
            act.EquipmentSnapshot.OrderNumber = _txtOrderNumber.Text;

            return act;
        }

        private List<KbActExecutor> BuildExecutors(string actId)
        {
            var executors = new List<KbActExecutor>();
            string executorName = _txtExecutorName.Text.Trim();
            string executorPosition = _cmbExecutorPosition.Text.Trim();
            if (string.IsNullOrWhiteSpace(executorName) &&
                string.IsNullOrWhiteSpace(executorPosition))
            {
                return executors;
            }

            (string lastName, string firstName, string middleName) = SplitPersonName(executorName);
            executors.Add(new KbActExecutor
            {
                ExecutorId = _draftExecutors
                    .OrderBy(static executor => executor.SortOrder)
                    .FirstOrDefault()?.ExecutorId ?? string.Empty,
                ActId = actId,
                SortOrder = 0,
                LastName = lastName,
                FirstName = firstName,
                MiddleName = middleName,
                Position = executorPosition
            });

            return executors;
        }

        private void SelectActType(KbActType actType)
        {
            foreach (object? item in _cmbActType.Items)
            {
                if (item is ActTypeOption option && option.Value == actType)
                {
                    _cmbActType.SelectedItem = item;
                    return;
                }
            }

            _cmbActType.SelectedIndex = 0;
        }

        private static TableLayoutPanel CreateFormLayout(int rowCount)
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                ColumnCount = 2,
                RowCount = rowCount + 1,
                AutoScroll = true
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            for (int i = 0; i < rowCount; i++)
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            return layout;
        }

        private static void AddRow(
            TableLayoutPanel layout,
            int rowIndex,
            string labelText,
            Control editor)
        {
            layout.Controls.Add(CreateLabel(labelText), 0, rowIndex);
            layout.Controls.Add(editor, 1, rowIndex);
        }

        private static Label CreateLabel(string text) =>
            new()
            {
                Text = text,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 0, 8, 8)
            };

        private static TextBox CreateTextBox(string? text) =>
            new()
            {
                Dock = DockStyle.Fill,
                Text = text ?? string.Empty,
                Margin = new Padding(0, 0, 0, 8)
            };

        private static TextBox CreateReadOnlyTextBox(string text) =>
            new()
            {
                Dock = DockStyle.Left,
                Width = 180,
                ReadOnly = true,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                Text = text,
                Margin = new Padding(0, 0, 0, 8)
            };

        private static TextBox CreateMultilineTextBox(string? text, int height) =>
            new()
            {
                Dock = DockStyle.Fill,
                Height = height,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                WordWrap = true,
                Text = text ?? string.Empty,
                Margin = new Padding(0, 0, 0, 8)
            };

        private static ComboBox CreateEditableComboBox(IEnumerable<string> options, string? text)
        {
            var comboBox = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDown,
                Text = text ?? string.Empty,
                Margin = new Padding(0, 0, 0, 8)
            };
            comboBox.Items.AddRange(options.Cast<object>().ToArray());
            return comboBox;
        }

        private static DateTimePicker CreateDatePicker(DateTime value) =>
            new()
            {
                Dock = DockStyle.Left,
                Width = 130,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd.MM.yyyy",
                Value = value.Date,
                Margin = new Padding(0, 0, 0, 8)
            };

        private static DateTimePicker CreateOptionalDatePicker(DateTime value) =>
            new()
            {
                Dock = DockStyle.Left,
                Width = 150,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd.MM.yyyy",
                ShowCheckBox = true,
                Value = value.Date,
                Margin = new Padding(0, 0, 0, 8)
            };

        private static string FormatStatus(KbActStatus status) =>
            status switch
            {
                KbActStatus.Draft => "Черновик",
                KbActStatus.Generated => "Сформирован",
                KbActStatus.Signed => "Подписан",
                KbActStatus.Cancelled => "Отменен",
                KbActStatus.Archived => "Архив",
                KbActStatus.Annulled => "Аннулирован",
                _ => "Черновик"
            };

        private static string SelectDefault(string? value, string defaultValue) =>
            string.IsNullOrWhiteSpace(value) ? defaultValue : value.Trim();

        private static string FormatExecutorName(KbActExecutor? executor)
        {
            if (executor == null)
                return string.Empty;

            return string.Join(
                " ",
                new[]
                {
                    executor.LastName?.Trim(),
                    executor.FirstName?.Trim(),
                    executor.MiddleName?.Trim()
                }.Where(static part => !string.IsNullOrWhiteSpace(part)));
        }

        private static (string LastName, string FirstName, string MiddleName) SplitPersonName(string name)
        {
            string[] parts = name
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            return parts.Length switch
            {
                0 => (string.Empty, string.Empty, string.Empty),
                1 => (parts[0], string.Empty, string.Empty),
                2 => (parts[0], parts[1], string.Empty),
                _ => (parts[0], parts[1], string.Join(" ", parts.Skip(2)))
            };
        }

        private static KbActExecutor CloneExecutor(KbActExecutor executor) =>
            new()
            {
                ExecutorId = executor.ExecutorId,
                ActId = executor.ActId,
                SortOrder = executor.SortOrder,
                LastName = executor.LastName,
                FirstName = executor.FirstName,
                MiddleName = executor.MiddleName,
                Position = executor.Position
            };

        private sealed class ActTypeOption
        {
            public ActTypeOption(KbActType value, string text)
            {
                Value = value;
                Text = text;
            }

            public KbActType Value { get; }

            private string Text { get; }

            public override string ToString() => Text;
        }
    }
}

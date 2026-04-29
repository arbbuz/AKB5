using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase
{
    public sealed class KnowledgeBaseMaintenanceScheduleScreenControl : UserControl
    {
        private readonly KnowledgeBaseMaintenanceScheduleState _emptyState = new();

        private Label _lblSource = null!;
        private Label _lblSummary = null!;
        private Button _btnConfigure = null!;
        private Button _btnDelete = null!;
        private Label _lblInclusionValue = null!;
        private Label _lblTo1HoursValue = null!;
        private Label _lblTo2HoursValue = null!;
        private Label _lblTo3HoursValue = null!;

        private KnowledgeBaseMaintenanceScheduleState _currentState = new();

        public KnowledgeBaseMaintenanceScheduleScreenControl()
        {
            Dock = DockStyle.Fill;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16),
                ColumnCount = 1,
                RowCount = 4
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            _lblSource = new Label
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.DimGray,
                Margin = new Padding(0, 0, 0, 8)
            };

            _lblSummary = new Label
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 12)
            };

            var actionsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 12)
            };

            _btnConfigure = CreateActionButton("Настроить...");
            _btnConfigure.Click += (_, _) => ConfigureRequested?.Invoke(this, EventArgs.Empty);
            _btnDelete = CreateActionButton("Удалить профиль");
            _btnDelete.Click += (_, _) => DeleteRequested?.Invoke(this, EventArgs.Empty);

            actionsPanel.Controls.Add(_btnConfigure);
            actionsPanel.Controls.Add(_btnDelete);

            var detailsGroup = new GroupBox
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(10),
                Text = "Параметры"
            };

            var detailsLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2
            };
            detailsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200F));
            detailsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            AddValueRow(detailsLayout, 0, "Участвует в плане", out _lblInclusionValue);
            AddValueRow(detailsLayout, 1, "Норма часов ТО1", out _lblTo1HoursValue);
            AddValueRow(detailsLayout, 2, "Норма часов ТО2", out _lblTo2HoursValue);
            AddValueRow(detailsLayout, 3, "Норма часов ТО3", out _lblTo3HoursValue);

            detailsGroup.Controls.Add(detailsLayout);

            layout.Controls.Add(_lblSource, 0, 0);
            layout.Controls.Add(_lblSummary, 0, 1);
            layout.Controls.Add(actionsPanel, 0, 2);
            layout.Controls.Add(detailsGroup, 0, 3);
            Controls.Add(layout);

            ApplyState(_emptyState);
        }

        public event EventHandler? ConfigureRequested;

        public event EventHandler? DeleteRequested;

        public void ApplyState(KnowledgeBaseMaintenanceScheduleState state)
        {
            _currentState = state ?? _emptyState;

            _lblSource.Text = _currentState.SourceText;
            _lblSummary.Text = !string.IsNullOrWhiteSpace(_currentState.SummaryText)
                ? _currentState.SummaryText
                : _currentState.EmptyStateText;

            _lblInclusionValue.Text = _currentState.HasProfile ? _currentState.InclusionText : "-";
            _lblTo1HoursValue.Text = _currentState.HasProfile ? _currentState.To1HoursText : "-";
            _lblTo2HoursValue.Text = _currentState.HasProfile ? _currentState.To2HoursText : "-";
            _lblTo3HoursValue.Text = _currentState.HasProfile ? _currentState.To3HoursText : "-";

            _btnConfigure.Enabled = _currentState.SupportsEditing;
            _btnDelete.Enabled = _currentState.SupportsEditing && _currentState.HasProfile;
        }

        private static void AddValueRow(
            TableLayoutPanel layout,
            int rowIndex,
            string labelText,
            out Label valueLabel)
        {
            if (layout.RowCount <= rowIndex)
                layout.RowCount = rowIndex + 1;

            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var label = new Label
            {
                Text = labelText,
                AutoSize = true,
                Margin = new Padding(0, 4, 12, 8)
            };

            valueLabel = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Margin = new Padding(0, 4, 0, 8)
            };

            layout.Controls.Add(label, 0, rowIndex);
            layout.Controls.Add(valueLabel, 1, rowIndex);
        }

        private static Button CreateActionButton(string text) =>
            new()
            {
                Text = text,
                AutoSize = true,
                Margin = new Padding(0, 0, 8, 8)
            };
    }
}

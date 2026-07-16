namespace AsutpKnowledgeBase.Controls
{
    public sealed class HistorySuggestionDeletedEventArgs : EventArgs
    {
        public HistorySuggestionDeletedEventArgs(string value)
        {
            Value = value;
        }

        public string Value { get; }

        public bool Cancel { get; set; }
    }

    public sealed class HistoryInputControl : UserControl
    {
        private const int ControlHeight = 26;
        private const int DropDownRightInset = 38;
        private const int DropDownRowHeight = 32;
        private const int MaximumVisibleRows = 8;

        private readonly TextBox _textBox;
        private readonly ComboDropDownButton _dropDownButton;
        private readonly List<string> _suggestions = new();
        private ToolStripDropDown? _dropDown;

        public HistoryInputControl()
        {
            AutoScaleMode = AutoScaleMode.Font;
            Height = ControlHeight;
            MinimumSize = new Size(120, ControlHeight);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 28F));

            _textBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Margin = Padding.Empty
            };
            _dropDownButton = new ComboDropDownButton
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(2, 0, 0, 0),
                Padding = Padding.Empty,
                TabStop = false
            };
            _dropDownButton.Click += (_, _) => ToggleDropDown();

            layout.Controls.Add(_textBox, 0, 0);
            layout.Controls.Add(_dropDownButton, 1, 0);
            Controls.Add(layout);
            UpdateDropDownButtonState();
        }

        public string Value
        {
            get => _textBox.Text;
            set => _textBox.Text = value ?? string.Empty;
        }

        public event EventHandler<HistorySuggestionDeletedEventArgs>? SuggestionDeleted;

        public event EventHandler? InputValueChanged
        {
            add => _textBox.TextChanged += value;
            remove => _textBox.TextChanged -= value;
        }

        public void SetSuggestions(IEnumerable<string>? suggestions)
        {
            _suggestions.Clear();
            if (suggestions != null)
            {
                _suggestions.AddRange(suggestions
                    .Where(static value => !string.IsNullOrWhiteSpace(value))
                    .Distinct(StringComparer.OrdinalIgnoreCase));
            }

            CloseDropDown();
            UpdateDropDownButtonState();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                CloseDropDown();

            base.Dispose(disposing);
        }

        private void ToggleDropDown()
        {
            if (_dropDown is { Visible: true })
            {
                CloseDropDown();
                return;
            }

            ShowDropDown();
        }

        private void ShowDropDown()
        {
            CloseDropDown();
            if (_suggestions.Count == 0)
                return;

            int dropDownWidth = Math.Max(120, Width - DropDownRightInset);
            int visibleRowCount = Math.Min(_suggestions.Count, MaximumVisibleRows);
            int dropDownHeight = visibleRowCount * DropDownRowHeight;
            var rowsPanel = new Panel
            {
                AutoScroll = _suggestions.Count > MaximumVisibleRows,
                BackColor = SystemColors.Window,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                Size = new Size(dropDownWidth, dropDownHeight)
            };

            for (int index = 0; index < _suggestions.Count; index++)
            {
                Panel row = CreateSuggestionRow(_suggestions[index], dropDownWidth);
                row.Location = new Point(0, index * DropDownRowHeight);
                rowsPanel.Controls.Add(row);
            }

            var host = new ToolStripControlHost(rowsPanel)
            {
                AutoSize = false,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                Size = rowsPanel.Size
            };
            _dropDown = new ToolStripDropDown
            {
                AutoClose = true,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            _dropDown.Items.Add(host);
            _dropDown.Closed += (_, _) => _dropDown = null;
            _dropDown.Show(this, new Point(0, Height + 3));
        }

        private Panel CreateSuggestionRow(string value, int width)
        {
            var row = new Panel
            {
                BackColor = SystemColors.Window,
                Height = DropDownRowHeight,
                Width = width
            };
            var deleteArea = new Panel
            {
                BackColor = SystemColors.Control,
                Dock = DockStyle.Right,
                Padding = new Padding(6, 2, 3, 2),
                Width = 38
            };
            deleteArea.Paint += (_, e) =>
                e.Graphics.DrawLine(SystemPens.ControlDark, 0, 4, 0, deleteArea.Height - 5);

            var deleteButton = new Button
            {
                BackColor = SystemColors.Control,
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                ForeColor = SystemColors.GrayText,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                TabStop = false,
                Text = "×"
            };
            deleteButton.FlatAppearance.BorderSize = 0;
            deleteButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(246, 220, 220);
            deleteButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(253, 235, 235);
            deleteButton.MouseEnter += (_, _) => deleteButton.ForeColor = Color.Firebrick;
            deleteButton.MouseLeave += (_, _) => deleteButton.ForeColor = SystemColors.GrayText;
            deleteButton.Click += (_, _) => DeleteSuggestion(value);

            var valueLabel = new Label
            {
                AutoEllipsis = true,
                BackColor = SystemColors.Window,
                Dock = DockStyle.Fill,
                Padding = new Padding(6, 0, 4, 0),
                Text = value,
                TextAlign = ContentAlignment.MiddleLeft
            };
            valueLabel.Click += (_, _) => SelectSuggestion(value);

            row.Controls.Add(valueLabel);
            deleteArea.Controls.Add(deleteButton);
            row.Controls.Add(deleteArea);
            return row;
        }

        private void SelectSuggestion(string value)
        {
            Value = value;
            CloseDropDown();
            _textBox.Focus();
            _textBox.SelectionStart = _textBox.TextLength;
        }

        private void DeleteSuggestion(string value)
        {
            var eventArgs = new HistorySuggestionDeletedEventArgs(value);
            SuggestionDeleted?.Invoke(this, eventArgs);
            if (eventArgs.Cancel)
                return;

            _suggestions.RemoveAll(existing =>
                string.Equals(existing, value, StringComparison.OrdinalIgnoreCase));
            CloseDropDown();
            UpdateDropDownButtonState();
        }

        private void CloseDropDown()
        {
            ToolStripDropDown? dropDown = _dropDown;
            _dropDown = null;
            if (dropDown == null)
                return;

            dropDown.Close();
            dropDown.Dispose();
        }

        private void UpdateDropDownButtonState() =>
            _dropDownButton.Enabled = _suggestions.Count > 0;

        private sealed class ComboDropDownButton : Button
        {
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);

                float centerX = ClientSize.Width / 2F;
                float centerY = (ClientSize.Height / 2F) - 0.5F;
                PointF[] points =
                [
                    new PointF(centerX - 4F, centerY - 2F),
                    new PointF(centerX, centerY + 2F),
                    new PointF(centerX + 4F, centerY - 2F)
                ];
                using var pen = new Pen(
                    Enabled ? SystemColors.ControlText : SystemColors.GrayText,
                    1.4F)
                {
                    StartCap = System.Drawing.Drawing2D.LineCap.Round,
                    EndCap = System.Drawing.Drawing2D.LineCap.Round,
                    LineJoin = System.Drawing.Drawing2D.LineJoin.Round
                };
                System.Drawing.Drawing2D.SmoothingMode previousSmoothingMode = e.Graphics.SmoothingMode;
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.DrawLines(pen, points);
                e.Graphics.SmoothingMode = previousSmoothingMode;
            }
        }
    }
}

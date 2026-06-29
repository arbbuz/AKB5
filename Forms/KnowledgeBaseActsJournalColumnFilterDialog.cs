namespace AsutpKnowledgeBase
{
    public sealed class KnowledgeBaseActsJournalColumnFilterDialog : Form
    {
        private readonly CheckedListBox _valuesList;
        private readonly CheckBox _selectAllCheckBox;
        private readonly TextBox _searchBox;
        private readonly List<string> _allValues = new();
        private readonly HashSet<string> _selectedValues = new(StringComparer.Ordinal);
        private bool _isUpdatingChecks;

        public KnowledgeBaseActsJournalColumnFilterDialog(
            string columnTitle,
            IEnumerable<string> availableValues,
            IEnumerable<string> selectedValues)
        {
            Text = $"Фильтр: {columnTitle}";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(380, 460);
            AppIconProvider.Apply(this);

            var rootLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(12)
            };
            rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            _selectAllCheckBox = new CheckBox
            {
                Text = "Выбрать все",
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 8)
            };
            _selectAllCheckBox.CheckedChanged += SelectAllCheckBox_CheckedChanged;

            _searchBox = new TextBox
            {
                Dock = DockStyle.Top,
                PlaceholderText = "Поиск значения",
                Margin = new Padding(0, 0, 0, 8)
            };
            _searchBox.TextChanged += (_, _) => RefreshVisibleValues();

            _valuesList = new CheckedListBox
            {
                Dock = DockStyle.Fill,
                CheckOnClick = true,
                IntegralHeight = false
            };
            _valuesList.ItemCheck += ValuesList_ItemCheck;

            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                AutoSize = true,
                Margin = new Padding(0, 12, 0, 0)
            };

            var btnCancel = new Button
            {
                Text = "Отмена",
                DialogResult = DialogResult.Cancel,
                AutoSize = true
            };
            var btnOk = new Button
            {
                Text = "ОК",
                DialogResult = DialogResult.OK,
                AutoSize = true
            };
            var btnClear = new Button
            {
                Text = "Сбросить фильтр",
                AutoSize = true
            };
            btnClear.Click += (_, _) =>
            {
                ClearFilterRequested = true;
                DialogResult = DialogResult.OK;
                Close();
            };

            buttonPanel.Controls.Add(btnCancel);
            buttonPanel.Controls.Add(btnOk);
            buttonPanel.Controls.Add(btnClear);

            rootLayout.Controls.Add(_selectAllCheckBox, 0, 0);
            rootLayout.Controls.Add(_searchBox, 0, 1);
            rootLayout.Controls.Add(_valuesList, 0, 2);
            rootLayout.Controls.Add(buttonPanel, 0, 3);
            Controls.Add(rootLayout);

            AcceptButton = btnOk;
            CancelButton = btnCancel;

            PopulateValues(availableValues, selectedValues);
        }

        public bool ClearFilterRequested { get; private set; }

        public IReadOnlyList<string> SelectedValues =>
            _allValues
                .Where(_selectedValues.Contains)
                .ToList();

        private void PopulateValues(
            IEnumerable<string> availableValues,
            IEnumerable<string> selectedValues)
        {
            _allValues.Clear();
            _allValues.AddRange(availableValues
                .Select(static value => value?.Trim() ?? string.Empty)
                .Distinct(StringComparer.Ordinal));

            _selectedValues.Clear();
            foreach (string value in selectedValues
                .Select(static value => value?.Trim() ?? string.Empty)
                .Where(value => _allValues.Contains(value, StringComparer.Ordinal)))
            {
                _selectedValues.Add(value);
            }

            RefreshVisibleValues();
        }

        private void RefreshVisibleValues()
        {
            string searchText = _searchBox.Text.Trim();
            _isUpdatingChecks = true;
            try
            {
                _valuesList.Items.Clear();
                foreach (string value in _allValues.Where(value => MatchesSearch(value, searchText)))
                {
                    var item = new FilterValueItem(value);
                    int index = _valuesList.Items.Add(item);
                    _valuesList.SetItemChecked(index, _selectedValues.Contains(value));
                }

                _selectAllCheckBox.Enabled = _valuesList.Items.Count > 0;
                UpdateSelectAllCheckState();
            }
            finally
            {
                _isUpdatingChecks = false;
            }
        }

        private void SelectAllCheckBox_CheckedChanged(object? sender, EventArgs e)
        {
            if (_isUpdatingChecks)
                return;

            bool isChecked = _selectAllCheckBox.Checked;
            _isUpdatingChecks = true;
            try
            {
                for (int i = 0; i < _valuesList.Items.Count; i++)
                {
                    if (_valuesList.Items[i] is not FilterValueItem item)
                        continue;

                    if (isChecked)
                        _selectedValues.Add(item.Value);
                    else
                        _selectedValues.Remove(item.Value);

                    _valuesList.SetItemChecked(i, isChecked);
                }
            }
            finally
            {
                _isUpdatingChecks = false;
            }
        }

        private void ValuesList_ItemCheck(object? sender, ItemCheckEventArgs e)
        {
            if (_isUpdatingChecks)
                return;

            if (_valuesList.Items[e.Index] is FilterValueItem item)
            {
                if (e.NewValue == CheckState.Checked)
                    _selectedValues.Add(item.Value);
                else
                    _selectedValues.Remove(item.Value);
            }

            BeginInvoke(new Action(UpdateSelectAllCheckState));
        }

        private void UpdateSelectAllCheckState()
        {
            if (_valuesList.Items.Count == 0)
            {
                _selectAllCheckBox.Checked = false;
                return;
            }

            _isUpdatingChecks = true;
            try
            {
                _selectAllCheckBox.Checked = _valuesList.CheckedItems.Count == _valuesList.Items.Count;
            }
            finally
            {
                _isUpdatingChecks = false;
            }
        }

        private static bool MatchesSearch(string value, string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return true;

            string displayValue = string.IsNullOrEmpty(value) ? "(пусто)" : value;
            return displayValue.Contains(searchText, StringComparison.CurrentCultureIgnoreCase);
        }

        private sealed class FilterValueItem
        {
            public FilterValueItem(string value)
            {
                Value = value;
            }

            public string Value { get; }

            public override string ToString() =>
                string.IsNullOrEmpty(Value) ? "(пусто)" : Value;
        }
    }
}

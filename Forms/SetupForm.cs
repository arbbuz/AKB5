using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase
{
    /// <summary>
    /// Форма настройки структуры: количество уровней и их названия для подсказок.
    /// </summary>
    public class SetupForm : Form
    {
        private static readonly string[] DefaultLevelNames =
        {
            "Цех",
            "Отделение",
            "Участок",
            "Линия",
            "Щит",
            "Контроллер",
            "Модуль",
            "Датчик",
            "Устройство",
            "Параметр"
        };

        public KbConfig Config { get; }

        private readonly FlowLayoutPanel _pnlInputs;
        private readonly NumericUpDown _nudCount;
        private readonly List<TextBox> _levelTextBoxes = new();

        public SetupForm(KbConfig current)
        {
            Config = new KbConfig
            {
                MaxLevels = current.MaxLevels,
                LevelNames = new List<string>(current.LevelNames)
            };

            Text = "Настройка структуры";
            Size = new Size(450, 600);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            _pnlInputs = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Margin = new Padding(20),
                FlowDirection = FlowDirection.TopDown
            };
            _nudCount = new NumericUpDown
            {
                Minimum = 2,
                Maximum = 50,
                Value = Math.Max(2, Config.MaxLevels),
                Width = 80,
                Margin = new Padding(5),
                Font = new Font("Microsoft Sans Serif", 10, FontStyle.Bold)
            };

            var btnApply = new Button
            {
                Text = "✅ Применить изменения",
                Dock = DockStyle.Bottom,
                Height = 45,
                Margin = new Padding(20, 15, 20, 20),
                Font = new Font("Microsoft Sans Serif", 10, FontStyle.Bold),
                BackColor = Color.LightGreen
            };

            Controls.Add(btnApply);
            Controls.Add(_pnlInputs);

            _nudCount.ValueChanged += (s, e) => RefreshInputs();
            btnApply.Click += (s, e) =>
            {
                ReadInputs();
                DialogResult = DialogResult.OK;
            };

            RefreshInputs();
        }

        private void RefreshInputs()
        {
            var currentValues = new List<string>();
            foreach (var tb in _levelTextBoxes)
                currentValues.Add(tb.Text);

            _pnlInputs.Controls.Clear();
            _levelTextBoxes.Clear();

            _pnlInputs.Controls.Add(new Label
            {
                Text = "Количество уровней:",
                AutoSize = true,
                Margin = new Padding(5),
                Font = new Font("Microsoft Sans Serif", 10, FontStyle.Bold)
            });
            _pnlInputs.Controls.Add(new Label
            {
                Text = "💡 Уровни не отображаются в дереве. Они только контролируют глубину.",
                AutoSize = true,
                Margin = new Padding(5, 0, 5, 10),
                ForeColor = Color.DarkBlue,
                Font = new Font("Microsoft Sans Serif", 9, FontStyle.Italic)
            });
            _pnlInputs.Controls.Add(_nudCount);

            int count = (int)_nudCount.Value;
            _pnlInputs.Controls.Add(new Label
            {
                Text = "\nНазвания уровней (для подсказок):",
                AutoSize = true,
                Margin = new Padding(5, 15, 5, 10),
                Font = new Font("Microsoft Sans Serif", 9, FontStyle.Bold)
            });

            for (int i = 0; i < count; i++)
            {
                string existing;
                if (i < currentValues.Count && !string.IsNullOrWhiteSpace(currentValues[i]))
                    existing = currentValues[i];
                else if (i < Config.LevelNames.Count && !string.IsNullOrWhiteSpace(Config.LevelNames[i]))
                    existing = Config.LevelNames[i];
                else if (i < DefaultLevelNames.Length)
                    existing = DefaultLevelNames[i];
                else
                    existing = $"Уровень {i + 1}";

                _pnlInputs.Controls.Add(new Label
                {
                    Text = $"Ур. {i + 1}:",
                    AutoSize = true,
                    Width = 60,
                    Margin = new Padding(5, 5, 5, 0)
                });

                var textBox = new TextBox
                {
                    Text = existing,
                    Width = 280,
                    Tag = i,
                    Margin = new Padding(0, 5, 5, 5),
                    Font = new Font("Microsoft Sans Serif", 9)
                };

                _levelTextBoxes.Add(textBox);
                _pnlInputs.Controls.Add(textBox);
            }
        }

        private void ReadInputs()
        {
            Config.MaxLevels = (int)_nudCount.Value;
            Config.LevelNames.Clear();
            foreach (var tb in _levelTextBoxes)
                Config.LevelNames.Add(tb.Text.Trim());
        }
    }
}

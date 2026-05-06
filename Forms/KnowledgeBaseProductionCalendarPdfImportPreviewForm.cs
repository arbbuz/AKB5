using System.Globalization;
using System.Text;
using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase
{
    public sealed class KnowledgeBaseProductionCalendarPdfImportPreviewForm : Form
    {
        private const string RussianDateFormat = "dd.MM.yyyy";

        private readonly KnowledgeBaseProductionCalendarPdfImportResult _importResult;
        private readonly string _sourceFileName;

        public KnowledgeBaseProductionCalendarPdfImportPreviewForm(
            KnowledgeBaseProductionCalendarPdfImportResult importResult,
            string sourceFileName)
        {
            _importResult = importResult;
            _sourceFileName = sourceFileName;
            ResultYears = importResult.ProductionCalendarYears;
            InitializeComponent();
        }

        public IReadOnlyList<KbProductionCalendarYear> ResultYears { get; }

        private void InitializeComponent()
        {
            Text = "Импорт производственного календаря из PDF";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(720, 520);
            Size = new Size(860, 620);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(12)
            };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            root.Controls.Add(new Label
            {
                Text = "Проверьте найденные даты перед применением.",
                AutoSize = true,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 8)
            }, 0, 0);

            var txtPreview = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 10F),
                Text = BuildPreviewText()
            };
            root.Controls.Add(txtPreview, 0, 1);

            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                Margin = new Padding(0, 10, 0, 0)
            };

            var btnApply = new Button
            {
                Text = "Применить",
                DialogResult = DialogResult.OK,
                Width = 110,
                Height = 32
            };

            var btnCancel = new Button
            {
                Text = "Отмена",
                DialogResult = DialogResult.Cancel,
                Width = 100,
                Height = 32
            };

            buttonPanel.Controls.Add(btnApply);
            buttonPanel.Controls.Add(btnCancel);
            root.Controls.Add(buttonPanel, 0, 2);

            Controls.Add(root);
            AcceptButton = btnApply;
            CancelButton = btnCancel;
        }

        private string BuildPreviewText()
        {
            var builder = new StringBuilder();
            builder.AppendLine($"Файл: {_sourceFileName}");
            builder.AppendLine($"Найдено лет: {_importResult.ImportedYearCount}");
            builder.AppendLine();

            foreach (KbProductionCalendarYear year in _importResult.ProductionCalendarYears.OrderBy(static year => year.Year))
            {
                builder.AppendLine($"Год: {year.Year}");
                builder.AppendLine($"Дополнительные нерабочие дни: {year.AdditionalNonWorkingDays.Count}");
                AppendDates(builder, year.AdditionalNonWorkingDays);
                builder.AppendLine();
                builder.AppendLine($"Дополнительные рабочие дни: {year.AdditionalWorkingDays.Count}");
                AppendDates(builder, year.AdditionalWorkingDays);
                builder.AppendLine();
            }

            if (_importResult.Warnings.Count > 0)
            {
                builder.AppendLine("Предупреждения:");
                foreach (string warning in _importResult.Warnings)
                    builder.AppendLine($"- {warning}");
            }
            else
            {
                builder.AppendLine("Предупреждений нет.");
            }

            return builder.ToString();
        }

        private static void AppendDates(StringBuilder builder, IEnumerable<DateOnly> dates)
        {
            string[] formattedDates = dates
                .OrderBy(static date => date)
                .Select(static date => date.ToString(RussianDateFormat, CultureInfo.InvariantCulture))
                .ToArray();

            if (formattedDates.Length == 0)
            {
                builder.AppendLine("  - нет");
                return;
            }

            foreach (string date in formattedDates)
                builder.AppendLine($"  - {date}");
        }
    }
}

using System.Globalization;
using System.Runtime.InteropServices;

namespace AsutpKnowledgeBase.Services
{
    /// <summary>
    /// Сравнивает строки в natural/logical порядке, близком к проводнику Windows:
    /// без учета регистра и с корректной обработкой чисел внутри имени.
    /// </summary>
    public sealed class KnowledgeBaseNaturalStringComparer : IComparer<string>
    {
        public static KnowledgeBaseNaturalStringComparer Instance { get; } = new();

        private KnowledgeBaseNaturalStringComparer()
        {
        }

        public int Compare(string? x, string? y)
        {
            if (ReferenceEquals(x, y))
                return 0;
            if (x is null)
                return -1;
            if (y is null)
                return 1;

            int result = OperatingSystem.IsWindows()
                ? StrCmpLogicalW(x, y)
                : CultureInfo.CurrentCulture.CompareInfo.Compare(x, y, CompareOptions.IgnoreCase);

            if (result != 0)
                return result;

            return string.Equals(x, y, StringComparison.CurrentCultureIgnoreCase)
                ? 0
                : string.Compare(x, y, StringComparison.Ordinal);
        }

        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern int StrCmpLogicalW(string left, string right);
    }
}

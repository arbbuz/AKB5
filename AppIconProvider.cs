namespace AsutpKnowledgeBase
{
    internal static class AppIconProvider
    {
        private static Icon? _cachedIcon;

        public static void Apply(Form form)
        {
            ArgumentNullException.ThrowIfNull(form);

            Icon? icon = GetIcon();
            if (icon != null)
                form.Icon = (Icon)icon.Clone();
        }

        private static Icon? GetIcon()
        {
            if (_cachedIcon != null)
                return _cachedIcon;

            string iconPath = Path.Combine(AppContext.BaseDirectory, "resources", "app.ico");
            if (File.Exists(iconPath))
            {
                try
                {
                    _cachedIcon = new Icon(iconPath);
                    return _cachedIcon;
                }
                catch
                {
                }
            }

            try
            {
                _cachedIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch
            {
            }

            return _cachedIcon;
        }
    }
}

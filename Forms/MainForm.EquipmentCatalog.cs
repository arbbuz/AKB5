namespace AsutpKnowledgeBase
{
    public partial class MainForm
    {
        private void EditEquipmentCatalog(object? sender, EventArgs e)
        {
            SaveCurrentWorkshopState();

            using var dialog = new KnowledgeBaseEquipmentCatalogForm(_session.EquipmentCatalogItems);
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            _session.ReplaceEquipmentCatalogItems(dialog.ResultItems);
            UpdateDirtyState();
            UpdateUI(refreshSelectedNodeState: false);
            SetLastActionText($"Каталог оборудования обновлён: {_session.EquipmentCatalogItems.Count} зап.");
        }
    }
}

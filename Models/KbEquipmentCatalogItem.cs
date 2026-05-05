namespace AsutpKnowledgeBase.Models
{
    public class KbEquipmentCatalogItem
    {
        public string CatalogItemId { get; set; } = string.Empty;
        public string EquipmentKind { get; set; } = string.Empty;
        public string Manufacturer { get; set; } = string.Empty;
        public string Series { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public KbNodeType DefaultNodeType { get; set; } = KbNodeType.Device;
        public string Description { get; set; } = string.Empty;
        public List<KbEquipmentCatalogProperty> Properties { get; set; } = new();
    }

    public class KbEquipmentCatalogProperty
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}

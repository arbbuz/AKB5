namespace AsutpKnowledgeBase.Models
{
    public class KbObjectTemplate
    {
        public string TemplateId { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public KbObjectTemplateNode RootNode { get; set; } = new();

        public List<KbObjectTemplateCompositionEntry> CompositionEntries { get; set; } = new();

        public List<KbObjectTemplateDocumentLink> DocumentLinks { get; set; } = new();

        public List<KbObjectTemplateSoftwareRecord> SoftwareRecords { get; set; } = new();

        public List<KbObjectTemplateNetworkFileReference> NetworkFileReferences { get; set; } = new();

        public List<KbObjectTemplateMaintenanceScheduleProfile> MaintenanceScheduleProfiles { get; set; } = new();

        public List<KbObjectTemplateNetworkInterfaceStub> NetworkInterfaceStubs { get; set; } = new();
    }

    public class KbObjectTemplateNode
    {
        public string TemplateNodeId { get; set; } = string.Empty;

        public string CatalogItemId { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public KbNodeType NodeType { get; set; } = KbNodeType.Device;

        public KbNodeDetails Details { get; set; } = new();

        public List<KbObjectTemplateNode> Children { get; set; } = new();
    }

    public class KbObjectTemplateCompositionEntry
    {
        public string ParentTemplateNodeId { get; set; } = string.Empty;

        public int? SlotNumber { get; set; }

        public int PositionOrder { get; set; }

        public string ComponentType { get; set; } = string.Empty;

        public string Model { get; set; } = string.Empty;

        public string IpAddress { get; set; } = string.Empty;

        public DateTime? LastCalibrationAt { get; set; }

        public DateTime? NextCalibrationAt { get; set; }

        public string Notes { get; set; } = string.Empty;
    }

    public class KbObjectTemplateDocumentLink
    {
        public string OwnerTemplateNodeId { get; set; } = string.Empty;

        public KbDocumentKind Kind { get; set; } = KbDocumentKind.Manual;

        public string Title { get; set; } = string.Empty;

        public string Path { get; set; } = string.Empty;

        public DateTime? UpdatedAt { get; set; }
    }

    public class KbObjectTemplateSoftwareRecord
    {
        public string OwnerTemplateNodeId { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Path { get; set; } = string.Empty;

        public DateTime? AddedAt { get; set; }

        public DateTime? LastChangedAt { get; set; }

        public DateTime? LastBackupAt { get; set; }

        public string Notes { get; set; } = string.Empty;
    }

    public class KbObjectTemplateNetworkFileReference
    {
        public string OwnerTemplateNodeId { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Path { get; set; } = string.Empty;
    }

    public class KbObjectTemplateMaintenanceScheduleProfile
    {
        public string OwnerTemplateNodeId { get; set; } = string.Empty;

        public bool IsIncludedInSchedule { get; set; }

        public int To1Hours { get; set; }

        public int To2Hours { get; set; }

        public int To3Hours { get; set; }

        public List<KbMaintenanceYearScheduleEntry> YearScheduleEntries { get; set; } = new();
    }

    public class KbObjectTemplateNetworkInterfaceStub
    {
        public string OwnerTemplateNodeId { get; set; } = string.Empty;

        public string InterfaceId { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string IpAddress { get; set; } = string.Empty;

        public string SubnetMask { get; set; } = string.Empty;

        public string Gateway { get; set; } = string.Empty;

        public string Protocol { get; set; } = string.Empty;

        public string Notes { get; set; } = string.Empty;
    }
}

namespace AsutpKnowledgeBase.Models
{
    public enum KbActType
    {
        InspectionWork = 0,
        EquipmentFailure = 1
    }

    public enum KbActStatus
    {
        Draft = 0,
        Generated = 1,
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Naming",
            "CA1720:Identifier contains type name",
            Justification = "The accepted acts roadmap uses Signed as the act status name.")]
        Signed = 2,
        Cancelled = 3,
        Archived = 4,
        Annulled = 5
    }

    public class KbAct
    {
        public string ActId { get; set; } = string.Empty;

        public int ActYear { get; set; }

        public string ActNumber { get; set; } = string.Empty;

        public KbActType ActType { get; set; } = KbActType.EquipmentFailure;

        public KbActStatus Status { get; set; } = KbActStatus.Draft;

        public DateTime? ActDate { get; set; }

        public string WorkshopName { get; set; } = string.Empty;

        public string Lvl3NodeId { get; set; } = string.Empty;

        public string Lvl3NameSnapshot { get; set; } = string.Empty;

        public string ObjectNameSnapshot { get; set; } = string.Empty;

        public string ObjectPathSnapshot { get; set; } = string.Empty;

        public string RackId { get; set; } = string.Empty;

        public int? RackNumberSnapshot { get; set; }

        public string RackNameSnapshot { get; set; } = string.Empty;

        public string CompositionEntryId { get; set; } = string.Empty;

        public string EquipmentName { get; set; } = string.Empty;

        public KbActEquipmentSnapshot EquipmentSnapshot { get; set; } = new();

        public DateTime? FailureDate { get; set; }

        public string FaultDescription { get; set; } = string.Empty;

        public string FailureReason { get; set; } = string.Empty;

        public string InspectionResult { get; set; } = string.Empty;

        public string FaultCriterion { get; set; } = string.Empty;

        public string RequestDocument { get; set; } = string.Empty;

        public string ActualLaborHours { get; set; } = string.Empty;

        public string CustomerName { get; set; } = string.Empty;

        public string CustomerPosition { get; set; } = string.Empty;

        public string ApproverName { get; set; } = string.Empty;

        public string ApproverPosition { get; set; } = string.Empty;

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}

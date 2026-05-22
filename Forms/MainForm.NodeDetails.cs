using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase
{
    public partial class MainForm
    {
        private void HandleNodeDetailsChanged(Action<KbNodeDetails> updateDetails)
        {
            if (_isApplyingSelectedNodeState || tvTree.SelectedNode?.Tag is not KbNode selectedNode)
                return;

            int visibleLevel = _nodePresentationService.GetVisibleLevel(GetVisibleTreeData(), selectedNode);
            selectedNode.Details ??= new KbNodeDetails();
            updateDetails(selectedNode.Details);

            if (!KnowledgeBaseNodeMetadataService.SupportsInventoryNumber(visibleLevel))
                selectedNode.Details.InventoryNumber = string.Empty;

            if (!KnowledgeBaseNodeMetadataService.SupportsLocation(visibleLevel))
                selectedNode.Details.Location = string.Empty;

            if (!KnowledgeBaseNodeMetadataService.SupportsPhoto(visibleLevel))
                selectedNode.Details.PhotoPath = string.Empty;

            if (!KnowledgeBaseNodeMetadataService.SupportsNetworkTopology(visibleLevel))
                selectedNode.Details.NetworkTopology = new KbNetworkTopology();

            if (!KnowledgeBaseNodeMetadataService.SupportsTechnicalFields(selectedNode.NodeType, visibleLevel))
            {
                selectedNode.Details.IpAddress = string.Empty;
                selectedNode.Details.SchemaLink = string.Empty;
            }

            UpdateDirtyState();
            UpdateUI(refreshSelectedNodeState: false);
        }
    }
}

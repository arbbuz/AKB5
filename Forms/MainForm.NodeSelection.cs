using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase
{
    public partial class MainForm
    {
        private bool TryGetSelectedTreeNode(out KbNode selectedNode)
        {
            if (tvTree.SelectedNode?.Tag is KbNode node)
            {
                selectedNode = node;
                return true;
            }

            selectedNode = new KbNode();
            return false;
        }

        private int GetVisibleLevelForNode(KbNode node) =>
            _nodePresentationService.GetVisibleLevel(GetVisibleTreeData(), node);
    }
}

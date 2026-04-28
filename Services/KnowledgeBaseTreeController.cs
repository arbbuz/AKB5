using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    /// <summary>
    /// Координирует прикладные операции над деревом знаний и буфером копирования.
    /// Не зависит от UI и может тестироваться отдельно.
    /// </summary>
    public class KnowledgeBaseTreeController
    {
        private readonly KnowledgeBaseSessionService _session;

        public KnowledgeBaseTreeController(KnowledgeBaseSessionService session)
        {
            _session = session;
        }

        public bool HasClipboardNode => ClipboardNode != null;

        public KbNode? ClipboardNode { get; private set; }

        public void ClearClipboard() => ClipboardNode = null;

        public bool CanAddNode(KbNode? parentNode)
        {
            var service = CreateKnowledgeBaseService();
            return parentNode == null ? service.CanAddRootNode() : service.CanAddChild(parentNode);
        }

        public KbNode AddNode(string workshopName, KbNode? parentNode, string nodeName) =>
            AddNode(
                workshopName,
                parentNode,
                new KbNode
                {
                    Name = nodeName.Trim()
                });

        public KbNode AddNode(string workshopName, KbNode? parentNode, KbNode newNode)
        {
            var service = CreateKnowledgeBaseService();

            if (parentNode == null)
                service.AddRootNode(workshopName, newNode);
            else
                service.AddChildNode(parentNode, newNode);

            return newNode;
        }

        public bool DeleteNode(string workshopName, KbNode nodeToRemove) =>
            CreateKnowledgeBaseService().DeleteNode(workshopName, nodeToRemove);

        public void CopyNode(KbNode node) =>
            ClipboardNode = CreateKnowledgeBaseService().CloneNode(node, preserveNodeIds: false);

        public bool CanPasteNode(KbNode? parentNode) =>
            ClipboardNode != null &&
            parentNode != null &&
            CreateKnowledgeBaseService().CanAttachSubtree(parentNode, ClipboardNode);

        public KbNode PasteNode(KbNode parentNode)
        {
            var service = CreateKnowledgeBaseService();
            var newNode = service.CloneNode(ClipboardNode!, preserveNodeIds: false);
            service.AddChildNode(parentNode, newNode);
            return newNode;
        }

        public void RenameNode(KbNode node, string newName) =>
            node.Name = newName.Trim();

        public bool CanMoveNode(KbNode targetNode, KbNode draggedNode) =>
            !WouldCreateCycle(targetNode, draggedNode) &&
            CreateKnowledgeBaseService().CanAttachSubtree(targetNode, draggedNode);

        public bool WouldCreateCycle(KbNode targetNode, KbNode draggedNode) =>
            CreateKnowledgeBaseService().ContainsNode(draggedNode, targetNode);

        public bool MoveNode(string workshopName, KbNode draggedNode, KbNode? oldParentNode, KbNode targetNode)
        {
            var service = CreateKnowledgeBaseService();

            bool removed = oldParentNode != null
                ? oldParentNode.Children.Remove(draggedNode)
                : service.DeleteNode(workshopName, draggedNode);

            if (!removed)
                return false;

            service.AddChildNode(targetNode, draggedNode);
            return true;
        }

        private KnowledgeBaseService CreateKnowledgeBaseService() => new(_session.Config, _session.Workshops);
    }
}

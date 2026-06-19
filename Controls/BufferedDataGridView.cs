namespace AsutpKnowledgeBase
{
    internal sealed class BufferedDataGridView : DataGridView
    {
        public BufferedDataGridView()
        {
            DoubleBuffered = true;
        }
    }
}

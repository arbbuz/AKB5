using System.Text.Json;

namespace AsutpKnowledgeBase.Services
{
    public class UndoRedoService
    {
        private static readonly JsonSerializerOptions SnapshotOptions = new() { WriteIndented = false };

        private readonly List<string> _undoHistory = new();
        private readonly Stack<string> _redoStack = new();
        private readonly int _maxUndoSteps;

        public UndoRedoService(int maxUndoSteps = 50)
        {
            _maxUndoSteps = maxUndoSteps > 0 ? maxUndoSteps : 50;
        }

        public bool CanUndo => _undoHistory.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        public void SaveState<T>(T data) => SaveState(JsonSerializer.Serialize(data, SnapshotOptions));

        public void SaveState(string snapshot)
        {
            _undoHistory.Add(snapshot);
            if (_undoHistory.Count > _maxUndoSteps)
                _undoHistory.RemoveAt(0);

            _redoStack.Clear();
        }

        public string? Undo(string currentState)
        {
            if (!CanUndo)
                return null;

            _redoStack.Push(currentState);
            int lastIndex = _undoHistory.Count - 1;
            string snapshot = _undoHistory[lastIndex];
            _undoHistory.RemoveAt(lastIndex);
            return snapshot;
        }

        public string? Redo(string currentState)
        {
            if (!CanRedo)
                return null;

            _undoHistory.Add(currentState);
            if (_undoHistory.Count > _maxUndoSteps)
                _undoHistory.RemoveAt(0);

            return _redoStack.Pop();
        }

        public void Clear()
        {
            _undoHistory.Clear();
            _redoStack.Clear();
        }
    }
}

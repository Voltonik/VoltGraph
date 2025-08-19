using System.Collections.Generic;

public interface IUndoRedoAction {
    void Undo();
    void Redo();
}

public class UndoRedoSystem {
    private Stack<IUndoRedoAction> m_undoStack = new Stack<IUndoRedoAction>();
    private Stack<IUndoRedoAction> m_redoStack = new Stack<IUndoRedoAction>();

    public void AddAction(IUndoRedoAction action) {
        m_undoStack.Push(action);
        m_redoStack.Clear();
    }

    public void ClearActions() {
        m_undoStack.Clear();
        m_redoStack.Clear();
    }

    public void Undo() {
        if (m_undoStack.Count > 0) {
            IUndoRedoAction action = m_undoStack.Pop();
            action.Undo();
            m_redoStack.Push(action);
        }
    }

    public void Redo() {
        if (m_redoStack.Count > 0) {
            IUndoRedoAction action = m_redoStack.Pop();
            action.Redo();
            m_undoStack.Push(action);
        }
    }
}

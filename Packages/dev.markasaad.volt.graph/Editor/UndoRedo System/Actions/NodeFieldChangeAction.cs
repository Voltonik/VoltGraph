using System.Reflection;

using UnityEngine.UIElements;

public class NodeFieldChangeAction<TField> : IUndoRedoAction {
    private BaseField<TField> m_field;
    private FieldInfo m_fieldInfo;
    private object m_obj;
    private object m_changedData;

    private bool m_setToIndex;

    public NodeFieldChangeAction(BaseField<TField> field, FieldInfo fieldInfo, object obj, bool setToIndex = false) {
        m_field = field;
        m_fieldInfo = fieldInfo;
        m_obj = obj;
        m_setToIndex = setToIndex;

        m_changedData = setToIndex ? (int)fieldInfo.GetValue(obj) : (TField)fieldInfo.GetValue(obj);
    }

    public void Undo() {
        ToggleValue();
    }

    public void Redo() {
        ToggleValue();
    }

    private void ToggleValue() {
        if (m_setToIndex) {
            var popupField = (PopupField<TField>)m_field;
            var previousChange = popupField.index;

            popupField.index = (int)m_changedData;
            m_fieldInfo.SetValue(m_obj, (int)m_changedData);
            m_changedData = previousChange;
        } else {
            var previousChange = m_field.value;

            m_field.SetValueWithoutNotify((TField)m_changedData);
            m_fieldInfo.SetValue(m_obj, m_changedData);
            m_changedData = previousChange;
        }
    }
}

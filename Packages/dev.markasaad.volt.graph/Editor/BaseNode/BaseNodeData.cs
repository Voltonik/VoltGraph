using System;

using UnityEngine;

[Serializable]
public abstract class BaseNodeData<TNodeFields> {
    public string GUID;
    public Vector2 Position;
    public bool EntryPoint;
    public TNodeFields FieldsData;
    public int ExtraPorts;
}
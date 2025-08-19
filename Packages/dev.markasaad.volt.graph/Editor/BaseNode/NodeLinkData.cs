using System;

[Serializable]
public struct NodeLinkData {
    public string BaseNodeGUID;
    public string PortName;
    public string TargetNodeGUID;
    public int InputIndex;
    public int OutputIndex;
}
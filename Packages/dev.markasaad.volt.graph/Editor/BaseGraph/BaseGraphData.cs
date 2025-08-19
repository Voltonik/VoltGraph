using System;
using System.Collections.Generic;

using UnityEngine;

[Serializable]
public abstract class BaseGraphData : ScriptableObject {
    public List<NodeLinkData> NodeLinks = new List<NodeLinkData>();
}
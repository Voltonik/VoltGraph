public abstract class SaveableNode<TGraphData> : BaseNode {
    public virtual object GetData() => null;
    public virtual void SetData(object data) { }
    public virtual object GetDefaultDataInstance() => null;

    public virtual void SaveNodeTo(TGraphData graphData) { }
}
namespace RICHYEngine.Views.Holders.GraphHolder
{
    public interface IGraphPointDrawer : ISingleCanvasElement
    {
        IGraphPointValue? graphPointValue { get; set; }
    }
}

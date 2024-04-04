namespace RICHYEngine.Views.Holders.GraphHolder.Elements
{
    public interface IGraphPointDrawer : ISingleCanvasElement
    {
        IGraphPointValue? graphPointValue { get; set; }
    }
}

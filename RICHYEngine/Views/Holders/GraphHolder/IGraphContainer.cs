namespace RICHYEngine.Views.Holders.GraphHolder
{
    public interface IGraphContainer
    {
        ICanvasHolder PointAndLineCanvasHolder { get; }
        ICanvasHolder LabelXCanvasHolder { get; }
        ICanvasHolder LabelYCanvasHolder { get; }
        ICanvasHolder AxisCanvasHolder { get; }
        ICanvasHolder GridDashCanvasHolder { get; }
        ICanvasHolder PopupCanvasHolder { get; }
        public float GraphHeight { get; }
        public float GraphWidth { get; }

        public void Clear()
        {
            PointAndLineCanvasHolder.Clear();
            LabelXCanvasHolder.Clear();
            LabelYCanvasHolder.Clear();
            AxisCanvasHolder.Clear();
            GridDashCanvasHolder.Clear();
        }
    }
}

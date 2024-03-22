using System.Numerics;

namespace RICHYEngine.Views.Holders.GraphHolder
{
    public interface IGraphLabelDrawer : ISingleCanvasElement
    {
        void SetText(string text);

        float DesiredHeight { get; }
        float DesiredWidth { get; }
    }
}

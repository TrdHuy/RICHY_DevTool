using System.Numerics;

namespace RICHYEngine.Views.Holders.GraphHolder.Elements
{
    public interface IGraphLineDrawer : ICanvasChild
    {
        void SetPositionOnCanvas(Vector2 firstPoint, Vector2 secondPoint);
    }
}

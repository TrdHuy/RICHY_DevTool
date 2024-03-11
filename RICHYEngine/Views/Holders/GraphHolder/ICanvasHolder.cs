using System.Numerics;

namespace RICHYEngine.Views.Holders.GraphHolder
{
    public interface ICanvasHolder
    {
        void SetCanvasPosition(Vector2 position);

        public void Clear();

        bool AddChild(ICanvasChild child);

        bool RemoveChild(ICanvasChild child);
    }
}

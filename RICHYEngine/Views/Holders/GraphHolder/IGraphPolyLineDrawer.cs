using System.Numerics;

namespace RICHYEngine.Views.Holders.GraphHolder
{
    public interface IGraphPolyLineDrawer : ICanvasChild
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="point"></param>
        /// <returns>Inserted index</returns>
        int AddNewPoint(Vector2 point, bool toLast = true);

        void RemovePoint(Vector2 point);

        void ChangePointPosition(Vector2 oldPos, Vector2 newPos);
        void ChangePointPosition(int pointIndex, Vector2 newPos);

        int TotalPointCount { get; }

        [Obsolete("For debugging")]
        object Drawer { get; }
    }
}

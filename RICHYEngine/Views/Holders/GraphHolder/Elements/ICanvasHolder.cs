using System.Numerics;

namespace RICHYEngine.Views.Holders.GraphHolder.Elements
{
    public interface ICanvasHolder
    {
        void SetCanvasPosition(Vector2 position);

        /// <summary>
        /// Clear all of the child from parents
        /// </summary>
        /// <returns>List of the childs has been cleared</returns>

        public HashSet<ICanvasChild> Clear();

        bool AddChild(ICanvasChild child);

        bool RemoveChild(ICanvasChild child);
    }

    public static class CanvasHolderProxy
    {
        public static void AddCanvasChildAndAssignNewState(this ICanvasHolder holder, ICanvasChild child, CanvasChildStatus pointStats)
        {
            holder.AddChild(child);
            pointStats.IsAttachedToParent = true;
        }

        public static void RemoveCanvasChildAndAssignNewState(this ICanvasHolder holder, ICanvasChild child, CanvasChildStatus pointStats)
        {
            holder.RemoveChild(child);
            pointStats.IsAttachedToParent = false;
        }

        public static void ClearAllChildAndAssignNewState(this ICanvasHolder holder, ElementCacheCollection<ICanvasChild, CanvasChildStatus> cache)
        {
            var childCache = holder.Clear();
            foreach (var child in childCache)
            {
                cache.GetElementStat(child).IsAttachedToParent = false;
            }
        }
    }
}

using System.Collections.ObjectModel;
using System.Numerics;

namespace RICHYEngine.Views.Holders.GraphHolder.Elements
{
    public class CanvasChildStatus
    {
        public bool IsAttachedToParent { get; set; }
    }

    public class ElementCacheCollection<ELEMENT, ELESTATE>
        where ELESTATE : new()
        where ELEMENT : ICanvasChild
    {
        public Collection<ELEMENT> canvasElements = new Collection<ELEMENT>();
        public Dictionary<ELEMENT, ELESTATE> canvasElementMap = new Dictionary<ELEMENT, ELESTATE>();

        public ELESTATE AddElementToCache(ELEMENT drawer, bool toLast = true)
        {
            if (toLast)
            {
                canvasElements.Add(drawer);
            }
            else
            {
                canvasElements.Insert(0, drawer);
            }
            var state = new ELESTATE();
            canvasElementMap[drawer] = state;
            return state;
        }

        public int GetElementCacheCount()
        {
            return canvasElements.Count;
        }

        public ELESTATE GetElementStat(ELEMENT ele)
        {
            return canvasElementMap[ele];
        }

        public ELEMENT GetElementAt(int pointIndex)
        {
            return canvasElements[pointIndex];
        }

        public void RemoveElementAt(int pointIndex)
        {
            canvasElementMap.Remove(canvasElements[pointIndex]);
            canvasElements.RemoveAt(pointIndex);
        }

        public void ClearCache()
        {
            canvasElements.Clear();
            canvasElementMap.Clear();
        }
    }

    public class GraphElementCache
    {
        public IGraphPolyLineDrawer? lineConnectionDrawer = null;
        public ElementCacheCollection<IGraphLabelDrawer, CanvasChildStatus> labelXDrawers
            = new ElementCacheCollection<IGraphLabelDrawer, CanvasChildStatus>();
        public ElementCacheCollection<IGraphLabelDrawer, CanvasChildStatus> labelYDrawers
            = new ElementCacheCollection<IGraphLabelDrawer, CanvasChildStatus>();
        public ElementCacheCollection<IGraphPointDrawer, CanvasChildStatus> pointDrawers
            = new ElementCacheCollection<IGraphPointDrawer, CanvasChildStatus>();

        #region Popup cache
        public IGraphLabelDrawer? pointDetailLabelCache = null;
        public Vector2 pointDetailMousePos = Vector2.Zero;
        #endregion

        #region Animation cache
        public SemaphoreSlim addingNewValueSemaphore = new SemaphoreSlim(1, 1);
        #endregion


        public void Clear()
        {
            lineConnectionDrawer = null;
            pointDrawers.ClearCache();
            labelXDrawers.ClearCache();
            labelYDrawers.ClearCache();

            pointDetailLabelCache = null;
            pointDetailMousePos = Vector2.Zero;
        }

        public string DumpLog()
        {
            return "GraphElementCache log: \n" +
                $"pointDrawers: Count= {pointDrawers.GetElementCacheCount()}\n" +
                $"labelXDrawers: Count= {labelXDrawers.GetElementCacheCount()}\n" +
                $"labelYDrawers: Count= {labelYDrawers.GetElementCacheCount()}\n" +
                $"lineConnectionDrawer: PointCount= {lineConnectionDrawer?.TotalPointCount ?? 0}\n";
        }
    }

}

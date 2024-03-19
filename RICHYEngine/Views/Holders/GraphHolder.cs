
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using static RICHYEngine.Views.Holders.ICanvasChild;

namespace RICHYEngine.Views.Holders
{
    public interface ICanvasHolder
    {
        void SetCanvasPosition(Vector2 position);
        public void Clear();
    }
    public interface IGraphPointValue
    {
        int YValue { get; }
        object? XValue { get; }
    }
    public interface IGraphContainer
    {
        ICanvasHolder PointAndLineCanvasHolder { get; }
        public float GraphHeight { get; }
        public float GraphWidth { get; }

        public void Clear();
    }
    public interface ICanvasChild
    {
        public enum GraphElement
        {
            Ox, Oy, Point, Line, LabelX, LabelY, DashX, DashY
        }

        /// <summary>
        /// Unity Implementation Eg: 
        /// point.transform.SetParent(container, false);
        /// </summary>
        bool SetParent(GraphElement targetElement);

        /// <summary>
        /// Unity Implementation Eg: 
        /// lastPoint.GetComponent<RectTransform>().anchoredPosition;
        /// </summary>
        Vector2 GetPositionOnCanvas();


        /// <summary>
        /// Unity Implementation Eg:
        /// line.GetComponent<Image>().sprite = lineSpr;
        /// line.GetComponent<Image>().type = Image.Type.Sliced;
        /// </summary>
        void SetUpVisual(GraphElement targetElement);


        Vector2 GetPositions()
        {
            return GetPositions();
        }
    }
    public interface IGraphLineDrawer : ICanvasChild
    {
        void SetPositionOnCanvas(Vector2 firstPoint, Vector2 secondPoint);
    }
    public interface IGraphPointDrawer : ICanvasChild
    {
        void SetPositionOnCanvas(GraphElement targetElement, Vector2 position);

    }

    public interface IGraphLabelDrawer : IGraphPointDrawer
    {
        void SetText(string text);
    }

    public class GraphHolder
    {
        private class GrapElementCache
        {
            public Collection<IGraphLineDrawer> lineDrawers = new Collection<IGraphLineDrawer>();
            public Collection<IGraphPointDrawer> pointDrawers = new Collection<IGraphPointDrawer>();
            public Collection<IGraphLabelDrawer> labelXDrawers = new Collection<IGraphLabelDrawer>();
            public Collection<IGraphLabelDrawer> labelYDrawers = new Collection<IGraphLabelDrawer>();

            public void Clear()
            {
                lineDrawers.Clear();
                pointDrawers.Clear();
                labelXDrawers.Clear();
                labelYDrawers.Clear();
            }
        }
        const float dashDistanceY = 50f;
        const float dashDistanceX = 50f;
        const float displayOffsetY = 30f;
        const float displayOffsetX = 80f;
        const float X_POINT_DISTANCE_MIN = 20f;

        private IGraphContainer mGraphContainer;
        private Func<GraphElement, IGraphPointDrawer> mGraphPointGenerator;
        private Func<GraphElement, IGraphLineDrawer> mGraphLineGenerator;
        private Func<GraphElement, IGraphLabelDrawer> mGraphLabelGenerator;

        private GrapElementCache elementCache = new GrapElementCache();
        private List<IGraphPointValue>? mCurrentShowingValueList;
        private float yMax = 100f;
        private float xPointDistance = 50f;
        private int mPointCanvasHolderLeft;
        private int mPointCanvasHolderTop;

        public GraphHolder(IGraphContainer graphContainer
            , Func<GraphElement, IGraphPointDrawer> graphPointGenerator
            , Func<GraphElement, IGraphLineDrawer> graphLineGenerator
            , Func<GraphElement, IGraphLabelDrawer> graphLabelGenerator)
        {
            mGraphContainer = graphContainer;
            mGraphPointGenerator = graphPointGenerator;
            mGraphLineGenerator = graphLineGenerator;
            mGraphLabelGenerator = graphLabelGenerator;
        }
        #region public API
        public float GetYValueAtMouse(Vector2 mousePos)
        {
            var rate = yMax / mGraphContainer.GraphHeight;
            return (-displayOffsetY + mousePos.Y + mPointCanvasHolderTop) * rate;
        }

        public void AddPointValue(IGraphPointValue newValue)
        {
            if (mCurrentShowingValueList != null)
            {
                mCurrentShowingValueList.Add(newValue);

            }
            else
            {
                mCurrentShowingValueList = new List<IGraphPointValue>();
                mCurrentShowingValueList.Add(newValue);
            }

            var index = elementCache.pointDrawers.Count;
            GenerateLabelX(newValue, displayOffsetY, displayOffsetX, index);
            var lastPoint = elementCache.pointDrawers.LastOrDefault();
            var point = GeneratePoint(newValue, index, mGraphContainer.GraphHeight);
            if (lastPoint != null)
            {
                GeneratePointConnection(lastPoint.GetPositionOnCanvas(), point.GetPositionOnCanvas());
            }
        }

        public void MoveGraph(int offsetLeft, int offsetTop)
        {
            mPointCanvasHolderLeft += offsetLeft;
            mPointCanvasHolderTop += offsetTop;
            mGraphContainer.PointAndLineCanvasHolder.SetCanvasPosition(new Vector2(mPointCanvasHolderLeft, mPointCanvasHolderTop));
            if (offsetTop != 0)
            {
                InvalidateLabelY(mGraphContainer.GraphHeight, dashDistanceY);
            }


            if (offsetLeft != 0)
            {
                InvalidateLabelX(xPointDistance);
            }
        }

        public void ChangeYMax(float offset)
        {
            yMax = offset;
            if (mCurrentShowingValueList != null)
            {
                ShowGraph(mCurrentShowingValueList);
            }
        }

        public void ChangeXDistance(float distance)
        {
            var newDistance = distance < X_POINT_DISTANCE_MIN ? X_POINT_DISTANCE_MIN : distance;
            if (mCurrentShowingValueList != null && xPointDistance != newDistance)
            {
                xPointDistance = newDistance;
                ShowGraph(mCurrentShowingValueList);
            }
        }


        public void ShowGraph(List<IGraphPointValue> valueList)
        {
            mCurrentShowingValueList = valueList;
            mGraphContainer.PointAndLineCanvasHolder.Clear();
            mGraphContainer.Clear();
            elementCache.Clear();

            float graphHeight = mGraphContainer.GraphHeight;
            float graphWidth = mGraphContainer.GraphWidth;
            SetupDash(graphHeight, graphWidth, dashDistanceX, dashDistanceY, displayOffsetY, displayOffsetX);
            SetupPointAndConnection(mCurrentShowingValueList,
                graphHeight, displayOffsetY, displayOffsetX);
            SetUpLabelY(graphHeight, dashDistanceY, displayOffsetX, displayOffsetY);

            IGraphLineDrawer oX = mGraphLineGenerator.Invoke(GraphElement.Ox);
            if (oX.SetParent(GraphElement.Ox))
            {
                oX.SetUpVisual(GraphElement.Ox);
                oX.SetPositionOnCanvas(new Vector2(0, 0 + displayOffsetY), new Vector2(graphWidth, 0 + displayOffsetY));
            }

            IGraphLineDrawer oY = mGraphLineGenerator.Invoke(GraphElement.Oy);
            if (oY.SetParent(GraphElement.Oy))
            {
                oY.SetUpVisual(GraphElement.Oy);
                oY.SetPositionOnCanvas(new Vector2(0 + displayOffsetX, 0), new Vector2(0 + displayOffsetX, graphHeight));
            }
        }
        #endregion

        private void SetUpLabelY(float graphHeight, float dashYDistance, float displayOffsetX, float displayOffsetY)
        {
            for (int i = 0; i < graphHeight / dashYDistance; i++)
            {
                var labelY = mGraphLabelGenerator.Invoke(GraphElement.LabelY);
                var normalizedValue = i * dashYDistance / graphHeight;
                var offset = mPointCanvasHolderTop / graphHeight;
                float yPos = i * dashYDistance;
                if (labelY.SetParent(GraphElement.LabelY))
                {
                    labelY.SetUpVisual(GraphElement.LabelY);
                    labelY.SetText((yMax * (normalizedValue + offset)).ToString("F2"));
                    labelY.SetPositionOnCanvas(GraphElement.LabelY, new Vector2(-20 + displayOffsetX, yPos + displayOffsetY));
                    elementCache.labelYDrawers.Add(labelY);
                }

            }
        }

        private void InvalidateLabelY(float graphHeight, float dashYDistance)
        {
            for (int i = 0; i < elementCache.labelYDrawers.Count; i++)
            {
                float yPos = i * dashYDistance;
                var offset = mPointCanvasHolderTop / graphHeight;
                var normalizedValue = i * dashYDistance / graphHeight;
                var labelY = elementCache.labelYDrawers[i];
                labelY.SetText((yMax * (normalizedValue + offset)).ToString("F2"));
                labelY.SetPositionOnCanvas(GraphElement.LabelY, new Vector2(-20 + displayOffsetX, yPos + displayOffsetY));
            }
        }
        private void InvalidateLabelX(float xPointDistance)
        {
            for (int i = 0; i < elementCache.labelXDrawers.Count; i++)
            {
                float xPos = i * xPointDistance;
                var labelX = elementCache.labelXDrawers[i];
                labelX.SetPositionOnCanvas(GraphElement.LabelX, new Vector2(xPos + displayOffsetX + mPointCanvasHolderLeft, -10 + displayOffsetY));
            }
        }
        private void SetupPointAndConnection(List<IGraphPointValue> showingList,
            float graphHeight,
            float displayOffsetY,
            float displayOffsetX)
        {
            IGraphPointDrawer? lastPoint = null;
            for (int i = 0; i < showingList.Count; i++)
            {
                GenerateLabelX(showingList[i], displayOffsetY, displayOffsetX, i);

                var point = GeneratePoint(showingList[i], i, graphHeight);
                if (lastPoint != null)
                {
                    GeneratePointConnection(lastPoint.GetPositionOnCanvas(), point.GetPositionOnCanvas());
                }
                lastPoint = point;
            }
        }

        private IGraphLabelDrawer GenerateLabelX(IGraphPointValue pointValue,
            float displayOffsetY,
            float displayOffsetX,
            float indexOnDisplayList)
        {
            var labelX = mGraphLabelGenerator.Invoke(GraphElement.LabelX);
            if (labelX.SetParent(GraphElement.LabelX))
            {
                float xPos = indexOnDisplayList * xPointDistance;
                labelX.SetUpVisual(GraphElement.LabelX);
                labelX.SetText(pointValue.XValue?.ToString() ?? indexOnDisplayList.ToString());
                labelX.SetPositionOnCanvas(GraphElement.LabelX, new Vector2(xPos + displayOffsetX + mPointCanvasHolderLeft, -10 + displayOffsetY));
                elementCache.labelXDrawers.Add(labelX);
            }
            return labelX;
        }

        private void SetupDash(
            float graphHeight,
            float graphWidth,
            float dashXDistance,
            float dashYDistance,
            float displayOffsetY,
            float displayOffsetX)
        {
            for (int i = 0; i < graphWidth / dashXDistance; i++)
            {
                float xPos = i * dashXDistance;

                var dashX = mGraphLineGenerator.Invoke(GraphElement.DashX);
                if (dashX.SetParent(GraphElement.DashX))
                {
                    dashX.SetUpVisual(GraphElement.DashX);
                    dashX.SetPositionOnCanvas(new Vector2(xPos + displayOffsetX - dashXDistance, 0), new Vector2(xPos + displayOffsetX - dashXDistance, graphHeight));
                }
            }


            for (int i = 0; i < graphHeight / dashYDistance; i++)
            {
                float yPos = i * dashYDistance;
                var dashY = mGraphLineGenerator.Invoke(GraphElement.DashY);
                if (dashY.SetParent(GraphElement.DashY))
                {
                    dashY.SetUpVisual(GraphElement.DashY);
                    dashY.SetPositionOnCanvas(new Vector2(0, yPos + displayOffsetY), new Vector2(graphWidth, yPos + displayOffsetY));
                }
            }
        }


        private IGraphPointDrawer GeneratePoint(IGraphPointValue graphPointValue, int pointIndex, float graphHeight)
        {
            //TODO: Current support to add new point at last index only
            Debug.Assert(elementCache.pointDrawers.Count == pointIndex);

            float xPos = pointIndex * xPointDistance + displayOffsetX;
            float yPos = (graphPointValue.YValue / yMax) * graphHeight + displayOffsetY;

            IGraphPointDrawer point = mGraphPointGenerator.Invoke(GraphElement.Point);
            if (point.SetParent(GraphElement.Point))
            {
                point.SetUpVisual(GraphElement.Point);
                point.SetPositionOnCanvas(GraphElement.Point, new Vector2(xPos, yPos));
                elementCache.pointDrawers.Add(point);
            }
            return point;
        }

        private void GeneratePointConnection(Vector2 posA, Vector2 posB)
        {
            IGraphLineDrawer line = mGraphLineGenerator.Invoke(GraphElement.Line);
            if (line.SetParent(GraphElement.Line))
            {
                line.SetUpVisual(GraphElement.Line);
                line.SetPositionOnCanvas(posA, posB);
                elementCache.lineDrawers.Add(line);
            }
        }


    }
}


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
        /// point.transform.SetIntoParent(container, false);
        /// </summary>
        bool SetIntoParent(GraphElement targetElement);

        bool RemoveFromParent(GraphElement targetElement);

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

    }
    public interface IGraphLineDrawer : ICanvasChild
    {
        void SetPositionOnCanvas(Vector2 firstPoint, Vector2 secondPoint);
    }
    public interface IGraphPolyLineDrawer : ICanvasChild
    {
        void AddNewPoint(Vector2 point);

        void ChangePointPosition(int pointIndex, Vector2 newPos);
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
        protected class GraphElementCache
        {
            public IGraphPolyLineDrawer? lineConnectionDrawer = null;
            public Collection<IGraphPointDrawer> pointDrawers = new Collection<IGraphPointDrawer>();
            public Collection<IGraphLabelDrawer> labelXDrawers = new Collection<IGraphLabelDrawer>();
            public Collection<IGraphLabelDrawer> labelYDrawers = new Collection<IGraphLabelDrawer>();

            public void Clear()
            {
                lineConnectionDrawer = null;
                pointDrawers.Clear();
                labelXDrawers.Clear();
                labelYDrawers.Clear();
            }
        }
        protected const float dashDistanceY = 50f;
        protected const float dashDistanceX = 50f;
        protected const float DISPLAY_OFFSET_Y = 30f;
        protected const float DISPLAY_OFFSET_X = 80f;
        protected const float X_POINT_DISTANCE_MIN = 20f;
        protected const float X_POINT_DISTANCE_DEF = 50f;
        protected const float X_POINT_DISTANCE_MAX = 80f;

        protected IGraphContainer mGraphContainer;
        protected Func<GraphElement, IGraphPointDrawer> mGraphPointGenerator;
        protected Func<GraphElement, IGraphLineDrawer> mGraphLineGenerator;
        protected Func<GraphElement, IGraphPolyLineDrawer> mGraphPolyLineGenerator;
        protected Func<GraphElement, IGraphLabelDrawer> mGraphLabelGenerator;

        protected GraphElementCache elementCache = new GraphElementCache();
        protected List<IGraphPointValue>? mCurrentShowingValueList;
        protected float yMax = 100f;
        protected float xPointDistance = X_POINT_DISTANCE_DEF;
        protected int mPointCanvasHolderLeft;
        protected int mPointCanvasHolderTop;

        public GraphHolder(IGraphContainer graphContainer
            , Func<GraphElement, IGraphPointDrawer> graphPointGenerator
            , Func<GraphElement, IGraphLineDrawer> graphLineGenerator
            , Func<GraphElement, IGraphLabelDrawer> graphLabelGenerator
            , Func<GraphElement, IGraphPolyLineDrawer> graphPolyLineGenerator)
        {
            mGraphContainer = graphContainer;
            mGraphPointGenerator = graphPointGenerator;
            mGraphLineGenerator = graphLineGenerator;
            mGraphLabelGenerator = graphLabelGenerator;
            mGraphPolyLineGenerator = graphPolyLineGenerator;
        }
        #region public API
        public float GetYValueAtMouse(Vector2 mousePos)
        {
            var rate = yMax / mGraphContainer.GraphHeight;
            return (-DISPLAY_OFFSET_Y + mousePos.Y + mPointCanvasHolderTop) * rate;
        }

        public virtual void AddPointValue(IGraphPointValue newValue)
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
            GenerateLabelX(newValue, DISPLAY_OFFSET_Y, DISPLAY_OFFSET_X, index);
            if(elementCache.lineConnectionDrawer == null)
            {
                IGraphPolyLineDrawer graphPolyLineDrawer = mGraphPolyLineGenerator.Invoke(GraphElement.Line);
                elementCache.lineConnectionDrawer = graphPolyLineDrawer;
                GeneratePoint(newValue, index, mGraphContainer.GraphHeight, graphPolyLineDrawer);

            }
            else
            {
                GeneratePoint(newValue, index, mGraphContainer.GraphHeight, elementCache.lineConnectionDrawer);
            }

        }

        public virtual void MoveGraph(int offsetLeft, int offsetTop)
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

        public virtual void ChangeYMax(float offset)
        {
            yMax = offset;
            if (mCurrentShowingValueList != null)
            {
                ShowGraph(mCurrentShowingValueList);
            }
        }

        public virtual void ChangeXDistance(float distance)
        {
            var newDistance = distance < X_POINT_DISTANCE_MIN ? X_POINT_DISTANCE_MIN : distance;
            if (mCurrentShowingValueList != null && xPointDistance != newDistance)
            {
                xPointDistance = newDistance;
                ShowGraph(mCurrentShowingValueList);
            }
        }


        public virtual void ShowGraph(List<IGraphPointValue> valueList)
        {
            mCurrentShowingValueList = valueList;
            mGraphContainer.PointAndLineCanvasHolder.Clear();
            mGraphContainer.Clear();
            elementCache.Clear();

            float graphHeight = mGraphContainer.GraphHeight;
            float graphWidth = mGraphContainer.GraphWidth;
            SetupDash(graphHeight, graphWidth, dashDistanceX, dashDistanceY, DISPLAY_OFFSET_Y, DISPLAY_OFFSET_X);
            SetupPointAndConnection(mCurrentShowingValueList,
                graphHeight, DISPLAY_OFFSET_Y, DISPLAY_OFFSET_X);
            SetUpLabelY(graphHeight, dashDistanceY, DISPLAY_OFFSET_X, DISPLAY_OFFSET_Y);

            IGraphLineDrawer oX = mGraphLineGenerator.Invoke(GraphElement.Ox);
            if (oX.SetIntoParent(GraphElement.Ox))
            {
                oX.SetUpVisual(GraphElement.Ox);
                oX.SetPositionOnCanvas(new Vector2(0, 0 + DISPLAY_OFFSET_Y), new Vector2(graphWidth, 0 + DISPLAY_OFFSET_Y));
            }

            IGraphLineDrawer oY = mGraphLineGenerator.Invoke(GraphElement.Oy);
            if (oY.SetIntoParent(GraphElement.Oy))
            {
                oY.SetUpVisual(GraphElement.Oy);
                oY.SetPositionOnCanvas(new Vector2(0 + DISPLAY_OFFSET_X, 0), new Vector2(0 + DISPLAY_OFFSET_X, graphHeight));
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
                if (labelY.SetIntoParent(GraphElement.LabelY))
                {
                    labelY.SetUpVisual(GraphElement.LabelY);
                    labelY.SetText((yMax * (normalizedValue + offset)).ToString("F2"));
                    labelY.SetPositionOnCanvas(GraphElement.LabelY, new Vector2(-20 + displayOffsetX, yPos + displayOffsetY));
                    elementCache.labelYDrawers.Add(labelY);
                }

            }
        }

        protected void InvalidateLabelY(float graphHeight, float dashYDistance)
        {
            for (int i = 0; i < elementCache.labelYDrawers.Count; i++)
            {
                float yPos = i * dashYDistance;
                var offset = mPointCanvasHolderTop / graphHeight;
                var normalizedValue = i * dashYDistance / graphHeight;
                var labelY = elementCache.labelYDrawers[i];
                labelY.SetText((yMax * (normalizedValue + offset)).ToString("F2"));
                labelY.SetPositionOnCanvas(GraphElement.LabelY, new Vector2(-20 + DISPLAY_OFFSET_X, yPos + DISPLAY_OFFSET_Y));
            }
        }
        private void InvalidateLabelX(float xPointDistance)
        {
            for (int i = 0; i < elementCache.labelXDrawers.Count; i++)
            {
                float xPos = i * xPointDistance;
                var labelX = elementCache.labelXDrawers[i];
                labelX.SetPositionOnCanvas(GraphElement.LabelX, new Vector2(xPos + DISPLAY_OFFSET_X + mPointCanvasHolderLeft, -10 + DISPLAY_OFFSET_Y));
            }
        }
        private void SetupPointAndConnection(List<IGraphPointValue> showingList,
            float graphHeight,
            float displayOffsetY,
            float displayOffsetX)
        {
            IGraphPolyLineDrawer graphPolyLineDrawer = mGraphPolyLineGenerator.Invoke(GraphElement.Line);
            elementCache.lineConnectionDrawer = graphPolyLineDrawer;
            graphPolyLineDrawer.SetIntoParent(targetElement:GraphElement.Line);
            graphPolyLineDrawer.SetUpVisual(targetElement:GraphElement.Line);
            for (int i = 0; i < showingList.Count; i++)
            {
                GenerateLabelX(showingList[i], displayOffsetY, displayOffsetX, i);

                var point = GeneratePoint(showingList[i], i, graphHeight, graphPolyLineDrawer);

                //if (lastPoint != null)
                //{
                //    GeneratePointConnection(lastPoint.GetPositionOnCanvas(), point.GetPositionOnCanvas(), i - 1);
                //}
            }
        }

        protected virtual IGraphLabelDrawer GenerateLabelX(IGraphPointValue pointValue,
            float displayOffsetY,
            float displayOffsetX,
            float pointIndex)
        {
            var labelX = mGraphLabelGenerator.Invoke(GraphElement.LabelX);
            if (labelX.SetIntoParent(GraphElement.LabelX))
            {
                float xPos = pointIndex * xPointDistance;
                labelX.SetUpVisual(GraphElement.LabelX);
                labelX.SetText(pointValue.XValue?.ToString() ?? pointIndex.ToString());
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
                if (dashX.SetIntoParent(GraphElement.DashX))
                {
                    dashX.SetUpVisual(GraphElement.DashX);
                    dashX.SetPositionOnCanvas(new Vector2(xPos + displayOffsetX - dashXDistance, 0), new Vector2(xPos + displayOffsetX - dashXDistance, graphHeight));
                }
            }


            for (int i = 0; i < graphHeight / dashYDistance; i++)
            {
                float yPos = i * dashYDistance;
                var dashY = mGraphLineGenerator.Invoke(GraphElement.DashY);
                if (dashY.SetIntoParent(GraphElement.DashY))
                {
                    dashY.SetUpVisual(GraphElement.DashY);
                    dashY.SetPositionOnCanvas(new Vector2(0, yPos + displayOffsetY), new Vector2(graphWidth, yPos + displayOffsetY));
                }
            }
        }


        protected virtual IGraphPointDrawer GeneratePoint(IGraphPointValue graphPointValue, int pointIndex, float graphHeight, IGraphPolyLineDrawer graphPolyLineDrawer)
        {
            //TODO: Current support to add new point at last index only
            Debug.Assert(elementCache.pointDrawers.Count == pointIndex);

            float xPos = pointIndex * xPointDistance + DISPLAY_OFFSET_X;
            float yPos = (graphPointValue.YValue / yMax) * graphHeight + DISPLAY_OFFSET_Y;
            graphPolyLineDrawer.AddNewPoint(new Vector2(xPos, yPos));
            IGraphPointDrawer point = mGraphPointGenerator.Invoke(GraphElement.Point);
            if (point.SetIntoParent(GraphElement.Point))
            {
                point.SetUpVisual(GraphElement.Point);
                point.SetPositionOnCanvas(GraphElement.Point, new Vector2(xPos, yPos));
                elementCache.pointDrawers.Add(point);
            }
            return point;
        }

        protected virtual void GeneratePointConnection(Vector2 posA, Vector2 posB, int lineIndex)
        {
            IGraphLineDrawer line = mGraphLineGenerator.Invoke(GraphElement.Line);
            if (line.SetIntoParent(GraphElement.Line))
            {
                line.SetUpVisual(GraphElement.Line);
                line.SetPositionOnCanvas(posA, posB);
            }
        }


    }
}

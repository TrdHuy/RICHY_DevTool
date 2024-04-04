using RICHYEngine.Views.Holders.GraphHolder.Elements;
using System.Numerics;

namespace RICHYEngine.Views.Holders.GraphHolder
{
    public interface IRectDrawer : ISingleCanvasElement
    {
        void SetRectSize(float width, float height);

        Vector2 GetRectSize();
    }

    public delegate void GraphPosChanged(int oldLeft, int oldTop, int newLeft, int newTop);

    public class GraphHolder
    {
        public event GraphPosChanged? GraphPosChanged;
        private const string TAG = "GraphHolder";

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
        protected Func<GraphElement, IRectDrawer> mRectGenerator;

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
            , Func<GraphElement, IGraphPolyLineDrawer> graphPolyLineGenerator
            , Func<GraphElement, IRectDrawer> rectGenerator)
        {
            mGraphContainer = graphContainer;
            mGraphPointGenerator = graphPointGenerator;
            mGraphLineGenerator = graphLineGenerator;
            mGraphLabelGenerator = graphLabelGenerator;
            mGraphPolyLineGenerator = graphPolyLineGenerator;
            mRectGenerator = rectGenerator;
        }

        #region public API
        public virtual string Dump(bool isDumpGraphLineDrawer = false)
        {
            var dump = elementCache.DumpLog() + "\n" +
                $"xPointDistance={xPointDistance}\n" +
                $"yMax={yMax}\n" +
                $"yRate= yMax / graphHeight ={yMax / mGraphContainer.GraphHeight} ";
            if (isDumpGraphLineDrawer)
            {
                dump += "\n" + elementCache.lineConnectionDrawer?.Dump();
            }
            return dump;
        }

        public void ShowPointDetail(bool isShowing, Vector2 mousePos)
        {
            const int CONTENT_PADDING = 10;

            if (!isShowing)
            {
                elementCache.pointDetailLabelCache = null;
                mGraphContainer.PopupCanvasHolder.Clear();
                return;
            }
            if (mCurrentShowingValueList == null ||
                    (elementCache.pointDetailLabelCache != null &&
                        elementCache.pointDetailMousePos == mousePos))
            {
                return;
            }

            var pointIndex = GetPointIndexViaMousePos(mousePos);
            if (pointIndex == -1)
            {
                return;
            }
            mGraphContainer.PopupCanvasHolder.Clear();
            var content = $"Value={mCurrentShowingValueList[pointIndex].YValue}";
            var xValue = mCurrentShowingValueList[pointIndex].XValue;
            if (xValue is DateTime)
            {
                content += $"\nX={((DateTime)xValue).ToString("U")}";
            }
            var label = mGraphLabelGenerator.Invoke(GraphElement.Etc);
            label.SetUpVisual(GraphElement.Etc);
            label.SetText(content);
            label.SetPositionOnCanvas(GraphElement.Etc, new Vector2(CONTENT_PADDING, CONTENT_PADDING + label.DesiredHeight) + mousePos);
            var backgroundEle = mRectGenerator.Invoke(GraphElement.Etc);
            var bgWidth = label.DesiredWidth + CONTENT_PADDING * 2;
            var bgHeight = label.DesiredHeight + CONTENT_PADDING * 2;

            backgroundEle.SetUpVisual(GraphElement.Etc);
            backgroundEle.SetPositionOnCanvas(GraphElement.Etc, new Vector2(0, bgHeight) + mousePos);
            backgroundEle.SetRectSize(bgWidth, bgHeight);

            var pointPos = GetPointPosOnMainCanvas(pointIndex);
            var point = mGraphPointGenerator.Invoke(GraphElement.Etc);
            point.SetUpVisual(GraphElement.Etc);
            point.graphPointValue = mCurrentShowingValueList[pointIndex];
            point.SetPositionOnCanvas(GraphElement.Etc, pointPos);
            mGraphContainer.PopupCanvasHolder.AddChild(point);
            mGraphContainer.PopupCanvasHolder.AddChild(backgroundEle);
            mGraphContainer.PopupCanvasHolder.AddChild(label);

            elementCache.pointDetailLabelCache = label;
            elementCache.pointDetailMousePos = mousePos;
        }

        public float GetYValueAtMouse(Vector2 mousePos)
        {
            var rate = yMax / mGraphContainer.GraphHeight;
            return (-DISPLAY_OFFSET_Y + mousePos.Y - mPointCanvasHolderTop) * rate;
        }

        public virtual int AddPointValue(IGraphPointValue newValue)
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

            var index = elementCache.pointDrawers.GetElementCacheCount();
            GenerateLabelX(newValue, DISPLAY_OFFSET_Y, DISPLAY_OFFSET_X, index);
            if (elementCache.lineConnectionDrawer == null)
            {
                IGraphPolyLineDrawer graphPolyLineDrawer = mGraphPolyLineGenerator.Invoke(GraphElement.Line);
                elementCache.lineConnectionDrawer = graphPolyLineDrawer;
                GeneratePoint(newValue, index, mGraphContainer.GraphHeight, graphPolyLineDrawer);
            }
            else
            {
                GeneratePoint(newValue, index, mGraphContainer.GraphHeight, elementCache.lineConnectionDrawer);
            }
            return index;
        }

        public virtual void MoveGraph(int offsetLeft, int offsetTop)
        {
            mPointCanvasHolderLeft += offsetLeft;
            mPointCanvasHolderTop += offsetTop;
            mGraphContainer.PointAndLineCanvasHolder.SetCanvasPosition(new Vector2(GetXPosForPointCanvas(), GetYPosForPointCanvas()));
            mGraphContainer.LabelXCanvasHolder.SetCanvasPosition(new Vector2(GetXPosForLabelXCanvas(), GetYPosForLabelXCanvas()));
            if (offsetTop != 0)
            {
                InvalidateLabelY(mGraphContainer.GraphHeight, dashDistanceY);
            }
            GraphPosChanged?.Invoke(mPointCanvasHolderLeft - offsetLeft, mPointCanvasHolderTop - offsetTop, mPointCanvasHolderLeft, mPointCanvasHolderTop);
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
            mGraphContainer.Clear();
            elementCache.Clear();
            mGraphContainer.PointAndLineCanvasHolder.SetCanvasPosition(new Vector2(GetXPosForPointCanvas(), GetYPosForPointCanvas()));

            float graphHeight = mGraphContainer.GraphHeight;
            float graphWidth = mGraphContainer.GraphWidth;
            SetupDash(graphHeight, graphWidth, dashDistanceX, dashDistanceY, DISPLAY_OFFSET_Y, DISPLAY_OFFSET_X);
            SetUpAxis(graphHeight, graphWidth);
            SetupPointNConnectionNLabelX(mCurrentShowingValueList,
                graphHeight, DISPLAY_OFFSET_Y, DISPLAY_OFFSET_X);
            SetUpLabelY(graphHeight, dashDistanceY, DISPLAY_OFFSET_X, DISPLAY_OFFSET_Y);

        }

        private void SetUpAxis(float graphHeight, float graphWidth)
        {
            IGraphLineDrawer oX = mGraphLineGenerator.Invoke(GraphElement.Ox);
            if (mGraphContainer.AxisCanvasHolder.AddChild(oX))
            {
                oX.SetUpVisual(GraphElement.Ox);
                oX.SetPositionOnCanvas(new Vector2(0, 0 + DISPLAY_OFFSET_Y), new Vector2(graphWidth, 0 + DISPLAY_OFFSET_Y));
            }

            IGraphLineDrawer oY = mGraphLineGenerator.Invoke(GraphElement.Oy);
            if (mGraphContainer.AxisCanvasHolder.AddChild(oY))
            {
                oY.SetUpVisual(GraphElement.Oy);
                oY.SetPositionOnCanvas(new Vector2(0 + DISPLAY_OFFSET_X, 0), new Vector2(0 + DISPLAY_OFFSET_X, graphHeight));
            }
        }
        #endregion

        protected Vector2 GetPointPosOnMainCanvas(int pointIndex)
        {
            if (mCurrentShowingValueList == null || pointIndex >= mCurrentShowingValueList.Count)
            {
                return Vector2.Zero;
            }
            var xPos = GetXPosForPointBaseOnPointIndex(pointIndex) + GetXPosForPointCanvas();
            var yPos = GetYPosForPointBaseOnValue(mCurrentShowingValueList[pointIndex]) + GetYPosForPointCanvas();
            return new Vector2(xPos, yPos);
        }

        private void SetUpLabelY(float graphHeight, float dashYDistance, float displayOffsetX, float displayOffsetY)
        {
            mGraphContainer.LabelYCanvasHolder.SetCanvasPosition(new Vector2(GetPosXForLabelYCanvas(), GetYPosForLabelYCanvas()));
            for (int i = 0; i < graphHeight / dashYDistance; i++)
            {
                var labelY = mGraphLabelGenerator.Invoke(GraphElement.LabelY);
                var normalizedValue = i * dashYDistance / graphHeight;
                var offset = mPointCanvasHolderTop / graphHeight;
                float yPos = GetPosYForLabelY(i);
                labelY.SetUpVisual(GraphElement.LabelY);
                labelY.SetText((yMax * (normalizedValue + offset)).ToString("F2"));
                labelY.SetPositionOnCanvas(GraphElement.LabelY, new Vector2(GetPosXForLabelY(), yPos));
                var state = elementCache.labelYDrawers.AddElementToCache(labelY);
                mGraphContainer.LabelYCanvasHolder.AddCanvasChildAndAssignNewState(labelY, state);
            }
        }

        protected int GetPointIndexViaMousePos(Vector2 mousePos)
        {
            if (mCurrentShowingValueList == null) return -1;

            var startVisibilityRange = GetXPosForPointCanvas();
            var endVisibilityRange = (mCurrentShowingValueList.Count - 1) * xPointDistance + startVisibilityRange;
            if (mousePos.X < startVisibilityRange)
            {
                return 0;
            }
            else if (mousePos.X > endVisibilityRange)
            {
                return mCurrentShowingValueList.Count - 1;
            }
            else
            {
                return (int)Math.Round((mousePos.X - startVisibilityRange) / xPointDistance);
            }
        }

        protected void InvalidateLabelY(float graphHeight, float dashYDistance)
        {
            for (int i = 0; i < elementCache.labelYDrawers.GetElementCacheCount(); i++)
            {
                var offset = -mPointCanvasHolderTop / graphHeight;
                var normalizedValue = i * dashYDistance / graphHeight;
                var labelY = elementCache.labelYDrawers.GetElementAt(i);
                labelY.SetText((yMax * (normalizedValue + offset)).ToString("F2"));
            }
        }


        protected virtual void SetupPointNConnectionNLabelX(List<IGraphPointValue> showingList,
            float graphHeight,
            float displayOffsetY,
            float displayOffsetX)
        {
            IGraphPolyLineDrawer graphPolyLineDrawer = mGraphPolyLineGenerator.Invoke(GraphElement.Line);
            elementCache.lineConnectionDrawer = graphPolyLineDrawer;
            mGraphContainer.LabelXCanvasHolder.SetCanvasPosition(new Vector2(GetXPosForLabelXCanvas(), GetYPosForLabelXCanvas()));
            if (mGraphContainer.PointAndLineCanvasHolder.AddChild(graphPolyLineDrawer))
            {
                graphPolyLineDrawer.SetUpVisual(targetElement: GraphElement.Line);
                for (int i = 0; i < showingList.Count; i++)
                {
                    GenerateLabelX(showingList[i], displayOffsetY, displayOffsetX, i);

                    GeneratePoint(showingList[i], i, graphHeight, graphPolyLineDrawer);
                }
            }

        }

        protected virtual void GenerateLabelX(IGraphPointValue pointValue,
            float displayOffsetY,
            float displayOffsetX,
            int pointIndex,
            bool toLast = true,
            bool addToParent = true)
        {
            var labelX = mGraphLabelGenerator.Invoke(GraphElement.LabelX);

            float xPos = GetXPosForPointBaseOnPointIndex(pointIndex);
            labelX.SetUpVisual(GraphElement.LabelX);
            var xValue = pointValue.XValue;
            if (xValue is DateTime && xValue != null)
            {
                labelX.SetText(((DateTime)xValue).ToString("m"));
            }
            else
            {
                labelX.SetText(pointValue.XValue?.ToString() ?? pointIndex.ToString());
            }
            labelX.SetPositionOnCanvas(GraphElement.LabelX, new Vector2(xPos, 0));
            var state = elementCache.labelXDrawers.AddElementToCache(labelX, toLast);
            if (addToParent)
            {
                mGraphContainer.LabelXCanvasHolder.AddCanvasChildAndAssignNewState(labelX, state);
            }
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
                if (mGraphContainer.GridDashCanvasHolder.AddChild(dashX))
                {
                    dashX.SetUpVisual(GraphElement.DashX);
                    dashX.SetPositionOnCanvas(new Vector2(xPos + displayOffsetX - dashXDistance, 0), new Vector2(xPos + displayOffsetX - dashXDistance, graphHeight));
                }
            }

            for (int i = 0; i < graphHeight / dashYDistance; i++)
            {
                float yPos = i * dashYDistance;
                var dashY = mGraphLineGenerator.Invoke(GraphElement.DashY);
                if (mGraphContainer.GridDashCanvasHolder.AddChild(dashY))
                {
                    dashY.SetUpVisual(GraphElement.DashY);
                    dashY.SetPositionOnCanvas(new Vector2(0, yPos + displayOffsetY), new Vector2(graphWidth, yPos + displayOffsetY));
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="graphPointValue"></param>
        /// <param name="pointIndex"></param>
        /// <param name="graphHeight"></param>
        /// <param name="graphPolyLineDrawer"></param>
        /// <param name="toLast"></param>
        /// <param name="addToParent"></param>
        /// <returns>
        /// Item1: current point drawer
        /// Item2: index of current point drawer
        /// Item3: index of current point drawer on poly line
        /// </returns>
        protected virtual (IGraphPointDrawer, int, int) GeneratePoint(IGraphPointValue graphPointValue, int pointIndex, float graphHeight,
            IGraphPolyLineDrawer graphPolyLineDrawer,
            bool toLast = true,
            bool addToParent = true,
            Vector2? polyLineStartPoint = null)
        {
            float xPos = GetXPosForPointBaseOnPointIndex(pointIndex);
            float yPos = GetYPosForPointBaseOnValue(graphPointValue);
            IGraphPointDrawer point = mGraphPointGenerator.Invoke(GraphElement.Point);
            point.graphPointValue = graphPointValue;
            if (addToParent)
            {
                mGraphContainer.PointAndLineCanvasHolder.AddChild(point);
            }
            point.SetUpVisual(GraphElement.Point);
            point.SetPositionOnCanvas(GraphElement.Point, new Vector2(xPos, yPos));
            var index = 0;
            if (toLast)
            {
                elementCache.pointDrawers.AddElementToCache(point);
                index = elementCache.pointDrawers.GetElementCacheCount() - 1;
            }
            else
            {
                elementCache.pointDrawers.AddElementToCache(point, toLast: false);
            }
            var indexOnPolyLine = graphPolyLineDrawer.AddNewPoint(polyLineStartPoint ?? new Vector2(xPos, yPos), toLast);

            return new(point, index, indexOnPolyLine);
        }

        /// <summary>
        /// To get X coordinate base on point index, used for label of X axis and point
        /// </summary>
        /// <param name="pointIndexOnGraph"></param>
        /// <returns></returns>
        protected float GetXPosForPointBaseOnPointIndex(int pointIndexOnGraph)
        {
            return GetXPosForPointBaseOnPointIndex(pointIndexOnGraph, xPointDistance);
        }

        protected float GetXPosForPointBaseOnPointIndex(int pointIndexOnGraph, float xDistance)
        {
            float xPos = pointIndexOnGraph * xDistance;
            return xPos;
        }

        /// <summary>
        /// To get Y coordinate base on point value, used for label of Y axis and point
        /// </summary>
        /// <param name="pointIndexOnGraph"></param>
        /// <returns></returns>
        protected float GetYPosForPointBaseOnValue(IGraphPointValue pointValue)
        {
            return GetYPosForPointBaseOnValue(pointValue, yMax);
        }

        protected float GetYPosForPointBaseOnValue(IGraphPointValue pointValue, float yMax)
        {
            float yPos = pointValue.YValue / yMax * mGraphContainer.GraphHeight;
            return yPos;
        }

        protected float GetXPosForPointCanvas()
        {
            return mPointCanvasHolderLeft + DISPLAY_OFFSET_X;
        }

        protected float GetYPosForPointCanvas()
        {
            return mPointCanvasHolderTop + DISPLAY_OFFSET_Y;
        }

        protected float GetXPosForLabelXCanvas()
        {
            return mPointCanvasHolderLeft + DISPLAY_OFFSET_X;
        }

        protected float GetYPosForLabelXCanvas()
        {
            return DISPLAY_OFFSET_Y - 10;
        }

        protected float GetPosXForLabelYCanvas()
        {
            return DISPLAY_OFFSET_X - 10;
        }

        protected float GetYPosForLabelYCanvas()
        {
            return DISPLAY_OFFSET_Y;
        }
        protected float GetPosXForLabelY()
        {
            return 0;
        }
        protected float GetPosYForLabelY(int index)
        {
            return index * dashDistanceY;
        }
    }
}

using System.Diagnostics;
using System.Numerics;
using static RICHYEngine.Views.Holders.GraphHolder.ICanvasChild;
using static RICHYEngine.LogCompat.Logger.RICHYEngineLogger;

namespace RICHYEngine.Views.Holders.GraphHolder
{
    public class VirtualizingGraphHolder : GraphHolder
    {
        protected int mCurrentStartIndex = 0;
        protected int mCurrentEndIndex = 0;

        public VirtualizingGraphHolder(IGraphContainer graphContainer,
            Func<GraphElement, IGraphPointDrawer> graphPointGenerator,
            Func<GraphElement, IGraphLineDrawer> graphLineGenerator,
            Func<GraphElement, IGraphLabelDrawer> graphLabelGenerator,
            Func<GraphElement, IGraphPolyLineDrawer> graphPolyLineGenerator) : base(graphContainer,
                graphPointGenerator,
                graphLineGenerator,
                graphLabelGenerator,
                graphPolyLineGenerator)
        { }

        public override void MoveGraph(int offsetLeft, int offsetTop)
        {
            base.MoveGraph(offsetLeft, offsetTop);
            UpdateDisplayRangeAndModifyElements(xPointDistance, xPointDistance);
        }

        public override void ShowGraph(List<IGraphPointValue> valueList)
        {
            mCurrentStartIndex = GetStartPointIndex();
            mCurrentEndIndex = GetEndPointIndex();
            base.ShowGraph(valueList);
        }

        public override void ChangeYMax(float newYMax)
        {
            if (yMax != newYMax)
            {
                yMax = newYMax;
                RearrangePointAndConnection(xPointDistance, xPointDistance);
                InvalidateLabelY(mGraphContainer.GraphHeight, dashDistanceY);
            }
        }

        public override void ChangeXDistance(float distance)
        {
            var newDistance = distance < 1 ? 1 : distance;
            if (mCurrentShowingValueList != null && xPointDistance != newDistance)
            {
                var oldXDistance = xPointDistance;
                xPointDistance = newDistance;
                //UpdateDisplayRangeAndModifyElements(oldXDistance, distance);
                //RearrangePointAndConnection(oldXDistance, distance);
                mCurrentStartIndex = GetStartPointIndex();
                mCurrentEndIndex = GetEndPointIndex();
                elementCache.pointDrawers.Clear();
                elementCache.labelXDrawers.Clear();
                mGraphContainer.LabelXCanvasHolder.Clear();
                mGraphContainer.PointAndLineCanvasHolder.Clear();
                SetupPointNConnectionNLabelX(mCurrentShowingValueList,
                    mGraphContainer.GraphHeight, DISPLAY_OFFSET_Y, DISPLAY_OFFSET_X);
            }
        }

        private void RearrangePointAndConnection(float oldXDistance, float newXDistance)
        {
            if (mCurrentShowingValueList == null)
            {
                throw new Exception("Should not be null here");
            }
            for (int i = mCurrentStartIndex; i <= mCurrentEndIndex
                && i < mCurrentShowingValueList.Count
                && i - mCurrentStartIndex < elementCache.pointDrawers.Count; i++)
            {
                var labelX = elementCache.labelXDrawers[i - mCurrentStartIndex];
                float xPos = GetXPosForPointBaseOnPointIndex(i);
                labelX.SetPositionOnCanvas(GraphElement.LabelX, new Vector2(xPos, 0));

                float yPos = GetYPosForPointBaseOnValue(mCurrentShowingValueList![i]);
                var point = elementCache.pointDrawers[i - mCurrentStartIndex];
                var oldPointPos = point.GetPositionOnCanvas();
                var newPointPos = new Vector2(xPos, yPos);
                point.SetPositionOnCanvas(GraphElement.Point, newPointPos);
                elementCache.lineConnectionDrawer!.ChangePointPosition(oldPointPos, newPointPos);
            }
        }


        private void UpdateDisplayRangeAndModifyElements(float oldXDistance, float newXDistance)
        {
            Debug.WriteLine($"HUY -------------- UpdateDisplayRangeAndModifyElements: oldXDistance={oldXDistance},newXDistance={newXDistance}");
            var newStartIndex = GetStartPointIndex();
            var newEndIndex = GetEndPointIndex();
            if (mCurrentShowingValueList == null || elementCache.lineConnectionDrawer == null)
            {
                throw new Exception("Should not be null here");
            }

            var totalPointCount = mCurrentShowingValueList.Count;
            if (mCurrentStartIndex < newStartIndex)
            {
                Debug.WriteLine($"HUY mCurrentStartIndex={mCurrentStartIndex}");
                Debug.WriteLine($"HUY newStartIndex={newStartIndex}");
                Debug.WriteLine($"HUY visibleItemCount={elementCache.pointDrawers.Count}");

                for (int i = mCurrentStartIndex; i < newStartIndex &&
                        i < totalPointCount &&
                        elementCache.pointDrawers.Count > 0; i++)
                {
                    var pointCache = elementCache.pointDrawers[0];
                    elementCache.pointDrawers.RemoveAt(0);
                    mGraphContainer.PointAndLineCanvasHolder.RemoveChild(pointCache);
                    mGraphContainer.LabelXCanvasHolder.RemoveChild(elementCache.labelXDrawers[0]);
                    elementCache.labelXDrawers.RemoveAt(0);
                    elementCache.lineConnectionDrawer!.RemovePoint(pointCache.GetPositionOnCanvas());
                    Debug.WriteLine($"HUY Remove at={i - mCurrentStartIndex}");
                }
                mCurrentStartIndex = newStartIndex;
            }
            else if (mCurrentStartIndex > newStartIndex)
            {
                Debug.WriteLine($"HUY mCurrentStartIndex={mCurrentStartIndex}");
                Debug.WriteLine($"HUY newStartIndex={newStartIndex}");
                Debug.WriteLine($"HUY visibleItemCount={elementCache.pointDrawers.Count}");

                var startAddIndex = mCurrentStartIndex - 1 >= totalPointCount ? totalPointCount - 1 : mCurrentStartIndex - 1;
                for (int i = startAddIndex; i >= newStartIndex && i < totalPointCount; i--)
                {
                    GeneratePoint(mCurrentShowingValueList[i], i, mGraphContainer.GraphHeight, elementCache.lineConnectionDrawer, toLast: false);
                    GenerateLabelX(mCurrentShowingValueList[i], DISPLAY_OFFSET_Y, DISPLAY_OFFSET_X, i, toLast: false);
                    Debug.WriteLine($"HUY Add i={i}");
                }
                mCurrentStartIndex = newStartIndex;
            }

            if (mCurrentEndIndex < newEndIndex)
            {
                Debug.WriteLine($"HUY mCurrentEndIndex={mCurrentEndIndex}");
                Debug.WriteLine($"HUY newEndIndex={newEndIndex}");
                Debug.WriteLine($"HUY visibleItemCount={elementCache.pointDrawers.Count}");
                for (int i = mCurrentEndIndex + 1; i <= newEndIndex
                        && i < totalPointCount
                        && i >= mCurrentStartIndex; i++)
                {
                    GeneratePoint(mCurrentShowingValueList[i], i, mGraphContainer.GraphHeight, elementCache.lineConnectionDrawer, toLast: true);
                    GenerateLabelX(mCurrentShowingValueList[i], DISPLAY_OFFSET_Y, DISPLAY_OFFSET_X, i, toLast: true);
                    Debug.WriteLine($"HUY Add i={i}");
                }
                mCurrentEndIndex = newEndIndex;
            }
            else if (mCurrentEndIndex > newEndIndex)
            {
                Debug.WriteLine($"HUY mCurrentEndIndex={mCurrentEndIndex}");
                Debug.WriteLine($"HUY newEndIndex={newEndIndex}");
                Debug.WriteLine($"HUY visibleItemCount={elementCache.pointDrawers.Count}");
                var deleteFromIndex = mCurrentEndIndex < totalPointCount ? mCurrentEndIndex : totalPointCount - 1;
                for (int i = deleteFromIndex; i > newEndIndex
                        && i - mCurrentStartIndex < elementCache.pointDrawers.Count; i--)
                {
                    var lastIndex = elementCache.pointDrawers.Count - 1;
                    var pointCache = elementCache.pointDrawers[lastIndex];
                    elementCache.pointDrawers.RemoveAt(lastIndex);
                    mGraphContainer.PointAndLineCanvasHolder.RemoveChild(pointCache);
                    mGraphContainer.LabelXCanvasHolder.RemoveChild(elementCache.labelXDrawers[lastIndex]);
                    elementCache.labelXDrawers.RemoveAt(lastIndex);
                    elementCache.lineConnectionDrawer!.RemovePoint(pointCache.GetPositionOnCanvas());
                    Debug.WriteLine($"HUY Remove at={lastIndex}");
                }
                mCurrentEndIndex = newEndIndex;
            }
            assert();

        }

        private int GetStartPointIndex()
        {
            var graphPosXOnCanvas = mPointCanvasHolderLeft + DISPLAY_OFFSET_X;
            var startPoint = 0;
            if (graphPosXOnCanvas < 0)
            {
                startPoint = (int)Math.Ceiling(graphPosXOnCanvas / xPointDistance * -1) - 1;
            }
            //Debug.WriteLine($"graphPosXOnCanvas={graphPosXOnCanvas}");
            //Debug.WriteLine($"startPoint={startPoint}");
            return startPoint;
        }

        private int GetEndPointIndex()
        {
            var graphPosXOnCanvas = mPointCanvasHolderLeft + DISPLAY_OFFSET_X;
            var graphWidth = mGraphContainer.GraphWidth;
            var rangeX = graphWidth - graphPosXOnCanvas;
            var endPoint = (int)(rangeX / xPointDistance) + 1;
            if (rangeX < 0)
            {
                endPoint = -1;
            }
            //Debug.WriteLine($"rangeX={rangeX}");
            //Debug.WriteLine($"endPoint={endPoint}");
            return endPoint;
        }

        protected override void SetupPointNConnectionNLabelX(List<IGraphPointValue> showingList, float graphHeight, float displayOffsetY, float displayOffsetX)
        {
            IGraphPolyLineDrawer graphPolyLineDrawer = mGraphPolyLineGenerator.Invoke(GraphElement.Line);
            elementCache.lineConnectionDrawer = graphPolyLineDrawer;
            mGraphContainer.LabelXCanvasHolder.SetCanvasPosition(new Vector2(GetXPosForLabelXCanvas(), GetYPosForLabelXCanvas()));
            if (mGraphContainer.PointAndLineCanvasHolder.AddChild(graphPolyLineDrawer))
            {
                graphPolyLineDrawer.SetUpVisual(targetElement: GraphElement.Line);
                for (int i = mCurrentStartIndex; i < showingList.Count && i <= mCurrentEndIndex; i++)
                {
                    GenerateLabelX(showingList[i], displayOffsetY, displayOffsetX, i);

                    GeneratePoint(showingList[i], i, graphHeight, graphPolyLineDrawer);
                }
            }
        }

        private void assert()
        {
            var start = 0;
            var temp = 0;
            if (mCurrentShowingValueList == null) return;

            if (mCurrentEndIndex < mCurrentShowingValueList.Count)
            {
                Debug.Assert(elementCache.pointDrawers.Count == mCurrentEndIndex - mCurrentStartIndex + 1);
            }
            else if (mCurrentStartIndex < mCurrentShowingValueList.Count)
            {
                Debug.Assert(elementCache.pointDrawers.Count == mCurrentShowingValueList.Count - mCurrentStartIndex);
            }
            else
            {
                Debug.Assert(elementCache.pointDrawers.Count == 0);
            }
            foreach (var p in elementCache.pointDrawers)
            {
                if (start == 0)
                {
                    temp = Convert.ToInt32(p.graphPointValue!.XValue);
                }
                else
                {
                    Debug.Assert(temp == Convert.ToInt32(p.graphPointValue!.XValue) - 1);
                    temp = Convert.ToInt32(p.graphPointValue!.XValue);
                }
                start++;
            }
        }
    }


    public class VirtualizingGraphHolder2 : VirtualizingGraphHolder
    {
        private const string TAG = "VirtualizingGraphHolder2";
        private const float MINIMUM_BETWEEN_TWO_POINTS = 60f;

        public VirtualizingGraphHolder2(IGraphContainer graphContainer,
            Func<GraphElement, IGraphPointDrawer> graphPointGenerator,
            Func<GraphElement, IGraphLineDrawer> graphLineGenerator,
            Func<GraphElement, IGraphLabelDrawer> graphLabelGenerator,
            Func<GraphElement, IGraphPolyLineDrawer> graphPolyLineGenerator)
            : base(graphContainer, graphPointGenerator, graphLineGenerator, graphLabelGenerator, graphPolyLineGenerator)
        {
        }

        public override void ChangeYMax(float newYMax)
        {
            if (yMax != newYMax)
            {
                var oldYMax = yMax;
                yMax = newYMax;
                RearrangePointAndConnection(xPointDistance, xPointDistance, oldYMax, newYMax);
                InvalidateLabelY(mGraphContainer.GraphHeight, dashDistanceY);
            }
        }

        public override void ChangeXDistance(float distance)
        {
            var newXDistance = distance < 1 ? 1 : distance;
            if (mCurrentShowingValueList != null && xPointDistance != newXDistance)
            {
                var oldXDistance = xPointDistance;
                xPointDistance = newXDistance;
                UpdateDisplayRangeAndModifyElements(oldXDistance, newXDistance);
                RearrangePointAndConnection(oldXDistance, newXDistance, yMax, yMax);
            }
        }

        private void RearrangePointAndConnection(float oldXDistance, float newXDistance, float oldYMax, float newYMax)
        {
            if (mCurrentShowingValueList == null)
            {
                throw new Exception("Should not be null here");
            }
            for (int i = mCurrentStartIndex, j = 0; i <= mCurrentEndIndex
                    && i < mCurrentShowingValueList.Count; i++)
            {
                float newXPos = GetXPosForPointBaseOnPointIndex(i);
                float newYPos = GetYPosForPointBaseOnValue(mCurrentShowingValueList[i]);
                float oldXPos = GetXPosForPointBaseOnPointIndex(i, oldXDistance);
                float oldYPos = GetYPosForPointBaseOnValue(mCurrentShowingValueList[i], oldYMax);

                var newPointPos = new Vector2(newXPos, newYPos);
                var oldPointPos = new Vector2(oldXPos, oldYPos);
                if (CheckIndexVisibilityByXDistance(i) && j < elementCache.pointDrawers.Count)
                {
                    var labelX = elementCache.labelXDrawers[j];
                    labelX.SetPositionOnCanvas(GraphElement.LabelX, new Vector2(newXPos, 0));
                    var point = elementCache.pointDrawers[j];
                    point.SetPositionOnCanvas(GraphElement.Point, newPointPos);
                    if (point.graphPointValue != mCurrentShowingValueList[i])
                    {
                        point.graphPointValue = mCurrentShowingValueList[i];
                        labelX.SetText(mCurrentShowingValueList[i].XValue?.ToString() ?? i.ToString());
                    }
                    j++;
                }

                if (oldXDistance != newXDistance)
                {
                    var newStartIndex = GetStartPointIndex(newXDistance);
                    var newEndIndex = GetEndPointIndex(newXDistance);
                    var oldStartIndex = GetStartPointIndex(oldXDistance);
                    var oldEndIndex = GetEndPointIndex(oldXDistance);
                    if (newStartIndex < oldStartIndex && i < oldStartIndex)
                    {

                    }
                    else if (newEndIndex > oldEndIndex && i > oldEndIndex)
                    {

                    }
                    else
                    {
                        elementCache.lineConnectionDrawer!.ChangePointPosition(oldPointPos, newPointPos);
                    }
                }
                else
                {
                    elementCache.lineConnectionDrawer!.ChangePointPosition(oldPointPos, newPointPos);
                }
            }
        }

        private void UpdateDisplayRangeAndModifyElements(float oldXDistance, float newXDistance)
        {
            var newStartIndex = GetStartPointIndex(newXDistance);
            var newEndIndex = GetEndPointIndex(newXDistance);
            if (mCurrentShowingValueList == null || elementCache.lineConnectionDrawer == null)
            {
                throw new Exception("Should not be null here");
            }

            var totalPointCount = mCurrentShowingValueList.Count;
            if (mCurrentStartIndex < newStartIndex)
            {
                D(TAG, $"mCurrentStartIndex={mCurrentStartIndex}");
                D(TAG, $"newStartIndex={newStartIndex}");
                D(TAG, $"visibleItemCount={elementCache.pointDrawers.Count}");

                for (int i = mCurrentStartIndex; i < newStartIndex &&
                        i < totalPointCount &&
                        elementCache.pointDrawers.Count > 0; i++)
                {
                    D(TAG, $"Remove at={i - mCurrentStartIndex}");
                    float oldXPos = GetXPosForPointBaseOnPointIndex(i, oldXDistance);
                    float oldYPos = GetYPosForPointBaseOnValue(mCurrentShowingValueList[i], yMax);
                    var oldPointPos = new Vector2(oldXPos, oldYPos);

                    if (CheckIndexVisibilityByXDistance(i))
                    {
                        var pointCache = elementCache.pointDrawers[0];
                        elementCache.pointDrawers.RemoveAt(0);
                        mGraphContainer.PointAndLineCanvasHolder.RemoveChild(pointCache);
                        mGraphContainer.LabelXCanvasHolder.RemoveChild(elementCache.labelXDrawers[0]);
                        elementCache.labelXDrawers.RemoveAt(0);
                        D(TAG, $"Remove at={i - mCurrentStartIndex}, visibity = 1");
                    }

                    elementCache.lineConnectionDrawer!.RemovePoint(oldPointPos);
                }
                mCurrentStartIndex = newStartIndex;
            }
            else if (mCurrentStartIndex > newStartIndex)
            {
                D(TAG, $"mCurrentStartIndex={mCurrentStartIndex}");
                D(TAG, $"newStartIndex={newStartIndex}");
                D(TAG, $"visibleItemCount={elementCache.pointDrawers.Count}");

                var startAddIndex = mCurrentStartIndex - 1 >= totalPointCount ? totalPointCount - 1 : mCurrentStartIndex - 1;
                for (int i = startAddIndex; i >= newStartIndex && i < totalPointCount; i--)
                {
                    GenerateLabelXAndPoint(mCurrentShowingValueList[i],
                       mGraphContainer.GraphHeight,
                       DISPLAY_OFFSET_Y, DISPLAY_OFFSET_X,
                       elementCache.lineConnectionDrawer, i, toLast: false);
                    D(TAG, $"Add pointIndex={i}");
                }
                mCurrentStartIndex = newStartIndex;
            }
            assert();

            if (mCurrentEndIndex < newEndIndex)
            {
                D(TAG, $"mCurrentEndIndex={mCurrentEndIndex}");
                D(TAG, $"newEndIndex={newEndIndex}");
                D(TAG, $"visibleItemCount={elementCache.pointDrawers.Count}");
                for (int i = mCurrentEndIndex + 1; i <= newEndIndex
                        && i < totalPointCount
                        && i >= mCurrentStartIndex; i++)
                {
                    GenerateLabelXAndPoint(mCurrentShowingValueList[i],
                        mGraphContainer.GraphHeight,
                        DISPLAY_OFFSET_Y, DISPLAY_OFFSET_X,
                        elementCache.lineConnectionDrawer, i, toLast: true);
                    D(TAG, $"Add pointIndex={i}");
                }
                mCurrentEndIndex = newEndIndex;
            }
            else if (mCurrentEndIndex > newEndIndex)
            {
                D(TAG, $"mCurrentEndIndex={mCurrentEndIndex}");
                D(TAG, $"newEndIndex={newEndIndex}");
                D(TAG, $"visiblePolyPointCount={elementCache.lineConnectionDrawer.TotalPointCount}");
                var deleteFromIndex = mCurrentEndIndex < totalPointCount ? mCurrentEndIndex : totalPointCount - 1;
                for (int i = deleteFromIndex; i > newEndIndex
                        && i - mCurrentStartIndex < elementCache.lineConnectionDrawer.TotalPointCount; i--)
                {
                    float oldXPos = GetXPosForPointBaseOnPointIndex(i, oldXDistance);
                    float oldYPos = GetYPosForPointBaseOnValue(mCurrentShowingValueList[i], yMax);
                    var oldPointPos = new Vector2(oldXPos, oldYPos);
                    var lastIndex = elementCache.pointDrawers.Count - 1;

                    if (CheckIndexVisibilityByXDistance(i))
                    {
                        var pointCache = elementCache.pointDrawers[lastIndex];
                        elementCache.pointDrawers.RemoveAt(lastIndex);
                        mGraphContainer.PointAndLineCanvasHolder.RemoveChild(pointCache);
                        mGraphContainer.LabelXCanvasHolder.RemoveChild(elementCache.labelXDrawers[lastIndex]);
                        elementCache.labelXDrawers.RemoveAt(lastIndex);
                    }

                    elementCache.lineConnectionDrawer!.RemovePoint(oldPointPos);
                    D(TAG, $"Remove at: displayIndex={lastIndex}, realIndex={i}");
                }
                mCurrentEndIndex = newEndIndex;
            }
            assert();

        }

        private int GetStartPointIndex(float newXDistance)
        {
            var graphPosXOnCanvas = mPointCanvasHolderLeft + DISPLAY_OFFSET_X;
            var startPoint = 0;
            if (graphPosXOnCanvas < 0)
            {
                startPoint = (int)Math.Ceiling(graphPosXOnCanvas / newXDistance * -1) - 1;
            }
            D(TAG, $"graphPosXOnCanvas={graphPosXOnCanvas}");
            D(TAG, $"startPoint={startPoint}");
            return startPoint;
        }

        private int GetEndPointIndex(float newXDistance)
        {
            var graphPosXOnCanvas = mPointCanvasHolderLeft + DISPLAY_OFFSET_X;
            var graphWidth = mGraphContainer.GraphWidth;
            var rangeX = graphWidth - graphPosXOnCanvas;
            var endPoint = (int)(rangeX / newXDistance) + 1;
            if (rangeX < 0)
            {
                endPoint = -1;
            }
            D(TAG, $"rangeX={rangeX}");
            D(TAG, $"endPoint={endPoint}");
            return endPoint;
        }

        protected override void SetupPointNConnectionNLabelX(List<IGraphPointValue> showingList, float graphHeight, float displayOffsetY, float displayOffsetX)
        {
            IGraphPolyLineDrawer graphPolyLineDrawer = mGraphPolyLineGenerator.Invoke(GraphElement.Line);
            elementCache.lineConnectionDrawer = graphPolyLineDrawer;
            mGraphContainer.LabelXCanvasHolder.SetCanvasPosition(new Vector2(GetXPosForLabelXCanvas(), GetYPosForLabelXCanvas()));
            if (mGraphContainer.PointAndLineCanvasHolder.AddChild(graphPolyLineDrawer))
            {
                graphPolyLineDrawer.SetUpVisual(targetElement: GraphElement.Line);
                for (int i = mCurrentStartIndex; i < showingList.Count && i <= mCurrentEndIndex; i++)
                {
                    GenerateLabelXAndPoint(showingList[i], graphHeight, displayOffsetY, displayOffsetX, graphPolyLineDrawer, i, true);
                }
            }
        }

        private void GenerateLabelXAndPoint(IGraphPointValue pointValue,
            float graphHeight,
            float displayOffsetY,
            float displayOffsetX,
            IGraphPolyLineDrawer graphPolyLineDrawer,
            int pointIndex,
            bool toLast)
        {
            if (CheckIndexVisibilityByXDistance(pointIndex))
            {
                GenerateLabelX(pointValue, displayOffsetY, displayOffsetX, pointIndex, toLast);
                GeneratePoint(pointValue, pointIndex, graphHeight, graphPolyLineDrawer, toLast);
            }
            else
            {
                // in case not visible, only connect the poly line
                float xPos = GetXPosForPointBaseOnPointIndex(pointIndex);
                float yPos = GetYPosForPointBaseOnValue(pointValue);
                graphPolyLineDrawer.AddNewPoint(new Vector2(xPos, yPos), toLast);
            }
        }

        private bool CheckIndexVisibilityByXDistance(int realIndex)
        {
            D(TAG, $"xPointDistance={xPointDistance}");
            var x = (int)(MINIMUM_BETWEEN_TWO_POINTS / xPointDistance);
            if (MINIMUM_BETWEEN_TWO_POINTS % xPointDistance != 0)
            {
                x++;
            }
            var result = realIndex % x == 0;
            D(TAG, $"result={result}");
            return result;
        }
        private void assert()
        {
            //var start = 0;
            //var temp = 0;
            //foreach (var p in elementCache.pointDrawers)
            //{
            //    if (start == 0)
            //    {
            //        temp = Convert.ToInt32(p.graphPointValue!.XValue);
            //    }
            //    else
            //    {
            //        Debug.Assert(temp == Convert.ToInt32(p.graphPointValue!.XValue) - 1);
            //        temp = Convert.ToInt32(p.graphPointValue!.XValue);
            //    }
            //    start++;
            //}
        }
    }
}

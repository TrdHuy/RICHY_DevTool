﻿using System.Diagnostics;
using System.Numerics;
using static RICHYEngine.LogCompat.Logger.RICHYEngineLogger;

namespace RICHYEngine.Views.Holders.GraphHolder
{
    public class VirtualizingGraphHolder : GraphHolder
    {
        private const string TAG = "VirtualizingGraphHolder";
        private const float MINIMUM_BETWEEN_TWO_POINTS = 60f;
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
            var newStartIndex = GetStartPointIndex();
            var newEndIndex = GetEndPointIndex();

            if (newStartIndex >= mCurrentEndIndex && newEndIndex > newStartIndex)
            {
                UpdateDisplayRangeByRedraw();
            }
            else
            {
                UpdateDisplayRangeAndModifyElements(xPointDistance, xPointDistance, newStartIndex, newEndIndex);
            }
        }

        public override void ShowGraph(List<IGraphPointValue> valueList)
        {
            mCurrentStartIndex = GetStartPointIndex();
            mCurrentEndIndex = GetEndPointIndex();
            base.ShowGraph(valueList);
        }

        public override int AddPointValue(IGraphPointValue newValue)
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
            var index = mCurrentShowingValueList.Count - 1;
            if (index > mCurrentEndIndex || index < mCurrentStartIndex)
            {
                return -1;
            }
            var isShouldAddToParent = CheckIndexVisibilityByXDistance(index);
            GenerateLabelX(newValue, DISPLAY_OFFSET_Y, DISPLAY_OFFSET_X, index,
                addToParent: isShouldAddToParent);
            if (elementCache.lineConnectionDrawer == null)
            {
                IGraphPolyLineDrawer graphPolyLineDrawer = mGraphPolyLineGenerator.Invoke(GraphElement.Line);
                elementCache.lineConnectionDrawer = graphPolyLineDrawer;
                GeneratePoint(newValue, index, mGraphContainer.GraphHeight, graphPolyLineDrawer,
                    addToParent: isShouldAddToParent);
            }
            else
            {
                GeneratePoint(newValue, index, mGraphContainer.GraphHeight,
                    elementCache.lineConnectionDrawer,
                    addToParent: isShouldAddToParent);
            }
            return index;
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
                UpdateDisplayRangeByRedraw();
            }
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
                    var isShouldAddToParent = CheckIndexVisibilityByXDistance(i);
                    GenerateLabelX(showingList[i], displayOffsetY, displayOffsetX, i,
                        addToParent: isShouldAddToParent);
                    GeneratePoint(showingList[i], i, graphHeight, graphPolyLineDrawer,
                        addToParent: isShouldAddToParent);
                }
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

        private void UpdateDisplayRangeByRedraw()
        {
            mCurrentStartIndex = GetStartPointIndex();
            mCurrentEndIndex = GetEndPointIndex();
            elementCache.pointDrawers.Clear();
            elementCache.labelXDrawers.Clear();
            mGraphContainer.LabelXCanvasHolder.Clear();
            mGraphContainer.PointAndLineCanvasHolder.Clear();

            if (mCurrentShowingValueList == null) return;
            SetupPointNConnectionNLabelX(mCurrentShowingValueList,
                mGraphContainer.GraphHeight, DISPLAY_OFFSET_Y, DISPLAY_OFFSET_X);
        }

        private void UpdateDisplayRangeAndModifyElements(float oldXDistance, float newXDistance, int newStartIndex, int newEndIndex)
        {
            D(TAG, $"UpdateDisplayRangeAndModifyElements: oldXDistance={oldXDistance},newXDistance={newXDistance},newStartIndex={newStartIndex},newEndIndex={newEndIndex}");
            if (mCurrentShowingValueList == null || elementCache.lineConnectionDrawer == null)
            {
                throw new Exception("Should not be null here");
            }

            var totalPointCount = mCurrentShowingValueList.Count;
            if (mCurrentStartIndex < newStartIndex)
            {
                D(TAG, $"mCurrentStartIndex={mCurrentStartIndex}");
                D(TAG, $"newStartIndex ={newStartIndex}");
                D(TAG, $"visibleItemCount ={elementCache.pointDrawers.Count}");

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
                    D(TAG, $"Remove at={i - mCurrentStartIndex}");
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
                    var isShouldAddToParent = CheckIndexVisibilityByXDistance(i);
                    GeneratePoint(mCurrentShowingValueList[i], i, mGraphContainer.GraphHeight, elementCache.lineConnectionDrawer,
                        toLast: false,
                        addToParent: isShouldAddToParent);
                    GenerateLabelX(mCurrentShowingValueList[i], DISPLAY_OFFSET_Y, DISPLAY_OFFSET_X, i,
                        toLast: false,
                        addToParent: isShouldAddToParent);
                    D(TAG, $"Add i={i}");
                }
                mCurrentStartIndex = newStartIndex;
            }

            if (mCurrentEndIndex < newEndIndex)
            {
                D(TAG, $"mCurrentEndIndex={mCurrentEndIndex}");
                D(TAG, $"newEndIndex={newEndIndex}");
                D(TAG, $"visibleItemCount={elementCache.pointDrawers.Count}");
                for (int i = mCurrentEndIndex + 1; i <= newEndIndex
                        && i < totalPointCount
                        && i >= mCurrentStartIndex; i++)
                {
                    var isShouldAddToParent = CheckIndexVisibilityByXDistance(i);
                    GeneratePoint(mCurrentShowingValueList[i], i, mGraphContainer.GraphHeight, elementCache.lineConnectionDrawer,
                        toLast: true,
                        addToParent: isShouldAddToParent);
                    GenerateLabelX(mCurrentShowingValueList[i], DISPLAY_OFFSET_Y, DISPLAY_OFFSET_X, i,
                        toLast: true,
                        addToParent: isShouldAddToParent);
                    D(TAG, $"Add i={i}");
                }
                mCurrentEndIndex = newEndIndex;
            }
            else if (mCurrentEndIndex > newEndIndex)
            {
                D(TAG, $"mCurrentEndIndex={mCurrentEndIndex}");
                D(TAG, $"newEndIndex={newEndIndex}");
                D(TAG, $"visibleItemCount={elementCache.pointDrawers.Count}");
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
                    D(TAG, $"Remove at={lastIndex}");
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
            D(TAG, $"graphPosXOnCanvas={graphPosXOnCanvas}");
            D(TAG, $"startPoint={startPoint}");
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
            D(TAG, $"rangeX={rangeX}");
            D(TAG, $"endPoint={endPoint}");
            return endPoint;
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

using RICHYEngine.Views.Animation;
using RICHYEngine.Views.Holders.GraphHolder.Elements;
using System.Diagnostics;
using System.Numerics;
using static RICHYEngine.LogCompat.Logger.RICHYEngineLogger;

namespace RICHYEngine.Views.Holders.GraphHolder
{
    public class VirtualizingGraphHolder : GraphHolder
    {
        private const string TAG = "VirtualizingGraphHolder";
        private const float MINIMUM_BETWEEN_TWO_POINTS = 90f;
        protected int mCurrentStartIndex = 0;
        protected int mCurrentEndIndex = 0;

        public VirtualizingGraphHolder(IGraphContainer graphContainer,
            Func<GraphElement, IGraphPointDrawer> graphPointGenerator,
            Func<GraphElement, IGraphLineDrawer> graphLineGenerator,
            Func<GraphElement, IGraphLabelDrawer> graphLabelGenerator,
            Func<GraphElement, IGraphPolyLineDrawer> graphPolyLineGenerator,
            Func<GraphElement, IRectDrawer> rectGenerator) : base(graphContainer,
                graphPointGenerator,
                graphLineGenerator,
                graphLabelGenerator,
                graphPolyLineGenerator,
                rectGenerator)
        { }

        public override string Dump(bool isDumpGraphLineDrawer = false)
        {
            var dump = base.Dump(false);
            dump += $"\nmCurrentStartIndex={mCurrentStartIndex}\n";
            dump += $"mCurrentEndIndex={mCurrentEndIndex}\n";
            if (isDumpGraphLineDrawer)
            {
                dump += "\n" + elementCache.lineConnectionDrawer?.Dump();
            }
            return dump;
        }
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

        public async void AddPointValueWithAnimation(IGraphPointValue newValue)
        {
            try
            {
                await elementCache.addingNewValueSemaphore.WaitAsync();
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
                    elementCache.addingNewValueSemaphore.Release();
                    return;
                }
                var isShouldAddToParent = CheckIndexVisibilityByXDistance(index);
                GenerateLabelX(newValue, DISPLAY_OFFSET_Y, DISPLAY_OFFSET_X, index,
                    addToParent: isShouldAddToParent);
                IGraphPolyLineDrawer? graphPolyLineDrawer = elementCache.lineConnectionDrawer;
                if (graphPolyLineDrawer == null)
                {
                    graphPolyLineDrawer = mGraphPolyLineGenerator.Invoke(GraphElement.Line);
                    elementCache.lineConnectionDrawer = graphPolyLineDrawer;
                }
                var lastIndex = elementCache.pointDrawers.GetElementCacheCount() - 1;
                IGraphPointDrawer? lastPointCache = null;
                if (lastIndex > 0)
                {
                    lastPointCache = elementCache.pointDrawers.GetElementAt(lastIndex);
                }

                var point = GeneratePoint(newValue, index, mGraphContainer.GraphHeight, graphPolyLineDrawer,
                        addToParent: false,
                        toLast: true,
                        polyLineStartPoint: lastPointCache?.GetPositionOnCanvas());
                var pointDrawer = point.Item1;
                var pointIndex = point.Item2;
                var pointIndexOnPolyLine = point.Item3;

                if (pointIndex > 0)
                {
                    var previousPointDrawer = elementCache.pointDrawers.GetElementAt(pointIndex - 1);
                    var fromPos = previousPointDrawer.GetPositionOnCanvas();
                    var toPos = pointDrawer.GetPositionOnCanvas();
                    var movingPoint = mGraphPointGenerator.Invoke(GraphElement.Point);
                    mGraphContainer.PointAndLineCanvasHolder.AddChild(movingPoint);
                    movingPoint.SetUpVisual(GraphElement.Point);
                    MovePointAndLineToward(fromPos,
                        toPos,
                        movingPoint,
                        graphPolyLineDrawer,
                        pointIndexOnPolyLine,
                        durationMs: 500,
                        () =>
                        {
                            if (isShouldAddToParent)
                            {
                                mGraphContainer.PointAndLineCanvasHolder.AddChild(pointDrawer);
                            }
                            mGraphContainer.PointAndLineCanvasHolder.RemoveChild(movingPoint);
                            elementCache.addingNewValueSemaphore.Release();
                        });
                }
            }
            finally
            {
            }
        }

        private void MovePointAndLineToward(Vector2 fromPos,
            Vector2 toPos,
            IGraphPointDrawer targetPoint,
            IGraphPolyLineDrawer targetLine,
            int pointIndexOnPolyLine,
            int durationMs = 1000,
            Action? animationEndCallback = null)
        {
            var A = (fromPos.Y - toPos.Y) / (fromPos.X - toPos.X);
            var B = (toPos.Y * fromPos.X - fromPos.Y * toPos.X) / (fromPos.X - toPos.X);

            var curX = fromPos.X;
            var velocityX = xPointDistance / durationMs * 1000f;
            var isMoveRight = fromPos.X < toPos.X;
            void MovePointToWardAnimating(double fps)
            {
                var newPos = new Vector2(curX, curX * A + B);
                targetLine.ChangePointPosition(pointIndexOnPolyLine, newPos);
                targetPoint.SetPositionOnCanvas(GraphElement.Point, newPos);
                if (isMoveRight)
                {
                    if (curX >= toPos.X)
                    {
                        AnimationController.Animating -= MovePointToWardAnimating;
                        targetLine.ChangePointPosition(pointIndexOnPolyLine, toPos);
                        targetPoint.SetPositionOnCanvas(GraphElement.Point, toPos);
                        animationEndCallback?.Invoke();
                        return;
                    }
                    curX += velocityX / (float)fps;
                    if (curX > toPos.X)
                    {
                        curX = toPos.X;
                    }
                }
                else
                {
                    if (curX <= toPos.X)
                    {
                        AnimationController.Animating -= MovePointToWardAnimating;
                        targetLine.ChangePointPosition(pointIndexOnPolyLine, toPos);
                        targetPoint.SetPositionOnCanvas(GraphElement.Point, toPos);
                        animationEndCallback?.Invoke();
                        return;
                    }
                    curX -= velocityX / (float)fps;
                    if (curX < toPos.X)
                    {
                        curX = toPos.X;
                    }
                }
            }
            AnimationController.Animating += MovePointToWardAnimating; ;
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

        public void ChangeZoomX(float offset, Vector2 mousePos)
        {
            var temp = xPointDistance + offset;
            var newDistance = temp < 1 ? 1 : temp;
            if (mCurrentShowingValueList != null && xPointDistance != newDistance)
            {
                var startVisibilityRange = GetXPosForPointCanvas();
                var endVisibilityRange = (mCurrentShowingValueList.Count - 1) * xPointDistance + startVisibilityRange;
                var isIntersectWithGraphLine = mousePos.X >= startVisibilityRange && mousePos.X <= endVisibilityRange;
                if (isIntersectWithGraphLine)
                {
                    var pointIndex = GetPointIndexViaMousePos(mousePos);
                    var oldPos = pointIndex * xPointDistance;
                    ChangeXDistance(newDistance);
                    var newPos = pointIndex * xPointDistance;
                    var pointMovedOffset = newPos - oldPos;
                    MoveGraph(-(int)pointMovedOffset, 0);
                }
                else
                {
                    ChangeXDistance(newDistance);
                }
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
                && i - mCurrentStartIndex < elementCache.pointDrawers.GetElementCacheCount(); i++)
            {
                var labelX = elementCache.labelXDrawers.GetElementAt(i - mCurrentStartIndex);
                float xPos = GetXPosForPointBaseOnPointIndex(i);
                labelX.SetPositionOnCanvas(GraphElement.LabelX, new Vector2(xPos, 0));

                float yPos = GetYPosForPointBaseOnValue(mCurrentShowingValueList![i]);
                var point = elementCache.pointDrawers.GetElementAt(i - mCurrentStartIndex);
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
            elementCache.pointDrawers.ClearCache();
            elementCache.labelXDrawers.ClearCache();
            mGraphContainer.LabelXCanvasHolder.Clear();
            mGraphContainer.PointAndLineCanvasHolder.Clear();

            if (mCurrentShowingValueList == null) return;
            SetupPointNConnectionNLabelX(mCurrentShowingValueList,
                mGraphContainer.GraphHeight, DISPLAY_OFFSET_Y, DISPLAY_OFFSET_X);
        }

        private void UpdateDisplayRangeAndModifyElements(float oldXDistance, float newXDistance, int newStartIndex, int newEndIndex)
        {
            D(TAG, $"UpdateDisplayRangeAndModifyElements: oldXDistance={oldXDistance}," +
                $"newXDistance={newXDistance}," +
                $"mCurrentStartIndex={mCurrentStartIndex}," +
                $"mCurrentEndIndex={mCurrentEndIndex}," +
                $"newStartIndex={newStartIndex}," +
                $"newEndIndex={newEndIndex}");
            if (mCurrentShowingValueList == null || elementCache.lineConnectionDrawer == null)
            {
                throw new Exception("Should not be null here");
            }

            var totalPointCount = mCurrentShowingValueList.Count;
            if (mCurrentStartIndex < newStartIndex)
            {
                D(TAG, $"mCurrentStartIndex={mCurrentStartIndex}");
                D(TAG, $"newStartIndex ={newStartIndex}");
                D(TAG, $"visibleItemCount ={elementCache.pointDrawers.GetElementCacheCount()}");

                for (int i = mCurrentStartIndex; i < newStartIndex &&
                        i < totalPointCount &&
                        elementCache.pointDrawers.GetElementCacheCount() > 0; i++)
                {
                    var pointCache = elementCache.pointDrawers.GetElementAt(0);
                    elementCache.pointDrawers.RemoveElementAt(0);
                    mGraphContainer.PointAndLineCanvasHolder.RemoveChild(pointCache);
                    var labelXDrawer = elementCache.labelXDrawers.GetElementAt(0);
                    mGraphContainer.LabelXCanvasHolder.RemoveCanvasChildAndAssignNewState(labelXDrawer, elementCache.labelXDrawers.GetElementStat(labelXDrawer));
                    elementCache.labelXDrawers.RemoveElementAt(0);
                    elementCache.lineConnectionDrawer!.RemovePoint(pointCache.GetPositionOnCanvas());
                    D(TAG, $"Remove at={i - mCurrentStartIndex}");
                }
                mCurrentStartIndex = newStartIndex;
            }
            else if (mCurrentStartIndex > newStartIndex)
            {
                D(TAG, $"mCurrentStartIndex={mCurrentStartIndex}");
                D(TAG, $"newStartIndex={newStartIndex}");
                D(TAG, $"visibleItemCount={elementCache.pointDrawers.GetElementCacheCount()}");

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
                D(TAG, $"visibleItemCount={elementCache.pointDrawers.GetElementCacheCount()}");
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
                D(TAG, $"visibleItemCount={elementCache.pointDrawers.GetElementCacheCount()}");
                var deleteFromIndex = mCurrentEndIndex < totalPointCount ? mCurrentEndIndex : totalPointCount - 1;
                for (int i = deleteFromIndex; i > newEndIndex
                        && i - mCurrentStartIndex < elementCache.pointDrawers.GetElementCacheCount(); i--)
                {
                    var lastIndex = elementCache.pointDrawers.GetElementCacheCount() - 1;
                    var pointCache = elementCache.pointDrawers.GetElementAt(lastIndex);
                    elementCache.pointDrawers.RemoveElementAt(lastIndex);
                    mGraphContainer.PointAndLineCanvasHolder.RemoveChild(pointCache);
                    var labelXDrawer = elementCache.labelXDrawers.GetElementAt(lastIndex);
                    mGraphContainer.LabelXCanvasHolder.RemoveCanvasChildAndAssignNewState(labelXDrawer, elementCache.labelXDrawers.GetElementStat(labelXDrawer));
                    elementCache.labelXDrawers.RemoveElementAt(lastIndex);
                    var isLineDrawerRemovePointSuccess = elementCache.lineConnectionDrawer!.RemovePoint(pointCache.GetPositionOnCanvas());
                    D(TAG, $"Remove at={lastIndex}, isLineDrawerRemovePointSuccess={isLineDrawerRemovePointSuccess}");
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


        /// <summary>
        /// Hàm này kiểm tra xem 1 point có nên được hiển thị hay không dựa
        /// vào khoảng cách giữa các point (XDistance)
        /// </summary>
        /// <param name="realIndex"></param>
        /// <returns></returns>
        private bool CheckIndexVisibilityByXDistance(int realIndex)
        {
            //D(TAG, $"xPointDistance={xPointDistance}");
            var x = (int)(MINIMUM_BETWEEN_TWO_POINTS / xPointDistance);
            if (MINIMUM_BETWEEN_TWO_POINTS % xPointDistance != 0)
            {
                x++;
            }
            var result = realIndex % x == 0;
            //D(TAG, $"result={result}");
            return result;
        }

        private void assert()
        {
            var start = 0;
            var temp = 0;
            if (mCurrentShowingValueList == null) return;

            if (mCurrentEndIndex < mCurrentShowingValueList.Count)
            {
                Debug.Assert(elementCache.pointDrawers.GetElementCacheCount() == mCurrentEndIndex - mCurrentStartIndex + 1);
            }
            else if (mCurrentStartIndex < mCurrentShowingValueList.Count)
            {
                Debug.Assert(elementCache.pointDrawers.GetElementCacheCount() == mCurrentShowingValueList.Count - mCurrentStartIndex);
            }
            else
            {
                Debug.Assert(elementCache.pointDrawers.GetElementCacheCount() == 0);
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

}

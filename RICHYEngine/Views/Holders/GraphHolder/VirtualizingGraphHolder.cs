using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.PlayerLoop;
using static RICHYEngine.Views.Holders.GraphHolder.ICanvasChild;

namespace RICHYEngine.Views.Holders.GraphHolder
{
    public class VirtualizingGraphHolder : GraphHolder
    {
        private int mCurrentStartIndex = 0;
        private int mCurrentEndIndex = 0;
        protected int mCurrentPointOffsetCount = 0;

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
            UpdateDisplayRangeAndModifyElements();
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
                RearrangePointAndConnection();
                InvalidateLabelY(mGraphContainer.GraphHeight, dashDistanceY);
            }
        }

        public override void ChangeXDistance(float distance)
        {
            var newDistance = distance < 1 ? 1 : distance;
            if (mCurrentShowingValueList != null && xPointDistance != newDistance)
            {
                xPointDistance = newDistance;
                UpdateDisplayRangeAndModifyElements();
                RearrangePointAndConnection();
            }
        }

        private void RearrangePointAndConnection()
        {
            if (mCurrentShowingValueList == null)
            {
                throw new Exception("Should not be null here");
            }
            for (int i = mCurrentStartIndex; i <= mCurrentEndIndex && i < mCurrentShowingValueList.Count; i++)
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

        private void UpdateDisplayRangeAndModifyElements()
        {
            Debug.WriteLine("HUY --------------");
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
            assert();

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
            Debug.WriteLine($"graphPosXOnCanvas={graphPosXOnCanvas}");
            Debug.WriteLine($"startPoint={startPoint}");
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
            Debug.WriteLine($"rangeX={rangeX}");
            Debug.WriteLine($"endPoint={endPoint}");
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
}

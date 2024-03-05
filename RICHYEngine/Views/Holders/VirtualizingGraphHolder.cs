using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.PlayerLoop;
using static RICHYEngine.Views.Holders.ICanvasChild;

namespace RICHYEngine.Views.Holders
{
    public class VirtualizingGraphHolder : GraphHolder
    {
        private int mCurrentStartIndex = 0;
        private int mCurrentEndIndex = 0;

        public VirtualizingGraphHolder(IGraphContainer graphContainer,
            Func<GraphElement, IGraphPointDrawer> graphPointGenerator,
            Func<GraphElement, IGraphLineDrawer> graphLineGenerator,
            Func<GraphElement, IGraphLabelDrawer> graphLabelGenerator) : base(graphContainer,
                graphPointGenerator,
                graphLineGenerator,
                graphLabelGenerator)
        { }

        public override void AddPointValue(IGraphPointValue newValue)
        {
            base.AddPointValue(newValue);
            var addedIndex = elementCache.pointDrawers.Count - 1;
            if (addedIndex > mCurrentEndIndex)
            {
                elementCache.pointDrawers[addedIndex].RemoveFromParent(GraphElement.Point);
                elementCache.labelXDrawers[addedIndex].RemoveFromParent(GraphElement.Point);
                if (addedIndex - mCurrentEndIndex >= 2)
                {
                    elementCache.lineConnectionDrawers[addedIndex - 1].RemoveFromParent(GraphElement.Line);
                }
            }
        }

        public override void MoveGraph(int offsetLeft, int offsetTop)
        {
            base.MoveGraph(offsetLeft, offsetTop);
            UpdateDisplayingIndex();
        }

        public override void ShowGraph(List<IGraphPointValue> valueList)
        {
            base.ShowGraph(valueList);
            mCurrentStartIndex = GetStartPointIndex();
            mCurrentEndIndex = GetEndPointIndex();
        }

        public override void ChangeYMax(float newYMax)
        {
            if (yMax != newYMax)
            {
                yMax = newYMax;
                RearrangePointAndConnection(DISPLAY_OFFSET_X, DISPLAY_OFFSET_Y, yMax, mGraphContainer.GraphHeight);
            }
        }
        public override void ChangeXDistance(float distance)
        {
            var newDistance = distance < X_POINT_DISTANCE_MIN ? X_POINT_DISTANCE_MIN : distance;
            if (mCurrentShowingValueList != null && xPointDistance != newDistance)
            {
                xPointDistance = newDistance;
                RearrangePointAndConnection(DISPLAY_OFFSET_X, DISPLAY_OFFSET_Y, yMax, mGraphContainer.GraphHeight);
                UpdateDisplayingIndex();
            }
        }

        private void RearrangePointAndConnection(float displayOffsetX,
            float displayOffsetY,
            float yMax,
            float graphHeight)
        {
            for (int i = 0; i < elementCache.pointDrawers.Count; i++)
            {
                var labelX = elementCache.labelXDrawers[i];
                float xPos = i * xPointDistance + displayOffsetX;
                labelX.SetPositionOnCanvas(GraphElement.LabelX, new Vector2(xPos + mPointCanvasHolderLeft, -10 + displayOffsetY));

                float yPos = (mCurrentShowingValueList![i].YValue / yMax) * graphHeight + DISPLAY_OFFSET_Y;
                var point = elementCache.pointDrawers[i];
                point.SetPositionOnCanvas(GraphElement.Point, new Vector2(xPos, yPos));

                if (i > 0)
                {
                    var line = elementCache.lineConnectionDrawers[i - 1];
                    line.SetPositionOnCanvas(elementCache.pointDrawers[i - 1].GetPositionOnCanvas(),
                        point.GetPositionOnCanvas());
                }
            }
        }

        private void UpdateDisplayingIndex()
        {
            var newStartIndex = GetStartPointIndex();
            var newEndIndex = GetEndPointIndex();
            if (mCurrentStartIndex < newStartIndex)
            {
                for (int i = mCurrentStartIndex; i < newStartIndex; i++)
                {
                    elementCache.pointDrawers[i].RemoveFromParent(GraphElement.Point);
                    elementCache.labelXDrawers[i].RemoveFromParent(GraphElement.LabelX);
                    if (i >= 1)
                    {
                        elementCache.lineConnectionDrawers[i - 1].RemoveFromParent(GraphElement.Line);
                    }
                }
                mCurrentStartIndex = newStartIndex;
            }
            else if (mCurrentStartIndex > newStartIndex)
            {
                for (int i = newStartIndex; i < mCurrentStartIndex; i++)
                {
                    elementCache.pointDrawers[i].SetIntoParent(GraphElement.Point);
                    elementCache.labelXDrawers[i].SetIntoParent(GraphElement.LabelX);
                    if (i >= 1)
                    {
                        elementCache.lineConnectionDrawers[i - 1].SetIntoParent(GraphElement.Line);
                    }
                }
                mCurrentStartIndex = newStartIndex;
            }

            if (mCurrentEndIndex < newEndIndex)
            {
                for (int i = mCurrentEndIndex + 1; i <= newEndIndex && i < elementCache.pointDrawers.Count; i++)
                {
                    elementCache.pointDrawers[i].SetIntoParent(ICanvasChild.GraphElement.Point);
                    elementCache.labelXDrawers[i].SetIntoParent(GraphElement.LabelX);
                    if (i <= elementCache.pointDrawers.Count - 2)
                    {
                        elementCache.lineConnectionDrawers[i].SetIntoParent(ICanvasChild.GraphElement.Line);
                    }
                }
                mCurrentEndIndex = newEndIndex;
            }
            else if (mCurrentEndIndex > newEndIndex)
            {
                for (int i = mCurrentEndIndex; i > newEndIndex && i < elementCache.pointDrawers.Count; i--)
                {
                    elementCache.pointDrawers[i].RemoveFromParent(GraphElement.Point);
                    elementCache.labelXDrawers[i].RemoveFromParent(GraphElement.LabelX);
                    if (i <= elementCache.pointDrawers.Count - 2)
                    {
                        elementCache.lineConnectionDrawers[i].RemoveFromParent(GraphElement.Line);
                    }
                }
                mCurrentEndIndex = newEndIndex;
            }
        }

        private int GetStartPointIndex()
        {
            var graphPosXOnCanvas = mPointCanvasHolderLeft + DISPLAY_OFFSET_X;
            var startPoint = 0;
            if (graphPosXOnCanvas < 0)
            {
                startPoint = (int)Math.Ceiling(graphPosXOnCanvas / xPointDistance * -1);
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
            var endPoint = (int)(rangeX / xPointDistance);
            if (rangeX < 0)
            {
                endPoint = -1;
            }
            Debug.WriteLine($"rangeX={rangeX}");
            Debug.WriteLine($"endPoint={endPoint}");
            return endPoint;
        }
    }
}

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
            for (int i = 0; i < elementCache.pointDrawers.Count; i++)
            {
                var labelX = elementCache.labelXDrawers[i];
                float xPos = GetXPosForPointBaseOnPointIndex(i);
                labelX.SetPositionOnCanvas(GraphElement.LabelX, new Vector2(xPos, 0));

                float yPos = GetYPosForPointBaseOnValue(mCurrentShowingValueList![i]);
                var point = elementCache.pointDrawers[i];
                var oldPointPos = point.GetPositionOnCanvas();
                var newPointPos = new Vector2(xPos, yPos);
                point.SetPositionOnCanvas(GraphElement.Point, newPointPos);
                if (CheckIndexIsVisible(i))
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
                LinkedList<int> k = new LinkedList<int>();
                Debug.WriteLine($"HUY mCurrentStartIndex={mCurrentStartIndex}");
                Debug.WriteLine($"HUY newStartIndex={newStartIndex}");

                for (int i = mCurrentStartIndex; i < newStartIndex && i < totalPointCount; i++)
                {
                    var pointCache = elementCache.pointDrawers[i - mCurrentStartIndex];
                    elementCache.pointDrawers.RemoveAt(i - mCurrentStartIndex);
                    mGraphContainer.PointAndLineCanvasHolder.RemoveChild(pointCache);
                    mGraphContainer.LabelXCanvasHolder.RemoveChild(elementCache.labelXDrawers[i - mCurrentStartIndex]);
                    elementCache.labelXDrawers.RemoveAt(i - mCurrentStartIndex);
                    elementCache.lineConnectionDrawer!.RemovePoint(pointCache.GetPositionOnCanvas());
                    Debug.WriteLine($"HUY Remove at={i - mCurrentStartIndex}");
                }
                mCurrentStartIndex = newStartIndex;
            }
            else if (mCurrentStartIndex > newStartIndex)
            {
                //Debug.WriteLine($"HUY mCurrentStartIndex={mCurrentStartIndex}");
                //Debug.WriteLine($"HUY newStartIndex={newStartIndex}");
                for (int i = mCurrentStartIndex - 1; i >= newStartIndex && i < totalPointCount; i--)
                {
                    GeneratePoint(mCurrentShowingValueList[i], i, mGraphContainer.GraphHeight, elementCache.lineConnectionDrawer, toLast: false);
                    var newPointCache = elementCache.pointDrawers[0];
                    mGraphContainer.PointAndLineCanvasHolder.AddChild(newPointCache);
                    GenerateLabelX(mCurrentShowingValueList[i], DISPLAY_OFFSET_Y, DISPLAY_OFFSET_X, i, toLast: false);
                    var newLabelXCache = elementCache.labelXDrawers[0];
                    mGraphContainer.LabelXCanvasHolder.AddChild(newLabelXCache);
                    //Debug.WriteLine($"HUY Add i={i}");
                }
                mCurrentStartIndex = newStartIndex;
            }

            if (mCurrentEndIndex < newEndIndex)
            {
                //Debug.WriteLine($"HUY mCurrentEndIndex={mCurrentEndIndex}");
                //Debug.WriteLine($"HUY newEndIndex={newEndIndex}");
                for (int i = mCurrentEndIndex + 1; i <= newEndIndex && i < elementCache.pointDrawers.Count; i++)
                {
                    mGraphContainer.PointAndLineCanvasHolder.AddChild(elementCache.pointDrawers[i]);
                    mGraphContainer.LabelXCanvasHolder.AddChild(elementCache.labelXDrawers[i]);
                    elementCache
                       .lineConnectionDrawer!
                       .AddNewPoint(elementCache.pointDrawers[i].GetPositionOnCanvas(),
                       toLast: true);
                    //Debug.WriteLine($"HUY Add i={i}");
                }
                mCurrentEndIndex = newEndIndex;
            }
            else if (mCurrentEndIndex > newEndIndex)
            {
                //Debug.WriteLine($"HUY mCurrentEndIndex={mCurrentEndIndex}");
                //Debug.WriteLine($"HUY newEndIndex={newEndIndex}");
                var deleteFromIndex = mCurrentEndIndex < totalPointCount ? mCurrentEndIndex : totalPointCount - 1;
                for (int i = deleteFromIndex; i > newEndIndex && i < elementCache.pointDrawers.Count; i--)
                {
                    mGraphContainer.PointAndLineCanvasHolder.RemoveChild(elementCache.pointDrawers[i]);
                    mGraphContainer.LabelXCanvasHolder.RemoveChild(elementCache.labelXDrawers[i]);
                    elementCache.lineConnectionDrawer!.RemovePoint(elementCache.pointDrawers[i].GetPositionOnCanvas());
                    //Debug.WriteLine($"HUY Remove i={i}");
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

        //protected override IGraphLabelDrawer GenerateLabelX(IGraphPointValue pointValue, float displayOffsetY, float displayOffsetX, int pointIndex)
        //{
        //    if (pointIndex >= mCurrentStartIndex && pointIndex <= mCurrentEndIndex)
        //    {
        //        return base.GenerateLabelX(pointValue, displayOffsetY, displayOffsetX, pointIndex);
        //    }
        //    else
        //    {
        //        var labelX = mGraphLabelGenerator.Invoke(GraphElement.LabelX);
        //        float xPos = GetXPosForPointBaseOnPointIndex(pointIndex);
        //        labelX.SetUpVisual(GraphElement.LabelX);
        //        labelX.SetText(pointValue.XValue?.ToString() ?? pointIndex.ToString());
        //        labelX.SetPositionOnCanvas(GraphElement.LabelX, new Vector2(xPos, 0));
        //        elementCache.labelXDrawers.Add(labelX);
        //        return labelX;
        //    }
        //}

        //protected override void GeneratePoint(IGraphPointValue graphPointValue, int pointIndex, float graphHeight, IGraphPolyLineDrawer graphPolyLineDrawer)
        //{
        //    if (pointIndex >= mCurrentStartIndex && pointIndex <= mCurrentEndIndex)
        //    {
        //        base.GeneratePoint(graphPointValue, pointIndex, graphHeight, graphPolyLineDrawer);
        //    }
        //    else
        //    {
        //        float xPos = GetXPosForPointBaseOnPointIndex(pointIndex);
        //        float yPos = GetYPosForPointBaseOnValue(graphPointValue);

        //        IGraphPointDrawer point = mGraphPointGenerator.Invoke(GraphElement.Point);
        //        point.SetUpVisual(GraphElement.Point);
        //        point.SetPositionOnCanvas(GraphElement.Point, new Vector2(xPos, yPos));
        //        elementCache.pointDrawers.Add(point);
        //    }
        //}


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

        private bool CheckIndexIsVisible(int indexNeedToCheck)
        {
            return indexNeedToCheck >= mCurrentStartIndex && indexNeedToCheck <= mCurrentEndIndex;
        }

    }
}

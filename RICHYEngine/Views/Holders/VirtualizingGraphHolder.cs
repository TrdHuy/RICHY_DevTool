using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.PlayerLoop;

namespace RICHYEngine.Views.Holders
{
    public class VirtualizingGraphHolder : GraphHolder
    {
        private int mCurrentStartIndex = 0;
        private int mCurrentEndIndex = 0;

        public VirtualizingGraphHolder(IGraphContainer graphContainer,
            Func<ICanvasChild.GraphElement, IGraphPointDrawer> graphPointGenerator,
            Func<ICanvasChild.GraphElement, IGraphLineDrawer> graphLineGenerator,
            Func<ICanvasChild.GraphElement, IGraphLabelDrawer> graphLabelGenerator) : base(graphContainer,
                graphPointGenerator,
                graphLineGenerator,
                graphLabelGenerator)
        {

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

        private void UpdateDisplayingIndex()
        {
            var newStartIndex = GetStartPointIndex();
            var newEndIndex = GetEndPointIndex();
            if (mCurrentStartIndex < newStartIndex)
            {
                for (int i = mCurrentStartIndex; i < newStartIndex; i++)
                {
                    elementCache.pointDrawers[i].RemoveFromParent(ICanvasChild.GraphElement.Point);
                    if (i >= 1)
                    {
                        elementCache.lineConnectionDrawers[i - 1].RemoveFromParent(ICanvasChild.GraphElement.Line);
                    }
                }
                mCurrentStartIndex = newStartIndex;
            }
            else if (mCurrentStartIndex > newStartIndex)
            {
                for (int i = newStartIndex; i < mCurrentStartIndex; i++)
                {
                    elementCache.pointDrawers[i].SetIntoParent(ICanvasChild.GraphElement.Point);
                    if (i >= 1)
                    {
                        elementCache.lineConnectionDrawers[i - 1].SetIntoParent(ICanvasChild.GraphElement.Line);
                    }
                }
                mCurrentStartIndex = newStartIndex;
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

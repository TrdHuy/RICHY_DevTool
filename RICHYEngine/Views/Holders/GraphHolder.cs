
using System.Numerics;
using static RICHYEngine.Views.Holders.ICanvasChild;

namespace RICHYEngine.Views.Holders
{
    public interface IGraphContainer
    {
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
        Vector2 GetPosition();


        /// <summary>
        /// Unity Implementation Eg:
        /// line.GetComponent<Image>().sprite = lineSpr;
        /// line.GetComponent<Image>().type = Image.Type.Sliced;
        /// </summary>
        void SetUpVisual(GraphElement targetElement);
    }
    public interface IGraphLine : ICanvasChild
    {
        void SetPosition(Vector2 firstPoint, Vector2 secondPoint);
    }
    public interface IGraphPoint : ICanvasChild
    {
        void SetPosition(Vector2 position);
    }

    public interface IGraphLabel : IGraphPoint
    {
        void SetText(string text);
    }

    public class GraphHolder
    {
        private IGraphContainer mGraphContainer;
        private Func<IGraphPoint> mGraphPointGenerator;
        private Func<IGraphLine> mGraphLineGenerator;
        private Func<IGraphLabel> mGraphLabelGenerator;

        public GraphHolder(IGraphContainer graphContainer
            , Func<IGraphPoint> graphPointGenerator
            , Func<IGraphLine> graphLineGenerator
            , Func<IGraphLabel> graphLabelGenerator)
        {
            mGraphContainer = graphContainer;
            mGraphPointGenerator = graphPointGenerator;
            mGraphLineGenerator = graphLineGenerator;
            mGraphLabelGenerator = graphLabelGenerator;
        }

        public void ShowGraph(List<int> valueList)
        {
            mGraphContainer.Clear();
            float graphHeight = mGraphContainer.GraphHeight;
            float graphWidth = mGraphContainer.GraphWidth;
            float yMax = 100f;
            float xSize = 50f;
            IGraphPoint? lastPoint = null;
            for (int i = 0; i < valueList.Count; i++)
            {
                float xPos = i * xSize;
                float yPos = (valueList[i] / yMax) * graphHeight;

                if (i > 0)
                {
                    var labelX = mGraphLabelGenerator.Invoke();
                    if (labelX.SetParent(GraphElement.LabelX))
                    {
                        labelX.SetUpVisual(GraphElement.LabelX);
                        labelX.SetText(i.ToString());
                        labelX.SetPosition(new Vector2(xPos, -10));
                    }

                    var dashX = mGraphLineGenerator.Invoke();
                    if (dashX.SetParent(GraphElement.DashX))
                    {
                        dashX.SetUpVisual(GraphElement.DashX);
                        dashX.SetPosition(new Vector2(xPos, 0), new Vector2(xPos, graphHeight));
                    }
                }

                var point = CreatePoint(new Vector2(xPos, yPos));
                if (lastPoint != null)
                {
                    CreatePointConnection(lastPoint.GetPosition(), point.GetPosition());
                }
                lastPoint = point;
            }

            int separatorCount = 11;
            for (int i = 0; i < separatorCount; i++)
            {
                var labelY = mGraphLabelGenerator.Invoke();
                var normalizedValue = i * 1f / 10f;
                var yPos = normalizedValue * graphHeight;
                if (labelY.SetParent(GraphElement.LabelY))
                {
                    labelY.SetUpVisual(GraphElement.LabelY);
                    labelY.SetText(((int)(yMax * normalizedValue)).ToString());
                    labelY.SetPosition(new Vector2(-20, yPos));
                }
                if (i > 0)
                {
                    var dashY = mGraphLineGenerator.Invoke();
                    if (dashY.SetParent(GraphElement.DashY))
                    {
                        dashY.SetUpVisual(GraphElement.DashY);
                        dashY.SetPosition(new Vector2(0, yPos), new Vector2(graphWidth, yPos));
                    }
                }
            }

            IGraphLine oX = mGraphLineGenerator.Invoke();
            if (oX.SetParent(GraphElement.Ox))
            {
                oX.SetUpVisual(GraphElement.Ox);
                oX.SetPosition(new Vector2(0, 0), new Vector2(graphWidth, 0));
            }

            IGraphLine oY = mGraphLineGenerator.Invoke();
            if (oY.SetParent(GraphElement.Oy))
            {
                oY.SetUpVisual(GraphElement.Oy);
                oY.SetPosition(new Vector2(0, 0), new Vector2(0, graphHeight));
            }
        }

        private IGraphPoint CreatePoint(Vector2 pos)
        {
            IGraphPoint point = mGraphPointGenerator.Invoke();
            if (point.SetParent(GraphElement.Point))
            {
                point.SetUpVisual(GraphElement.Point);
                point.SetPosition(pos);
            }
            return point;
        }

        private void CreatePointConnection(Vector2 posA, Vector2 posB)
        {
            IGraphLine line = mGraphLineGenerator.Invoke();
            if (line.SetParent(GraphElement.Line))
            {
                line.SetUpVisual(GraphElement.Line);
                line.SetPosition(posA, posB);
            }
        }
    }
}

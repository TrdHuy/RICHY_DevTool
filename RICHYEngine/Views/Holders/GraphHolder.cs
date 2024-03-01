
using System.Numerics;
using static RICHYEngine.Views.Holders.ICanvasChild;

namespace RICHYEngine.Views.Holders
{
    public interface IGraphPointValue
    {
        int XValue { get; }
    }

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
    public interface IGraphLineDrawer : ICanvasChild
    {
        void SetPosition(Vector2 firstPoint, Vector2 secondPoint);
    }
    public interface IGraphPointDrawer : ICanvasChild
    {
        void SetPosition(GraphElement targetElement, Vector2 position);
    }

    public interface IGraphLabelDrawer : IGraphPointDrawer
    {
        void SetText(string text);
    }

    public class GraphHolder
    {
        private IGraphContainer mGraphContainer;
        private Func<IGraphPointDrawer> mGraphPointGenerator;
        private Func<IGraphLineDrawer> mGraphLineGenerator;
        private Func<IGraphLabelDrawer> mGraphLabelGenerator;

        private List<IGraphPointValue>? mCurrentShowingValueList;
        private float yMax = 100f;

        public GraphHolder(IGraphContainer graphContainer
            , Func<IGraphPointDrawer> graphPointGenerator
            , Func<IGraphLineDrawer> graphLineGenerator
            , Func<IGraphLabelDrawer> graphLabelGenerator)
        {
            mGraphContainer = graphContainer;
            mGraphPointGenerator = graphPointGenerator;
            mGraphLineGenerator = graphLineGenerator;
            mGraphLabelGenerator = graphLabelGenerator;
        }
        #region public API
        public void AddPointValue(IGraphPointValue newValue)
        {
            if (mCurrentShowingValueList != null)
            {
                mCurrentShowingValueList.Add(newValue);
            }
        }

        public void ChangeYMax(float offset)
        {
            yMax = offset;
            if (mCurrentShowingValueList != null)
            {
                ShowGraph(mCurrentShowingValueList);
            }
        }

        public void ShowGraph(List<IGraphPointValue> valueList)
        {
            mCurrentShowingValueList = valueList;
            mGraphContainer.Clear();
            const float dashDistanceY = 50f;
            const float dashDistanceX = 50f;
            float graphHeight = mGraphContainer.GraphHeight;
            float graphWidth = mGraphContainer.GraphWidth;
            SetupDash(graphHeight, graphWidth, dashDistanceX, dashDistanceY);
            SetupPointAndConnection(mCurrentShowingValueList, graphHeight, dashDistanceX);
            SetUpLabelY(graphHeight, dashDistanceY);

            IGraphLineDrawer oX = mGraphLineGenerator.Invoke();
            if (oX.SetParent(GraphElement.Ox))
            {
                oX.SetUpVisual(GraphElement.Ox);
                oX.SetPosition(new Vector2(0, 0), new Vector2(graphWidth, 0));
            }

            IGraphLineDrawer oY = mGraphLineGenerator.Invoke();
            if (oY.SetParent(GraphElement.Oy))
            {
                oY.SetUpVisual(GraphElement.Oy);
                oY.SetPosition(new Vector2(0, 0), new Vector2(0, graphHeight));
            }
        }
        #endregion

        private void SetUpLabelY(float graphHeight, float dashYDistance)
        {
            for (int i = 0; i < graphHeight / dashYDistance; i++)
            {
                var labelY = mGraphLabelGenerator.Invoke();
                var normalizedValue = i * dashYDistance / graphHeight;
                float yPos = i * dashYDistance;
                if (labelY.SetParent(GraphElement.LabelY))
                {
                    labelY.SetUpVisual(GraphElement.LabelY);
                    labelY.SetText((yMax * normalizedValue).ToString("F2"));
                    labelY.SetPosition(GraphElement.LabelY, new Vector2(-20, yPos));
                }
            }
        }

        private void SetupPointAndConnection(List<IGraphPointValue> showingList,
            float displayAreaHeight,
            float dashDistanceX)
        {
            IGraphPointDrawer? lastPoint = null;
            for (int i = 0; i < showingList.Count; i++)
            {
                float xPos = i * dashDistanceX;
                float yPos = (showingList[i].XValue / yMax) * displayAreaHeight;

                var labelX = mGraphLabelGenerator.Invoke();
                if (labelX.SetParent(GraphElement.LabelX))
                {
                    labelX.SetUpVisual(GraphElement.LabelX);
                    labelX.SetText(i.ToString());
                    labelX.SetPosition(GraphElement.LabelX, new Vector2(xPos, -10));
                }

                var point = CreatePoint(new Vector2(xPos, yPos));
                if (lastPoint != null)
                {
                    CreatePointConnection(lastPoint.GetPosition(), point.GetPosition());
                }
                lastPoint = point;
            }
        }

        private void SetupDash(
            float graphHeight,
            float graphWidth,
            float dashXDistance,
            float dashYDistance)
        {
            for (int i = 0; i < graphWidth / dashXDistance; i++)
            {
                float xPos = i * dashXDistance;

                var dashX = mGraphLineGenerator.Invoke();
                if (dashX.SetParent(GraphElement.DashX))
                {
                    dashX.SetUpVisual(GraphElement.DashX);
                    dashX.SetPosition(new Vector2(xPos, 0), new Vector2(xPos, graphHeight));
                }
            }


            for (int i = 0; i < graphHeight / dashYDistance; i++)
            {
                float yPos = i * dashYDistance;
                var dashY = mGraphLineGenerator.Invoke();
                if (dashY.SetParent(GraphElement.DashY))
                {
                    dashY.SetUpVisual(GraphElement.DashY);
                    dashY.SetPosition(new Vector2(0, yPos), new Vector2(graphWidth, yPos));
                }
            }
        }


        private IGraphPointDrawer CreatePoint(Vector2 pos)
        {
            IGraphPointDrawer point = mGraphPointGenerator.Invoke();
            if (point.SetParent(GraphElement.Point))
            {
                point.SetUpVisual(GraphElement.Point);
                point.SetPosition(GraphElement.Point, pos);
            }
            return point;
        }

        private void CreatePointConnection(Vector2 posA, Vector2 posB)
        {
            IGraphLineDrawer line = mGraphLineGenerator.Invoke();
            if (line.SetParent(GraphElement.Line))
            {
                line.SetUpVisual(GraphElement.Line);
                line.SetPosition(posA, posB);
            }
        }


    }
}

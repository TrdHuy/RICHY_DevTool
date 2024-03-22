using RICHYEngine.LogCompat;
using RICHYEngine.Views.Holders.GraphHolder;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace RICHY_DevTool.View.Widgets
{
    /// <summary>
    /// Interaction logic for RichyGraph.xaml
    /// </summary>
    public partial class RichyGraph : UserControl
    {
        private GraphHolder graphHolder;

        public RichyGraph()
        {
            InitializeComponent();
            var pointSize = new Vector2(8, 8);
            graphHolder = new VirtualizingGraphHolder(
               graphContainer: new GraphContainerImpl(MainContainerCanvas,
                    mPointContainerCanvas: PointAndLineContainerCanvas,
                    mLabelXCanvas: LabelXContainerCanvas,
                    mLabelYCanvas: LabelYContainerCanvas,
                    mAxisCanvas: AxisContainerCanvas,
                    mGridDashCanvas: GridDashContainerCanvas),
               graphPointGenerator: (targetEle) => new GraphPointImpl(PointAndLineContainerCanvas, new Ellipse(), pointSize),
               graphLineGenerator: (targetEle) =>
               {
                   if (targetEle == GraphElement.Line)
                   {
                       return new GraphLineImpl(PointAndLineContainerCanvas, new Line(), pointSize);
                   }
                   else if (targetEle == GraphElement.DashX || targetEle == GraphElement.DashY)
                   {
                       return new GraphLineImpl(GridDashContainerCanvas, new Line(), pointSize);
                   }
                   else if (targetEle == GraphElement.Ox || targetEle == GraphElement.Oy)
                   {
                       return new GraphLineImpl(AxisContainerCanvas, new Line(), pointSize);
                   }
                   throw new NotImplementedException();
               },
               graphLabelGenerator: (targetEle) =>
               {
                   if (targetEle == GraphElement.LabelX)
                   {
                       return new GraphLabelImpl(LabelXContainerCanvas, new TextBlock());
                   }
                   else if (targetEle == GraphElement.LabelY)
                   {
                       return new GraphLabelImpl(LabelYContainerCanvas, new TextBlock());
                   }
                   throw new NotImplementedException();
               },
               graphPolyLineGenerator: (targetEle) => new GraphPolyLineImpl(PointAndLineContainerCanvas, new Polyline()));
        }

        private void ContainerGrid_MouseEnter(object sender, MouseEventArgs e)
        {
            ContainerGrid.Focus();
        }

        private void ContainerGrid_MouseLeave(object sender, MouseEventArgs e)
        {
            FocusManager.SetFocusedElement(FocusManager.GetFocusScope(ContainerGrid), null);
        }

        private void ContainerGrid_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftCtrl || e.Key == Key.LeftShift || e.Key == Key.LeftAlt || e.Key == Key.System)
            {
                Oy.Visibility = Visibility.Hidden;
                Ox.Visibility = Visibility.Hidden;
                CoorText.Visibility = Visibility.Hidden;
                ReverseCoorText.Visibility = Visibility.Hidden;
                IndexText.Visibility = Visibility.Hidden;
            }
        }

        private void ContainerGrid_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftCtrl || e.Key == Key.LeftShift || e.Key == Key.LeftAlt || e.Key == Key.System)
            {
                Oy.Visibility = Visibility.Visible;
                Ox.Visibility = Visibility.Visible;
                if (e.Key == Key.LeftShift)
                {
                    CoorText.Visibility = Visibility.Visible;
                }
                else if (e.Key == Key.LeftCtrl)
                {
                    ReverseCoorText.Visibility = Visibility.Visible;
                }
                else if (e.Key == Key.LeftAlt || e.Key == Key.System)
                {
                    IndexText.Visibility = Visibility.Visible;
                }
                ShowCoor();
            }
        }

        private void ShowCoor()
        {
            var mousePos = Mouse.GetPosition(MainContainerCanvas);
            Ox.X1 = 0;
            Ox.Y1 = mousePos.Y;
            Ox.X2 = MainContainerCanvas.ActualWidth;
            Ox.Y2 = mousePos.Y;

            Oy.X1 = mousePos.X;
            Oy.Y1 = 0;
            Oy.X2 = mousePos.X;
            Oy.Y2 = MainContainerCanvas.ActualHeight;

            CoorText.Text = $"({mousePos.X},{mousePos.Y})";

            Canvas.SetLeft(CoorText, mousePos.X + 20);
            Canvas.SetTop(CoorText, mousePos.Y - 20);

            ReverseCoorText.Text = $"({mousePos.X},{MainContainerCanvas.ActualHeight - mousePos.Y})";
            Canvas.SetLeft(ReverseCoorText, mousePos.X + 20);
            Canvas.SetTop(ReverseCoorText, mousePos.Y - 20);

            IndexText.Text = $"{graphHolder.GetYValueAtMouse(new Vector2((float)mousePos.X, (float)(MainContainerCanvas.ActualHeight - mousePos.Y)))}";
            Canvas.SetLeft(IndexText, mousePos.X + 20);
            Canvas.SetTop(IndexText, mousePos.Y - 20);
        }

        private void ContainerCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (Ox.Visibility == Visibility.Visible)
            {
                ShowCoor();
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            showGraph();
        }

        private void showGraph()
        {
            var listValue = new List<IGraphPointValue>() {
                CreateValue(0,0),
                CreateValue(5,1) ,
                CreateValue(10,2) ,
                CreateValue(20,3) ,
                CreateValue(25,4) ,
                CreateValue(30,5) ,
                CreateValue(35,6) ,
                CreateValue(40,7) ,
                CreateValue(45,8) ,
                CreateValue(50,9) ,
                CreateValue(55,10) ,
                CreateValue(60,11) ,
                CreateValue(65,12) ,
                CreateValue(70,13) ,
                CreateValue(75,14) ,
                CreateValue(80,15) ,
               // CreateValue(40,16) 
            };
            graphHolder.ShowGraph(listValue);
        }

        IGraphPointValue CreateValue(int yValue, int xValue)
        {
            return new GraphPointValueImpl(yValue, xValue);
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sender == YMaxSlider)
            {
                graphHolder?.ChangeYMax((int)e.NewValue);
            }
            else if (sender == XDistanceSlider)
            {
                //graphHolder?.ChangeZoomX((int)e.NewValue - (int)e.OldValue);
                graphHolder?.ChangeXDistance((int)e.NewValue);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender == LeftBut)
            {
                graphHolder.MoveGraph(-10, 0);
            }
            else if (sender == RightBut)
            {
                graphHolder.MoveGraph(10, 0);
            }
            else if (sender == UpBut)
            {
                graphHolder.MoveGraph(0, 10);
            }
            else if (sender == DownBut)
            {
                graphHolder.MoveGraph(0, -10);
            }
            else if (sender == AddNewValueBut)
            {
                Random r = new Random();
                graphHolder.AddPointValue(CreateValue(r.Next(0, 200), 100));
            }
            else if (sender == Refresh)
            {
                showGraph();
            }
        }

        private void TextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var newDistance = Convert.ToInt32(XDistanceBox.Text);
                graphHolder?.ChangeXDistance((int)newDistance);
            }
        }
    }

    public class GraphPointValueImpl : IGraphPointValue
    {
        private int yValue;
        private int xValue;
        public int YValue => yValue;

        public object XValue => xValue;

        public GraphPointValueImpl(int yValue, int xValue)
        {
            this.yValue = yValue;
            this.xValue = xValue;
        }

    }
    public enum GraphZIndex
    {
        Line = 99,
        Point = 100,
        LineDash = 0,
        OxOy = 1
    }
    public abstract class BaseWpfCanvasElement
    {
        protected abstract float MainContainerCanvasHeight { get; }

        protected Vector2 GetReversePosForPoint(Vector2 originalPos)
        {
            return new Vector2(originalPos.X, MainContainerCanvasHeight - originalPos.Y);
        }

        protected Vector2 GetReversePosForCanvasHolder(Canvas canvasHolder, Vector2 originalPos)
        {
            return new Vector2(originalPos.X, MainContainerCanvasHeight - originalPos.Y - (float)canvasHolder.ActualHeight);
        }
    }
    public abstract class BaseCanvasChild : BaseWpfCanvasElement, ICanvasChild
    {
        protected Canvas ParentsCanvas { get; private set; }
        public abstract UIElement Child { get; }

        public BaseCanvasChild(Canvas container)
        {
            ParentsCanvas = container;
        }

        protected override float MainContainerCanvasHeight
        {
            get
            {
                return (float)ParentsCanvas.ActualHeight;
            }
        }

        public abstract void SetUpVisual(GraphElement targetElement);
    }

    public abstract class SingleElementImpl : BaseCanvasChild, ISingleCanvasElement
    {
        protected Vector2 Position { get; set; }

        public SingleElementImpl(Canvas container) : base(container) { }

        public Vector2 GetPositionOnCanvas()
        {
            return Position;
        }

        public virtual void SetPositionOnCanvas(GraphElement targetElement, Vector2 position)
        {
            Position = position;
            SetReversePos(position, Child);
        }

        protected void SetReversePos(Vector2 position, UIElement element)
        {
            var reversePos = GetReversePosForPoint(position);
            Canvas.SetLeft(element, reversePos.X);
            Canvas.SetTop(element, reversePos.Y);
        }
    }
    public class GraphLineImpl : BaseCanvasChild, IGraphLineDrawer
    {
        private Line mLine;

        public override UIElement Child => mLine;

        public GraphLineImpl(Canvas containerCanvas, Line line, Vector2 pointSize) : base(containerCanvas)
        {
            mLine = line;
        }

        public override void SetUpVisual(GraphElement targetElement)
        {
            if (targetElement == GraphElement.Ox || targetElement == GraphElement.Oy)
            {
                mLine.Stroke = Brushes.WhiteSmoke;
                mLine.StrokeThickness = 2;
            }
            else if (targetElement == GraphElement.DashX || targetElement == GraphElement.DashY)
            {
                mLine.Stroke = Brushes.Gray;
                mLine.StrokeThickness = 1;
                mLine.StrokeDashArray = new DoubleCollection() { 10, 5 };
            }
        }

        public void SetPositionOnCanvas(Vector2 firstPoint, Vector2 secondPoint)
        {
            var reverse1stPos = GetReversePosForPoint(firstPoint);
            var reverse2ndPos = GetReversePosForPoint(secondPoint);
            mLine.X1 = reverse1stPos.X;
            mLine.Y1 = reverse1stPos.Y;
            mLine.X2 = reverse2ndPos.X;
            mLine.Y2 = reverse2ndPos.Y;
        }
    }
    public class GraphPointImpl : SingleElementImpl, IGraphPointDrawer
    {
        private Ellipse mPoint;
        private Vector2 mPointSize;
        private IGraphPointValue? mGraphPointValue;
        public override UIElement Child => mPoint;
        public IGraphPointValue? graphPointValue { get => mGraphPointValue; set => mGraphPointValue = value; }

        public GraphPointImpl(Canvas containerCanvas, Ellipse point, Vector2 pointSize) : base(containerCanvas)
        {
            mPoint = point;
            mPointSize = pointSize;
        }


        public override void SetPositionOnCanvas(GraphElement targetElement, Vector2 position)
        {
            //base.SetPositionOnCanvas(targetElement, position);
            Position = position;
            SetReversePos(new Vector2(position.X - mPointSize.X / 2, position.Y + mPointSize.Y / 2), mPoint);
        }

        public override void SetUpVisual(GraphElement targetElement)
        {
            mPoint.Width = mPointSize.X;
            mPoint.Height = mPointSize.Y;
            mPoint.Fill = Brushes.Wheat;
        }

    }
    public class CanvasHolderImpl : BaseWpfCanvasElement, ICanvasHolder
    {
        private Canvas mChildCanvas;
        private Canvas mParentsCanvas;
        public CanvasHolderImpl(Canvas canvasHolder, Canvas mainContainerCanvas)
        {
            mChildCanvas = canvasHolder;
            mParentsCanvas = mainContainerCanvas;
        }

        protected override float MainContainerCanvasHeight => (float)mParentsCanvas.ActualHeight;

        public bool AddChild(ICanvasChild child)
        {
            var cast = child as BaseCanvasChild;
            if (cast != null)
            {
                if (!mChildCanvas.Children.Contains(cast.Child))
                {
                    mChildCanvas.Children.Add(cast.Child);
                    return true;
                }
                return false;
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public void Clear()
        {
            mChildCanvas.Children.Clear();
        }

        public bool RemoveChild(ICanvasChild child)
        {
            var cast = child as BaseCanvasChild;
            if (cast != null)
            {
                if (mChildCanvas.Children.Contains(cast.Child))
                {
                    mChildCanvas.Children.Remove(cast.Child);
                    return true;
                }
                return false;
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public void SetCanvasPosition(Vector2 position)
        {
            var reversePos = GetReversePosForCanvasHolder(mChildCanvas, position);
            Canvas.SetLeft(mChildCanvas, reversePos.X);
            Canvas.SetTop(mChildCanvas, reversePos.Y);
        }

    }
    public class GraphContainerImpl : IGraphContainer
    {
        private Canvas mContainerCanvas;
        private ICanvasHolder mPointContainerCanvasHolder;
        private ICanvasHolder mLabelXCanvasHolder;
        private ICanvasHolder mLabelYCanvasHolder;
        private ICanvasHolder mAxisCanvasHolder;
        private ICanvasHolder mGridDashCanvasHolder;

        public GraphContainerImpl(Canvas mContainerCanvas,
            Canvas mPointContainerCanvas,
            Canvas mLabelXCanvas,
            Canvas mLabelYCanvas,
            Canvas mAxisCanvas,
            Canvas mGridDashCanvas)
        {
            this.mContainerCanvas = mContainerCanvas;
            mPointContainerCanvasHolder = new CanvasHolderImpl(mPointContainerCanvas, mContainerCanvas);
            mLabelXCanvasHolder = new CanvasHolderImpl(mLabelXCanvas, mContainerCanvas);
            mLabelYCanvasHolder = new CanvasHolderImpl(mLabelYCanvas, mContainerCanvas);
            mAxisCanvasHolder = new CanvasHolderImpl(mAxisCanvas, mContainerCanvas);
            mGridDashCanvasHolder = new CanvasHolderImpl(mGridDashCanvas, mContainerCanvas);
        }

        public float GraphHeight => (float)mContainerCanvas.ActualHeight;

        public float GraphWidth => (float)mContainerCanvas.ActualWidth;

        public ICanvasHolder PointAndLineCanvasHolder => mPointContainerCanvasHolder;

        public ICanvasHolder LabelXCanvasHolder => mLabelXCanvasHolder;

        public ICanvasHolder LabelYCanvasHolder => mLabelYCanvasHolder;

        public ICanvasHolder AxisCanvasHolder => mAxisCanvasHolder;

        public ICanvasHolder GridDashCanvasHolder => mGridDashCanvasHolder;
    }
    public class GraphPolyLineImpl : BaseCanvasChild, IGraphPolyLineDrawer
    {
        public override UIElement Child => mPolyLine;

        public object Drawer => mPolyLine.Points;

        public int TotalPointCount => mPolyLine.Points.Count;

        private Polyline mPolyLine;

        public GraphPolyLineImpl(Canvas container, Polyline polyLine) : base(container)
        {
            mPolyLine = polyLine;
        }
        public override void SetUpVisual(GraphElement targetElement)
        {
            mPolyLine.Stroke = Brushes.Aqua;
            mPolyLine.StrokeThickness = 2;
        }

        public int AddNewPoint(Vector2 point, bool toLast = true)
        {
            var reversePos = GetReversePosForPoint(point);
            if (toLast)
            {
                mPolyLine.Points.Add(new Point(reversePos.X, reversePos.Y));
                return mPolyLine.Points.Count - 1;
            }
            else
            {
                mPolyLine.Points.Insert(0, new Point(reversePos.X, reversePos.Y));
                return 0;

            }
        }

        public void ChangePointPosition(Vector2 oldPos, Vector2 newPos)
        {
            var reverseNewPos = GetReversePosForPoint(newPos);
            var reverseOldPos = GetReversePosForPoint(oldPos);
            var oldPoint = new Point(reverseOldPos.X, reverseOldPos.Y);
            var newPoint = new Point(reverseNewPos.X, reverseNewPos.Y);
            var oldPointIndex = mPolyLine.Points.IndexOf(oldPoint);
            mPolyLine.Points[oldPointIndex] = newPoint;
        }

        public void RemovePoint(int pointIndex)
        {
            mPolyLine.Points.RemoveAt(pointIndex);
        }

        public void RemovePoint(Vector2 point)
        {
            var reversePos = GetReversePosForPoint(point);
            mPolyLine.Points.Remove(new Point(reversePos.X, reversePos.Y));
        }
    }
    public class GraphLabelImpl : SingleElementImpl, IGraphLabelDrawer
    {
        public override UIElement Child => TextBlock;
        public float DesiredHeight => (float)TextBlock.DesiredSize.Height;
        public float DesiredWidth => (float)TextBlock.DesiredSize.Width;
        private TextBlock TextBlock;

        public GraphLabelImpl(Canvas container, TextBlock textBlock) : base(container)
        {
            TextBlock = textBlock;
        }

        public override void SetPositionOnCanvas(GraphElement targetElement, Vector2 position)
        {
            Position = position;
            if (targetElement == GraphElement.LabelY)
            {
                SetPosStartFromLeft(new Vector2(position.X + 5, position.Y + 20));
            }
            else
            {
                SetReversePos(position, TextBlock);
            }
        }

        public void SetText(string text)
        {
            TextBlock.Text = text;
            TextBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        }

        public override void SetUpVisual(GraphElement targetElement)
        {
            TextBlock.Foreground = Brushes.White;
        }

        private void SetPosStartFromLeft(Vector2 pos)
        {
            SetReversePos(new Vector2(pos.X - (float)TextBlock.DesiredSize.Width, pos.Y), TextBlock);
        }
    }

}

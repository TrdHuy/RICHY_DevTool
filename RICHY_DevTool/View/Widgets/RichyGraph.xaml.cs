using RICHYEngine.Views.Holders;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static RICHYEngine.Views.Holders.ICanvasChild;

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
            graphHolder = new VirtualizingGraphHolder(new GraphContainerImpl(ContainerCanvas, PointContainerCanvas),
               (targetEle) => new GraphPointImpl(PointContainerCanvas, new Ellipse(), pointSize),
               (targetEle) =>
               {
                   if (targetEle == GraphElement.Line)
                   {
                       return new GraphLineImpl(PointContainerCanvas, new Line(), pointSize);
                   }
                   return new GraphLineImpl(ContainerCanvas, new Line(), pointSize);
               },
               (targetEle) => new GraphLabelImpl(ContainerCanvas, new TextBlock()));
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
            var mousePos = Mouse.GetPosition(ContainerCanvas);
            Ox.X1 = 0;
            Ox.Y1 = mousePos.Y;
            Ox.X2 = ContainerCanvas.ActualWidth;
            Ox.Y2 = mousePos.Y;

            Oy.X1 = mousePos.X;
            Oy.Y1 = 0;
            Oy.X2 = mousePos.X;
            Oy.Y2 = ContainerCanvas.ActualHeight;

            CoorText.Text = $"({mousePos.X},{mousePos.Y})";

            Canvas.SetLeft(CoorText, mousePos.X + 20);
            Canvas.SetTop(CoorText, mousePos.Y - 20);

            ReverseCoorText.Text = $"({mousePos.X},{ContainerCanvas.ActualHeight - mousePos.Y})";
            Canvas.SetLeft(ReverseCoorText, mousePos.X + 20);
            Canvas.SetTop(ReverseCoorText, mousePos.Y - 20);

            IndexText.Text = $"{graphHolder.GetYValueAtMouse(new Vector2((float)mousePos.X, (float)(ContainerCanvas.ActualHeight - mousePos.Y)))}";
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
            var listValue = new List<IGraphPointValue>() { CreateValue(32),
                CreateValue(42) ,
                CreateValue(53) ,
                CreateValue(12) ,
                CreateValue(64) ,
                CreateValue(63) ,
                CreateValue(87) ,
                CreateValue(130) ,
                CreateValue(1) ,
                CreateValue(3) ,
                CreateValue(64) };
            graphHolder.ShowGraph(listValue);
        }

        IGraphPointValue CreateValue(int xValue)
        {
            return new GraphPointValueImpl(xValue);
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sender == YMaxSlider)
            {
                graphHolder?.ChangeYMax((int)e.NewValue);
            }
            else if (sender == XDistanceSlider)
            {
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
                graphHolder.MoveGraph(0, -10);
            }
            else if (sender == DownBut)
            {
                graphHolder.MoveGraph(0, 10);
            }
            else if (sender == AddNewValueBut)
            {
                Random r = new Random();
                graphHolder.AddPointValue(CreateValue(r.Next(0, 200)));
            }
        }
    }

    public class GraphPointValueImpl : IGraphPointValue
    {
        private int yValue;
        public int YValue => yValue;

        public object XValue => null;

        public GraphPointValueImpl(int xValue)
        {
            this.yValue = xValue;
        }

    }

    public enum GraphZIndex
    {
        Line = 99,
        Point = 100,
        LineDash = 0,
    }

    public abstract class CanvasChildImpl : ICanvasChild
    {
        protected Canvas ContainerCanvas { get; private set; }
        protected abstract UIElement Child { get; }
        protected Vector2 Position { get; set; }

        protected float CanvasHeight
        {
            get
            {
                return (float)ContainerCanvas.ActualHeight;
            }
        }
        public CanvasChildImpl(Canvas container)
        {
            ContainerCanvas = container;
        }
        public Vector2 GetPositionOnCanvas()
        {
            return Position;
        }

        public bool SetIntoParent(GraphElement targetElement)
        {
            if (!ContainerCanvas.Children.Contains(Child))
            {
                ContainerCanvas.Children.Add(Child);
                var zIndex = 0;
                switch (targetElement)
                {
                    case GraphElement.Point:
                        zIndex = (int)GraphZIndex.Point;
                        break;
                    case GraphElement.Line:
                        zIndex = (int)GraphZIndex.Line;
                        break;
                    case GraphElement.DashX:
                    case GraphElement.DashY:
                        zIndex = (int)GraphZIndex.LineDash;
                        break;
                }
                Panel.SetZIndex(Child, zIndex);
            }
            return true;
        }

        public bool RemoveFromParent(GraphElement targetElement)
        {
            if (ContainerCanvas.Children.Contains(Child))
            {
                ContainerCanvas.Children.Remove(Child);
            }
            return false;
        }

        protected void SetReversePos(Vector2 position, UIElement element)
        {
            Canvas.SetLeft(element, position.X);
            Canvas.SetTop(element, CanvasHeight - position.Y);
        }

        public abstract void SetUpVisual(GraphElement targetElement);

    }

    public class GraphLineImpl : CanvasChildImpl, IGraphLineDrawer
    {
        private Line mLine;
        private Vector2 mPointSize;
        protected override Shape Child => mLine;

        public GraphLineImpl(Canvas containerCanvas, Line line, Vector2 pointSize) : base(containerCanvas)
        {
            mLine = line;
            mPointSize = pointSize;
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
            else if (targetElement == GraphElement.Line)
            {
                mLine.Stroke = Brushes.Aqua;
                mLine.StrokeThickness = 2;
            }
        }

        public void SetPositionOnCanvas(Vector2 firstPoint, Vector2 secondPoint)
        {
            mLine.X1 = Convert.ToDouble(firstPoint.X);
            mLine.Y1 = CanvasHeight - Convert.ToDouble(firstPoint.Y);
            mLine.X2 = Convert.ToDouble(secondPoint.X);
            mLine.Y2 = CanvasHeight - Convert.ToDouble(secondPoint.Y);
        }
    }
    public class GraphPointImpl : CanvasChildImpl, IGraphPointDrawer
    {
        private Ellipse mPoint;
        private Vector2 mPointSize;
        protected override Shape Child => mPoint;
        public GraphPointImpl(Canvas containerCanvas, Ellipse point, Vector2 pointSize) : base(containerCanvas)
        {
            mPoint = point;
            mPointSize = pointSize;
        }


        public void SetPositionOnCanvas(GraphElement targetElement, Vector2 position)
        {
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
    public class CanvasHolderImpl : ICanvasHolder
    {
        private Canvas mContainerCanvas;
        public CanvasHolderImpl(Canvas canvasHolder)
        {
            mContainerCanvas = canvasHolder;
        }

        public void Clear()
        {
            mContainerCanvas.Children.Clear();
        }

        public void SetCanvasPosition(Vector2 position)
        {
            Canvas.SetLeft(mContainerCanvas, position.X);
            Canvas.SetTop(mContainerCanvas, position.Y);
        }

    }
    public class GraphContainerImpl : IGraphContainer
    {
        private Canvas mContainerCanvas;
        private ICanvasHolder mPointContainerCanvasHolder;
        public GraphContainerImpl(Canvas containerCanvas, Canvas pointContainerCanvas)
        {
            mContainerCanvas = containerCanvas;
            mPointContainerCanvasHolder = new CanvasHolderImpl(pointContainerCanvas);
        }
        public float GraphHeight => (float)mContainerCanvas.ActualHeight;

        public float GraphWidth => (float)mContainerCanvas.ActualWidth;

        public ICanvasHolder PointAndLineCanvasHolder => mPointContainerCanvasHolder;

        public void Clear()
        {
            mContainerCanvas.Children.Clear();
        }
    }
    public class GraphLabelImpl : CanvasChildImpl, IGraphLabelDrawer
    {
        protected override UIElement Child => TextBlock;
        private TextBlock TextBlock;

        public GraphLabelImpl(Canvas container, TextBlock textBlock) : base(container)
        {
            TextBlock = textBlock;
        }

        public void SetPositionOnCanvas(GraphElement targetElement, Vector2 position)
        {
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

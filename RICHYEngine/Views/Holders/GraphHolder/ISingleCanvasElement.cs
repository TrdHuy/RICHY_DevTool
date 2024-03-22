using System.Numerics;

namespace RICHYEngine.Views.Holders.GraphHolder
{
    public interface ISingleCanvasElement : ICanvasChild
    {
        /// <summary>
        /// Unity Implementation Eg: 
        /// lastPoint.GetComponent<RectTransform>().anchoredPosition;
        /// </summary>
        Vector2 GetPositionOnCanvas();

        void SetPositionOnCanvas(GraphElement targetElement, Vector2 position);
    }
}

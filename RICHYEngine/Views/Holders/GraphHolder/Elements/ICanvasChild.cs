namespace RICHYEngine.Views.Holders.GraphHolder.Elements
{
    public interface ICanvasChild
    {

        /// <summary>
        /// Unity Implementation Eg:
        /// line.GetComponent<Image>().sprite = lineSpr;
        /// line.GetComponent<Image>().type = Image.Type.Sliced;
        /// </summary>
        void SetUpVisual(GraphElement targetElement);

    }
}

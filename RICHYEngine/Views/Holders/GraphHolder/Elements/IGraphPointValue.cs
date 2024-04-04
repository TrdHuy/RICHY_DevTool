using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RICHYEngine.Views.Holders.GraphHolder.Elements
{
    public interface IGraphPointValue
    {
        int YValue { get; }
        object? XValue { get; }
    }
}

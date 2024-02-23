using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RICHY_DevTool.Utils
{

    //internal class ReadOnlyCollection2<T> : IList, IReadOnlyList<T>
    //{
    //    private readonly IList2<T> list; // Do not rename (binary serialization)

    //    public ReadOnlyCollection2(IList2<T> list)
    //    {

    //        this.list = list;
    //    }

    //    public int Count => list.Count;

    //    public T this[int index] => list[index];

    //    public bool Contains(T value)
    //    {
    //        return list.Contains(value);
    //    }


    //    public IEnumerator<T> GetEnumerator()
    //    {
    //        return list.GetEnumerator();
    //    }


    //    protected IList2<T> Items => list;


    //    IEnumerator IEnumerable.GetEnumerator()
    //    {
    //        return ((IEnumerable)list).GetEnumerator();
    //    }

    //    bool ICollection.IsSynchronized => false;

    //    object ICollection.SyncRoot => list is ICollection coll ? coll.SyncRoot : this;

    //    bool IList.IsFixedSize => true;

    //    bool IList.IsReadOnly => true;

    //    object? IList.this[int index]
    //    {
    //        get
    //        {
    //            return list[index];
    //        }
    //        set
    //        {

    //        }
    //    }

    //    int IList.Add(object? value)
    //    {
    //        return -1;
    //    }

    //    void IList.Clear()
    //    {
    //    }

    //    private static bool IsCompatibleObject(object? value)
    //    {
    //        return (value is T) || (value == null && default(T) == null);
    //    }

    //    bool Contains(object? value)
    //    {
    //        if (IsCompatibleObject(value))
    //        {
    //            return Contains((T)value!);
    //        }
    //        return false;
    //    }

    //    //TODO
    //    int IndexOf(object? value)
    //    {
    //        if (IsCompatibleObject(value))
    //        {
    //            return IndexOf((T)value!);
    //        }
    //        return -1;
    //    }

    //    void IList.Insert(int index, object? value)
    //    {
    //    }

    //    void IList.Remove(object? value)
    //    {
    //    }

    //    void IList.RemoveAt(int index)
    //    {
    //    }
    //}
    internal interface IList2<out T> : ICollection2<T>, IEnumerable<T>, IEnumerable
    {
        T this[int index] { get; }
    }

    public interface ICollection2<out T> : IEnumerable<T>, IEnumerable
    {
        int Count { get; }
        bool IsReadOnly { get; }
    }
}

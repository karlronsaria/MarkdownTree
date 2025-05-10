using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarkdownTree;

public delegate T OrDefault<T>();

public class NonStandardEnumerator<T>(IList<T> list, OrDefault<T> orDefault) : MarkdownTree.Parse.IEnumerator<T>, ICloneable
{
    private readonly IList<T> _list = list;
    private int _index = -1;

    public T Current => _list[_index];

    // (karlr 2025-04-27): I really think I shouldn't have to do this.
#pragma warning disable CS8603 // Possible null reference return.
    object IEnumerator.Current => _list[_index] ?? orDefault();
#pragma warning restore CS8603 // Possible null reference return.

    public int Index() => _index;

    public object Clone() => new NonStandardEnumerator<T>(_list, orDefault) { _index = _index };

    public void Dispose() { }

    public bool MoveNext()
    {
        _index++;
        return _index < _list.Count;
    }

    public void Reset()
    {
        _index = -1;
    }
}
public class Enumerator<T>(IList<T> list) : MarkdownTree.Parse.IEnumerator<T>, ICloneable
    where T : new()
{
    private readonly IList<T> _list = list;
    private int _index = -1;

    public T Current => _list[_index];

    object IEnumerator.Current => _list[_index] ?? new T();

    public int Index() => _index;

    public object Clone() => new Enumerator<T>(_list) { _index = _index };

    public void Dispose() { }

    public bool MoveNext()
    {
        _index++;
        return _index < _list.Count;
    }

    public void Reset()
    {
        _index = -1;
    }
}

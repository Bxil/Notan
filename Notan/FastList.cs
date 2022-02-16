using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Notan;

internal struct FastList<T>
{
    private T[] array = Array.Empty<T>();
    public int Count { get; private set; } = 0;

    public FastList() { }

    public void Add(T t)
    {
        EnsureCapacity(Count + 1);
        array[Count] = t;
        Count++;
    }

    public void EnsureCapacity(int capacity)
    {
        int currentcapacity = array.Length;

        if (currentcapacity >= capacity)
        {
            return;
        }
        
        if (currentcapacity == 0)
        {
            currentcapacity = 8;
        }
        while (capacity > currentcapacity)
        {
            currentcapacity *= 2;
        }
        var newarray = ArrayPool<T>.Shared.Rent(currentcapacity);
        if (array.Length > 0)
        {
            array.AsSpan(0, Count).CopyTo(newarray.AsSpan());
            ArrayPool<T>.Shared.Return(array);
        }
        array = newarray;
    }

    public void EnsureSize(int size)
    {
        EnsureCapacity(size);
        if (size > Count)
        {
            Count = size;
        }
    }

    public void Clear()
    {
        for (var i = 0; i < Count; i++)
        {
            this[i] = default!;
        }
        Count = 0;
    }

    public void RemoveAt(int index)
    {
        Debug.Assert(index >= 0 && index < Count);
        this[index] = this[Count - 1];
        this[Count - 1] = default!;
        Count--;
        if (Count == 0)
        {
            ArrayPool<T>.Shared.Return(array);
            array = Array.Empty<T>();
        }
    }

    public bool Remove(T value)
    {
        var index = Array.IndexOf(array ?? Array.Empty<T>(), value);
        if (index != -1)
        {
            RemoveAt(index);
            return true;
        }
        else
        {
            return false;
        }
    }

    public int IndexOf(T item) => Array.IndexOf(array ?? Array.Empty<T>(), item, 0, Count);

    public ref T this[int index]
    {
        get
        {
            Debug.Assert(index >= 0 && index < Count);
            return ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), index);
        }
    }

    public Span<T> AsSpan() => (array ?? Array.Empty<T>()).AsSpan(0, Count);
}

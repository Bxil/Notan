﻿using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Notan
{
    internal struct FastList<T>
    {
        private T[] array;
        public int Count { get; private set; }

        public void Add(T t)
        {
            if (array == null)
            {
                array = new T[8];
            }
            if (array.Length == Count)
            {
                Array.Resize(ref array, Count * 2);
            }
            array[Count] = t;
            Count++;
        }

        public void EnsureCapacity(int capacity)
        {
            if (capacity > (array?.Length ?? 0))
            {
                Array.Resize(ref array, capacity);
            }
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
            for (int i = 0; i < Count; i++)
            {
                this[i] = default!;
            }
            Count = 0;
        }

        public void RemoveAt(int index)
        {
            this[index] = this[Count - 1];
            this[Count - 1] = default!;
            Count--;
        }

        public bool Remove(T value)
        {
            var index = Array.IndexOf(array, value);
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

        public int IndexOf(T item) => Array.IndexOf(array, item, 0, Count);

        public ref T this[int index] => ref array[index];

        public Span<T> AsSpan() => array.AsSpan(0, Count);
    }
}
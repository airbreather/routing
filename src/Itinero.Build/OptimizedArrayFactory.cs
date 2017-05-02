﻿using System;
using Reminiscence.Arrays;

namespace Itinero.Build
{
    internal sealed class OptimizedArrayFactory : IArrayFactory
    {
        public ArrayBase<T> CreateMemoryBackedArray<T>(long size) =>
            (BitConverter.IsLittleEndian &&
             (typeof(T) == typeof(byte) ||
              typeof(T) == typeof(sbyte) ||
              typeof(T) == typeof(short) ||
              typeof(T) == typeof(ushort) ||
              typeof(T) == typeof(int) ||
              typeof(T) == typeof(uint) ||
              typeof(T) == typeof(long) ||
              typeof(T) == typeof(ulong) ||
              typeof(T) == typeof(float) ||
              typeof(T) == typeof(double)))
                ? new UnmanagedMemoryArray<T>(size)
                : (ArrayBase<T>)new MemoryArray<T>(size);
    }
}

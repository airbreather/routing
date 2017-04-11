using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Reminiscence.Arrays;

namespace Itinero.Build
{
    public sealed unsafe class UnmanagedMemoryArray<T> : ArrayBase<T>
    {
        private byte* headPtr;

        private long length;

        public UnmanagedMemoryArray(long length)
        {
            if (!(typeof(T) == typeof(byte) ||
                  typeof(T) == typeof(sbyte) ||
                  typeof(T) == typeof(short) ||
                  typeof(T) == typeof(ushort) ||
                  typeof(T) == typeof(int) ||
                  typeof(T) == typeof(uint) ||
                  typeof(T) == typeof(long) ||
                  typeof(T) == typeof(ulong) ||
                  typeof(T) == typeof(float) ||
                  typeof(T) == typeof(double)))
            {
                throw new NotSupportedException(typeof(T).Name);
            }

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length), length, "Must be nonnegative.");
            }

            if (length == 0)
            {
                return;
            }

            this.length = length;
            long byteCount = length * Unsafe.SizeOf<T>();
            this.headPtr = (byte*)Marshal.AllocHGlobal(new IntPtr(byteCount)).ToPointer();
            GC.AddMemoryPressure(byteCount);
        }

        ~UnmanagedMemoryArray() => this.DisposeCore();

        public override T this[long idx]
        {
            get
            {
                // NOTE: these aren't actually runtime checks once the JIT gets through with them.
                if (typeof(T) == typeof(byte))
                {
                    return Unsafe.Read<T>(this.headPtr + idx);
                }

                if (typeof(T) == typeof(sbyte))
                {
                    return Unsafe.Read<T>((sbyte*)this.headPtr + idx);
                }

                if (typeof(T) == typeof(short))
                {
                    return Unsafe.Read<T>((short*)this.headPtr + idx);
                }

                if (typeof(T) == typeof(ushort))
                {
                    return Unsafe.Read<T>((ushort*)this.headPtr + idx);
                }

                if (typeof(T) == typeof(int))
                {
                    return Unsafe.Read<T>((int*)this.headPtr + idx);
                }

                if (typeof(T) == typeof(uint))
                {
                    return Unsafe.Read<T>((uint*)this.headPtr + idx);
                }

                if (typeof(T) == typeof(long))
                {
                    return Unsafe.Read<T>((long*)this.headPtr + idx);
                }

                if (typeof(T) == typeof(ulong))
                {
                    return Unsafe.Read<T>((ulong*)this.headPtr + idx);
                }

                if (typeof(T) == typeof(float))
                {
                    return Unsafe.Read<T>((float*)this.headPtr + idx);
                }

                if (typeof(T) == typeof(double))
                {
                    return Unsafe.Read<T>((double*)this.headPtr + idx);
                }

                throw new NotSupportedException(typeof(T).Name);
            }

            set
            {
                // NOTE: these aren't actually runtime checks once the JIT gets through with them.
                if (typeof(T) == typeof(byte))
                {
                    Unsafe.Write(this.headPtr + idx, value);
                    return;
                }

                if (typeof(T) == typeof(sbyte))
                {
                    Unsafe.Write((sbyte*)this.headPtr + idx, value);
                    return;
                }

                if (typeof(T) == typeof(short))
                {
                    Unsafe.Write((short*)this.headPtr + idx, value);
                    return;
                }

                if (typeof(T) == typeof(ushort))
                {
                    Unsafe.Write((ushort*)this.headPtr + idx, value);
                    return;
                }

                if (typeof(T) == typeof(int))
                {
                    Unsafe.Write((int*)this.headPtr + idx, value);
                    return;
                }

                if (typeof(T) == typeof(uint))
                {
                    Unsafe.Write((uint*)this.headPtr + idx, value);
                    return;
                }

                if (typeof(T) == typeof(long))
                {
                    Unsafe.Write((long*)this.headPtr + idx, value);
                    return;
                }

                if (typeof(T) == typeof(ulong))
                {
                    Unsafe.Write((ulong*)this.headPtr + idx, value);
                    return;
                }

                if (typeof(T) == typeof(float))
                {
                    Unsafe.Write((float*)this.headPtr + idx, value);
                    return;
                }

                if (typeof(T) == typeof(double))
                {
                    Unsafe.Write((double*)this.headPtr + idx, value);
                    return;
                }

                throw new NotSupportedException(typeof(T).Name);
            }
        }

        public override long Length => this.length;

        public override bool CanResize => true;

        public override void Dispose()
        {
            this.DisposeCore();
            GC.SuppressFinalize(this);
        }

        public override void Resize(long size)
        {
            if (this.length == size)
            {
                // already the proper size.
                return;
            }

            if (size == 0)
            {
                this.DisposeCore();
                return;
            }

            long oldByteCount = this.length * Unsafe.SizeOf<T>();
            GC.RemoveMemoryPressure(oldByteCount);

            long byteCount = size * Unsafe.SizeOf<T>();
            this.headPtr = (byte*)Marshal.ReAllocHGlobal(new IntPtr(this.headPtr), new IntPtr(byteCount)).ToPointer();
            this.length = size;
            GC.AddMemoryPressure(byteCount);
        }

        public override void CopyFrom(Stream stream)
        {
            if (this.length == 0)
            {
                return;
            }

            byte* cur = this.headPtr;
            byte* end = cur + (this.length * Unsafe.SizeOf<T>());
            byte[] buf = new byte[81920];
            fixed (byte* p = buf)
            {
                while (cur < end)
                {
                    int cnt = stream.Read(buf, 0, unchecked((int)Math.Min(end - cur, buf.Length)));
                    if (cnt == 0)
                    {
                        throw new EndOfStreamException();
                    }

                    Buffer.MemoryCopy(p, cur, end - cur, cnt);
                    cur += cnt;
                }
            }
        }

        public override long CopyTo(Stream stream)
        {
            if (this.length == 0)
            {
                return 0;
            }

            long total = this.length * Unsafe.SizeOf<T>();
            byte* cur = this.headPtr;
            byte* end = cur + total;
            byte[] buf = new byte[81920];
            fixed (byte* p = buf)
            {
                while (cur < end)
                {
                    int cnt = unchecked((int)Math.Min(end - cur, buf.Length));
                    Buffer.MemoryCopy(cur, p, buf.Length, cnt);
                    stream.Write(buf, 0, cnt);
                    cur += cnt;
                }
            }

            return total;
        }

        private void DisposeCore()
        {
            if (this.headPtr == null)
            {
                return;
            }

            Marshal.FreeHGlobal(new IntPtr(this.headPtr));
            this.headPtr = null;
            GC.RemoveMemoryPressure(this.length * Unsafe.SizeOf<T>());
            this.length = 0;
        }
    }
}

// 4.0.3.0 lets us write far less code to produce something slightly faster.
// It's currently in pre-release at time of writing, so you'd have to add the
// core team's MyGet site as a NuGet package source.
////#define NEWER_UNSAFE
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

using Reminiscence.Arrays;

using static System.Runtime.CompilerServices.Unsafe;

namespace Itinero.Build
{
    public sealed unsafe class UnmanagedMemoryArray<T> : ArrayBase<T>
    {
        private byte* headPtr;

        private long length;

        public UnmanagedMemoryArray(long size)
        {
            // TODO: support compatible complex types
            if (!(BitConverter.IsLittleEndian &&
                  (typeof(T) == typeof(byte) ||
                   typeof(T) == typeof(sbyte) ||
                   typeof(T) == typeof(short) ||
                   typeof(T) == typeof(ushort) ||
                   typeof(T) == typeof(int) ||
                   typeof(T) == typeof(uint) ||
                   typeof(T) == typeof(long) ||
                   typeof(T) == typeof(ulong) ||
                   typeof(T) == typeof(float) ||
                   typeof(T) == typeof(double))))
            {
                throw new NotSupportedException(typeof(T).Name);
            }

            if (size < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(size), size, "Must be nonnegative.");
            }

            this.Resize(size);
        }

        ~UnmanagedMemoryArray() => this.DisposeCore();

        public override T this[long idx]
        {
#if NEWER_UNSAFE
            get => Add(ref AsRef<T>(this.headPtr), new IntPtr(idx));
            set => Add(ref AsRef<T>(this.headPtr), new IntPtr(idx)) = value;
#else
            // at the time of writing, that's in pre-release, so:
            get
            {
                // NOTE: these aren't actually runtime checks once the JIT gets through with them.
                if (typeof(T) == typeof(byte))
                {
                    return Read<T>(this.headPtr + idx);
                }

                if (typeof(T) == typeof(sbyte))
                {
                    return Read<T>((sbyte*)this.headPtr + idx);
                }

                if (typeof(T) == typeof(short))
                {
                    return Read<T>((short*)this.headPtr + idx);
                }

                if (typeof(T) == typeof(ushort))
                {
                    return Read<T>((ushort*)this.headPtr + idx);
                }

                if (typeof(T) == typeof(int))
                {
                    return Read<T>((int*)this.headPtr + idx);
                }

                if (typeof(T) == typeof(uint))
                {
                    return Read<T>((uint*)this.headPtr + idx);
                }

                if (typeof(T) == typeof(long))
                {
                    return Read<T>((long*)this.headPtr + idx);
                }

                if (typeof(T) == typeof(ulong))
                {
                    return Read<T>((ulong*)this.headPtr + idx);
                }

                if (typeof(T) == typeof(float))
                {
                    return Read<T>((float*)this.headPtr + idx);
                }

                if (typeof(T) == typeof(double))
                {
                    return Read<T>((double*)this.headPtr + idx);
                }

                throw new NotSupportedException(typeof(T).Name);
            }

            set
            {
                // NOTE: these aren't actually runtime checks once the JIT gets through with them.
                if (typeof(T) == typeof(byte))
                {
                    Write(this.headPtr + idx, value);
                    return;
                }

                if (typeof(T) == typeof(sbyte))
                {
                    Write((sbyte*)this.headPtr + idx, value);
                    return;
                }

                if (typeof(T) == typeof(short))
                {
                    Write((short*)this.headPtr + idx, value);
                    return;
                }

                if (typeof(T) == typeof(ushort))
                {
                    Write((ushort*)this.headPtr + idx, value);
                    return;
                }

                if (typeof(T) == typeof(int))
                {
                    Write((int*)this.headPtr + idx, value);
                    return;
                }

                if (typeof(T) == typeof(uint))
                {
                    Write((uint*)this.headPtr + idx, value);
                    return;
                }

                if (typeof(T) == typeof(long))
                {
                    Write((long*)this.headPtr + idx, value);
                    return;
                }

                if (typeof(T) == typeof(ulong))
                {
                    Write((ulong*)this.headPtr + idx, value);
                    return;
                }

                if (typeof(T) == typeof(float))
                {
                    Write((float*)this.headPtr + idx, value);
                    return;
                }

                if (typeof(T) == typeof(double))
                {
                    Write((double*)this.headPtr + idx, value);
                    return;
                }

                throw new NotSupportedException(typeof(T).Name);
            }
#endif
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

            long oldByteCount = this.length * SizeOf<T>();
            long byteCount = size * SizeOf<T>();
            if (this.headPtr == null)
            {
                Debug.Assert(this.length == 0, "headPtr should be null iff our length is 0.");
                this.headPtr = (byte*)Marshal.AllocHGlobal(new IntPtr(byteCount)).ToPointer();
            }
            else
            {
                GC.RemoveMemoryPressure(oldByteCount);
                this.headPtr = (byte*)Marshal.ReAllocHGlobal(new IntPtr(this.headPtr), new IntPtr(byteCount)).ToPointer();
            }

            uint toInit;
            for (long initted = oldByteCount; initted < byteCount; initted += toInit)
            {
                toInit = unchecked((uint)Math.Min(UInt32.MaxValue, (byteCount - initted)));
                InitBlock(this.headPtr + initted, 0, toInit);
            }

            this.length = size;
            GC.AddMemoryPressure(byteCount);
        }

        public override void CopyFrom(Stream stream)
        {
            if (this.headPtr == null)
            {
                Debug.Assert(this.length == 0, "headPtr should be null iff our length is 0.");
                return;
            }

            byte* cur = this.headPtr;
            byte* end = cur + (this.length * SizeOf<T>());
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
            if (this.headPtr == null)
            {
                Debug.Assert(this.length == 0, "headPtr should be null iff our length is 0.");
                return 0;
            }

            long total = this.length * SizeOf<T>();
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
                Debug.Assert(this.length == 0, "headPtr should be null iff our length is 0.");
                return;
            }

            Marshal.FreeHGlobal(new IntPtr(this.headPtr));
            this.headPtr = null;
            GC.RemoveMemoryPressure(this.length * SizeOf<T>());
            this.length = 0;
        }
    }
}

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

            if (size < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(size), size, "Must be nonnegative.");
            }

            this.Resize(size);
        }

        ~UnmanagedMemoryArray() => this.DisposeCore();

        public override T this[long idx]
        {
            get => Add(ref AsRef<T>(this.headPtr), new IntPtr(idx));
            set => Add(ref AsRef<T>(this.headPtr), new IntPtr(idx)) = value;
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

            long byteCount = size * SizeOf<T>();
            if (this.headPtr == null)
            {
                Debug.Assert(this.length == 0, "headPtr should be null iff our length is 0.");
                this.headPtr = (byte*)Marshal.AllocHGlobal(new IntPtr(byteCount)).ToPointer();
            }
            else
            {
                long oldByteCount = this.length * SizeOf<T>();
                GC.RemoveMemoryPressure(oldByteCount);
                this.headPtr = (byte*)Marshal.ReAllocHGlobal(new IntPtr(this.headPtr), new IntPtr(byteCount)).ToPointer();
                if (oldByteCount < byteCount)
                {
                    InitBlockUnaligned(this.headPtr + oldByteCount, 0, unchecked((uint)(byteCount - oldByteCount)));
                }
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

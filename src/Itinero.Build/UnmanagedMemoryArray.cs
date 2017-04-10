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

        private int shBits;

        private long length;

        public UnmanagedMemoryArray(long length)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length), length, "Must be nonnegative.");
            }

            if (length == 0)
            {
                return;
            }

            // TODO: also test that T is blittable.
            if ((Unsafe.SizeOf<T>() & (Unsafe.SizeOf<T>() - 1)) != 0)
            {
                throw new NotSupportedException("Only structs with power-of-two size are supported.");
            }

            this.shBits = (int)Math.Log(Unsafe.SizeOf<T>(), 2);
            this.length = length;

            long byteCount = length << this.shBits;
            this.headPtr = (byte*)Marshal.AllocHGlobal(new IntPtr(byteCount)).ToPointer();
            GC.AddMemoryPressure(byteCount);
        }

        ~UnmanagedMemoryArray() => this.DisposeCore();

        public override T this[long idx]
        {
            get => Unsafe.Read<T>(this.headPtr + (idx << this.shBits));
            set => Unsafe.Write(this.headPtr + (idx << this.shBits), value);
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

            long oldByteCount = this.length << this.shBits;
            GC.RemoveMemoryPressure(oldByteCount);

            long byteCount = size << this.shBits;
            this.headPtr = (byte*)Marshal.ReAllocHGlobal(new IntPtr(this.headPtr), new IntPtr(byteCount)).ToPointer();
            this.length = size;
            GC.AddMemoryPressure(byteCount);
        }

        public override void CopyFrom(Stream stream)
        {
            if (this.Length == 0)
            {
                return;
            }

            byte[] buf = new byte[Unsafe.SizeOf<T>()];
            ref T cur = ref Unsafe.As<byte, T>(ref buf[0]);
            for (long i = 0; i < this.length; ++i)
            {
                int off = 0;
                int rem = buf.Length;
                do
                {
                    int cnt = stream.Read(buf, off, rem);
                    if (cnt == 0)
                    {
                        throw new EndOfStreamException();
                    }

                    rem -= cnt;
                    off += cnt;
                } while (rem != 0);

                this[i] = cur;
            }
        }

        public override long CopyTo(Stream stream)
        {
            if (this.length == 0)
            {
                return 0;
            }

            byte[] buf = new byte[Unsafe.SizeOf<T>()];
            ref T cur = ref Unsafe.As<byte, T>(ref buf[0]);
            for (long i = 0; i < this.length; ++i)
            {
                cur = this[i];
                stream.Write(buf, 0, buf.Length);
            }

            return this.length << this.shBits;
        }

        private void DisposeCore()
        {
            if (this.headPtr == null)
            {
                return;
            }

            Marshal.FreeHGlobal(new IntPtr(this.headPtr));
            this.headPtr = null;
            GC.RemoveMemoryPressure(this.length << this.shBits);
            this.length = 0;
        }
    }
}

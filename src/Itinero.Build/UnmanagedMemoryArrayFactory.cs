using Reminiscence.Arrays;

namespace Itinero.Build
{
    internal sealed class UnmanagedMemoryArrayFactory : IMemoryArrayFactory
    {
        public ArrayBase<T> CreateMemoryBackedArray<T>(long size) => new UnmanagedMemoryArray<T>(size);
    }
}

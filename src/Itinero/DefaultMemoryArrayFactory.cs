using Reminiscence.Arrays;

namespace Itinero
{
    public sealed class DefaultMemoryArrayFactory : IMemoryArrayFactory
    {
        public ArrayBase<T> CreateMemoryBackedArray<T>(long size)
        {
            return new MemoryArray<T>(size);
        }
    }
}

using Reminiscence.Arrays;

namespace Itinero
{
    public interface IMemoryArrayFactory
    {
        ArrayBase<T> CreateMemoryBackedArray<T>(long size);
    }
}

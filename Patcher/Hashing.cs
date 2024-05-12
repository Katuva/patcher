using System.Buffers;
using System.IO;
using System.IO.Hashing;

namespace Patcher;

public static class Hashing
{
    public static byte[] FileHash(string filename)
    {
        const int bufferSize = 4096;
        
        var hashAlgorithm = new XxHash3();
        
        var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        
        using var stream = File.OpenRead(filename);
        try
        {
            int bytesRead;
            while ((bytesRead = stream.Read(buffer)) > 0)
            {
                hashAlgorithm.Append(buffer.AsSpan(0, bytesRead));
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        return hashAlgorithm.GetHashAndReset();
    }
    
    public static byte[] ChunkHash(byte[] data)
    {
        return XxHash3.Hash(data);
    }
}
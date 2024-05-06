using System.Buffers;
using System.IO;
using System.IO.Hashing;

namespace Patcher;

public static class Hashing
{
    public static byte[] GetFileHash(string filename)
    {
        var hashAlgorithm = new XxHash3();
        
        using (Stream entryStream = File.OpenRead(filename))
        {
            var buffer = ArrayPool<byte>.Shared.Rent(4096);
            int bytesRead;
            
            while ((bytesRead = entryStream.Read(buffer)) > 0)
            {
                hashAlgorithm.Append(buffer.AsSpan(0, bytesRead));
            }
            
            ArrayPool<byte>.Shared.Return(buffer);
        }
        
        return hashAlgorithm.GetHashAndReset();
    }
    
    public static byte[] GetChunkHash(byte[] data)
    {
        return XxHash3.Hash(data);
    }
}
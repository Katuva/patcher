using System.Diagnostics;
using System.IO;
using MessagePack;
using ZstdSharp;

namespace Patcher;

public class FileDiff(string? oldFile, string? newFile, int chunkSize = 4096)
{
    private string OldFile { get; } = oldFile ?? throw new ArgumentNullException(nameof(oldFile));
    private string NewFile { get; } = newFile ?? throw new ArgumentNullException(nameof(newFile));
    private int ChunkSize { get; } = chunkSize;

    public void CreateDiff(string diffFile)
    {
        var stopwatch = new Stopwatch();

        stopwatch.Start();
        
        var oldHash = Hashing.GetFileHash(OldFile);
        var newHash = Hashing.GetFileHash(NewFile);
        
        if (oldHash.SequenceEqual(newHash))
        {
            Console.WriteLine(@"Files are identical, no need to create a diff");
            return;
        }

        using var oldFileStream = new FileStream(OldFile ?? throw new InvalidOperationException(), FileMode.Open);
        using var newFileStream = new FileStream(NewFile ?? throw new InvalidOperationException(), FileMode.Open);

        var bufferOld = new byte[ChunkSize];
        var bufferNew = new byte[ChunkSize];
        
        using (var diffStreamTmp = new FileStream(Path.ChangeExtension(diffFile, "tmp"), FileMode.Create))
        {
            while (true)
            {
                Array.Clear(bufferNew, 0, ChunkSize);
                
                var bytesReadOld = oldFileStream.Read(bufferOld, 0, ChunkSize);
                var bytesReadNew = newFileStream.Read(bufferNew, 0, ChunkSize);
                
                if (bytesReadOld == 0 && bytesReadNew == 0) break;

                if (bufferOld.SequenceEqual(bufferNew)) continue;
                
                Console.WriteLine($@"Difference found at offset {oldFileStream.Position - bytesReadOld}");
                
                if (bytesReadNew < ChunkSize)
                {
                    Array.Resize(ref bufferNew, bytesReadNew);
                }
                
                Console.WriteLine("New buffer size: " + bufferNew.Length);
                
                SerializeDifferencesToStream(diffStreamTmp, oldFileStream.Position - bytesReadOld, bufferNew);
                
                if (bufferNew.Length < ChunkSize)
                {
                    Array.Resize(ref bufferNew, ChunkSize);
                }
            }
            
            var diff = new DiffFile()
            {
                OldFile = oldHash,
                NewFile = newHash,
                Offset = 0
            };

            using var memoryStream = new MemoryStream();
            MessagePackSerializer.Serialize(memoryStream, diff);
            
            diff.Offset = memoryStream.Length;
            
            Console.WriteLine($@"Writing offset {diff.Offset}");

            using (var diffStream = new FileStream(diffFile, FileMode.Create))
            {
                MessagePackSerializer.Serialize(diffStream, diff);

                Console.WriteLine($@"Length of diff file: {diffStream.Length}");

                diffStream.Seek(0, SeekOrigin.End);
                diffStreamTmp.Seek(0, SeekOrigin.Begin);
                diffStreamTmp.CopyTo(diffStream);
            }
        }
        
        File.Delete(Path.ChangeExtension(diffFile, "tmp"));

        using (var input = File.OpenRead(diffFile))
        {
            using var output = File.OpenWrite(Path.ChangeExtension(diffFile, "binz"));
            using var compressionStream = new CompressionStream(output, Compressor.MaxCompressionLevel);
            input.CopyTo(compressionStream);
        }
        
        File.Delete(diffFile);

        stopwatch.Stop();

        Console.WriteLine($@"Elapsed time: {stopwatch.Elapsed}");
    }

    private static void SerializeDifferencesToStream(Stream stream, long offset, byte[] data)
    {
        var difference = new Difference()
        {
            Offset = offset,
            Data = data
        };
        
        Console.WriteLine($@"Serializing difference at offset {offset}");
        
        MessagePackSerializer.Serialize(stream, difference);
    }
}
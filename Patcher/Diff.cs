using System.IO;
using MessagePack;
using ZstdSharp;

namespace Patcher;

public class Diff
{
    public string? OutputFolder { get; init; }
    public int ChunkSize { get; init; } = 4096;
    public bool CompressPatch { get; init; } = true;

    private Header? _header;
    private List<string> _cleanup = [];

    public async Task<HeaderItem?> DiffFile(string? oldFile, string? newFile, string? root = null)
    {
        _ = OutputFolder ?? throw new ArgumentNullException(nameof(OutputFolder));
        
        string path;
        byte[] oldHash;
        byte[] newHash;
        FileInfo fileInfo;

        if (oldFile == null && newFile != null)
        {
            path = root != null ? Path.GetRelativePath(root, newFile) : Path.GetFileName(newFile);
            
            newHash = Hashing.FileHash(newFile);
            
            fileInfo = new FileInfo(newFile);
            
            return new HeaderItem()
            {
                Path = path,
                OldHash = [],
                NewHash = newHash,
                Size = fileInfo.Length,
                Command = "create"
            };
        } 
        
        if (oldFile != null && newFile == null)
        {
            path = root != null ? Path.GetRelativePath(root, oldFile) : Path.GetFileName(oldFile);
            
            oldHash = Hashing.FileHash(oldFile);
            
            fileInfo = new FileInfo(oldFile);
            
            return new HeaderItem()
            {
                Path = path,
                OldHash = oldHash,
                NewHash = [],
                Size = fileInfo.Length,
                Command = "delete"
            };
        }
        
        if (oldFile == null && newFile == null)
        {
            return null;
        }

        oldHash = Hashing.FileHash(oldFile!);
        newHash = Hashing.FileHash(newFile!);

        if (oldHash.SequenceEqual(newHash))
        {
            Console.WriteLine(@"Files are identical");
            return null;
        }
        
        await using var oldFileStream = new FileStream(oldFile!, FileMode.Open);
        await using var newFileStream = new FileStream(newFile!, FileMode.Open);

        var oldBuffer = new byte[ChunkSize];
        var newBuffer = new byte[ChunkSize];

        var outputFile = Path.ChangeExtension(Path.Combine(OutputFolder, Path.GetFileName(oldFile!)), "tmp");

        await using (var diffStream = new FileStream(outputFile, FileMode.Create))
        {
            while (true)
            {
                Array.Clear(newBuffer, 0, ChunkSize);

                var bytesReadOld = oldFileStream.Read(oldBuffer, 0, ChunkSize);
                var bytesReadNew = newFileStream.Read(newBuffer, 0, ChunkSize);

                if (bytesReadOld == 0 && bytesReadNew == 0) break;

                if (oldBuffer.SequenceEqual(newBuffer)) continue;

                if (bytesReadNew < ChunkSize) Array.Resize(ref newBuffer, bytesReadNew);

                var chunkDiff = new ChunkDiff()
                {
                    Offset = oldFileStream.Position - bytesReadOld,
                    Hash = Hashing.ChunkHash(newBuffer),
                    Data = newBuffer
                };

                await MessagePackSerializer.SerializeAsync(diffStream, chunkDiff);

                if (newBuffer.Length < ChunkSize) Array.Resize(ref newBuffer, ChunkSize);
            }
        }
        
        fileInfo = new FileInfo(outputFile);
        
        _cleanup.Add(outputFile);
        
        path = root != null ? Path.GetRelativePath(root, oldFile!) : Path.GetFileName(oldFile!);
        
        return new HeaderItem()
        {
            Path = path,
            OldHash = oldHash,
            NewHash = newHash,
            Size = fileInfo.Length,
            Command = "patch"
        };
    }

    public void CreateHeader(List<HeaderItem> items)
    {
        _header = new Header()
        {
            Size = 0,
            Items = items
        };
        
        using var memoryStream = new MemoryStream();
        MessagePackSerializer.Serialize(memoryStream, _header);
            
        _header.Size = memoryStream.Length;
    }

    public async void CreatePatchFromFile(string patchFile, string oldFile, string newFile)
    {
        var task = DiffFile(oldFile, newFile);
        var item = await task;

        if (item == null) return;

        try
        {
            CreateHeader([item]);

            ConstructPatchFile(patchFile);

            if (CompressPatch)
            {
                CompressFile(patchFile);
            }
        }
        finally
        {
            Cleanup();
        }
    }
    
    public async void CreatePatchFromDirectory(string patchFile, string oldDirectory, string newDirectory)
    {
        var items = new List<HeaderItem>();

        try
        {
            foreach (var newFilePath in Directory.GetFiles(newDirectory, "*", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(newDirectory, newFilePath);
                var oldFilePath = Path.Combine(oldDirectory, relativePath);

                var oldFile = File.Exists(oldFilePath) ? oldFilePath : null;
                var task = DiffFile(oldFile, newFilePath, newDirectory);
                var item = await task;
                if (item != null) items.Add(item);
            }
            
            foreach (var oldFilePath in Directory.GetFiles(oldDirectory, "*", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(oldDirectory, oldFilePath);
                var newFilePath = Path.Combine(newDirectory, relativePath);

                if (File.Exists(newFilePath)) continue;
                
                var task = DiffFile(oldFilePath, null, oldDirectory);
                var item = await task;
                if (item != null) items.Add(item);
            }

            CreateHeader(items);

            ConstructPatchFile(patchFile);

            if (CompressPatch)
            {
                CompressFile(patchFile);
            }
        }
        finally
        {
            Cleanup();
        }
    }

    public void ConstructPatchFile(string patchFile)
    {
        _ = OutputFolder ?? throw new ArgumentNullException(nameof(OutputFolder));
        
        patchFile = Path.Combine(OutputFolder, patchFile);
        using var patchStream = new FileStream(patchFile, FileMode.Create);
        MessagePackSerializer.Serialize(patchStream, _header);
        
        foreach (var item in _header!.Items)
        {
            var diffFile = Path.ChangeExtension(Path.Combine(OutputFolder, Path.GetFileName(item.Path)), "tmp");
            using var diffStream = new FileStream(diffFile, FileMode.Open);
            diffStream.CopyTo(patchStream);
        }
    }

    public void CompressFile(string file)
    {
        _ = OutputFolder ?? throw new ArgumentNullException(nameof(OutputFolder));
        
        file = Path.Combine(OutputFolder, file);
        
        using (var input = File.OpenRead(file))
        {
            using var output = File.OpenWrite(Path.ChangeExtension(file, "binz"));
            using var compressionStream = new CompressionStream(output, Compressor.MaxCompressionLevel);
            input.CopyTo(compressionStream);
        }
        
        _cleanup.Add(file);
    }
    
    public void Cleanup()
    {
        foreach (var file in _cleanup)
        {
            File.Delete(file);
        }
    }
}
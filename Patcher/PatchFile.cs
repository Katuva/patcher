using System.Buffers;
using System.IO;
using MessagePack;
using ZstdSharp;

namespace Patcher;

public class PatchFile(string? filename, string? patchFilename)
{
    private string Filename { get; set; } = filename ?? throw new ArgumentNullException(nameof(filename));
    private string PatchFilename { get; set; } = patchFilename ?? throw new ArgumentNullException(nameof(patchFilename));
    
    public async Task ApplyPatch()
    {
        {
            await using var input = File.OpenRead(PatchFilename);
            await using var output = File.OpenWrite(Path.ChangeExtension(PatchFilename, "bin"));
            await using var decompressionStream = new DecompressionStream(input);
            await decompressionStream.CopyToAsync(output);
        }
        
        await using var patchStream = new FileStream(Path.ChangeExtension(PatchFilename, "bin"), FileMode.Open);
        var reader = new MessagePackStreamReader(patchStream);
        
        DiffFile diffFile = null;
        
        if (await reader.ReadAsync(default) is ReadOnlySequence<byte> diffFileBlock)
        {
            diffFile = MessagePackSerializer.Deserialize<DiffFile>(diffFileBlock);
            
            var hash = Hashing.GetFileHash(Filename);
            
            if (hash.SequenceEqual(diffFile.OldFile))
            {
                Console.WriteLine(@"File hashes match, applying patch");
            }
            else
            {
                Console.WriteLine(@"File hashes do not match, aborting patch");
                return;
            }
            
            patchStream.Seek(diffFile.Offset, SeekOrigin.Begin);
        }
        
        await using var patchStreamTmp = new FileStream(Path.ChangeExtension(PatchFilename, "patch"), FileMode.Create);
        
        await patchStream.CopyToAsync(patchStreamTmp);
        
        patchStream.Close();
        patchStreamTmp.Seek(0, SeekOrigin.Begin);
        
        reader = new MessagePackStreamReader(patchStreamTmp);

        await using (var fileStream = new FileStream(Filename, FileMode.Open, FileAccess.ReadWrite))
        {
            while (await reader.ReadAsync(default) is ReadOnlySequence<byte> msgPackBlock)
            {
                var difference = MessagePackSerializer.Deserialize<Difference>(msgPackBlock);

                fileStream.Seek(difference.Offset, SeekOrigin.Begin);
                await fileStream.WriteAsync(difference.Data, 0, difference.Data.Length);
            }
        }
        
        patchStreamTmp.Close();
        File.Delete(Path.ChangeExtension(PatchFilename, "patch"));
        File.Delete(Path.ChangeExtension(PatchFilename, "bin"));

        var hashAfterPatch = Hashing.GetFileHash(Filename);

        Console.WriteLine(hashAfterPatch.SequenceEqual(diffFile.NewFile)
            ? @"Patch applied successfully"
            : @"Patch failed, file hashes do not match");
    }
}
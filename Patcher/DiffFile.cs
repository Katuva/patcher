using MessagePack;

namespace Patcher;

[MessagePackObject]
public class DiffFile
{
    [Key(0)]
    public byte[] OldFile { get; set; }
    
    [Key(1)]
    public byte[] NewFile { get; set; }
    
    [Key(2)]
    public long Offset { get; set; }
}
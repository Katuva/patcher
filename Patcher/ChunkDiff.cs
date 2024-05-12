using MessagePack;

namespace Patcher;

[MessagePackObject]
public partial class ChunkDiff
{
    [Key(0)]
    public long Offset { get; set; }
    [Key(1)]
    public byte[] Hash { get; set; }
    [Key(2)]
    public byte[] Data { get; set; }
}
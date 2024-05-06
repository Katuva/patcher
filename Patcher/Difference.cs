using MessagePack;

namespace Patcher;

[MessagePackObject]
public class Difference
{
    [Key(0)]
    public long Offset { get; set; }

    [Key(1)]
    public byte[] Data { get; set; }
}
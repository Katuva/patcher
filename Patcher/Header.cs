using MessagePack;

namespace Patcher;

[MessagePackObject]
public class HeaderItem
{
    [Key(0)]
    public string Path { get; set; }
    [Key(1)]
    public byte[] OldHash { get; set; }
    [Key(2)]
    public byte[] NewHash { get; set; }
    [Key(3)]
    public long Size { get; set; }
    [Key(4)]
    public string Command { get; set; }
}

[MessagePackObject]
public class Header
{
    [Key(0)]
    public long Size { get; set; }
    [Key(1)]
    public List<HeaderItem> Items { get; set; }
}
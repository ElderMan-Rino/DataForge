namespace Elder.DataForge.Models.Data
{
    public enum EnumType { Normal, Flag }

    public record EnumEntry(string Name, int Value, string Desc = "");

    public class EnumSchema
    {
        public string EnumName { get; set; }
        public EnumType EnumType { get; set; } = EnumType.Normal;
        public List<EnumEntry> Entries { get; set; } = new();
    }
}

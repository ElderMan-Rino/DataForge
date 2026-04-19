using System.Text.Json.Serialization;

namespace Elder.DataForge.Models.Data
{
    public class SheetRegistry
    {
        [JsonPropertyName("sheets")]
        public List<SheetEntry> Sheets { get; set; } = new();
    }
}

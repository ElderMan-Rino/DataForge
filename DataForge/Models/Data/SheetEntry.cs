using System.Text.Json.Serialization;

namespace Elder.DataForge.Models.Data
{
    public class SheetEntry
    {
        [JsonPropertyName("tableName")]
        public string TableName { get; set; }

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; } = true;
    }
}

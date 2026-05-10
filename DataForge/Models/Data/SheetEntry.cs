using System.Text.Json.Serialization;

namespace Elder.DataForge.Models.Data
{
    public class SheetEntry
    {
        [JsonPropertyName("tableName")]
        public string TableName { get; set; }

        [JsonPropertyName("dataName")]
        public string DataName { get; set; }

        [JsonPropertyName("isLanguageSheet")]
        public bool IsLanguageSheet { get; set; }

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; } = true;
    }
}
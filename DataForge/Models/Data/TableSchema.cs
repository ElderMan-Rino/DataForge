using Elder.DataForge.Models.Data.Excels;

namespace Elder.DataForge.Models.Data
{
    public class TableSchema
    {
        public string TableName { get; set; } 
        public List<FieldDefinition> Fields { get; set; } = new();
    }
}

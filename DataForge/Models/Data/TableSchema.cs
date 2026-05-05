namespace Elder.DataForge.Models.Data
{
    public class TableSchema
    {
        public string TableName { get; set; }
        public string DataName { get; set; }
        public bool IsLanguageSheet { get; set; } // ← 추가
        public List<AnalyzedField> AnalyzedFields { get; set; } = new();
        public List<List<string>> RawRows { get; set; } = new();
    }
}

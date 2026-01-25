namespace Elder.DataForge.Models.Data
{
    public class TableSchema
    {
        public string TableName { get; set; }
        public List<AnalyzedField> AnalyzedFields { get; set; } = new();
        // 실제 데이터 행들 (나중에 바이너리 익스포트 시 사용)
        public List<List<string>> RawRows { get; set; } = new();
    }
}

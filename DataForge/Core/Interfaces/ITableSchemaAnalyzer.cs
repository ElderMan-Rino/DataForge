using Elder.DataForge.Models.Data;
using Elder.DataForge.Models.Data.Excel;

namespace Elder.DataForge.Core.Interfaces
{
    public interface ITableSchemaAnalyzer
    {
        public List<TableSchema> AnalyzeFields(Dictionary<string, DocumentContentData> documentContents);
    }
}

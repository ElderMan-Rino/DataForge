using Elder.DataForge.Core.ContentExtracter.Excel;

namespace Elder.DataForge.Models.Data.Excel
{
    public readonly struct ExcelSheetData
    {
        public readonly string SheetName;
        public readonly string DataName;
        public readonly string LanguageCode; // ← 추가 ("Ko", "En", "" 등)
        public readonly List<FieldDefinition> FieldDefinitions;
        public readonly List<List<string>> Rows;
        public readonly Dictionary<int, List<string>> FieldValues;

        public ExcelSheetData(
            string sheetName,
            string dataName,
            string languageCode, // ← 추가
            List<FieldDefinition> fieldDefinitions,
            Dictionary<int, List<string>> fieldValues,
            List<List<string>> rows) // [추가] 생성자 매개변수 추가
        {
            SheetName = sheetName; 
            DataName = dataName;
            LanguageCode = languageCode ?? string.Empty;
            FieldDefinitions = fieldDefinitions;
            FieldValues = fieldValues;
            Rows = rows; 
        }
    }
}
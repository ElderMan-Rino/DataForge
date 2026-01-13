namespace Elder.DataForge.Models.Data.Excels
{
    public readonly struct ExcelSheetData
    {
        public readonly string SheetName;
        public readonly string DataName;
        public readonly List<FieldDefinition> FieldDefinitions;
        public readonly Dictionary<int, List<string>> FieldValues;
     
        public ExcelSheetData(string sheetName, string dataName, List<FieldDefinition> fieldDefinitions, Dictionary<int, List<string>> fieldValues)
        {
            SheetName = sheetName;
            DataName = dataName;
            FieldDefinitions = fieldDefinitions;
            FieldValues = fieldValues;
        }
    }
}

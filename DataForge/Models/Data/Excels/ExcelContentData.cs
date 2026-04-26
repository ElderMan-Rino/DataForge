namespace Elder.DataForge.Models.Data.Excel
{
    public class ExcelContentData : DocumentContentData
    {
        public readonly Dictionary<string, ExcelSheetData> SheetDatas;
        public readonly List<EnumSchema> EnumSchemas;

        public ExcelContentData(string name, Dictionary<string, ExcelSheetData> sheetDatas, List<EnumSchema> enumSchemas = null) : base(name)
        {
            SheetDatas = sheetDatas;
            EnumSchemas = enumSchemas ?? new List<EnumSchema>();
        }
    }
}

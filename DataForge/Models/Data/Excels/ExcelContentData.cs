namespace Elder.DataForge.Models.Data.Excel
{
    public class ExcelContentData : DocumentContentData
    {
        public readonly Dictionary<string, ExcelSheetData> SheetDatas;
        public readonly List<EnumSchema> EnumSchemas;
        public readonly string GroupName; // ← 추가 (없으면 empty)

        public ExcelContentData(string name, Dictionary<string, ExcelSheetData> sheetDatas, List<EnumSchema> enumSchemas = null, string groupName = null) : base(name)
        {
            SheetDatas = sheetDatas;
            EnumSchemas = enumSchemas ?? new List<EnumSchema>();
            GroupName = groupName ?? string.Empty;
        }
    }
}

namespace Elder.DataForge.Models.Data.Excels
{
    public class ExcelContentData : DocumentContentData
    {
        public readonly Dictionary<string, ExcelSheetData> SheetDatas;
        public ExcelContentData(string name, Dictionary<string, ExcelSheetData> sheetDatas) : base(name)
        {
            SheetDatas = sheetDatas;
        }
    }
}

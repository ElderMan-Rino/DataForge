using Elder.DataForge.Core.ContentExtracters;
using Elder.DataForge.Models.Data;
using Elder.DataForge.Models.Data.Excels;
using Elder.Helpers.Commons;
using OfficeOpenXml;
using System.IO;

namespace Elder.DataForge.Core.ContentLoaders.Excels
{
    public class ExcelContentExtracter : DocumentContentExtracterBase
    {
        private const string Colon = ":";
        private const string DataName = "DataName";

        protected override DocumentContentData ExtractDocumentContents(DocumentInfoData documentInfo)
        {
            return ExtractExcelContents(documentInfo.Name, documentInfo.Path);
        }

        private ExcelContentData ExtractExcelContents(string fileName, string directory)
        {
            string filePath = Path.Combine(directory, fileName);
            if (!File.Exists(filePath))
                return null;

            ExcelPackage.License.SetNonCommercialPersonal("ElderMan");
            var sheetDatas = new Dictionary<string, ExcelSheetData>();
            var fileInfo = new FileInfo(filePath);
            using (var package = new ExcelPackage(fileInfo))
            {
                var workSheets = package.Workbook.Worksheets;
                foreach (var worksheet in workSheets)
                {
                    // 시트 하나 기준으로 Sheet컨텐츠 하나씩 가지고 있어야하나?
                    string sheetName = worksheet.Name;
                    if (sheetDatas.ContainsKey(sheetName))
                        continue;

                    var fieldDefinitions = new List<FieldDefinition>();
                    var fieldValues = new Dictionary<int, List<string>>();
                    int rowCount = worksheet.Dimension?.Rows ?? 0;
                    int colCount = worksheet.Dimension?.Columns ?? 0;
                    var dataName = string.Empty;
                    for (int row = 1; row <= rowCount; row++)
                    {
                        var rowData = new List<string>();
                        for (int col = 1; col <= colCount; col++)
                            ProcessCell(worksheet.Cells[row, col].Text, col, ref dataName, fieldDefinitions, fieldValues);
                    }
                    sheetDatas.Add(sheetName, new ExcelSheetData(sheetName, dataName, fieldDefinitions, fieldValues));
                }
            }

            return new ExcelContentData(fileName, sheetDatas);
        }

        private void ProcessCell(string cellValue, int col, ref string dataName, List<FieldDefinition> fieldDefinitions, Dictionary<int, List<string>> fieldValues)
        {
            if (string.IsNullOrEmpty(cellValue))
                return;

            if (string.IsNullOrEmpty(dataName) && TryExtractDataName(cellValue, out dataName))
                return;

            if (TryExtractVariableInfo(cellValue, out var variableInfo))
            {
                var fieldOrder = col;
                fieldDefinitions.Add(new FieldDefinition(fieldOrder, variableInfo[0], variableInfo[1]));
            }
            else
            {
                if (!fieldValues.ContainsKey(col))
                    fieldValues[col] = new List<string>();

                fieldValues[col].Add(cellValue);
            }
        }

        private bool TryExtractVariableInfo(string cellValue, out string[] variableInfo)
        {
            variableInfo = default;
            if (!StringHelpers.ContainsText(cellValue, Colon))
                return false;

            var segments = cellValue.Split(Colon);
            if (segments == null || segments.Length <= 0 || segments.Length > 2)
                return false;

            variableInfo = segments;
            return true;
        }

        private bool TryExtractDataName(string cellValue, out string dataName)
        {
            dataName = string.Empty;
            if (!StringHelpers.ContainsHtmlTag(cellValue))
                return false;

            var isDataNameSummary = StringHelpers.ContainsText(cellValue, DataName);
            if (!isDataNameSummary)
                return false;

            dataName = StringHelpers.ExtractHtmlTagContent(cellValue);
            if (string.IsNullOrEmpty(dataName))
                return false;

            return true;
        }
    }
}

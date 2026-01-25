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
                    string sheetName = worksheet.Name;
                    if (sheetDatas.ContainsKey(sheetName))
                        continue;

                    var fieldDefinitions = new List<FieldDefinition>();
                    var fieldValues = new Dictionary<int, List<string>>();
                    var rows = new List<List<string>>(); // [추가] 행 단위 데이터를 담을 리스트

                    int rowCount = worksheet.Dimension?.Rows ?? 0;
                    int colCount = worksheet.Dimension?.Columns ?? 0;
                    var dataName = string.Empty;

                    for (int row = 1; row <= rowCount; row++)
                    {
                        var rowData = new List<string>(); // 현재 행의 모든 컬럼 값을 저장
                        bool isDataRow = false; // 해당 행에 실제 데이터가 포함되었는지 여부

                        for (int col = 1; col <= colCount; col++)
                        {
                            string cellText = worksheet.Cells[row, col].Text;

                            // 1. 기존 로직 수행 (필드 정의 및 컬럼별 데이터 수집)
                            ProcessCell(cellText, col, ref dataName, fieldDefinitions, fieldValues);

                            // 2. 데이터 행 판별 로직
                            // 셀 값이 비어있지 않고, 정의부(VariableInfo)나 메타데이터(DataName)가 아닐 경우 데이터 행으로 간주
                            if (!string.IsNullOrEmpty(cellText) &&
                                !TryExtractVariableInfo(cellText, out _) &&
                                !TryExtractDataName(cellText, out _))
                            {
                                isDataRow = true;
                            }

                            rowData.Add(cellText); // 현재 셀의 값을 행 리스트에 추가
                        }

                        // 3. 실제 데이터가 포함된 행만 리스트에 추가
                        if (isDataRow)
                        {
                            rows.Add(rowData);
                        }
                    }

                    // [수정] 업데이트된 ExcelSheetData 생성자 호출 (rows 포함)
                    sheetDatas.Add(sheetName, new ExcelSheetData(sheetName, dataName, fieldDefinitions, fieldValues, rows));
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

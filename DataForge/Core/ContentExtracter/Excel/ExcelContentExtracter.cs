using Elder.DataForge.Core.Interfaces;
using Elder.DataForge.Models.Data;
using Elder.DataForge.Models.Data.Excel;
using Elder.Helpers.Commons;
using OfficeOpenXml;
using System.IO;
using System.Reactive.Subjects;
using System.Text.RegularExpressions;

namespace Elder.DataForge.Core.ContentExtracter.Excel
{
    public class ExcelContentExtracter : IDocumentContentExtracter
    {
        private const string ExtractingStartText = "Data Extracting Start";
        private const string ExtractingEndText = "Data Extracting End";
        private const string ExtractingProgressText = "Extracting : ";

        private const string Colon = ":";
        private const string DataName = "DataName";
        private const string EnumSheetPrefix = "Enum_";
        private const string EnumNameColumn = "EnumName";
        private const string EnumValueColumn = "Value";
        private static readonly Regex EnumTypeTagRegex = new Regex(@"<EnumType=(\w+)>", RegexOptions.IgnoreCase);

        private Subject<string> _updateProgressLevel = new();
        private Subject<float> _updateProgressValue = new();
        private Subject<string> _updateOutputLog = new();

        public IObservable<string> OnProgressLevelUpdated => _updateProgressLevel;
        public IObservable<float> OnProgressValueUpdated => _updateProgressValue;
        public IObservable<string> OnOutputLogUpdated => _updateOutputLog;

        private void UpdateProgressLevel(string progressLevel) => _updateProgressLevel.OnNext(progressLevel);
        private void UpdateProgressValue(float progressValue) => _updateProgressValue.OnNext(progressValue);
 
        public async Task<Dictionary<string, DocumentContentData>> ExtractDocumentContentDataAsync(IEnumerable<DocumentInfoData> documentInfoData)
        {
            UpdateProgressLevel(ExtractingStartText);
            UpdateProgressValue(0f);

            var currentCount = 0;
            var totalLength = documentInfoData.Count();
            var extractedData = new Dictionary<string, DocumentContentData>();
            foreach (var info in documentInfoData)
            {
                var contentData = ExtractExcelContents(info);
                if (contentData != null)
                {
                    extractedData.Add(contentData.Name, contentData);
                    UpdateProgressLevel($"{ExtractingProgressText} {contentData.Name}");
                }
                await Task.Yield();

                var currentProgress = (float)++currentCount / (float)totalLength;
                UpdateProgressValue(currentProgress);
            }
            UpdateProgressLevel(ExtractingEndText);
            return extractedData;
        }

        private ExcelContentData ExtractExcelContents(DocumentInfoData info)
        {
            string fileName = info.Name;
            string directory = info.Path;
            string filePath = Path.Combine(directory, fileName);
            if (!File.Exists(filePath))
                return null;

            ExcelPackage.License.SetNonCommercialPersonal("ElderMan");
            var sheetDatas = new Dictionary<string, ExcelSheetData>();
            var enumSchemas = new List<EnumSchema>();
            var fileInfo = new FileInfo(filePath);
            using (var package = new ExcelPackage(fileInfo))
            {
                var workSheets = package.Workbook.Worksheets;
                foreach (var worksheet in workSheets)
                {
                    string sheetName = worksheet.Name;

                    if (sheetName.StartsWith(EnumSheetPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        var enumSchema = ExtractEnumSheet(worksheet, sheetName);
                        if (enumSchema != null)
                            enumSchemas.Add(enumSchema);
                        continue;
                    }

                    if (sheetDatas.ContainsKey(sheetName))
                        continue;

                    var fieldDefinitions = new List<FieldDefinition>();
                    var fieldValues = new Dictionary<int, List<string>>();
                    var rows = new List<List<string>>();

                    int rowCount = worksheet.Dimension?.Rows ?? 0;
                    int colCount = worksheet.Dimension?.Columns ?? 0;
                    var dataName = string.Empty;

                    for (int row = 1; row <= rowCount; row++)
                    {
                        var rowData = new List<string>();
                        bool isDataRow = false;

                        for (int col = 1; col <= colCount; col++)
                        {
                            string cellText = worksheet.Cells[row, col].Text;
                            ProcessCell(cellText, col, ref dataName, fieldDefinitions, fieldValues);
                            if (!string.IsNullOrEmpty(cellText) &&
                                !TryExtractVariableInfo(cellText, out _) &&
                                !TryExtractDataName(cellText, out _))
                            {
                                isDataRow = true;
                            }

                            rowData.Add(cellText);
                        }

                        if (isDataRow)
                        {
                            rows.Add(rowData);
                        }
                    }
                    sheetDatas.Add(sheetName, new ExcelSheetData(sheetName, dataName, fieldDefinitions, fieldValues, rows));
                }
            }

            return new ExcelContentData(fileName, sheetDatas, enumSchemas);
        }

        private EnumSchema ExtractEnumSheet(ExcelWorksheet worksheet, string sheetName)
        {
            string enumName = sheetName.Substring(EnumSheetPrefix.Length);
            if (string.IsNullOrEmpty(enumName))
                return null;

            int rowCount = worksheet.Dimension?.Rows ?? 0;
            int colCount = worksheet.Dimension?.Columns ?? 0;

            var enumType = EnumType.Normal;
            int nameColIndex = -1;
            int valueColIndex = -1;
            var entries = new List<EnumEntry>();

            for (int row = 1; row <= rowCount; row++)
            {
                // 첫 번째 행: EnumType 태그 탐색
                if (row == 1)
                {
                    for (int col = 1; col <= colCount; col++)
                    {
                        string cell = worksheet.Cells[row, col].Text?.Trim();
                        if (string.IsNullOrEmpty(cell)) continue;

                        var match = EnumTypeTagRegex.Match(cell);
                        if (match.Success)
                        {
                            string typeValue = match.Groups[1].Value;
                            if (typeValue.Equals("Flag", StringComparison.OrdinalIgnoreCase))
                                enumType = EnumType.Flag;
                        }
                    }
                    continue;
                }

                // 헤더 행: EnumName / Value 컬럼 인덱스 확정
                if (nameColIndex == -1)
                {
                    bool isHeaderRow = false;
                    for (int col = 1; col <= colCount; col++)
                    {
                        string cell = worksheet.Cells[row, col].Text?.Trim();
                        if (string.IsNullOrEmpty(cell)) continue;

                        // EnumName:string 또는 EnumName 형태 모두 허용
                        string colName = cell.Contains(Colon) ? cell.Split(Colon)[0].Trim() : cell;
                        if (colName.Equals(EnumNameColumn, StringComparison.OrdinalIgnoreCase))
                        {
                            nameColIndex = col;
                            isHeaderRow = true;
                        }
                        else if (colName.Equals(EnumValueColumn, StringComparison.OrdinalIgnoreCase))
                        {
                            valueColIndex = col;
                            isHeaderRow = true;
                        }
                    }
                    if (isHeaderRow) continue;
                }

                // 데이터 행
                if (nameColIndex == -1) continue;

                string entryName = nameColIndex > 0 ? worksheet.Cells[row, nameColIndex].Text?.Trim() : null;
                if (string.IsNullOrEmpty(entryName)) continue;

                int entryValue = entries.Count;
                if (valueColIndex > 0)
                {
                    string rawValue = worksheet.Cells[row, valueColIndex].Text?.Trim();
                    if (!string.IsNullOrEmpty(rawValue) && int.TryParse(rawValue, out int parsed))
                        entryValue = parsed;
                }

                entries.Add(new EnumEntry(entryName, entryValue));
            }

            if (entries.Count == 0)
                return null;

            return new EnumSchema { EnumName = enumName, EnumType = enumType, Entries = entries };
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

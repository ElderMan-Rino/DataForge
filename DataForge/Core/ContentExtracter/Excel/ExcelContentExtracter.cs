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
        private const string Language = "Language"; // ← 추가
        private const string EnumSheetPrefix = "Enum_";
        private const string EnumNameColumn = "EnumName";
        private const string EnumValueColumn = "Value";
        private const string IgnorePrefix = "#";
        private const string EnumDescColumn = "#Desc";

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
            if (!File.Exists(filePath)) return null;

            ExcelPackage.License.SetNonCommercialPersonal("ElderMan");
            var sheetDatas = new Dictionary<string, ExcelSheetData>();
            var enumSchemas = new List<EnumSchema>();
            var fileInfo = new FileInfo(filePath);

            using (var package = new ExcelPackage(fileInfo))
            {
                foreach (var worksheet in package.Workbook.Worksheets)
                {
                    string sheetName = worksheet.Name;

                    if (sheetName.StartsWith(EnumSheetPrefix,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        var enumSchema = ExtractEnumSheet(worksheet, sheetName);
                        if (enumSchema != null) enumSchemas.Add(enumSchema);
                        continue;
                    }

                    if (sheetDatas.ContainsKey(sheetName)) continue;

                    var fieldDefinitions = new List<FieldDefinition>();
                    var fieldValues = new Dictionary<int, List<string>>();
                    var rows = new List<List<string>>();
                    var dataName = string.Empty;
                    var languageCode = string.Empty; // ← 추가

                    int rowCount = worksheet.Cells.Any()
                        ? worksheet.Cells.Max(c => c.End.Row) : 0;
                    int colCount = worksheet.Cells.Any()
                        ? worksheet.Cells.Max(c => c.End.Column) : 0;

                    for (int row = 1; row <= rowCount; row++)
                    {
                        var rowData = new List<string>();
                        bool hasFieldDef = false;
                        bool hasDataCell = false;

                        for (int col = 1; col <= colCount; col++)
                        {
                            string cellText = worksheet.Cells[row, col].Text;

                            // ─── <Language> 태그 수집 ──────────────────────
                            // 아직 못 찾은 경우에만 시도
                            if (string.IsNullOrEmpty(languageCode)
                                && TryExtractLanguageCode(cellText, out var parsedCode))
                            {
                                languageCode = parsedCode;
                                rowData.Add(cellText);
                                // 헤더 행으로 간주 → rows에서 제외
                                hasFieldDef = true;
                                continue;
                            }

                            ProcessCell(cellText, col, ref dataName,
                                fieldDefinitions, fieldValues);

                            rowData.Add(cellText);

                            if (string.IsNullOrEmpty(cellText)) continue;

                            if (TryExtractVariableInfo(cellText, out _)
                                || TryExtractDataName(cellText, out _))
                                hasFieldDef = true;
                            else
                                hasDataCell = true;
                        }

                        if (!hasFieldDef && hasDataCell)
                            rows.Add(rowData);
                    }

                    sheetDatas.Add(sheetName, new ExcelSheetData(
                        sheetName,
                        dataName,
                        languageCode, // ← 추가
                        fieldDefinitions,
                        fieldValues,
                        rows));
                }
            }

            return new ExcelContentData(fileName, sheetDatas, enumSchemas);
        }

        private bool TryExtractLanguageCode(string cellValue, out string languageCode)
        {
            languageCode = string.Empty;
            if (string.IsNullOrEmpty(cellValue)) return false;
            if (!StringHelpers.ContainsHtmlTag(cellValue)) return false;
            if (!StringHelpers.ContainsText(cellValue, Language)) return false;

            languageCode = StringHelpers.ExtractHtmlTagContent(cellValue);
            return !string.IsNullOrEmpty(languageCode);
        }

        private EnumSchema ExtractEnumSheet(ExcelWorksheet worksheet, string sheetName)
        {
            string enumName = sheetName.Substring(EnumSheetPrefix.Length);
            if (string.IsNullOrEmpty(enumName))
                return null;

            int rowCount = worksheet.Cells.Any() ? worksheet.Cells.Max(c => c.End.Row) : 0;
            int colCount = worksheet.Cells.Any() ? worksheet.Cells.Max(c => c.End.Column) : 0;

            _updateOutputLog.OnNext($"[Enum] Parsing sheet: {sheetName} (rows={rowCount}, cols={colCount})");

            var enumType = EnumType.Normal;
            int nameColIndex = -1;
            int valueColIndex = -1;
            int descColIndex = -1;
            bool headerFound = false;
            var entries = new List<EnumEntry>();

            for (int row = 1; row <= rowCount; row++)
            {
                var rowDump = new System.Text.StringBuilder();
                rowDump.Append($"[Enum] row={row} cells: ");
                for (int col = 1; col <= colCount; col++)
                    rowDump.Append($"[{col}]='{worksheet.Cells[row, col].Text?.Trim()}' ");
                _updateOutputLog.OnNext(rowDump.ToString());

                // 각 행을 순회하며 EnumType 태그 / 헤더 / 데이터를 동시에 처리
                bool rowHasData = false;
                for (int col = 1; col <= colCount; col++)
                {
                    string cell = worksheet.Cells[row, col].Text?.Trim();
                    if (string.IsNullOrEmpty(cell)) continue;

                    // EnumType 태그 감지 (어느 행, 어느 셀에 있어도 처리)
                    var match = EnumTypeTagRegex.Match(cell);
                    if (match.Success)
                    {
                        string typeValue = match.Groups[1].Value;
                        enumType = typeValue.Equals("Flag", StringComparison.OrdinalIgnoreCase)
                            ? EnumType.Flag : EnumType.Normal;
                        _updateOutputLog.OnNext($"[Enum] EnumType={enumType}");
                        continue;
                    }

                    // 헤더 감지 (아직 못 찾은 경우)
                    if (!headerFound)
                    {
                        string colName = cell.Contains(Colon) ? cell.Split(Colon)[0].Trim() : cell;
                        if (colName.Equals(EnumNameColumn, StringComparison.OrdinalIgnoreCase))
                        {
                            nameColIndex = col;
                            rowHasData = true;
                        }
                        else if (colName.Equals(EnumValueColumn, StringComparison.OrdinalIgnoreCase))
                        {
                            valueColIndex = col;
                            rowHasData = true;
                        }
                        else if (colName.Equals(EnumDescColumn, StringComparison.OrdinalIgnoreCase))
                        {
                            descColIndex = col;
                            rowHasData = true;
                        }

                    }
                }

                // 이번 행에서 헤더를 새로 찾았으면 확정 후 다음 행으로
                if (!headerFound && rowHasData && nameColIndex != -1)
                {
                    headerFound = true;
                    _updateOutputLog.OnNext($"[Enum] Header found at row={row}, nameCol={nameColIndex}, valueCol={valueColIndex}");
                    continue;
                }

                // 헤더를 아직 못 찾았으면 데이터 행 처리 불가
                if (!headerFound) continue;

                // 데이터 행
                string entryName = worksheet.Cells[row, nameColIndex].Text?.Trim();
                if (string.IsNullOrEmpty(entryName)) continue;

                int entryValue = entries.Count;
                if (valueColIndex > 0)
                {
                    string rawValue = worksheet.Cells[row, valueColIndex].Text?.Trim();
                    if (!string.IsNullOrEmpty(rawValue) && int.TryParse(rawValue, out int parsed))
                        entryValue = parsed;
                }

                string entryDesc = descColIndex > 0 ? worksheet.Cells[row, descColIndex].Text?.Trim() ?? "" : "";

                entries.Add(new EnumEntry(entryName, entryValue, entryDesc));
                _updateOutputLog.OnNext($"[Enum] Entry: {entryName}={entryValue}");
            }

            if (entries.Count == 0)
            {
                _updateOutputLog.OnNext($"[Enum] WARNING: No entries found in sheet '{sheetName}'");
                return null;
            }

            _updateOutputLog.OnNext($"[Enum] '{enumName}' generated with {entries.Count} entries.");
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
            if (!StringHelpers.ContainsText(cellValue, Colon)) return false;
            if (cellValue.TrimStart().StartsWith(IgnorePrefix)) return false;

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

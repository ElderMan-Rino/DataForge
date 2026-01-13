using Elder.DataForge.Models.Data;
using Elder.DataForge.Models.Data.Excels;
using System.IO;

namespace Elder.DataForge.Core.InfoLoaders.Excels
{
    public class ExcelInfoLoader : DocumentInfoLoaderBase
    {
        protected override string _dialogTitle => "Load Excel File";
        protected override string _filter => "Excel Files (*.xlsx;*.xls)|*.xlsx;*.xls";
        protected override string _loadErrorMsg => "Load File Error";

        public override DocumentInfoData CreateDocumentData(string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            if (string.IsNullOrEmpty(fileName))
                return null;

            var directoryPath = Path.GetDirectoryName(filePath);
            if (string.IsNullOrEmpty(directoryPath))
                return null;

            return new ExcelInfoData(fileName, directoryPath);
        }
    }
}

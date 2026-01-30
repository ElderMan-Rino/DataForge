using Elder.DataForge.Core.Converters;
using Elder.DataForge.Models.Data;
using Elder.DataForge.Models.Data.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elder.DataForge.Core.Converters.MemoryPack
{
    internal class MemoryPackConverter : DataConverterBase
    {
        protected override ConversionData ConvertContent(DocumentContentData contentData)
        {
            if (contentData is not ExcelContentData excelContentData)
                return null;

            return ContentToMemoryPack(excelContentData);
        }

        private ConversionData ContentToMemoryPack(ExcelContentData excelContentData)
        {
            //var convertedData = new Dictionary<string, List<>>();
            //var sheetDatas = excelContentData.SheetDatas;
            //foreach (var sheetData in sheetDatas)
            //{
            //    var sheetName = sheetData.Key;
            //    if (!convertedData.ContainsKey(sheetName))
            //        convertedData[sheetName] = new();

            //    var data = sheetData.Value;
            //    FieldDefinitionsToMemoryPackClass(data.FieldDefinitions);
            //}
            return null;
        }

        private void FieldDefinitionsToMemoryPackClass(List<FieldDefinition> fieldDefinitions)
        {

        }
    }
}

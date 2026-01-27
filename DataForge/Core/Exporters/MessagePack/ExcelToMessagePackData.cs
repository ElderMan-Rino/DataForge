using Elder.DataForge.Core.Exporters;
using Elder.DataForge.Models.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elder.DataForge.Core.Exporters.MessagePack
{
    public class ExcelToMessagePackData : DataExporterBase
    {
        public override Task<bool> TryExportDataAsync(List<TableSchema> schemas, string outputPath)
        {
            throw new NotImplementedException();
        }
    }
}

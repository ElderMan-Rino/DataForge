using Elder.DataForge.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using Elder.DataForge.Core.Notifiers;
using Elder.DataForge.Models.Data;

namespace Elder.DataForge.Core.Exporters
{
    public abstract class DataExporterBase : ProgressReporter, IDataExporter
    {
        private const string ExportingStartText = "Data Exporting Start";
        private const string ExportingEndText = "Data Exporting End";

        public abstract Task<bool> TryExportDataAsync(List<TableSchema> schemas, string outputPath);
    }
}

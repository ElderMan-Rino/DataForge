using Elder.DataForge.Core.CodeGenerators.MessagePack;
using Elder.DataForge.Models.Data;

namespace Elder.DataForge.Core.Interfaces
{
    public interface ICodeTemplateEngine
    {
        public List<GeneratedSourceCode> BuildSourceCodes(TableSchema schema);
    }
}

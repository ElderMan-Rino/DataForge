using Elder.DataForge.Core.CodeGenerators.MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elder.DataForge.Core.Interfaces
{
    public interface ISourceCodeSaver
    {
        public Task<bool> SaveAsync(List<GeneratedSourceCode> sourceCodes, string outputDirectory);
    }
}

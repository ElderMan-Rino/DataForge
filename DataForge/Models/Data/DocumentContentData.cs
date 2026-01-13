using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elder.DataForge.Models.Data
{
    public abstract class DocumentContentData : IDisposable
    {
        public readonly string Name;

        protected DocumentContentData(string name)
        {
            Name = name;
        }

        public virtual void Dispose()
        {

        }
    }
}

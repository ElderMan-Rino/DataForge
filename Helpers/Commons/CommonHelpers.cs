using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elder.Helpers.Commons
{
    public static class CommonHelpers
    {
        public static float ToProgress(int current, int total)
        {
            return (float)current / (float)total;
        }
    }
}

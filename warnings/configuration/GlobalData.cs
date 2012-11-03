using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Roslyn.Services;

namespace warnings.configuration
{
    public class GlobalData
    {
        public static ISolution Solution { set; get; }
    }
}

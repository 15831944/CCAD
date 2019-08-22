using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCAD.DataType
{
    public class FontCheckResult
    {
        public FontCheckResult()
        {
            TrueTypeMissing = false;
            ShxFontMissing = false;
            BigFontMissing = false;
        }
        public bool TrueTypeMissing { get; set; }
        public bool ShxFontMissing { get; set; }
        public bool BigFontMissing { get; set; }
    }
}

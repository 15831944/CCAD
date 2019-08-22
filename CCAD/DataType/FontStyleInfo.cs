using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCAD.DataType
{
    public class FontStyleInfo
    {
        public enum FontFileType
        {
            TrueType,
            ShxFont,
            BigFont,
        }

        public FontStyleInfo()
        {
            Name = string.Empty;
            FileType = FontFileType.ShxFont;
            FontName = string.Empty;

            TrueTypeFontName = string.Empty;
            ShxFontName = string.Empty;
            BigFontName = string.Empty;
            CheckResult = new FontCheckResult();
        }
        public string Name { get; set; }
        public FontFileType FileType { get; set; }
        public string FontName { get; set; }

        public string TrueTypeFontName { get; set; }
        public string ShxFontName { get; set; }
        public string BigFontName { get; set; }
        public FontCheckResult CheckResult { get; }
    }
}

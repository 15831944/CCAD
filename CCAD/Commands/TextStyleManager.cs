using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCAD.Commands
{
    public class TextStyleManager : ICADCommand
    {
        public void Execute()
        {
            /// promote:
            /// 
            /// CMD
            /// 列出所有字体样式
            /// 序号，样式名，字体名，大字体，高度，宽度因子
            /// 字体类型：TrueType SHX字体 SHX+大字体
            /// Actions:
            /// 批量修改丢失字体：TrueType名，字体名，大字体名
            /// 修改选中样式的字体
            

        }
    }
}

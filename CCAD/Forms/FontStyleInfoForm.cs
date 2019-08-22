using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CCAD.DataType;

namespace CCAD.Forms
{
    public partial class FontStyleInfoForm : Form
    {
        public FontStyleInfoForm()
        {
            InitializeComponent();
        }

        public List<FontStyleInfo> StyleInfo { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace TableTool.Data
{
    public class ExcelMember : Member
    {
        [DefaultValue(false)]
        public bool Key { get; set; }

        public string Colume { get; set; }

        public ExcelMember() { }
        public ExcelMember(string colume) { Colume = colume; }
    }
}

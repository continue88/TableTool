using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using TableTool.Helper;

namespace TableTool.Data
{
    public class Member
    {
        public string Colume { get; set; }
        public string Name { get; set; }

        [DefaultValue(false)]
        public bool Key { get; set; }

        [TypeConverter(typeof(MemberTypeConverter))]
        public string Type { get; set; }

        [Browsable(false)]
        public int FieldIndex { get; set; }

        [Description("数组字符分隔符")]
        public char Seaperator { get; set; }

        public Member() { }
        public Member(string colume) { Colume = colume; }
    }
}

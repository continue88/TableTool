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
        public string Name { get; set; }
        public string Desc { get; set; }

        [TypeConverter(typeof(MemberTypeConverter))]
        public string Type { get; set; }

        [Browsable(false)]
        public int FieldIndex { get; set; }

        [Description("数组字符分隔符")]
        public char Seaperator { get; set; }

        public Member()
        {
            Type = MemberType.Int32.ToString();
            Name = "Member1";
        }

        public bool IsArray() => Seaperator != 0;
    }
}

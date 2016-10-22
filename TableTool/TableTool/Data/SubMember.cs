using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TableTool.Helper;
using System.ComponentModel;

namespace TableTool.Data
{
    public class SubMember
    {
        [TypeConverter(typeof(MemberTypeConverter))]
        public string Type { get; set; }

        public string Name { get; set; }

        public char Seaperator { get; set; }

        public string Desc { get; set; }

        public SubMember()
        {
            Type = MemberType.Int32.ToString();
            Name = "Member1";
        }

        public override string ToString()
        {
            return Type + " " + Name;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TableTool.Data
{
    public class Table
    {
        public string Name { get; set; }

        public string Desc { get; set; }

        public int MaxField;

        public List<ExcelMember> Members = new List<ExcelMember>();

        public List<SubClass> Classes = new List<SubClass>();

        public void GetKeys(out Member key1, out Member key2)
        {
            key1 = null;
            key2 = null;
            foreach (var member in Members)
            {
                if (!member.Key) continue;
                if (key1 == null) key1 = member;
                else if (key2 == null) key2 = member;
                else throw new Exception("Too much keys in table, max supported is 2.");
            }

            if (key1 == null)
                throw new Exception("No key found in table: " + Name);
        }

        public SubClass FindSubClasss(string type)
        {
            return Classes.FirstOrDefault(x => x.Name == type);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TableTool.Data
{
    public class SubClass
    {
        string mName;
        public string Name { get { return mName; } set { OnRename(mName, value); mName = value; } }

        public char Seaperator { get; set; }

        public List<Member> Members { get; set; }

        public bool Enum => false;

        public SubClass()
        {
            mName = "ClassName1";
            Seaperator = '|';
            Members = new List<Member>();
        }

        public override string ToString()
        {
            return Name;
        }

        void OnRename(string oldName, string newName)
        {
            if (MainForm.Instance == null || MainForm.Instance.CurrentTable == null)
                return;

            var table = MainForm.Instance.CurrentTable;
            table.Members.ForEach(x =>
            {
                if (x.Type == oldName)
                    x.Type = newName;
            });
            table.Classes.ForEach(x =>
            {
                if (x == this) return;

                x.Members.ForEach(m =>
                {
                    if (m.Type == oldName)
                        m.Type = newName;
                });
            });
        }
    }
}

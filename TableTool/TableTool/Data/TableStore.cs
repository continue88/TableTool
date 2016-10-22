using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TableTool.Data
{
    public class TableStore
    {
        public Dictionary<string, Table> Tables = new Dictionary<string, Table>();

        public void FixFieldIndex()
        {
            foreach (var table in Tables.Values)
            {
                var maxField = 0;
                foreach (var member in table.Members)
                    maxField = Math.Max(member.FieldIndex, maxField);

                foreach (var member in table.Members)
                {
                    if (member.FieldIndex == 0)
                        member.FieldIndex = ++maxField;
                }

                if (table.MaxField != maxField)
                {
                    MainForm.Instance.DirtyFalg(true);
                    table.MaxField = maxField;
                }
            }
        }
    }
}

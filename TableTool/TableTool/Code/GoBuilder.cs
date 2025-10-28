using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TableTool.Data;

namespace TableTool.Code
{
    public class GoBuilder : Builder
    {
        public override string Type { get { return "go"; } }

        public override string Build(Table table, object mainObj)
        {
            Member key1, key2;
            table.GetKeys(out key1, out key2);
            
            var keystr = (key2 != null) ?
                string.Format("int64(int64(item.Get{0}()) << 32) | int64(item.Get{1}())", key1.Name, key2.Name) :
                string.Format("int64(item.Get{0}())", key1.Name);

            return string.Format(GoTemplate.CodeTemplate, table.Name, keystr);
        }
    }
}

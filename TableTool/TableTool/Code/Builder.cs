using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TableTool.Data;

namespace TableTool.Code
{
    public abstract class Builder
    {
        public abstract string Type { get; }
        public abstract string Build(Table table, object mainObj);

        public const string STRING_POOL_FIELD = "StringPool";

        // build for editor...
        public string BuildProtoGen(Table table)
        {
            // header.
            var sb = new StringBuilder();
            CSharpBuilder.BuildTable(sb, table);
            // the key func.
            Member key1, key2;
            table.GetKeys(out key1, out key2);
            var keystr = (key2 != null) ?
                string.Format("((Int64)item.{0} << 32) + (Int64)item.{1}", key1.Name, key2.Name) :
                string.Format("item.{0}", key1.Name);

            var sortFunc = string.Format(@"
                delegate({0} x, {0} y) 
                {{ 
                    if (x == y) return 0;
                    Int64 kx = Key(x), ky = Key(y); 
                    if (kx < ky) return -1;
                    if (kx > ky) return 1;
                    if (kx >= (1 << 32))
                        throw new Exception(""重复Key: "" + (kx >> 32) + "","" + (Int32)kx);
                    throw new Exception(""重复Key: "" + kx);
                }}", table.Name);

            sb.AppendLine("[ProtoBuf.ProtoContract]");
            sb.AppendLine("public class " + table.Name + "Array");
            sb.AppendLine("{");
            sb.AppendLine("    [ProtoBuf.ProtoMember(1)]");
            sb.AppendFormat("    public {0}[] {1};", table.Name, Constants.ItemsField).AppendLine();
            sb.AppendFormat("    public static Int64 Key({0} item) {{ return {1}; }}", table.Name, keystr).AppendLine();
            sb.AppendFormat("    public void Sort() {{ Array.Sort(Items, {0}); }}", sortFunc).AppendLine();
            sb.AppendLine("}");

            return sb.ToString();
        }
    }
}

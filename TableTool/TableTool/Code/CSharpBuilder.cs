using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TableTool.Data;

namespace TableTool.Code
{
    public class CSharpBuilder : Builder
    {
        public override string Type { get { return "cs"; } }

        public override string Build(Table table, object mainObj)
        {
            // header.
            var sb = new StringBuilder();
            sb.AppendLine($"// {Constants.CopyrightComment}");
            BuildTable(sb, table);
            sb.AppendLine();

            BuildTableManager(sb, table);

            return sb.ToString();
        }

        public static void BuildTable(StringBuilder sb, Table table)
        {
            sb.AppendLine("using System;");
            sb.AppendLine("using System.ComponentModel;");
            sb.AppendLine();

            // the class.
            sb.AppendLine("[ProtoBuf.ProtoContract]");
            sb.AppendLine("public class " + table.Name);
            sb.AppendLine("{");

            // sub class.
            table.Classes.ForEach(subClass => BuildClass(sb, subClass));

            // member.
            table.Members.ForEach(member => BuildMember(sb, member));

            sb.AppendLine("}");
            sb.AppendLine();
        }

        static void BuildClass(StringBuilder sb, SubClass subClass)
        {
            var sp = "    ";
            sb.AppendLine(sp + "[ProtoBuf.ProtoContract]");
            sb.AppendLine(sp + "public class " + subClass.Name);
            sb.AppendLine(sp + "{");

            var idx = 1;
            subClass.Members.ForEach(x => BuildMember(sb, sp + "    ", idx++, x.Type, x.Name, x.Desc, x.Seaperator));

            // tostring...
            var tostring = "";
            subClass.Members.ForEach(x => tostring += x.Name + " + \"" + subClass.Seaperator + "\" + ");
            sb.AppendLine(sp + "    public override string ToString() { return " + tostring.Substring(0, tostring.Length - 9) + "; }");

            sb.AppendLine(sp + "}");
        }

        static void BuildMember(StringBuilder sb, ExcelMember member)
        {
            if (string.IsNullOrEmpty(member.Type) || string.IsNullOrEmpty(member.Name))
                return;

            BuildMember(sb, "    ", member.FieldIndex, member.Type, member.Name, member.Colume, member.Seaperator);
        }

        static void BuildMember(StringBuilder sb, string space, int idx, string type, string name, string desc, char seperator)
        {
            sb.AppendLine(space + "[ProtoBuf.ProtoMember(" + idx + "), Description(\"" + desc + "\")]");
            sb.AppendFormat(space + "public {0}{1} {2};", type, (seperator != 0) ? "[]" : "", name).AppendLine();
        }

        static void GetKeyFuncAndItem(Table table, out string keystr, out string itemFunc)
        {
            Member key1, key2;
            table.GetKeys(out key1, out key2);

            keystr = string.Format("item.{0}", key1.Name);
            itemFunc = "";
            if (key2 != null)
            {
                keystr = string.Format("((Int64)item.{0} << 32) + (Int64)item.{1}", key1.Name, key2.Name);
                itemFunc = string.Format("    public {0} GetItem({1} key1, {2} key2) {{ return GetItem(((Int64)key1 << 32) + (Int64)key2); }}\r\n", table.Name, key1.Type, key2.Type);
            }
        }

        static void BuildTableManager(StringBuilder sb, Table table)
        {
            string keystr, itemFunc;
            GetKeyFuncAndItem(table, out keystr, out itemFunc);

            sb.AppendFormat("public class {0}Manager : TableManager<{0}, {0}Manager>\r\n", table.Name);
            sb.AppendLine("{");
            sb.AppendFormat("    public override Int64 Key({0} item) {{ return {1}; }}\r\n", table.Name, keystr);
            sb.AppendFormat("{0}", itemFunc);
            sb.AppendLine("}");

        }
    }
}

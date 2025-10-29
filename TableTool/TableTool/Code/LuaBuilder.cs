using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TableTool.Data;

namespace TableTool.Code
{
    public class LuaBuilder : Builder
    {
        public override string Type { get { return "lua"; } }
        private readonly int MinDefaultNum = 5;

        public override string Build(Table table, object mainObj)
        {
            var mainType = mainObj.GetType();
            var itemsField = mainType.GetField(Constants.ItemsField);
            var itemArray = itemsField.GetValue(mainObj) as Array;
            var keyFunc = mainType.GetMethod("Key");
            var memberDefaultValues = BuildDefault(table, itemArray);

            var sb = new StringBuilder();
            {
                sb.AppendLine($"-- {Constants.CopyrightComment}");
                sb.AppendLine();

                // 生成lua的默认值表格
                if (memberDefaultValues.Count > 0)
                    sb.AppendLine($"local {table.Name}Default = {GenerateDefault(table, memberDefaultValues)}");

                // 生成lua里面的表格。
                sb.AppendLine("local " + table.Name + "= {");
                foreach (var item in itemArray)
                {
                    var key = keyFunc.Invoke(null, new object[] { item }).ToString();
                    sb.Append("  [" + key + "]" + "=");
                    Convert(table, sb, item, memberDefaultValues).AppendLine(",");
                }
                sb.AppendLine("}");
                if (memberDefaultValues.Count > 0)
                {
                    sb.AppendLine($"for k,v in pairs({table.Name}) do");
                    sb.AppendLine($"    setmetatable(v, {{['__index'] = {table.Name}Default}})");
                    sb.AppendLine("end");
                }
                sb.AppendLine();
                sb.AppendLine("-- export table: " + table.Name);
                sb.AppendLine("return " + table.Name);
            }

            return sb.ToString();
        }

        /// <summary>
        /// 生成默认项
        /// </summary>
        /// <param name="members"></param>
        /// <returns></returns>
        Dictionary<Member, string> BuildDefault(Table table, Array itemArray)
        {
            // 统计出现最多次数的数值为默认值，从而减小输出表格大小。
            var itemType = itemArray.GetType().GetElementType();
            var memberDefaultValues = new Dictionary<Member, string>();
            table.Members.ForEach(member =>
            {
                var fieldInfo = itemType.GetField(member.Name);
                if (fieldInfo.FieldType != typeof(string) && (fieldInfo.FieldType.IsClass || fieldInfo.FieldType.IsArray))
                    return; // 跳过子类型和数组

                var valueCount = new Dictionary<object, int>();
                var nullCount = 0;
                foreach (var item in itemArray)
                {
                    var fieldValue = fieldInfo.GetValue(item);
                    if (fieldValue == null)
                    {
                        nullCount++;
                        continue;
                    }
                    if (!valueCount.TryGetValue(fieldValue, out var itemCount))
                        valueCount[fieldValue] = 1;
                    else
                        valueCount[fieldValue] = itemCount + 1;
                }
                if (valueCount.Count == 0)
                {
                    if (nullCount >= MinDefaultNum) memberDefaultValues[member] = null;
                    return;
                }
                // 获取最多的项
                var maxItemCount = valueCount.Max(x => x.Value);
                if (maxItemCount < MinDefaultNum)
                {
                    if (nullCount >= MinDefaultNum) memberDefaultValues[member] = null;
                    return;
                }

                if (nullCount >= maxItemCount)
                    memberDefaultValues[member] = null;
                else
                    memberDefaultValues[member] = valueCount.First(x => x.Value == maxItemCount).Key?.ToString();
            });
            return memberDefaultValues;
        }

        string GenerateDefault(Table table, Dictionary<Member, string> memberDefaultValues)
        {
            var sb = new StringBuilder();
            sb.Append("{");
            memberDefaultValues.ToList().ForEach(memberDefault =>
            {
                var member = memberDefault.Key;
                var defaultValue = memberDefault.Value;
                if (member.IsArray()) return; // 跳过数组
                // 如果是字符串，需要添加双引号
                if (member.Type == "String") defaultValue = $"\"{defaultValue}\"";
                // LUA里面的Boolean是小写开头true&false（话说只有傻逼才是大写！）
                if (member.Type == "Boolean") defaultValue = defaultValue.ToLower();
                var subType = table.FindSubClasss(member.Type);
                if (subType != null)
                {
                    // 如果是枚举，需要添加前缀
                    if (subType.Enum) defaultValue = $"{subType.Name}.{defaultValue}";
                    else return; // 跳过子类型
                }
                sb.Append($"{member.Name}={defaultValue},");
            });
            sb.Append("}");
            return sb.ToString();
        }

        /// <summary>
        /// convert item
        /// </summary>
        /// <param name="table"></param>
        /// <param name="sb"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        protected virtual StringBuilder Convert(Table table, StringBuilder sb, object obj, Dictionary<Member, string> mMemberDefaultValues = null)
        {
            if (obj == null)
                return sb.Append("nil");

            var type = obj.GetType();
            if (type.IsValueType || type.IsEnum)
            {
                var value = obj.ToString();

                // LUA里面的bool是小写。
                if (type == typeof(bool))
                    value = value.ToLower();

                return sb.Append(value);
            }

            if (type == typeof(string))
                return PutString(sb, (string)obj);

            if (type.IsArray)
            {
                var array = obj as Array;
                sb.Append('{');
                foreach (var item in array)
                    Convert(table, sb, item).Append(',');
                return sb.Append('}');
            }

            sb.Append("{");

            var members = table.Name != type.Name ?
                table.FindSubClasss(type.Name).Members :
                table.Members.Cast<Member>().ToList();
            members.ForEach(member =>
            {
                var field = type.GetField(member.Name);
                var value = field?.GetValue(obj);

                // 值类型（包含字符串）需要检查默认值
                if (!field.FieldType.IsArray &&
                    mMemberDefaultValues != null &&
                    mMemberDefaultValues.TryGetValue(member, out var defaultValue) &&
                    value?.ToString() == defaultValue)
                    return;

                // 跳过空值。
                if (field.FieldType != typeof(string) && value == null) return;

                // 设置变量
                sb.Append(field.Name).Append('=');
                Convert(table, sb, value).Append(',');
            });
            sb.Append("}");
            return sb;
        }

        private StringBuilder PutString(StringBuilder sb, string str)
        {
            sb.Append('"');

            var n = str.Length;
            for (var i = 0; i < n; i++)
            {
                switch (str[i])
                {
                    case '\n':
                        sb.Append("\\n");
                        continue;
                    case '\r':
                        sb.Append("\\r");
                        continue;

                    case '\t':
                        sb.Append("\\t");
                        continue;
                    case '"':
                        //case '\\': // 这里如果是字符串里面故意有\, 原样输出\
                        sb.Append('\\');
                        sb.Append(str[i]);
                        continue;
                    case '\f':
                        sb.Append("\\f");
                        continue;
                    case '\b':
                        sb.Append("\\b");
                        continue;
                }
                sb.Append(str[i]);
            }
            return sb.Append('"');
        }
    }
}

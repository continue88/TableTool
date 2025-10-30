using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TableTool.Data;
using TableTool.Code;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.Reflection;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace TableTool.Helper
{
    public class CodeBuilder
    {
        Dictionary<string, Builder> mBuilders = new Dictionary<string, Builder>();
        Builder mCurrentBuilder = null;
        Table mBuildTable = null;
        int mStringNum = 0;
        int mUniqueNum = 0;

        class MemberInfo
        {
            public Member member;
            public int colume;
            public FieldInfo field;
        }

        public int StringNum { get { return mStringNum; } }
        public int UniqueNum { get { return mUniqueNum; } }

        public CodeBuilder()
        {
            Register(new CSharpBuilder());
            Register(new GoBuilder());
            Register(new LuaBuilder());
        }

        void Register(Builder builder)
        {
            mBuilders[builder.Type] = builder;
        }

        public void Reset()
        {
            mStringNum = 0;
            mUniqueNum = 0;
        }

        public void Build(string type, Table table, bool build_data, bool build_proto, bool build_code, Func<string, object[,]> dataSource, out string code, out string proto, out object mainObj)
        {
            code = "";
            proto = "";
            mainObj = null;

            var compiledType = CompileTable(table, type);

            if (build_data) mainObj = BuildData(compiledType, dataSource);
            if (build_proto) proto = BuildProto(compiledType);
            if (build_code) code = BuildCode(type, mainObj);
        }

        public Type CompileTable(Table table, string code)
        {
            mBuildTable = table;
            mCurrentBuilder = mBuilders[code];

            var genCode = mCurrentBuilder.BuildProtoGen(mBuildTable);
            var results = CompileSource(genCode);
            var compiledType = results.CompiledAssembly.GetType(mBuildTable.Name + "Array");
            if (compiledType == null)
                throw new Exception("Compiled failed.");
            return compiledType;
        }

        CompilerResults CompileSource(string code)
        {
            var codeProvider = new CSharpCodeProvider();
            var parameters = new CompilerParameters();
            parameters.GenerateInMemory = true;
            parameters.GenerateExecutable = false;
            parameters.OutputAssembly = "Runtime";
            parameters.ReferencedAssemblies.Add("System.dll");
            parameters.ReferencedAssemblies.Add(Path.Combine(Application.StartupPath, "PokUtility.dll"));
            var results = codeProvider.CompileAssemblyFromSource(parameters, code);
            if (results.Errors.Count > 0)
            {
                string compilerError = "";
                foreach (CompilerError CompErr in results.Errors)
                {
                    compilerError += "Line number " + CompErr.Line +
                                ", Error Number: " + CompErr.ErrorNumber +
                                ", '" + CompErr.ErrorText + ";" +
                                Environment.NewLine + Environment.NewLine;
                }

                throw new Exception(compilerError);
            }
            return results;
        }

        object BuildData(Type mainType, Func<string, object[,]> dataSource)
        {
            // create the main object.
            var mainObject = Activator.CreateInstance(mainType);

            // read the data out.
            var excelData = dataSource(mBuildTable.Name);

            // get the effect items count.
            var skipRows = 2;
            var itemsCount = excelData.GetLength(0) - skipRows;
            while (itemsCount > 0)
            {
                var cell = excelData[itemsCount + skipRows, 1];
                if (cell != null && cell.ToString().Length > 0)
                    break;
                itemsCount--;
            }

            // create the array object.
            var itemsInfo = mainType.GetField(Constants.ItemsField);
            var arrayObject = Activator.CreateInstance(itemsInfo.FieldType, itemsCount) as Array;
            itemsInfo.SetValue(mainObject, arrayObject);

            // get the item type.
            var itemType = itemsInfo.FieldType.GetElementType();
            var activeMembers = new List<MemberInfo>();
            foreach (var member in mBuildTable.Members)
            {
                if (string.IsNullOrEmpty(member.Colume) ||
                    string.IsNullOrEmpty(member.Name) ||
                    string.IsNullOrEmpty(member.Type))
                    continue;

                var columeIndex = 0;
                for (var i = 1; i <= excelData.GetLength(1); i++)
                {
                    //由于翻译的关系，中文能会被其他替换掉，所以修改为比较属性名。
                    //var cell = excelData[1, i];
                    //if (cell == null) continue;
                    //if (cell.ToString() == member.Colume)
                    //{
                    //    columeIndex = i;
                    //    break;
                    //}
                    var cell = excelData[2, i];
                    if (cell == null) continue;
                    if (cell.ToString() == member.Name)
                    {
                        columeIndex = i;
                        break;
                    }
                }

                if (columeIndex == 0)
                    throw new Exception(string.Format("Colume {0} not found in excel file: {1}", member.Colume, mBuildTable.Name));

                var fieldInfo = itemType.GetField(member.Name);
                var memberInfo = new MemberInfo();
                memberInfo.member = member;
                memberInfo.colume = columeIndex;
                memberInfo.field = fieldInfo;
                activeMembers.Add(memberInfo);
            }

            // build string pool
            var stringPool = new List<string>();
            stringPool.Add(string.Empty); // the default [index=0] is empty.

            // read the data out.
            for (var i = 0; i < itemsCount; i++)
            {
                var itemObj = Activator.CreateInstance(itemType);
                arrayObject.SetValue(itemObj, i);

                int row = i + 1 + skipRows;
                foreach (var memberInfo in activeMembers)
                {
                    var value = excelData[row, memberInfo.colume];
                    var strValue = (value != null) ? value.ToString() : "";
                    if (string.IsNullOrEmpty(strValue))
                        continue;

                    try
                    {
                        var targetValue = ConvertValue(strValue, memberInfo.field.FieldType, memberInfo.member.Seaperator);
                        if (targetValue == null)
                            continue;

                        memberInfo.field.SetValue(itemObj, targetValue);
                    }
                    catch (Exception ex)
                    {
                        var err = "Cast fail:\n";
                        err += "Table: " + mBuildTable.Name + ".xlsx\n";
                        err += "Exception: " + ex.Message + "\n";
                        err += "Value: " + strValue + "\n";
                        err += "Type: " + memberInfo.field.FieldType.ToString() + "\n";
                        err += "Row: " + row + "\n";
                        err += "Colume: " + excelData[1, memberInfo.colume];
                        throw new Exception(err);
                    }
                }
            }

            // update the string pool value.
            var stringPoolInfo = mainType.GetField("StringPool");
            if (stringPoolInfo != null)
                stringPoolInfo.SetValue(mainObject, stringPool.ToArray());

            // 对打表出来的数组进行排序。
            try
            {
                mainType.InvokeMember(
                    "Sort",
                    BindingFlags.Default | BindingFlags.InvokeMethod,
                    null,
                    mainObject,
                    null);
            }
            catch (Exception ex)
            {
                var message = ex.InnerException.InnerException.Message;
                throw new Exception(message);
            }

            return mainObject;
        }

        object ConvertValue(string strValue, Type targetType, char seaperator)
        {
            // this is array.
            if (targetType.IsArray && seaperator != 0)
            {
                var items = strValue.Split(seaperator);
                var arrayObject = Activator.CreateInstance(targetType, items.Length) as Array;
                var itemType = targetType.GetElementType();
                for (var i = 0; i < items.Length; i++)
                    arrayObject.SetValue(ConvertValue(items[i], itemType, (char)0), i);

                return arrayObject;
            }

            // not a simple type.
            if (!Enum.GetNames(typeof(MemberType)).Any(x => targetType.Name == x))
            {
                var targetClass = mBuildTable.Classes.Find(x => x.Name == targetType.Name);
                if (targetClass != null)
                {
                    var items = strValue.Split(targetClass.Seaperator);
                    var targetObj = Activator.CreateInstance(targetType);
                    for (var i = 0; i < items.Length && i < targetClass.Members.Count; i++)
                    {
                        var subStrValue = items[i];
                        if (string.IsNullOrEmpty(subStrValue))
                            continue;

                        var member = targetClass.Members[i];
                        var field = targetType.GetField(member.Name);
                        var value = ConvertValue(subStrValue, field.FieldType, member.Seaperator);
                        field.SetValue(targetObj, value);
                    }
                    return targetObj;
                }
            }

            if (targetType == typeof(bool))
            {
                if (strValue == "√" || strValue == "1" || strValue == "True") return true;
                if (strValue == "×" || strValue == "0" || strValue == "False") return false;
            }

            return Convert.ChangeType(strValue, targetType);
        }

        string BuildProto(Type compiledType)
        {
            var proto = ProtoBuf.Serializer.GetProto(compiledType);

            mBuildTable.Classes.ForEach(x => proto = Regex.Replace(proto, @"\b" + x.Name + @"\b", mBuildTable.Name + "_" + x.Name));

            Member key1, key2;
            mBuildTable.GetKeys(out key1, out key2);

            var comment = string.Format(@"// auto generated by TableTool v1.0, copyright PokGame@2014
package table;

import ""github.com/gogo/protobuf/gogoproto/gogo.proto"";

option (gogoproto.sizer_all) = true;
option (gogoproto.marshaler_all) = true;
option (gogoproto.unmarshaler_all) = true;

// Excel: {0}.xlsx, Key: {1}", mBuildTable.Name, key1.Name);

            //var comment = string.Format("package table;\r\n\r\nimport \"code.google.com/p/gogoprotobuf/gogoproto/gogo.proto\";\r\n\r\n// Excel: {0}.xlsx, Key: {1}", mBuildTable.Name, key1.Name);
            if (null != key2)
                comment = string.Format("{0} + {1}", comment, key2.Name);
            return comment + proto;
        }

        string BuildCode(string type, object mainObj)
        {
            if (!mBuilders.ContainsKey(type))
                throw new Exception("Language not supported now.");

            var builder = mBuilders[type];
            return builder.Build(mBuildTable, mainObj);
        }
    }
}

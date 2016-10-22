using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using TableTool.Data;
using System.IO;
using System.Reflection;

namespace TableTool
{
    public partial class TableReaderForm : Form
    {
        public TableReaderForm(string fileName)
        {
            InitializeComponent();

            Text = fileName;

            // find the table.
            var key = Path.GetFileNameWithoutExtension(fileName);
            var table = MainForm.Instance.TableStore.Tables[key];
            var type = MainForm.Instance.CodeBuilder.CompileTable(table, "cs");

            // load the data.
            var data = File.ReadAllBytes(fileName);
            var obj = TryToParse(type, data);

            // extract the array.
            var itemArrayType = type.GetFields()[0];
            var itemArray = itemArrayType.GetValue(obj) as Array;
            var itemType = itemArrayType.FieldType.GetElementType();

            // build the columns.
            var memberFieldInfos = new Dictionary<Member, FieldInfo>();
            listView1.Columns.Add("Index");
            foreach (var member in table.Members)
            {
                listView1.Columns.Add(member.Colume);
                memberFieldInfos.Add(member, itemType.GetField(member.Name));
            }

            // fill the list view.
            foreach (var item in itemArray)
            {
                var listViewItem = listView1.Items.Add((listView1.Items.Count + 1).ToString());
                foreach (var memberFieldInfo in memberFieldInfos)
                {
                    var fieldInfo = memberFieldInfo.Value;
                    if (fieldInfo == null)
                        continue;

                    var fieldValue = fieldInfo.GetValue(item);
                    var value = ConvertString(fieldInfo.FieldType, fieldValue, memberFieldInfo.Key.Seaperator);
                    listViewItem.SubItems.Add(value);
                }
            }
        }

        string ConvertString(Type fieldType, object fieldValue, char seaperator)
        {
            if (fieldValue == null)
                return string.Empty;

            if (fieldType.IsArray)
            {
                var str = "";
                var array = fieldValue as Array;
                foreach (var obj in array)
                    str += obj.ToString() + seaperator;
                return str.Substring(0, str.Length - 1);
            }

            return fieldValue.ToString();
        }

        object TryToParse(Type type, byte[] data)
        {
            try
            {
                var stream = new MemoryStream(data);
                var obj = ProtoBuf.Meta.RuntimeTypeModel.Default.Deserialize(stream, null, type);
                if (stream.Position != stream.Length)
                    throw new Exception("Not enough data readed.");
                return obj;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Try to decode this data: " + ex.Message);
            }

            byte code = data[data.Length - 1];
            for (int i = 0; i < data.Length - 1; i++)
                data[i] ^= code;
            data = Zlib.DeCompress(data);
            return ProtoBuf.Meta.RuntimeTypeModel.Default.Deserialize(new MemoryStream(data), null, type);
        }
    }
}

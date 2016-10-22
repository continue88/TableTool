using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using TableTool.Code;
using TableTool.Data;
using TableTool.Helper;
using PokUtility;
using System.Windows.Forms;

namespace TableTool
{
    public class CommandProcess
    {
        const string ConfigFile = "config.json";
        const string ClientDir = "client";
        const string ServerDir = "server";

        public void Execute(string[] args)
        {
            try
            {
                foreach (var arg in args)
                {
                    if (arg.EndsWith(".xlsx"))
                        ProcessExcel(arg);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n" + ex.StackTrace, "发现错误！！！", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            MessageBox.Show("恭喜，打包完成！！！");
        }

        void ProcessExcel(string excelFile)
        {
            var fileInfo = new FileInfo(excelFile);
            var excelKey = Path.GetFileNameWithoutExtension(excelFile);

            var configFiles = fileInfo.Directory.GetFiles(ConfigFile);
            if (configFiles.Length == 0)
                throw new Exception("Config.json文件没有找到，请确认和excel放在同一个目录下。");

            var configStr = File.ReadAllText(configFiles[0].FullName, Encoding.UTF8);
            var tableStore = LitJson.JsonMapper.ToObject<TableStore>(configStr, false);
            if (!tableStore.Tables.ContainsKey(excelKey))
                throw new Exception("配置文件里面找不到此文件的配置信息：" + excelKey);

            var table = tableStore.Tables[excelKey];

            BuildTable(excelFile, table, Path.Combine(fileInfo.Directory.FullName, ClientDir), true);

            BuildTable(excelFile, table, Path.Combine(fileInfo.Directory.FullName, ServerDir), false);
        }

        void BuildTable(string excelFile, Table table, string outputPath, bool encode)
        {
            if (!Directory.Exists(outputPath))
                Directory.CreateDirectory(outputPath);

            string code;
            string proto;
            object mainObject;

            var codeBuilder = new CodeBuilder();
            codeBuilder.Build("cs", table, true, false, false, name =>
                {
                    return ExcelHelper.ReadExcel(excelFile);
                },
                out code,
                out proto, 
                out mainObject);

            var path = Path.Combine(outputPath, table.Name + ".bytes");
            using (var memory = new MemoryStream())
            {
                ProtoBuf.Meta.RuntimeTypeModel.Default.Serialize(memory, mainObject);
                var buffer = memory.ToArray();
                if (encode)
                {
                    buffer = Zlib.Compress(buffer);
                    byte key = buffer[buffer.Length - 1];
                    for (int i = 0; i < buffer.Length - 1; i++)
                        buffer[i] ^= key;
                }

                File.WriteAllBytes(path, buffer);
            }
        }
    }
}

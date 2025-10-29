using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TableTool.Data
{
    class UserConfig
    {
        public const string PathCategory = "路径设置";

        public static UserConfig Instance { private set; get; }

        public string WorkPath { get; set; }
        public string DataPath { get; set; }
        public int BuildLanguage { get; set; }
        public string ClientCodePath { get; set; }
        public string ClientDataPath { get; set; }
        public string ServerCodePath { get; set; }
        public string ServerDataPath { get; set; }
        public string ServerProtoPath { get; set; }

        #region "Static functions"

        public static void Save()
        {
            var path = Path.Combine(Application.StartupPath, Constants.UserConfig);
            var text = LitJson.JsonMapper.ToJson(Instance);
            File.WriteAllText(path, text);
        }

        public static void Load()
        {
            var path = Path.Combine(Application.StartupPath, Constants.UserConfig);
            if (!File.Exists(path))
            {
                if (Instance == null) Instance = new UserConfig();
                Instance.WorkPath = "";
                return;
            }
            var text = File.ReadAllText(path);
            Instance = LitJson.JsonMapper.ToObject<UserConfig>(text, false);

            // 如果是相对路径，那么就设置一下启动路径为当前路径。
            if (!Path.IsPathRooted(Instance.WorkPath))
                Directory.SetCurrentDirectory(Application.StartupPath);
        }
        #endregion
    }
}

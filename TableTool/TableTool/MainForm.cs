using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using PokUtility;
using TableTool.Data;
using TableTool.Properties;
using TableTool.Controls;
using TableTool.Helper;

namespace TableTool
{
    public partial class MainForm : Form
    {
        public static MainForm Instance { get ; private set; }

        TableStore mTableStore = new TableStore();
        TablePanel mTablePanel;
        CodeBuilder mCodeBuilder;
        bool mDirty = false;

        public Table CurrentTable { get { return mTablePanel != null ? mTablePanel.Table : null; } }
        public TableStore TableStore { get { return mTableStore; } }
        public CodeBuilder CodeBuilder { get { return mCodeBuilder; } }

        public MainForm()
        {
            Instance = this;

            UserConfig.Load();

            InitializeComponent();

            mTablePanel = new TablePanel();
            mTablePanel.Dock = DockStyle.Fill;

            mCodeBuilder = new CodeBuilder();

            splitContainer1.Panel2.Controls.Add(mTablePanel);

            LoadConfigFile();

            ProcessCommandLines();
        }

        void ProcessCommandLines()
        {
            foreach (var arg in Environment.GetCommandLineArgs())
            {
                if (arg.EndsWith(".bytes"))
                    ReadTableFiles(arg);
            }
        }

        private void LoadConfigFile()
        {
            if (string.IsNullOrEmpty(UserConfig.Instance.WorkPath))
                return;

            string configText = File.ReadAllText(UserConfig.Instance.WorkPath);
            mTableStore = LitJson.JsonMapper.ToObject<TableStore>(configText, false);
            mTableStore.FixFieldIndex();

            RefreshTables();
        }

        private void LoadExcel()
        {
            listView1.Items.Clear();
            foreach (String fileName in openFileDialog1.FileNames)
            {
                ListViewItem lvItem = listView1.Items.Add((listView1.Items.Count + 1).ToString());
                lvItem.SubItems.Add(Path.GetFileNameWithoutExtension(fileName));
                lvItem.Tag = fileName;
            }
        }

        private void RefreshTables()
        {
            var path = Path.GetDirectoryName(UserConfig.Instance.WorkPath);
            var pathInfo = new DirectoryInfo(path);

            listView1.Items.Clear();
            var newKeys = new List<string>();
            foreach (var excelFile in pathInfo.GetFiles("*.xlsx"))
            {
                if ((excelFile.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                    continue;

                var fullName = excelFile.FullName;
                var key = Path.GetFileNameWithoutExtension(fullName);
                newKeys.Add(key);

                var lvItem = listView1.Items.Add(key, key, 0);
                lvItem.Name = key;
                lvItem.Tag = fullName;

                if (mTableStore.Tables.ContainsKey(key))
                    lvItem.SubItems.Add(mTableStore.Tables[key].Desc);
            }

            var oldKeys = new List<string>();
            oldKeys.AddRange(mTableStore.Tables.Keys);
            foreach (string oldKey in oldKeys)
            {
                if (newKeys.Contains(oldKey))
                    continue;
                mTableStore.Tables.Remove(oldKey);
            }
        }

        public void RefreshTable(Table table)
        {
            var idx = listView1.Items.IndexOfKey(table.Name);
            if (idx >= 0)
            {
                var tableItem = listView1.Items[idx];
                tableItem.SubItems[1].Text = table.Desc;
            }
        }

        /// <summary>
        /// build the init table info.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private Table BuildInitTableInfo(string name, string fileName)
        {
            var oldTable = (Table)null;
            mTableStore.Tables.TryGetValue(name, out oldTable);

            var tableInfo = new Table();
            tableInfo.Name = name;

            if (oldTable != null)
            {
                tableInfo.Desc = oldTable.Desc;
                tableInfo.Classes = oldTable.Classes;
            }

            var excelCeils = ExcelHelper.ReadExcel(fileName, name);
            for (var columIndex = 0; columIndex < excelCeils.GetLength(1); columIndex++)
            {
                var cell = excelCeils[1, columIndex + 1];
                var text = cell != null ? cell.ToString() : "";
                if (string.IsNullOrEmpty(text))
                    continue;

                var nameText = (excelCeils[2, columIndex + 1] != null) ? 
                    excelCeils[2, columIndex + 1].ToString() : "";

                var newMember = new ExcelMember(text);
                if (oldTable != null)
                {
                    var oldMember = oldTable.Members.Find(x => { return x.Colume == text; });
                    if (oldMember != null)
                        newMember = ObjectHelper.Clone(oldMember);
                }

                newMember.Name = nameText.Trim();
                newMember.FieldIndex = tableInfo.Members.Count + 1;

                if (tableInfo.Members.Count == 0)
                    newMember.Key = true;

                tableInfo.Members.Add(newMember);
            }
            return tableInfo;
        }

        public object[,] ReadExcelData(string find)
        {
            foreach (ListViewItem lvItem in listView1.Items)
            {
                string fullPath = lvItem.Tag.ToString();
                string key = Path.GetFileNameWithoutExtension(fullPath);
                if (key == find)
                    return ExcelHelper.ReadExcel(fullPath, key);
            }
            return null;
        }

        void BuildSelected(string type, string save_path, bool build_data, bool build_proto, bool build_code, bool encode)
        {
            // find the selected tables.
            var tables = new List<Table>();
            foreach (ListViewItem lvItem in listView1.SelectedItems)
            {
                var key = Path.GetFileNameWithoutExtension(lvItem.Tag.ToString());
                if (!mTableStore.Tables.ContainsKey(key))
                    continue;

                tables.Add(mTableStore.Tables[key]);
            }

            try
            {
                mCodeBuilder.Reset();
                foreach (var table in tables)
                {
                    string code;
                    string proto;
                    object mainObject;
                    mCodeBuilder.Build(type, table, build_data, build_proto, build_code, ReadExcelData, out code, out proto, out mainObject);

                    if (build_data)
                    {
                        string path = save_path + "\\" + table.Name + ".bytes";
                        using (var memory = new MemoryStream())
                        {
                            ProtoBuf.Meta.RuntimeTypeModel.Default.Serialize(memory, mainObject);
                            byte[] buffer = memory.ToArray();
                            if (encode)
                            {
                                buffer = Zlib.Compress(buffer);
                                byte key = buffer[buffer.Length - 1];
                                for (int i = 0; i < buffer.Length - 1; i++)
                                    buffer[i] ^= key;
                                File.WriteAllBytes(path, buffer);
                            }
                            else
                            {
                                File.WriteAllBytes(path, buffer);
                            }
                        }
                    }

                    if (build_proto)
                    {
                        string path = save_path + "\\" + table.Name + ".proto";
                        File.WriteAllText(path, proto);
                    }

                    if (build_code)
                    {
                        string path = save_path + "\\" + table.Name + "Manager." + type;
                        File.WriteAllText(path, code, Encoding.UTF8);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Build Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void ReadTableFiles(params string[] fileNames)
        {
            var currentFile = string.Empty;
            try
            {
                foreach (var fileName in fileNames)
                {
                    currentFile = fileName;

                    var tableReader = new TableReaderForm(fileName);
                    tableReader.Show(this);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Fail to load data, file:" + currentFile, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void DirtyFalg(bool dirty)
        {
            mDirty = dirty;
        }

        #region "Events"
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (mDirty)
            {
                DialogResult result = MessageBox.Show(
                    "Do you want to save?",
                    "Data has been changed.",
                    MessageBoxButtons.YesNoCancel);
                if (result == DialogResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }

                if (result == DialogResult.Yes)
                    onSaveClicked(sender, e);
            }
        }

        private void onOpenFileClicked(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() != DialogResult.OK)
                return;

            UserConfig.Instance.WorkPath = openFileDialog1.FileName;
            UserConfig.Save();

            LoadConfigFile();
        }

        private void onSaveClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(UserConfig.Instance.WorkPath))
            {
                if (saveFileDialog1.ShowDialog() != DialogResult.OK)
                    return;
                UserConfig.Instance.WorkPath = saveFileDialog1.FileName;
            }

            using (StreamWriter streamWriter = new StreamWriter(UserConfig.Instance.WorkPath))
            {
                var writer = new LitJson.JsonWriter();
                writer.PrettyPrint = true;
                LitJson.JsonMapper.ToJson(mTableStore, writer);
                string config = writer.ToString();
                streamWriter.Write(config);
            }

            UserConfig.Save();

            mDirty = false;
        }

        private void onTableFileSelected(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0)
                return;

            string fullPath = listView1.SelectedItems[0].Tag.ToString();
            string key = Path.GetFileNameWithoutExtension(fullPath);
            if (!mTableStore.Tables.ContainsKey(key))
            {
                mTableStore.Tables[key] = BuildInitTableInfo(key, fullPath);
                mTableStore.FixFieldIndex();
                mDirty = true;
            }

            var tableInfo = mTableStore.Tables[key];
            mTablePanel.EditTable(tableInfo);
        }

        private void onBuildAllClientClicked(object sender, EventArgs e)
        {
            onBuildCodeCSharp(sender, e);
            onBuildDataClicked(sender, e);
        }

        private void onBuildAllServerClicked(object sender, EventArgs e)
        {
            onBuildCodeGo(sender, e);
            onBuildProtoClicked(sender, e);
            onBuildDataServerClicked(sender, e);
        }

        private void onBuildDataClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(UserConfig.Instance.ClientDataPath))
            {
                if (saveFileDialog1.ShowDialog() != DialogResult.OK)
                    return;
                UserConfig.Instance.ClientDataPath = Path.GetDirectoryName(saveFileDialog1.FileName);
            }
            BuildSelected("cs", UserConfig.Instance.ClientDataPath, true, false, false, true);
        }

        private void onBuildDataServerClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(UserConfig.Instance.ServerDataPath))
            {
                if (saveFileDialog1.ShowDialog() != DialogResult.OK)
                    return;
                UserConfig.Instance.ServerDataPath = Path.GetDirectoryName(saveFileDialog1.FileName);
            }
            BuildSelected("go", UserConfig.Instance.ServerDataPath, true, false, false, false);
        }

        private void onBuildProtoClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(UserConfig.Instance.ServerProtoPath))
            {
                if (saveFileDialog1.ShowDialog() != DialogResult.OK)
                    return;
                UserConfig.Instance.ServerProtoPath = Path.GetDirectoryName(saveFileDialog1.FileName);
            }
            BuildSelected("go", UserConfig.Instance.ServerProtoPath, false, true, false, false);
        }

        private void onBuildCodeCSharp(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(UserConfig.Instance.ClientCodePath))
            {
                if (saveFileDialog1.ShowDialog() != DialogResult.OK)
                    return;
                UserConfig.Instance.ClientCodePath = Path.GetDirectoryName(saveFileDialog1.FileName);
            }
            BuildSelected("cs", UserConfig.Instance.ClientCodePath, false, false, true, false);
        }

        private void onBuildCodeGo(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(UserConfig.Instance.ServerCodePath))
            {
                if (saveFileDialog1.ShowDialog() != DialogResult.OK)
                    return;
                UserConfig.Instance.ServerCodePath = Path.GetDirectoryName(saveFileDialog1.FileName);
            }
            BuildSelected("go", UserConfig.Instance.ServerCodePath, false, false, true, false);
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0)
                return;
            string fileName = listView1.SelectedItems[0].Tag.ToString();
            System.Diagnostics.Process.Start(fileName);
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            var key = "";
            var items = (listView1.SelectedItems.Count > 0) ? listView1.SelectedItems.Cast<ListViewItem>() : listView1.Items.Cast<ListViewItem>();
            items.ToList().ForEach(item =>
            {
                var fullPath = item.Tag.ToString();
                key = Path.GetFileNameWithoutExtension(fullPath);
                mTableStore.Tables[key] = BuildInitTableInfo(key, fullPath);
            });

            RefreshTables();

            for (var i = 0; i < listView1.Items.Count; i++)
            {
                if (key == listView1.Items[i].Name)
                    listView1.Items[i].Selected = true;
            }

            mTableStore.FixFieldIndex();
            mDirty = true;
        }

        private void onSettingsClicked(object sender, EventArgs e)
        {
            var dlg = new InputValueDlg("Settings", UserConfig.Instance);
            if (dlg.ShowDialog() == DialogResult.OK)
                UserConfig.Save();
        }

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            openFileDialog1.FileName = "*.bytes";
            if (openFileDialog1.ShowDialog() != DialogResult.OK)
                return;

            ReadTableFiles(openFileDialog1.FileNames);
        }

        private void onTableFileDragEnter(object sender, DragEventArgs e)
        {
            var allowDrop = false;
            if (e.Data.GetDataPresent(DataFormats.FileDrop, true))
            {
                foreach (var file in (string[])e.Data.GetData(DataFormats.FileDrop))
                {
                    if (file.EndsWith(".bytes"))
                    {
                        allowDrop = true;
                        break;
                    }
                }
            }

            if (allowDrop)
                e.Effect = DragDropEffects.All;
        }

        private void onTableFileDragDrop(object sender, DragEventArgs e)
        {
            ReadTableFiles(e.Data.GetData(DataFormats.FileDrop) as string[]);
        }

        private void readDataClientToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var files = listView1.SelectedItems.Cast<ListViewItem>().Select(item =>
            {
                var fullPath = item.Tag.ToString();
                var key = Path.GetFileNameWithoutExtension(fullPath);
                return Path.Combine(UserConfig.Instance.ClientDataPath, key + ".bytes");
            }).ToArray();
            ReadTableFiles(files);
        }

        private void readDataServerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var files = listView1.SelectedItems.Cast<ListViewItem>().Select(item =>
            {
                var fullPath = item.Tag.ToString();
                var key = Path.GetFileNameWithoutExtension(fullPath);
                return Path.Combine(UserConfig.Instance.ServerDataPath, key + ".bytes");
            }).ToArray();
            ReadTableFiles(files);
        }

        private void buildLUAToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog() != DialogResult.OK)
                return;
            var savePath = Path.GetDirectoryName(saveFileDialog1.FileName);
            BuildSelected("lua", savePath, true, false, true, false);
        }

        private void userSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var dlg = new InputValueDlg("User Settings", UserConfig.Instance);
            if (dlg.ShowDialog() == DialogResult.OK)
                UserConfig.Save();
        }

        private void projectSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var dlg = new InputValueDlg("Project settings", mTableStore);
            if (dlg.ShowDialog() == DialogResult.OK)
                UserConfig.Save();
        }

        #endregion
    }
}

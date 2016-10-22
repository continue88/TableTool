using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Reflection;

namespace PokUtility
{
    public class ListViewWrapper<T>
    {
        public delegate void ShowDataProc();
        public delegate T CreateObjectProc();

        ListView mListView;
        List<T> mObjectList;
        Object mOwner;
        String mName;
        EventHandler mDoubleClickHandler;
        PreviewKeyDownEventHandler mPreviewKeyDownEventHandler;
        ShowDataProc mShowDataProc;
        CreateObjectProc mCreateObjectProc;

        public void UpdateOwner(Object owner) { mOwner = owner; }
        public void UpdateList(List<T> list) { mObjectList = list; ShowData(); }

        public ListViewWrapper(ListView listView)
        {
            Init(listView, null, null, null, null, null);
        }

        public ListViewWrapper(
            ListView listView, String name, ShowDataProc showDataProc,
            CreateObjectProc createObjectProc)
        {
            Init(listView, null, null, name, showDataProc, createObjectProc);
        }

        public ListViewWrapper(
            ListView listView, 
            List<T> objectList,
            Object owner, String name,
            ShowDataProc showDataProc,
            CreateObjectProc createObjectProc)
        {
            Init(listView, objectList, owner, name, showDataProc, createObjectProc);
        }

        void Init(
            ListView listView,
            List<T> objectList,
            Object owner, String name,
            ShowDataProc showDataProc,
            CreateObjectProc createObjectProc)
        {
            mListView = listView;
            mObjectList = objectList;
            mOwner = owner;
            mName = name;
            mShowDataProc = showDataProc;
            mCreateObjectProc = createObjectProc;

            mDoubleClickHandler = new EventHandler(listView_DoubleClick);
            mPreviewKeyDownEventHandler = new PreviewKeyDownEventHandler(listView_PreviewKeyDown);

            listView.DoubleClick += mDoubleClickHandler;
            listView.PreviewKeyDown += mPreviewKeyDownEventHandler;

            if (mShowDataProc == null)
            {
                mListView.Columns.Clear();
                mListView.Columns.Add("Index");
                
                foreach (var propertyInfo in typeof(T).GetProperties())
                    mListView.Columns.Add(propertyInfo.Name).Tag = propertyInfo;
            }
        }

        void listView_DoubleClick(object sender, EventArgs e)
        {
            if (mListView.SelectedItems.Count > 0)
                EditObject();
            else
                EditObjectList();
        }

        void listView_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (Control.ModifierKeys == Keys.Control)
            {
                switch (e.KeyCode)
                {
                    case Keys.C:
                        CopyObject();
                        break;
                    case Keys.V:
                        PasteObject();
                        break;
                    default:
                        break;
                }
            }
            else
            {
                switch (e.KeyCode)
                {
                    case Keys.Enter:
                        EditObject();
                        break;
                    case Keys.F3:
                        EditObjectList();
                        break;
                    case Keys.N:
                        NewObject();
                        break;
                    case Keys.Delete:
                        DeleteObject();
                        break;
                }
            }
        }

        void ShowData()
        {
            if (mShowDataProc != null)
                mShowDataProc();
            else
            {
                mListView.Items.Clear();
                foreach (T data in mObjectList)
                {
                    var lvItem = mListView.Items.Add((mListView.Items.Count + 1).ToString());
                    lvItem.Tag = data;

                    foreach (var column in mListView.Columns.Cast<ColumnHeader>())
                    {
                        if (column.Tag == null)
                            continue;

                        var propertyInfo = column.Tag as PropertyInfo;
                        var obj = propertyInfo.GetValue(data, null);
                        var text = (obj != null) ? obj.ToString() : "";
                        lvItem.SubItems.Add(text);
                    }
                }
            }
        }

        public class ObjectListContent
        {
            List<T> mDataList = new List<T>();
            public List<T> DataList { get { return mDataList; } }
        };

        public void NewObject()
        {
            if (mObjectList == null)
                return;

            T newObject = (mCreateObjectProc != null) ? mCreateObjectProc() : Activator.CreateInstance<T>();
            InputValueDlg dlg = new InputValueDlg("新建", newObject);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                mObjectList.Add(newObject);
                ShowData();
            }
        }

        void CopyObject()
        {
            if (mListView.SelectedItems.Count > 0)
            {
                ObjectListContent content = new ObjectListContent();
                foreach (ListViewItem item in mListView.SelectedItems)
                {
                    if (item.Tag != null)
                        content.DataList.Add((T)item.Tag);
                }

                if (content.DataList.Count > 0)
                    Clipboard.SetText(PropertySerializer.ObjectToString(content));

                Logger.Log("恭喜，您复制了" + content.DataList.Count.ToString() + "个劳动成果！！！");
            }
            else
                Logger.Log("您必须选择至少一个劳动成果进行复制！！！");
        }

        void PasteObject()
        {
            try
            {
                if (mObjectList == null)
                {
                    Logger.Log("错误，数据列表【mObjectList】不存在!!!");
                    return;
                }

                // convert string to stream
                ObjectListContent content =
                    (ObjectListContent)PropertySerializer.StringToObject(
                        Clipboard.GetText(), typeof(ObjectListContent));

                foreach (T data in content.DataList)
                    mObjectList.Add(data);

                ShowData();

                Logger.Log("恭喜，您粘贴了" + content.DataList.Count + "个劳动成果！！！");
            }
            catch (Exception ex)
            {
                Logger.Log("无法进行粘贴，错误信息：" + ex.Message);
            }
        }

        void DeleteObject()
        {
            if (mObjectList == null)
            {
                Logger.Log("错误，数据列表【mObjectList】不存在!!!");
                return;
            }

            if (mListView.SelectedIndices.Count == 1)
            {
                int selectIdx = mListView.SelectedIndices[0];
                T select_obj = (T)mListView.SelectedItems[0].Tag;;
                if (select_obj == null)
                {
                    Logger.Log("错误，所选择的行无对象【Tag】绑定!!!");
                    return;
                }

                mObjectList.Remove(select_obj);

                ShowData();

                if (mListView.Items.Count > selectIdx)
                    mListView.Items[selectIdx].Selected = true;

                Logger.Log("删除成功");
            }
            else if (mListView.SelectedIndices.Count == 0)
                Logger.Log("请选择一条记录进行删除");
            else
                Logger.Log("无法一次删除多个记录");
        }

        void EditObject()
        {
            if (mListView.SelectedIndices.Count == 0)
                return;

            T select_obj = (T)mListView.SelectedItems[0].Tag;
            if (select_obj == null)
                return;

            InputValueDlg dlg = new InputValueDlg("编辑单个对象", select_obj);
            if (dlg.ShowDialog() == DialogResult.OK)
                ShowData();
        }

        void EditObjectList()
        {
            if (mOwner != null)
            {
                CollectionEditor.EditValue(mListView, mOwner, mName);
                ShowData();
            }
        }

        public void RemoveDoubleClickHandler()
        {
            if (mDoubleClickHandler != null)
                mListView.DoubleClick -= mDoubleClickHandler;
            mDoubleClickHandler = null;
        }
    }
}

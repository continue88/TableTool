using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using TableTool.Data;
using PokUtility;
using TableTool.Helper;

namespace TableTool.Controls
{
    public partial class TablePanel : UserControl
    {
        Table mTable;
        CodeBuilder mCodeBuilder;
        ListViewWrapper<Member> mMemberListView;
        ListViewWrapper<SubClass> mSubClassListView;

        public Table Table { get { return mTable; } }

        public TablePanel()
        {
            InitializeComponent();

            mCodeBuilder = new CodeBuilder();
            mMemberListView = new ListViewWrapper<Member>(listViewMember);
            mSubClassListView = new ListViewWrapper<SubClass>(listViewSubClass);
        }

        public void EditTable(Table table)
        {
            mTable = table;

            mMemberListView.UpdateList(table.Members);
            mSubClassListView.UpdateList(table.Classes);

            propertyGrid1.SelectedObject = table;
        }

        private void onMemberSelected(object sender, EventArgs e)
        {
            var selectedMembers = new List<Member>();
            foreach (ListViewItem lvItem in listViewMember.SelectedItems)
                selectedMembers.Add(lvItem.Tag as Member);

            propertyGrid1.SelectedObjects = selectedMembers.ToArray();
        }

        private void onValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            if (propertyGrid1.SelectedObject == mTable)
                MainForm.Instance.RefreshTable(mTable);
            else
                mMemberListView.UpdateList(mTable.Members);
            MainForm.Instance.DirtyFalg(true);
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace PokUtility
{
    public partial class InputValueDlg : Form
    {
        public Object Value { get { return propertyGrid1.SelectedObject; } }

        public InputValueDlg(string title, object value)
        {
            InitializeComponent();

            Text = title;
            propertyGrid1.SelectedObject = value;
        }
    }


    public class IntValue
    {
        public IntValue(int v)
        {
            mValue = v;
        }

        int mValue = 0;
        public int Value { get { return mValue; } set { mValue = value; } }
    }

    public class FloatValue
    {
        public FloatValue(float v)
        {
            mValue = v;
        }

        float mValue = 0;
        public float Value { get { return mValue; } set { mValue = value; } }
    }

    public class StringValue
    {
        public StringValue(String v)
        {
            mValue = v;
        }

        String mValue = "";
        public String Value { get { return mValue; } set { mValue = value; } }
    }
}

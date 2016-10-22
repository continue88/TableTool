using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace TableTool.Helper
{
    public class MemberTypeConverter : TypeConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            var strValues = new List<string>(
                Enum.GetNames(typeof(TableTool.Data.MemberType)));
            strValues.AddRange(MainForm.Instance.CurrentTable.Classes.Select(x => x.Name));
            return new StandardValuesCollection(strValues);
        }

        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Type sourceType)
        {
            if (sourceType == typeof(string))
                return true;
            return base.CanConvertFrom(context, sourceType);
        }
           
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
        {
            if (value.GetType() == typeof(string))
                return value;
            return base.ConvertFrom(context, culture, value);
        }
    }
}

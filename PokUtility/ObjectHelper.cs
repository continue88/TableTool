using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PokUtility
{
    public static class ObjectHelper
    {
        public static T Clone<T>(T src)
        {
            string json = LitJson.JsonMapper.ToJson(src);
            return LitJson.JsonMapper.ToObject<T>(json, true);
        }
    }
}

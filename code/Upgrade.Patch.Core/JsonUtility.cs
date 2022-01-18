using System;
using System.Collections.Generic;
using System.Text;

namespace XTC.oelUpgrade
{
    public interface IJsonConvert
    {
        string ToJson<T>(T _object);

        T FromJson<T>(string _json);
    }

    public static class JsonUtility
    {
        public static IJsonConvert convert { get; set; }

        public static string ToJson<T>(T _object)
        {
            return convert.ToJson<T>(_object);
        }

        public static T FromJson<T>(string _json)
        {
            return convert.FromJson<T>(_json);
        }
    }
}

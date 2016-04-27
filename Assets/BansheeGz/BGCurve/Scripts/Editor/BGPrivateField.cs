using System;
using System.Reflection;
using BansheeGz.BGSpline.Curve;

namespace BansheeGz.BGSpline.Editor
{
    public static  class BGPrivateField
    {

        public static void SetSettings(BGCurve curve, BGCurveSettings settings)
        {
            Set(curve, "settings", settings);
        }
        public static BGCurveSettings GetSettings(BGCurve curve)
        {
            return Get<BGCurveSettings>(curve, "settings");
        }

        // == utility
        private static T Get<T>(object obj, string name)
        {
            return (T) GetField(obj, name).GetValue(obj);
        }

        private static void Set<T>(object obj, string name, T value)
        {
            GetField(obj, name).SetValue(obj, value);
        }

        private static FieldInfo GetField(object obj, string name)
        {
            var targetField = obj.GetType().GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (targetField != null) return targetField;


            var basetype = obj.GetType().BaseType;
            while (targetField == null && basetype != null && basetype != typeof(object))
            {
                targetField = basetype.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                basetype = basetype.BaseType;
            }
            return targetField;
        }

    }
}
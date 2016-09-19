using System.Reflection;
using BansheeGz.BGSpline.Curve;

namespace BansheeGz.BGSpline.Editor
{
    /// <summary> we dont want to expose Editor Only fields as public </summary>
    public static class BGPrivateField
    {
        //--------------------------------------- Settings
        public static void SetSettings(BGCurve curve, BGCurveSettings settings)
        {
            Set(curve, "settings", settings);
        }

        public static BGCurveSettings GetSettings(BGCurve curve)
        {
            return Get<BGCurveSettings>(curve, "settings");
        }

        //--------------------------------------- Cc
        public static bool GetShowHandles(BGCc cc)
        {
            return Get<bool>(cc, "showHandles");
        }

        // == utility
        public static T Get<T>(object obj, string name)
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
            while (targetField == null && basetype != null && basetype != typeof (object))
            {
                targetField = basetype.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                basetype = basetype.BaseType;
            }
            return targetField;
        }
    }
}
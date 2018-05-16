using System;
using System.Reflection;
using BansheeGz.BGSpline.Curve;
using UnityEngine;

namespace BansheeGz.BGSpline.Editor
{
    // access private members
    public static class BGPrivateField
    {
        //--------------------------------------- Settings
        public static void SetSettings(BGCurve curve, BGCurveSettings settings)
        {
            Set(curve, "settings", settings);
        }

        public static BGCurveSettings GetSettings(BGCurve curve)
        {
            return curve.Settings;
//            return Get<BGCurveSettings>(curve, "settings");
        }

        //--------------------------------------- Fields
        public static bool GetShowHandles(BGCurvePointField field)
        {
            return Get<bool>(field, "showHandles");
        }

        public static int GetHandlesType(BGCurvePointField field)
        {
            return Get<int>(field, "handlesType");
        }

        public static Color GetHandlesColor(BGCurvePointField field)
        {
            return Get<Color>(field, "handlesColor");
        }

        public static bool GetShowInPointsMenu(BGCurvePointField field)
        {
            return Get<bool>(field, "showInPointsMenu");
        }

        public static void SetShowHandles(BGCurvePointField field, bool value)
        {
            Set(field, "showHandles", value);
        }

        public static void SetHandlesType(BGCurvePointField field, int value)
        {
            Set(field, "handlesType", value);
        }

        public static void SetHandlesColor(BGCurvePointField field, Color value)
        {
            Set(field, "handlesColor", value);
        }

        public static void SetShowInPointsMenu(BGCurvePointField field, bool value)
        {
            Set(field, "showInPointsMenu", value);
        }

        //--------------------------------------- Cc
        public static bool GetShowHandles(BGCc cc)
        {
            return cc.ShowHandles;
//            return Get<bool>(cc, "showHandles");
        }

        // == utility
        public static T Get<T>(object obj, string name)
        {
            return (T) GetField(obj, name).GetValue(obj);
        }

        public static void Set<T>(object obj, string name, T value)
        {
            GetField(obj, name).SetValue(obj, value);
        }

        private static FieldInfo GetField(object obj, string name)
        {
            var isStatic = obj is Type;
            var type = isStatic ? (Type) obj : obj.GetType();

            var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            if (isStatic) bindingFlags |= BindingFlags.Static;

            var targetField = type.GetField(name, bindingFlags);

            if (targetField != null) return targetField;


            var basetype = type.BaseType;
            while (targetField == null && basetype != null && basetype != typeof(object))
            {
                targetField = basetype.GetField(name, bindingFlags);
                basetype = basetype.BaseType;
            }
            return targetField;
        }

        public static object Invoke(object obj, string methodName, params object[] parameters)
        {
            return obj.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(obj, parameters);
        }

        public static object Invoke(object obj, string methodName, Type[] types, params object[] parameters)
        {
            return obj.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance, null, types, null).Invoke(obj, parameters);
        }
    }
}
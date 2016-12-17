using System;

#if NETFX_CORE
using System.Reflection;
using System.Collections.Generic;
#endif

namespace BansheeGz.BGSpline.Curve
{
    /// <summary>Reflection related stuff </summary>
    //thanks to qwerty
    public static class BGReflectionAdapter
    {
#if NETFX_CORE
        public static object[] GetCustomAttributes(Type type, Type attributeType, bool inherit)
        {
            var enumerator = type.GetTypeInfo().GetCustomAttributes(attributeType, inherit);
            var result = new List<object>();
            foreach(var item in enumerator) result.Add(item);
            return result.ToArray();
        }

        public static bool IsAbstract(Type type)
        {
            return type.GetTypeInfo().IsAbstract;
        }

        public static bool IsClass(Type type)
        {
            return type.GetTypeInfo().IsClass;
        }

        public static bool IsSubclassOf(Type type, Type typeToCheck)
        {
            return type.GetTypeInfo().IsSubclassOf(typeToCheck);
        }

        public static bool IsValueType(Type type)
        {
            return type.GetTypeInfo().IsValueType;
        }
#else

        public static object[] GetCustomAttributes(Type type, Type attributeType, bool inherit)
        {
            return type.GetCustomAttributes(attributeType, inherit);
        }

        public static bool IsAbstract(Type type)
        {
            return type.IsAbstract;
        }

        public static bool IsClass(Type type)
        {
            return type.IsClass;
        }

        public static bool IsSubclassOf(Type type, Type typeToCheck)
        {
            return type.IsSubclassOf(typeToCheck);
        }

        public static bool IsValueType(Type type)
        {
            return type.IsValueType;
        }

#endif
    }
}
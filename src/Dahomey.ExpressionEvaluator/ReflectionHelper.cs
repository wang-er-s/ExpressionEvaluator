#region License

/* Copyright © 2017, Dahomey Technologies and Contributors
 * For conditions of distribution and use, see copyright notice in license.txt file
 */

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Dahomey.ExpressionEvaluator
{
    public static class ReflectionHelper
    {
        public static Func<T, TP> CreateDelegate<T, TP>(MemberInfo memberInfo)
        {
            switch (memberInfo)
            {
                case PropertyInfo propertyInfo:
                    return (Func<T, TP>)Delegate.CreateDelegate(typeof(Func<T, TP>), propertyInfo.GetGetMethod());
                case FieldInfo fieldInfo:
                    return (obj) => (TP)fieldInfo.GetValue(obj);
            }

            return null;
        }
        
        public static Func<TR> CreateDelegate<TR>(MethodInfo methodInfo, object target = null)
        {
            return (Func<TR>)Delegate.CreateDelegate(typeof(Func<TR>), target, methodInfo);
        }

        public static Func<T1, TR> CreateDelegate<T1, TR>(MethodInfo methodInfo, object target = null)
        {
            return (Func<T1, TR>)Delegate.CreateDelegate(typeof(Func<T1, TR>), target, methodInfo);
        }

        public static Func<T1, T2, TR> CreateDelegate<T1, T2, TR>(MethodInfo methodInfo, object target = null)
        {
            return (Func<T1, T2, TR>)Delegate.CreateDelegate(typeof(Func<T1, T2, TR>), target, methodInfo);
        }

        public static Func<T1, T2, T3, TR> CreateDelegate<T1, T2, T3, TR>(MethodInfo methodInfo, object target = null)
        {
            return (Func<T1, T2, T3, TR>)Delegate.CreateDelegate(typeof(Func<T1, T2, T3, TR>), target, methodInfo);
        }

        public static Func<T1, T2, T3, T4, TR> CreateDelegate<T1, T2, T3, T4, TR>(MethodInfo methodInfo, object target = null)
        {
            return (Func<T1, T2, T3, T4, TR>)Delegate.CreateDelegate(typeof(Func<T1, T2, T3, T4, TR>), target, methodInfo);
        }

        public static bool IsList(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>);
        }

        public static bool IsNumberList(Type type)
        {
            Type itemType;

            if (type.IsArray)
            {
                itemType = type.GetElementType();
            }
            else if (IsList(type))
            {
                itemType = type.GetGenericArguments()[0];
            }
            else
            {
                return false;
            }

            return IsNumber(itemType);
        }

        public static bool IsNumber(Type type)
        {
            return type == typeof(sbyte)
                || type == typeof(byte)
                || type == typeof(short)
                || type == typeof(ushort)
                || type == typeof(int)
                || type == typeof(uint)
                || type == typeof(long)
                || type == typeof(ulong)
                || type == typeof(float)
                || type == typeof(double)
                || type.IsEnum;
        }

        public static Type GetType(IEnumerable<Assembly> assemblies, string name)
        {
            return assemblies
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.Name == name);
        }

        public static Func<T, double> GenerateConverter<T>()
        {
            string methodName;

            if (typeof(T).IsEnum)
            {
                // Hack: we force cast Func<int, double> to Func<TEnum, double>
                methodName = "Int32ToDouble";
            }
            else if (typeof(T) == typeof(sbyte))
            {
                methodName = "SByteToDouble";
            }
            else if (typeof(T) == typeof(byte))
            {
                methodName = "ByteToDouble";
            }
            else if (typeof(T) == typeof(short))
            {
                methodName = "Int16ToDouble";
            }
            else if (typeof(T) == typeof(ushort))
            {
                methodName = "UInt16ToDouble";
            }
            else if (typeof(T) == typeof(int))
            {
                methodName = "Int32ToDouble";
            }
            else if (typeof(T) == typeof(uint))
            {
                methodName = "UInt32ToDouble";
            }
            else if (typeof(T) == typeof(long))
            {
                methodName = "Int64ToDouble";
            }
            else if (typeof(T) == typeof(ulong))
            {
                methodName = "UInt64ToDouble";
            }
            else if (typeof(T) == typeof(float))
            {
                methodName = "SingleToDouble";
            }
            else if (typeof(T) == typeof(double))
            {
                methodName = "DoubleToDouble";
            }
            else
            {
                throw new NotSupportedException(string.Format("Cannot convert type {0} to double", typeof(T).Name));
            }

            MethodInfo methodInfo = typeof(ReflectionHelper).GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
            return CreateDelegate<T, double>(methodInfo);
        }

        private static Regex numberRegex = new Regex(@"([\d\.]+)(\w*)");

        public static Type GetNumberType(string numberToken, out double number)
        {
            var match = numberRegex.Match(numberToken);
            Type result = typeof(int);
            number = 0;
            if (!match.Success)
            {
                throw new NotSupportedException("Invalid number token: " + numberToken);
            }

            number = double.Parse(match.Groups[1].Value);
            switch (match.Groups[2].Value)
            {
                case null:
                case "":
                    result = typeof(int);
                    break;
                case "ui":
                    result = typeof(uint);
                    break;
                case "i":
                    result = typeof(int);
                    break;
                case "f":
                    result = typeof(float);
                    break;
                case "d":
                    result = typeof(double);
                    break;
                case "l":
                    result = typeof(long);
                    break;
                case "ul":
                    result = typeof(ulong);
                    break;
                default:
                    throw new NotSupportedException("Invalid number token: " + numberToken);
            }

            return result;
        }
        
        public static IEnumerable<MethodInfo> GetExtensionMethods(this Type type, List<Type> staticTypes)
        {
            var query = from t in staticTypes
                    from m in t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    where m.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false)
                    where m.GetParameters()[0].ParameterType == type
                    select m;

            return query;
        }

        public static MethodInfo GetExtensionMethod(Type type,  List<Type> staticTypes, string name)
        {
            return type.GetExtensionMethods(staticTypes).FirstOrDefault(m => m.Name == name);
        }

        public static MethodInfo GetExtensionMethod(Type type, List<Type> staticTypes, string name, Type[] types)
        {
            var methods = (from m in type.GetExtensionMethods(staticTypes)
                where m.Name == name
                        && m.GetParameters().Count() == types.Length + 1 // + 1 because extension method parameter (this)
                select m).ToList();

            if (!methods.Any())
            {
                return default(MethodInfo);
            }

            if (methods.Count() == 1)
            {
                return methods.First();
            }

            foreach (var methodInfo in methods)
            {
                var parameters = methodInfo.GetParameters();

                bool found = true;
                for (byte b = 0; b < types.Length; b++)
                {
                    found = true;
                    if (parameters[b].GetType() != types[b])
                    {
                        found = false;
                    }
                }

                if (found)
                {
                    return methodInfo;
                }
            }

            return default(MethodInfo);
        }

        public static MethodInfo GetGenericMethod(Type type, string name, Type[] paraTypes, int genericCount)
        {
            var methodInfos = type.GetMethods(BindingFlags.Instance |
                        BindingFlags.Static | BindingFlags.Public |
                        BindingFlags.NonPublic)
                    .Where(info =>
                            info.Name == name && info.IsGenericMethod &&
                            info.GetGenericArguments().Length == genericCount &&
                            info.GetParameters().Length == paraTypes.Length).ToList();
            // 如果只找到一个方法，那就是需要的泛型方法
            if (methodInfos.Count == 1)
            {
                return methodInfos[0];
            }

            // 如果找到多个，那就选参数匹配最多的那个方法
            int maxMatchCount = 0;
            int targetMethod = -1;
            for (var i = 0; i < methodInfos.Count; i++)
            {
                var param = methodInfos[i].GetParameters();
                int matchCount = 0;
                for (int j = 0; j < param.Length; j++)
                {
                    var paramType = param[j].ParameterType;
                    if (paraTypes[j] == paramType)
                    {
                        matchCount += 10;
                    }
                    else if (paramType.IsGenericParameter)
                    {
                        matchCount++;
                    }
                    // 类型即不一样，也不是泛型参数，可以直接pass
                    else
                    {
                        matchCount = int.MinValue;
                        break;
                    }
                }

                if (matchCount > maxMatchCount)
                {
                    targetMethod = i;
                    maxMatchCount = matchCount;
                }
            }

            return methodInfos[targetMethod];
        }

        public static Type GetCalcNumberType(Type type1, Type type2)
        {
            if (type1 == type2) return type1;
            if (type1 == typeof(double) || type2 == typeof(double))
            {
                return typeof(double);
            }

            if (type1 == typeof(float) || type2 == typeof(float))
            {
                return typeof(float);
            }

            if (type1 == typeof(ulong) || type2 == typeof(ulong))
            {
                return typeof(ulong);
            }

            if (type1 == typeof(long) || type2 == typeof(long))
            {
                return typeof(long);
            }

            return typeof(int);
        }

        public static Func<object, object> GenerateFromDoubleConverter(Type target)
        {
            string methodName;

            if (target.IsEnum)
            {
                // Hack: we force cast Func<double, int> to Func<double, TEnum>
                methodName = "DoubleToInt32";
            }
            else if (target == typeof(sbyte))
            {
                methodName = "DoubleToSByte";
            }
            else if (target == typeof(byte))
            {
                methodName = "DoubleToByte";
            }
            else if (target == typeof(short))
            {
                methodName = "DoubleToInt16";
            }
            else if (target == typeof(ushort))
            {
                methodName = "DoubleToUInt16";
            }
            else if (target == typeof(int))
            {
                methodName = "DoubleToInt32";
            }
            else if (target == typeof(uint))
            {
                methodName = "DoubleToUInt32";
            }
            else if (target == typeof(long))
            {
                methodName = "DoubleToInt64";
            }
            else if (target == typeof(ulong))
            {
                methodName = "DoubleToUInt64";
            }
            else if (target == typeof(float))
            {
                methodName = "DoubleToSingle";
            }
            else if (target == typeof(double))
            {
                methodName = "DoubleToDouble";
            }
            else
            {
                throw new NotSupportedException();
            }

            MethodInfo methodInfo = typeof(ReflectionHelper).GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
            return (doub) => methodInfo.Invoke(null, new object[] { doub });
        }

        private static double SByteToDouble(sbyte value)
        {
            return value;
        }

        private static double ByteToDouble(byte value)
        {
            return value;
        }

        private static double Int16ToDouble(short value)
        {
            return value;
        }

        private static double UInt16ToDouble(ushort value)
        {
            return value;
        }

        private static double Int32ToDouble(int value)
        {
            return value;
        }

        private static double UInt32ToDouble(uint value)
        {
            return value;
        }

        private static double Int64ToDouble(long value)
        {
            return value;
        }

        private static double UInt64ToDouble(ulong value)
        {
            return value;
        }

        private static double SingleToDouble(float value)
        {
            return value;
        }

        private static double DoubleToDouble(double value)
        {
            return value;
        }

        private static sbyte DoubleToSByte(double value)
        {
            return (sbyte)value;
        }

        private static byte DoubleToByte(double value)
        {
            return (byte)value;
        }

        private static short DoubleToInt16(double value)
        {
            return (short)value;
        }

        private static ushort DoubleToUInt16(double value)
        {
            return (ushort)value;
        }

        private static int DoubleToInt32(double value)
        {
            return (int)value;
        }

        private static uint DoubleToUInt32(double value)
        {
            return (uint)value;
        }

        private static long DoubleToInt64(double value)
        {
            return (long)value;
        }

        private static ulong DoubleToUInt64(double value)
        {
            return (ulong)value;
        }

        private static float DoubleToSingle(double value)
        {
            return (float)value;
        }
    }
}

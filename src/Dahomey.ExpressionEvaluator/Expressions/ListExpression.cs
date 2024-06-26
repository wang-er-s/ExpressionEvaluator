﻿#region License

/* Copyright © 2017, Dahomey Technologies and Contributors
 * For conditions of distribution and use, see copyright notice in license.txt file
 */

#endregion

using System;
#if NET35
using System.Linq;
#endif
using System.Collections.Generic;
using System.Reflection;

namespace Dahomey.ExpressionEvaluator.Expressions
{
    public class ListExpression : IExpression
    {
        public List<IExpression> Expressions { get; private set; }

        public ListExpression(List<IExpression> expressions)
        {
            Expressions = expressions;
        }
        
        public Func<Dictionary<string, object>, object> GetItemGetter(int index, Type itemType)
        {
            IExpression itemExpr = Expressions[index];

            if (ReflectionHelper.IsNumber(itemType))
            {
                var numericExpr = (IObjectExpression)itemExpr;
                var converter = ReflectionHelper.GenerateFromDoubleConverter(itemType);
                return dic => converter(numericExpr.GetInstance(dic));
            }
            
            IObjectExpression objectExpr = (IObjectExpression)itemExpr;
            return dic => objectExpr.GetInstance(dic);
        }

        public Func<Dictionary<string, object>, T> GetItemGetter<T>(int index)
        {
            IExpression itemExpr = Expressions[index];

            Type itemType = typeof(T);

            if (ReflectionHelper.IsNumber(itemType))
            {
                INumericExpression numericExpr = (INumericExpression)itemExpr;

                return default;
            }
            else if (itemType == typeof(bool))
            {
                MethodInfo generateGetterMethod = GetType()
                    .GetMethod("GetBooleanItemGetter", BindingFlags.NonPublic | BindingFlags.Static);

                Func<IBooleanExpression, Func<Dictionary<string, object>, T>> generateGetterDelegate =
                    ReflectionHelper.CreateDelegate<IBooleanExpression, Func<Dictionary<string, object>, T>>(generateGetterMethod);

                IBooleanExpression booleanExpr = (IBooleanExpression)itemExpr;
                return generateGetterDelegate(booleanExpr);
            }
            else if (itemType == typeof(string))
            {
                MethodInfo generateGetterMethod = GetType()
                    .GetMethod("GetStringItemGetter", BindingFlags.NonPublic | BindingFlags.Static);

                Func<IStringExpression, Func<Dictionary<string, object>, T>> generateGetterDelegate =
                    ReflectionHelper.CreateDelegate<IStringExpression, Func<Dictionary<string, object>, T>>(generateGetterMethod);

                IStringExpression booleanExpr = (IStringExpression)itemExpr;
                return generateGetterDelegate(booleanExpr);
            }
            else
            {
                IObjectExpression objectExpr = (IObjectExpression)itemExpr;

                return variables => (T)objectExpr.GetInstance(variables);
            }
        }

        private static Func<Dictionary<string, object>, bool> GetBooleanItemGetter(IBooleanExpression booleanExpr)
        {
            return variables => booleanExpr.Evaluate(variables);
        }

        private static Func<Dictionary<string, object>, string> GetStringItemGetter(IStringExpression stringExpr)
        {
            return variables => stringExpr.Evaluate(variables);
        }

        public override string ToString()
        {
#if NET35
            return string.Join(",", Expressions.Select(e => e.ToString()).ToArray());
#else
            return string.Join(",", Expressions);
#endif
        }
    }
}

#region License

/* Copyright © 2017, Dahomey Technologies and Contributors
 * For conditions of distribution and use, see copyright notice in license.txt file
 */

#endregion

using Dahomey.ExpressionEvaluator.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Dahomey.ExpressionEvaluator
{
    public class ObjectMethodExpression : IObjectExpression
    {
        private IObjectExpression containingObject;
        private MethodInfo methodInfo;
        private ListExpression argumentsExpr;
        private Func<Dictionary<string, object>, object, object> evaluator;
        public Type ObjectType { get; private set; }

        public ObjectMethodExpression(IObjectExpression containingObject, MethodInfo methodInfo,
        ListExpression argumentsExpr)
        {
            this.containingObject = containingObject;
            this.methodInfo = methodInfo;
            this.argumentsExpr = argumentsExpr;

            ObjectType = methodInfo.ReturnType;
            var paraTypes = methodInfo.GetParameters().Select((p) => p.ParameterType).ToList();
            var paraLength = paraTypes.Count;

            Func<Dictionary<string, object>, object>[] paramFuncs = null;

            if (paraLength != 0)
            {
                paramFuncs = new Func<Dictionary<string, object>, object>[paraLength];
                int paraIndex = 0;
                // 如果是拓展方法,则把参数往前移动一个
                if (methodInfo.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false))
                {
                    paraIndex++;
                    paramFuncs[0] = containingObject.GetInstance;
                }

                for (int i = paraIndex; i < paraLength; i++)
                {
                    paramFuncs[i] = argumentsExpr.GetItemGetter(i - paraIndex, paraTypes[i]);
                }
            }

            evaluator = (variables, ins) =>
            {
                object[] param = null;
                MethodInfo genericMethodInfo = this.methodInfo;
                if (paramFuncs != null)
                {
                    param = new object[paramFuncs.Length];
                    for (var i = 0; i < paramFuncs.Length; i++)
                    {
                        param[i] = paramFuncs[i].Invoke(variables);
                    }
                }

                return genericMethodInfo.Invoke(ins, param);
            };
        }

        public object GetInstance(Dictionary<string, object> variables)
        {
            object instance = containingObject.GetInstance(variables);
            return evaluator(variables, instance);
        }

     
        public override string ToString()
        {
            return string.Format("{0}.{1}({2})", containingObject, methodInfo.Name, argumentsExpr);
        }
    }
}

#region License

/* Copyright © 2017, Dahomey Technologies and Contributors
 * For conditions of distribution and use, see copyright notice in license.txt file
 */

#endregion

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Dahomey.ExpressionEvaluator
{
    public class ObjectPropertyExpression : IObjectExpression
    {
        private IObjectExpression containingObject;
        private MemberInfo memberInfo;
        private bool isStatic;
        private readonly Func<object, object> evaluator;

        public Type ObjectType { get; private set; }

        public ObjectPropertyExpression(IObjectExpression containingObject, MemberInfo memberInfo)
        {
            this.containingObject = containingObject;
            this.memberInfo = memberInfo;

            switch (memberInfo)
            {
                case PropertyInfo propertyInfo:
                    isStatic = propertyInfo.GetGetMethod().IsStatic;
                    evaluator = (obj) => propertyInfo.GetValue(obj);
                    this.ObjectType = propertyInfo.PropertyType;
                    break;
                case FieldInfo fieldInfo:
                    isStatic = fieldInfo.IsStatic;
                    evaluator = (obj) => fieldInfo.GetValue(obj);
                    this.ObjectType = fieldInfo.FieldType;
                    break;
            }
        }

        public object GetInstance(Dictionary<string, object> variables)
        {
            if (isStatic)
            {
                return evaluator(null);
            }
            else
            {
                object containingObjectInstance = containingObject.GetInstance(variables);
                return evaluator(containingObjectInstance);
            }
        }

        public override string ToString()
        {
            return string.Format("{0}.{1}", containingObject, memberInfo.Name);
        }
    }
}

using System;
using System.Collections.Generic;

namespace Dahomey.ExpressionEvaluator
{
    public class TypeConvertExpression : IObjectExpression
    {
        public IObjectExpression Expression { get; set; }
        
        public TypeConvertExpression(Type convertType)
        {
            this.ObjectType = convertType;
        }
        
        public Type ObjectType { get; private set; }
        public object GetInstance(Dictionary<string, object> variables)
        {
            return Convert.ChangeType(this.Expression.GetInstance(variables), this.ObjectType);
        }
    }
}
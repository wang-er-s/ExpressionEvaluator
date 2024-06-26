﻿#region License

/* Copyright © 2017, Dahomey Technologies and Contributors
 * For conditions of distribution and use, see copyright notice in license.txt file
 */

#endregion

using System;
using System.Collections.Generic;
using System.Text;

namespace Dahomey.ExpressionEvaluator
{
    public class BooleanLogicalExpression : IBooleanExpression
    {
        public Operator Operator { get; set; }
        public IBooleanExpression LeftExpr { get; set; }
        public IBooleanExpression RightExpr { get; set; }

        public bool Evaluate(Dictionary<string, object> variables)
        {
            switch (Operator)
            {
                case Operator.And:
                    return LeftExpr.Evaluate(variables) && RightExpr.Evaluate(variables);

                case Operator.Or:
                    return LeftExpr.Evaluate(variables) || RightExpr.Evaluate(variables);

                case Operator.Not:
                    return !LeftExpr.Evaluate(variables);

                default:
                    throw new NotSupportedException(string.Format("Operator {0} not supported", Operator));
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            if (LeftExpr != null)
            {
                sb.Append(LeftExpr).Append(' ');
            }
            
            sb.Append(Operator.PrettyPrint());

            if (RightExpr != null)
            {
                sb.Append(RightExpr).Append(' ');
            }

            return sb.ToString();
        }

        public Type ObjectType => typeof(bool);
        public object GetInstance(Dictionary<string, object> variables)
        {
            return Evaluate(variables);
        }
    }
}

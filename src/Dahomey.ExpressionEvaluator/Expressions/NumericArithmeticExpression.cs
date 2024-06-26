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
    public class NumericArithmeticExpression : INumericExpression
    {
        public Operator Operator { get; private set; }
        public INumericExpression LeftExpr { get; private set; }
        public INumericExpression RightExpr { get; private set; }

        public NumericArithmeticExpression(INumericExpression leftExpr, Operator @operator, INumericExpression rightExpr)
        {
            LeftExpr = leftExpr;
            Operator = @operator;
            RightExpr = rightExpr;
            if (rightExpr == null)
            {
                this.ObjectType = leftExpr.ObjectType;
            }
            else
            {
                this.ObjectType = ReflectionHelper.GetCalcNumberType(leftExpr.ObjectType, rightExpr.ObjectType);
            }
        }

        public double Evaluate(Dictionary<string, object> variables)
        {
            switch (Operator)
            {
                case Operator.Plus:
                    return RightExpr == null ?
                        LeftExpr.Evaluate(variables) :
                        LeftExpr.Evaluate(variables) + RightExpr.Evaluate(variables);

                case Operator.Minus:
                    return RightExpr == null ?
                        -LeftExpr.Evaluate(variables) :
                        LeftExpr.Evaluate(variables) - RightExpr.Evaluate(variables);

                case Operator.Mult:
                    return LeftExpr.Evaluate(variables) * RightExpr.Evaluate(variables);

                case Operator.Div:
                    return LeftExpr.Evaluate(variables) / RightExpr.Evaluate(variables);

                case Operator.Mod:
                    return LeftExpr.Evaluate(variables) % RightExpr.Evaluate(variables);

                case Operator.BitwiseAnd:
                    return (int)LeftExpr.Evaluate(variables) & (int)RightExpr.Evaluate(variables);

                case Operator.BitwiseOr:
                    return (int)LeftExpr.Evaluate(variables) | (int)RightExpr.Evaluate(variables);

                case Operator.BitwiseXor:
                    return (int)LeftExpr.Evaluate(variables) ^ (int)RightExpr.Evaluate(variables);

                case Operator.BitwiseComplement:
                    return ~(int)LeftExpr.Evaluate(variables);

                case Operator.LeftShift:
                    return (int)LeftExpr.Evaluate(variables) << (int)RightExpr.Evaluate(variables);

                case Operator.RightShift:
                    return (int)LeftExpr.Evaluate(variables) >> (int)RightExpr.Evaluate(variables);

                default:
                    throw new NotSupportedException(string.Format("operator {0} not support", Operator));
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

        public Type ObjectType { get; private set; }
        public object GetInstance(Dictionary<string, object> variables)
        {
            return Evaluate(variables);
        }
    }
}

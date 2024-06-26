﻿#region License

/* Copyright © 2017, Dahomey Technologies and Contributors
 * For conditions of distribution and use, see copyright notice in license.txt file
 */

#endregion

using System;
using System.Collections.Generic;
using System.Globalization;

namespace Dahomey.ExpressionEvaluator
{
    public class NumericLiteralExpression : INumericExpression
    {
        private double value;

        public NumericLiteralExpression(double value, Type numberType)
        {
            this.value = value;
            ObjectType = numberType;
        }

        public double Evaluate(Dictionary<string, object> variables)
        {
            return value;
        }

        public override string ToString()
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }
        
        public Type ObjectType { get; private set; }
        public object GetInstance(Dictionary<string, object> variables)
        {
            return Evaluate(variables);
        }
    }
}

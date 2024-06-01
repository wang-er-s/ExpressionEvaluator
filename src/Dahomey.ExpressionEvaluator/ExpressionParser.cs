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
    public class ExpressionParser
    {
        private ExpressionLexer lexer;
        private Dictionary<string, Type> variableTypes = new Dictionary<string, Type>();
        private Dictionary<string, Type> types = new Dictionary<string, Type>();
        private Dictionary<string, Delegate> functions = new Dictionary<string, Delegate>();
        // 用来找拓展方法的
        private List<Type> staticTypes = new List<Type>();
        private static ExpressionParser defaultVal;

        // 为了不影响平常的性能，延迟初始化，使用的时候才会初始化
        public static event Action InitAction;

        public static ExpressionParser Default
        {
            get
            {
                if (defaultVal.variableTypes.Count <= 0)
                {
                    InitAction();
                }
                return defaultVal;
            }
            set
            {
                defaultVal = value;
            }
        }

        public void RegisterVariable(string variableName, Type variableType)
        {
            variableTypes[variableName] = variableType;
        }

        public void RegisterType(string typeName, Type type)
        {
            types[typeName] = type;
        }

        public void RegisterVariable<T>(string variableName)
        {
            variableTypes[variableName] = typeof(T);
        }

        public void RegisterFunction(string functionName, Delegate function)
        {
            functions[functionName] = function;
        }

        public void RegisterStaticType(Type type)
        {
            this.staticTypes.Add(type);
        }

        public IObjectExpression ObjectExpression(string expression)
        {
            lexer = new ExpressionLexer(expression);
            return (IObjectExpression)Expression();
        }

        private IExpression Expression()
        {
            return ConditionalExpression();
        }

        // bool ? a : b
        private IExpression ConditionalExpression()
        {
            IExpression expr = LogicalOrExpression();

            if (lexer.Accept(TokenType.Interrogation))
            {
                IExpression leftExpr = Expression();
                lexer.Expect(TokenType.Colon);
                IExpression rightExpr = Expression();

                return new NumericConditionalExpression
                {
                    ConditionExpr = (IBooleanExpression)expr,
                    LeftExpr = (INumericExpression)leftExpr,
                    RightExpr = (INumericExpression)rightExpr,
                };
            }

            return expr;
        }

        // exp || exp
        private IExpression LogicalOrExpression()
        {
            IExpression expr = LogicalAndExpression();

            while (lexer.Accept(TokenType.Or))
            {
                expr = new BooleanLogicalExpression
                {
                    Operator = Operator.Or,
                    LeftExpr = (IBooleanExpression)expr,
                    RightExpr = (IBooleanExpression)LogicalAndExpression()
                };
            }

            return expr;
        }

        // exp && exp
        private IExpression LogicalAndExpression()
        {
            IExpression expr = BitwiseOrExpression();

            while (lexer.Accept(TokenType.And))
            {
                expr = new BooleanLogicalExpression
                {
                    Operator = Operator.And,
                    LeftExpr = (IBooleanExpression)expr,
                    RightExpr = (IBooleanExpression)BitwiseOrExpression()
                };
            }

            return expr;
        }

        // exp | exp
        private IExpression BitwiseOrExpression()
        {
            IExpression expr = BitwiseXorExpression();

            while (lexer.Accept(TokenType.BitwiseOr))
            {
                expr = new NumericArithmeticExpression((INumericExpression)expr, Operator.BitwiseOr, (INumericExpression)BitwiseXorExpression());
            }

            return expr;
        }

        // exp ^ exp
        private IExpression BitwiseXorExpression()
        {
            IExpression expr = BitwiseAndExpression();

            while (lexer.Accept(TokenType.BitwiseXor))
            {
                expr = new NumericArithmeticExpression((INumericExpression)expr, Operator.BitwiseXor, (INumericExpression)BitwiseAndExpression());
            }

            return expr;
        }

        // exp & exp
        private IExpression BitwiseAndExpression()
        {
            IExpression expr = EqualityExpression();

            while (lexer.Accept(TokenType.BitwiseAnd))
            {
                expr = new NumericArithmeticExpression
                (
                    (INumericExpression)expr,
                    Operator.BitwiseAnd,
                    (INumericExpression)EqualityExpression()
                );
            }

            return expr;
        }

        // exp == exp   exp != exp
        private IExpression EqualityExpression()
        {
            IExpression expr = RelationalExpression();

            Operator op;
            if (lexer.Accept(TokenType.Eq))
            {
                op = Operator.Equal;
            }
            else if (lexer.Accept(TokenType.Ne))
            {
                op = Operator.NotEqual;
            }
            else
            {
                return expr;
            }

            return new NumericComparisonExpression
            {
                Operator = op,
                LeftExpr = (INumericExpression)expr,
                RightExpr = (INumericExpression)RelationalExpression()
            };
        }

        // exp <= exp   exp < exp   exp >= exp   exp > exp
        private IExpression RelationalExpression()
        {
            IExpression expr = ShiftExpression();

            Operator op;
            if (lexer.Accept(TokenType.Lt))
            {
                op = Operator.LessThan;
            }
            else if (lexer.Accept(TokenType.Le))
            {
                op = Operator.LessThanOrEqual;
            }
            else if (lexer.Accept(TokenType.Ge))
            {
                op = Operator.GreaterThanOrEqual;
            }
            else if (lexer.Accept(TokenType.Gt))
            {
                op = Operator.GreaterThan;
            }
            else
            {
                return expr;
            }

            return new NumericComparisonExpression
            {
                Operator = op,
                LeftExpr = (INumericExpression)expr,
                RightExpr = (INumericExpression)ShiftExpression()
            };
        }

        // exp << exp  exp >> exp 
        private IExpression ShiftExpression()
        {
            IExpression expr = AdditiveExpression();

            while (!lexer.Peek(TokenType.None))
            {
                Operator op;
                if (lexer.Accept(TokenType.LeftShift))
                {
                    op = Operator.LeftShift;
                }
                else if (lexer.Accept(TokenType.RightShift))
                {
                    op = Operator.RightShift;
                }
                else
                {
                    break;
                }

                expr = new NumericArithmeticExpression
                (
                    (INumericExpression)expr,
                    op,
                    (INumericExpression)AdditiveExpression()
                );
            }

            return expr;
        }

        // exp + exp  exp - exp
        private IExpression AdditiveExpression()
        {
            IExpression expr = MultiplicativeExpression();

            while (!lexer.Peek(TokenType.None))
            {
                Operator op;
                if (lexer.Accept(TokenType.Plus))
                {
                    op = Operator.Plus;
                }
                else if (lexer.Accept(TokenType.Minus))
                {
                    op = Operator.Minus;
                }
                else
                {
                    break;
                }

                expr = new NumericArithmeticExpression
                (
                    (INumericExpression)expr,
                    op,
                    (INumericExpression)MultiplicativeExpression()
                );
            }

            return expr;
        }

        //  * / %
        private IExpression MultiplicativeExpression()
        {
            IExpression expr = UnaryExpression();

            while (!lexer.Peek(TokenType.None))
            {
                Operator op;
                if (lexer.Accept(TokenType.Mult))
                {
                    op = Operator.Mult;
                }
                else if (lexer.Accept(TokenType.Div))
                {
                    op = Operator.Div;
                }
                else if (lexer.Accept(TokenType.Mod))
                {
                    op = Operator.Mod;
                }
                else
                {
                    break;
                }

                expr = new NumericArithmeticExpression
                (
                    (INumericExpression)expr,
                    op,
                    (INumericExpression)UnaryExpression()
                );
            }

            return expr;
        }

        // ~exp  !exp -exp
        private IExpression UnaryExpression()
        {
            if (lexer.Accept(TokenType.Minus))
            {
                return new NumericArithmeticExpression
                (
                    (INumericExpression)PrimaryExpression(),
                    Operator.Minus,null
                );
            }
            else if (lexer.Accept(TokenType.BitwiseComplement))
            {
                return new NumericArithmeticExpression
                (
                    (INumericExpression)PrimaryExpression(),
                    Operator.BitwiseComplement,null
                );
            }
            else if (lexer.Accept(TokenType.Not))
            {
                return new BooleanLogicalExpression
                {
                    Operator = Operator.Not,
                    LeftExpr = (IBooleanExpression)PrimaryExpression()
                };
            }
            else
            {
                return PrimaryExpression();
            }
        }

        // 具体值
        private IExpression PrimaryExpression()
        {
            if (lexer.Peek(TokenType.OpenParenthesis))
            {
                return ParenthesizedExpression();
            }
            else if (lexer.Peek(TokenType.Identifier))
            {
                return VariableExpression();
            }

            return Literal();
        }

        private IExpression VariableExpression()
        {
            IExpression expr = ElementExpression(VariableOrFunctionExpression());

            while (lexer.Accept(TokenType.Dot))
            {
                IObjectExpression containingObjectExpr = expr as IObjectExpression;

                if (containingObjectExpr == null)
                {
                    throw BuildException("Cannot access property or method on expression {0}", expr);
                }

                expr = ElementExpression(MemberExpression(containingObjectExpr));
            }

            return expr;
        }

        private IExpression ElementExpression(IExpression expr)
        {
            if (lexer.Peek(TokenType.OpenBracket))
            {
                INumericExpression indexExpr = (INumericExpression)BracketExpression();
                IObjectExpression listExpr = expr as IObjectExpression;

                if (listExpr == null)
                {
                    throw BuildException("Cannot apply indexing with [] on expression", expr);
                }

                expr = new ObjectListElementExpression(listExpr, indexExpr);
            }

            return expr;
        }

        private IExpression VariableOrFunctionExpression()
        {
            string identifier = lexer.Identifier();

            // function
            if (lexer.Peek(TokenType.OpenParenthesis))
            {
                ListExpression argsExpr = (ListExpression)this.ParenthesisExpression();

                Delegate function;

                if (!functions.TryGetValue(identifier, out function))
                {
                    throw BuildException("Unknown function '{0}()'", identifier);
                }

                return new ObjectFuncExpression(identifier, function, argsExpr);
            }
            // variable or enum

            Type identifierType;
            // variable
            if (this.variableTypes.TryGetValue(identifier, out identifierType))
            {
                return new ObjectVariableExpression(identifier, identifierType);
            }

            // type
            if (this.types.TryGetValue(identifier, out identifierType))
            {
                // 如果是枚举
                if (identifierType.IsEnum)
                {
                    this.lexer.Expect(TokenType.Dot);
                    string enumValue = this.lexer.Identifier();

                    Enum value = (Enum)Enum.Parse(identifierType, enumValue);
                    return new EnumLiteralExpression(value);
                }
                
                // 如果后边跟的是) 是某个类来做强转的？
                if (this.lexer.Peek(TokenType.CloseParenthesis))
                {
                    var exp = new TypeConvertExpression(identifierType);
                    return exp;
                }

                // 否则就是个静态类型了
                return new StaticTypeExpression(identifier, identifierType);

            }

            throw this.BuildException($"Unknown variable '{identifier}'");
        }

        private IExpression MemberExpression(IObjectExpression containingObjectExpr)
        {
            string identifier = lexer.Identifier();

            // method
            // 如果右边是(
            if (lexer.Peek(TokenType.OpenParenthesis))
            {
                ListExpression argsExpr = (ListExpression)this.ParenthesisExpression();
                Type[] parameterTypes = Type.EmptyTypes;
                if (argsExpr.Expressions.Count > 0)
                {
                    parameterTypes = new Type[argsExpr.Expressions.Count];
                    for (int i = 0; i < argsExpr.Expressions.Count; i++)
                    {
                        parameterTypes[i] = (argsExpr.Expressions[i] as IObjectExpression).ObjectType;
                    }
                }

                MethodInfo methodInfo = containingObjectExpr.ObjectType.GetMethod(identifier,
                    BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null,
                    parameterTypes, null);
                if (methodInfo == null)
                {
                    methodInfo = ReflectionHelper.GetExtensionMethod(containingObjectExpr.ObjectType, this.staticTypes, identifier, parameterTypes);
                }

                return new ObjectMethodExpression(containingObjectExpr, methodInfo, argsExpr);
            }
            // 如果右边是< 则是泛型方法
            if (lexer.Peek(TokenType.Lt))
            {
                List<Type> genericTypes = GenericExpression();
                ListExpression argsExpr = (ListExpression)this.ParenthesisExpression();
                Type[] parameterTypes = Type.EmptyTypes;
                if (argsExpr.Expressions.Count > 0)
                {
                    parameterTypes = new Type[argsExpr.Expressions.Count];
                    for (int i = 0; i < argsExpr.Expressions.Count; i++)
                    {
                        parameterTypes[i] = (argsExpr.Expressions[i] as IObjectExpression).ObjectType;
                    }
                }

                var methodInfo = ReflectionHelper.GetGenericMethod(containingObjectExpr.ObjectType, identifier, parameterTypes, genericTypes.Count);
                return new ObjectMethodExpression(containingObjectExpr, methodInfo.MakeGenericMethod(genericTypes.ToArray()), argsExpr);
            }
            // property
            else
            {
                MemberInfo propertyInfo = containingObjectExpr.ObjectType.GetMember(identifier,
                    BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)[0];
                return new ObjectPropertyExpression(containingObjectExpr, propertyInfo);
            }
        }

        private IExpression Literal()
        {
            if (lexer.Accept(TokenType.True))
            {
                return new BooleanLiteralExpression(true);
            }

            if (lexer.Accept(TokenType.False))
            {
                return new BooleanLiteralExpression(false);
            }

            if (lexer.Peek(TokenType.Number))
            {
                var numberType = lexer.Number(out var number);
                return new NumericLiteralExpression(number, numberType);
            }

            if (lexer.Peek(TokenType.String))
            {
                return new StringLiteralExpression(lexer.String());
            }

            throw BuildException("Expected boolean, number or string literal");
        }

        // 括号内的表达式
        private IExpression ParenthesizedExpression()
        {
            lexer.Expect(TokenType.OpenParenthesis);
            IExpression expr = Expression();
            lexer.Expect(TokenType.CloseParenthesis);
            if (expr is TypeConvertExpression typeConvertExpression)
            {
                typeConvertExpression.Expression = (IObjectExpression)this.Expression();
            }
            
            return expr;
        }

        private IExpression BracketExpression()
        {
            lexer.Expect(TokenType.OpenBracket);
            IExpression expr = Expression();
            lexer.Expect(TokenType.CloseBracket);
            return expr;
        }

        /// <summary>
        /// 获取括号内的参数
        /// </summary>
        /// <returns></returns>
        private IExpression ParenthesisExpression()
        {
            lexer.Expect(TokenType.OpenParenthesis);

            List<IExpression> args = new List<IExpression>();
            ListExpression argsExpr = new ListExpression(args);

            if (lexer.Accept(TokenType.CloseParenthesis))
            {
                return argsExpr;
            }

            do
            {
                args.Add(Expression());
            }
            while (lexer.Accept(TokenType.Comma));

            lexer.Expect(TokenType.CloseParenthesis);

            return argsExpr;
        }

        private List<Type> GenericExpression()
        {
            lexer.Expect(TokenType.Lt);
            List<Type> types = new List<Type>();
            
            if (lexer.Accept(TokenType.CloseParenthesis))
            {
                return types;
            }

            do
            {
                types.Add((VariableOrFunctionExpression() as IObjectExpression).ObjectType);
            }
            while (lexer.Accept(TokenType.Comma));

            lexer.Expect(TokenType.Gt);

            return types;
        }

        public Exception BuildException(string message)
        {
            return lexer.BuildException(message);
        }

        public Exception BuildException(string message, params object[] args)
        {
            return lexer.BuildException(message, args);
        }
    }
}

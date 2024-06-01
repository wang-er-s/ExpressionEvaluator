运行时使用反射解析代码，主要用于调试使用，完全没考虑性能
可以配合 [IngameDebugConsole](https://github.com/yasirkula/UnityIngameDebugConsole) 在unity打包后进行调试

```csharp
using System.Collections.Generic;
using NUnit.Framework;

namespace Dahomey.ExpressionEvaluator.Tests
{
    
    public class AA
    {
        public string Print(int a)
        {
            return $"int {a}";
        }

        public static string StaticVal = "StaticVal";

        public string Print(long a)
        {
            return $"long {a}";
        }

        public string Print(float a)
        {
            return $"float {a}";
        }

        public string Print<T>(T a)
        {
            return $"generic {a}";
        }

        public int Add(int a, int b)
        {
            return a + b;
        }
    }

    public static class AAExtension
    {
        public static string ExtensionFunction(this AA aa, int a)
        {
            return $"extension {a}";
        }
    }
    
    public class ExpressionParserTest
    {

        [Test]
        [TestCase("aa.Print(123)", "int 123")]
        // f表示float
        [TestCase("aa.Print(123f)", "float 123")]
        // 强转
        [TestCase("aa.Print((float)123)", "float 123")]
        // 泛型
        [TestCase("aa.Print<string>(\"hello\")", "generic hello")]
        [TestCase("aa.Print(aa.Add(1,2))", "int 3")]
        // 计算
        [TestCase("aa.Print(2 + 3 * 2)", "int 8")]
        // l表示long
        [TestCase("aa.Print(2l + 3 * 2)", "long 8")]
        // 拓展方法
        [TestCase("aa.ExtensionFunction(123)", "extension 123")]
        [TestCase("AA.StaticVal", "StaticVal")]
        public void BooleanExpressionTest(string expression, string expectedValue)
        {
            var expressionParser = new ExpressionParser();
            expressionParser.RegisterVariable<AA>("aa");
            expressionParser.RegisterType("AA", typeof(AA));
            expressionParser.RegisterType("string", typeof(string));
            expressionParser.RegisterType("float", typeof(float));
            expressionParser.RegisterStaticType(typeof(AAExtension));
            IObjectExpression expr = expressionParser.ObjectExpression(expression);
            Assert.That(expr, Is.Not.Null);
            Assert.That(expectedValue, Is.EqualTo(expr.GetInstance(new Dictionary<string, object>() { { "aa", new AA() } })));
        }
    }
}

```

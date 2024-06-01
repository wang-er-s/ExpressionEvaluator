- 运行时使用反射解析代码，主要用于调试使用，完全没考虑性能
- 可以配合 [IngameDebugConsole](https://github.com/yasirkula/UnityIngameDebugConsole) 在unity打包后进行调试
- 为 [ET](https://github.com/egametang/ET) 专门适配了拓展方法，再结合et的树形结构，可以实现运行时调用各种方法
 
```csharp
        private static void InitGM(Scene root)
        {
            // 会在执行的时候把Root传进去，同时输出执行结果
            // 一般是配合IngameDebugConsole在GM结束拷贝代码来执行
            // 例如 "Root.GetComponent<CurrentScenesComponent>().Scene.GetComponent<BlockGame>().GetComponent<BlockCanvas>().Print()"
            DebugLogConsole.AddCommand<string>("系统/执行脚本","", (cmd) =>
            {
                var exp = ExpressionParser.Default.ObjectExpression(cmd);
                Log.Info(exp.GetInstance(new Dictionary<string, object>() { { "Root", root } }).ToString());
            });

            ExpressionParser parser = new ExpressionParser();
            ExpressionParser.Default = parser;
            // 第一次使用才会初始化，防止影响性能
            ExpressionParser.InitAction += () =>
            {
                Regex regex = new Regex(@"\b[a-zA-Z0-9_]+\b", RegexOptions.RightToLeft);
                foreach ((string key, Type value) in CodeTypes.Instance.GetTypes())
                {
                    parser.RegisterType(regex.Match(key).Value, value);
                    if (value.IsAbstract && value.IsSealed)
                    {
                        parser.RegisterStaticType(value);
                    }
                }
                parser.RegisterType("int", typeof(int));
                parser.RegisterType("float", typeof(float));
                parser.RegisterType("double", typeof(double));
                parser.RegisterType("string", typeof(string));
                parser.RegisterType("bool", typeof(bool));
                parser.RegisterVariable<Scene>("Root");
            };
        }

        
```

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

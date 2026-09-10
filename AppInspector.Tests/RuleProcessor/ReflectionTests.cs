using System.IO;
using System.Linq;
using System.Text;
using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.CST.RecursiveExtractor;
using Xunit;

namespace AppInspector.Tests.RuleProcessor;

public class ReflectionTests
{
    private readonly Microsoft.ApplicationInspector.RulesEngine.Languages _languages = new();

    [Fact]
    public void DetectMethodInfoInvoke()
    {
        var testCode = @"
using System;
using System.Reflection;

class Program
{
    static void Main()
    {
        Type type = typeof(MyClass);
        MethodInfo method = type.GetMethod(""MyMethod"");
        method.Invoke(obj, new object[] { });
    }
}";
        RuleSet rules = new();
        rules.AddDirectory("/home/runner/work/ApplicationInspector/ApplicationInspector/AppInspector/rules/default/os");
        var processor = new Microsoft.ApplicationInspector.RulesEngine.RuleProcessor(rules, new RuleProcessorOptions());
        
        if (_languages.FromFileNameOut("test.cs", out var info))
        {
            var matches = processor.AnalyzeFile(testCode, new FileEntry("test.cs", new MemoryStream()), info);
            var methodInvokeMatches = matches.Where(m => m.Tags?.Contains("OS.Reflection.MethodInvocation") ?? false).ToList();
            Assert.NotEmpty(methodInvokeMatches);
        }
        else
        {
            Assert.Fail("Failed to get language info");
        }
    }

    [Fact]
    public void DetectConstructorInfoInvoke()
    {
        var testCode = @"
using System;
using System.Reflection;

class Program
{
    static void Main()
    {
        Type type = typeof(MyClass);
        ConstructorInfo constructor = type.GetConstructor(Type.EmptyTypes);
        object instance = constructor.Invoke(null);
    }
}";
        RuleSet rules = new();
        rules.AddDirectory("/home/runner/work/ApplicationInspector/ApplicationInspector/AppInspector/rules/default/os");
        var processor = new Microsoft.ApplicationInspector.RulesEngine.RuleProcessor(rules, new RuleProcessorOptions());
        
        if (_languages.FromFileNameOut("test.cs", out var info))
        {
            var matches = processor.AnalyzeFile(testCode, new FileEntry("test.cs", new MemoryStream()), info);
            var constructorInvokeMatches = matches.Where(m => m.Tags?.Contains("OS.Reflection.ConstructorInvocation") ?? false).ToList();
            Assert.NotEmpty(constructorInvokeMatches);
        }
        else
        {
            Assert.Fail("Failed to get language info");
        }
    }

    [Fact]
    public void DetectAssemblyLoad()
    {
        var testCode = @"
using System;
using System.Reflection;

class Program
{
    static void Main()
    {
        Assembly assembly1 = Assembly.Load(""MyAssembly"");
        Assembly assembly2 = Assembly.LoadFrom(""path/to/assembly.dll"");
        Assembly assembly3 = Assembly.LoadFile(""C:\\path\\to\\assembly.dll"");
    }
}";
        RuleSet rules = new();
        rules.AddDirectory("/home/runner/work/ApplicationInspector/ApplicationInspector/AppInspector/rules/default/os");
        var processor = new Microsoft.ApplicationInspector.RulesEngine.RuleProcessor(rules, new RuleProcessorOptions());
        
        if (_languages.FromFileNameOut("test.cs", out var info))
        {
            var matches = processor.AnalyzeFile(testCode, new FileEntry("test.cs", new MemoryStream()), info);
            var assemblyLoadMatches = matches.Where(m => m.Tags?.Contains("OS.Reflection.AssemblyLoading") ?? false).ToList();
            // Should detect Assembly.Load (LoadFrom and LoadFile are in load_dll.json)
            Assert.NotEmpty(assemblyLoadMatches);
        }
        else
        {
            Assert.Fail("Failed to get language info");
        }
    }

    [Fact]
    public void DetectInvokeMember()
    {
        var testCode = @"
using System;
using System.Reflection;

class Program
{
    static void Main()
    {
        Type type = typeof(MyClass);
        object result = type.InvokeMember(""MyMethod"", 
            BindingFlags.InvokeMethod, null, obj, new object[] { });
    }
}";
        RuleSet rules = new();
        rules.AddDirectory("/home/runner/work/ApplicationInspector/ApplicationInspector/AppInspector/rules/default/os");
        var processor = new Microsoft.ApplicationInspector.RulesEngine.RuleProcessor(rules, new RuleProcessorOptions());
        
        if (_languages.FromFileNameOut("test.cs", out var info))
        {
            var matches = processor.AnalyzeFile(testCode, new FileEntry("test.cs", new MemoryStream()), info);
            var invokeMemberMatches = matches.Where(m => m.Tags?.Contains("OS.Reflection.InvokeMember") ?? false).ToList();
            Assert.NotEmpty(invokeMemberMatches);
        }
        else
        {
            Assert.Fail("Failed to get language info");
        }
    }

    [Fact]
    public void DetectActivatorCreateInstance()
    {
        var testCode = @"
using System;

class Program
{
    static void Main()
    {
        object instance1 = Activator.CreateInstance(typeof(MyClass));
        object instance2 = Activator.CreateInstance(""MyAssembly"", ""MyNamespace.MyClass"");
    }
}";
        RuleSet rules = new();
        rules.AddDirectory("/home/runner/work/ApplicationInspector/ApplicationInspector/AppInspector/rules/default/os");
        var processor = new Microsoft.ApplicationInspector.RulesEngine.RuleProcessor(rules, new RuleProcessorOptions());
        
        if (_languages.FromFileNameOut("test.cs", out var info))
        {
            var matches = processor.AnalyzeFile(testCode, new FileEntry("test.cs", new MemoryStream()), info);
            var createInstanceMatches = matches.Where(m => m.Tags?.Contains("OS.Reflection.CreateInstance") ?? false).ToList();
            Assert.NotEmpty(createInstanceMatches);
        }
        else
        {
            Assert.Fail("Failed to get language info");
        }
    }

    [Fact]
    public void DetectGetMethod()
    {
        var testCode = @"
using System;
using System.Reflection;

class Program
{
    static void Main()
    {
        Type type = typeof(MyClass);
        MethodInfo method = type.GetMethod(""MyMethod"");
    }
}";
        RuleSet rules = new();
        rules.AddDirectory("/home/runner/work/ApplicationInspector/ApplicationInspector/AppInspector/rules/default/os");
        var processor = new Microsoft.ApplicationInspector.RulesEngine.RuleProcessor(rules, new RuleProcessorOptions());
        
        if (_languages.FromFileNameOut("test.cs", out var info))
        {
            var matches = processor.AnalyzeFile(testCode, new FileEntry("test.cs", new MemoryStream()), info);
            var getMethodMatches = matches.Where(m => m.Tags?.Contains("OS.Reflection.GetMethod") ?? false).ToList();
            Assert.NotEmpty(getMethodMatches);
        }
        else
        {
            Assert.Fail("Failed to get language info");
        }
    }

    [Fact]
    public void DetectGetType()
    {
        var testCode = @"
using System;

class Program
{
    static void Main()
    {
        Type type = Type.GetType(""System.String"");
    }
}";
        RuleSet rules = new();
        rules.AddDirectory("/home/runner/work/ApplicationInspector/ApplicationInspector/AppInspector/rules/default/os");
        var processor = new Microsoft.ApplicationInspector.RulesEngine.RuleProcessor(rules, new RuleProcessorOptions());
        
        if (_languages.FromFileNameOut("test.cs", out var info))
        {
            var matches = processor.AnalyzeFile(testCode, new FileEntry("test.cs", new MemoryStream()), info);
            var getTypeMatches = matches.Where(m => m.Tags?.Contains("OS.Reflection.GetType") ?? false).ToList();
            Assert.NotEmpty(getTypeMatches);
        }
        else
        {
            Assert.Fail("Failed to get language info");
        }
    }

    [Fact]
    public void NoFalsePositiveOnNonReflectionCode()
    {
        var testCode = @"
using System;

class Program
{
    static void Main()
    {
        Console.WriteLine(""Hello World"");
        var myClass = new MyClass();
        myClass.DoSomething();
    }
}";
        RuleSet rules = new();
        rules.AddDirectory("/home/runner/work/ApplicationInspector/ApplicationInspector/AppInspector/rules/default/os");
        var processor = new Microsoft.ApplicationInspector.RulesEngine.RuleProcessor(rules, new RuleProcessorOptions());
        
        if (_languages.FromFileNameOut("test.cs", out var info))
        {
            var matches = processor.AnalyzeFile(testCode, new FileEntry("test.cs", new MemoryStream()), info);
            var reflectionMatches = matches.Where(m => 
                m.Tags?.Any(tag => tag.Contains("OS.Reflection")) ?? false).ToList();
            Assert.Empty(reflectionMatches);
        }
        else
        {
            Assert.Fail("Failed to get language info");
        }
    }
}

namespace CSVHelperGenerator {
  using Microsoft.CodeAnalysis;
  using Microsoft.CodeAnalysis.CSharp;
  using Microsoft.CodeAnalysis.CSharp.Syntax;
  using Microsoft.CodeAnalysis.Text;

  using System;
  using System.Collections.Generic;
  using System.Collections.Immutable;
  using System.Diagnostics;
  using System.Linq;
  using System.Text;

  //  [Generator]
  //  public class Gen : ISourceGenerator {
  //    public void Initialize(GeneratorInitializationContext context) {

  //    }

  //    public void Execute(GeneratorExecutionContext context) {
  //      //var Txts = context.AdditionalFiles.First(E => E.Path.EndsWith(".txt"))
  //      //  .GetText(context.CancellationToken).ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
  //      //  .Select(E => $@"Console.WriteLine(""From {E}"");");
  //      //{string.Join(Environment.NewLine, Txts)}
  //      var Code = $@"
  //using System;
  //namespace CodeGed{{
  //  public static class Generated{{
  //    public static void ReadText(){{
  //      Console.WriteLine(""StartText"");
  //    }}
  //  }}
  //}}
  //";
  //      context.AddSource("g1", SourceText.From(Code, new UTF8Encoding(false)));

  //    }
  //  }

  //  [Generator]
  //  public class AutoRegister2DI : ISourceGenerator {
  //    const string AttrCode = @"
  //namespace CodeGed{
  //  using System;
  //  [AttributeUsage(AttributeTargets.Class,Inherited=false,AllowMultiple=false)]
  //  public class AutoRegisterAttribute:Attribute{}
  //}
  //";

  //    public void Initialize(GeneratorInitializationContext context) {
  //      context.RegisterForSyntaxNotifications(() => new RegisterReceiver());
  //    }

  //    public void Execute(GeneratorExecutionContext context) {
  //      //if (!Debugger.IsAttached) Debugger.Launch();
  //      context.AddSource("g2", SourceText.From(AttrCode, new UTF8Encoding(false)));
  //      if (context.SyntaxReceiver is RegisterReceiver Receiver) {
  //        CSharpParseOptions Options = (context.Compilation as CSharpCompilation).SyntaxTrees[0].Options as CSharpParseOptions;
  //        var AttributeTree = CSharpSyntaxTree.ParseText(SourceText.From(AttrCode, new UTF8Encoding(false)), Options);
  //        var Compilation = context.Compilation.AddSyntaxTrees(AttributeTree);
  //        var Symbol = Compilation.GetTypeByMetadataName("CodeGed.AutoRegisterAttribute");
  //        List<string> RegCalls = new List<string>();
  //        foreach (var C in Receiver.Classes) {
  //          SemanticModel Model = Compilation.GetSemanticModel(C.SyntaxTree);
  //          if (Model.GetDeclaredSymbol(C) is ITypeSymbol TSymbol
  //            && TSymbol.GetAttributes()
  //            .Any(E => E.AttributeClass.Equals(Symbol, SymbolEqualityComparer.Default))) {
  //            var Name = C.GetFullDefined() + "." + C.Identifier.Text;
  //            RegCalls.Add($"Services.AddSingleton<{Name}>();");
  //          }
  //        }
  //        var Full = $@"
  //          namespace CodeGed{{
  //            using System;
  //            using Microsoft.Extensions.DependencyInjection;
  //            public static class AutoRegKits{{
  //              public static void GeneratedRegister(this IServiceCollection Services){{
  //                {string.Join(Environment.NewLine, RegCalls)}
  //              }}
  //            }}
  //          }}
  //        ";
  //        context.AddSource("g3", SourceText.From(Full, new UTF8Encoding(false)));
  //      }
  //    }

  //    public class RegisterReceiver : ISyntaxReceiver {


  //      public List<ClassDeclarationSyntax> Classes { get; } = new List<ClassDeclarationSyntax>();
  //      public void OnVisitSyntaxNode(SyntaxNode syntaxNode) {
  //        if (syntaxNode is ClassDeclarationSyntax CDS && CDS.AttributeLists.Count > 0) {
  //          Classes.Add(CDS);
  //        }
  //      }
  //    }
  //  }

  public static class SyntaxKits {
    public static bool TryGetParentSyntax<T>(this SyntaxNode Node, out T Parent) where T : SyntaxNode {
      Parent = null;
      if (Node == null) {
        return false;
      }
      try {
        Node = Node.Parent;
        if (Node == null) {
          return false;
        }
        if (Node.GetType() == typeof(T)) {
          Parent = Node as T;
          return true;
        }
        return TryGetParentSyntax<T>(Node, out Parent);
      }
      catch {
        return false;
      }
    }
    public static string GetFullDefined(this SyntaxNode Node) {
      var PNode = Node;
      string Res = "";
      while (PNode != null) {
        if (PNode is NamespaceDeclarationSyntax ND) {
          Res += ".";
          Res = Res + ND.Name.ToString();
        }
        PNode = PNode.Parent;
      }
      return Res.Trim('.');
    }
  }



  [Generator]
  public class CSVModelAttributeGenerator : ISourceGenerator {
    internal const string ModelAttrCode =
@"
namespace CSV {
  using System;
  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
  public class CSVTableAttribute : Attribute {
    public const string DefaultTableName = ""default"";
    public string Name { get; set; } = DefaultTableName;
    public const char Separator = ',';
    public const string NewLine = ""\r\n"";
  }
}
";
    internal const string ColumnAttrCode =
@"
namespace CSV{
 using System;
 using System.Reflection;
 [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = false)]
  public class CSVColumnAttribute : Attribute, IComparable<CSVColumnAttribute> {
    public string HeaderText { get; set; }
    public int Order { get; set; }
    public string TableName { get; set; } = CSVTableAttribute.DefaultTableName;
    public CSVColumnAttribute(string Header) { HeaderText = Header; }
    public int CompareTo(CSVColumnAttribute other) {
      return other == null ? 0 : Order.CompareTo(other.Order);
    }
    internal MemberInfo Member { get; set; }
    internal int PreAllocIndex { get; set; }
  }
}
";
    internal const string KitsCode =
@"
namespace CSV{
  public partial static class CSVGendKits{
    internal static readonly 
    static CSVGendKits(){
      {GeneratedDict}
    }
    public static string ToCSV({IsStructHasIn} this {Type} This){
    
    }
  }
}
";

    public void Initialize(GeneratorInitializationContext context) {
    }

    class CSVModelSyntax {
      public string NameSpace;
      public string Model;
      public bool IsStruct;
      public List<CSVColumnSyntax> Tables;
      public string ToDefineString() =>
$@"
namespace {NameSpace}{{
  internal class {Model}FromCSVAutoGenerated{{
    
  }}
}}
";
      public string ToSerializeString() =>
$@"

";
      public string ToDeserializeString() =>
$@"
 
";
    }
    class CSVTableSyntax {
      public CSVModelSyntax Model;
      public string TableName;
      public List<CSVColumnSyntax> Columns;
      public string ToDefineString() =>
$@"
internal class {TableName}ToCSVAutoGenerated{{
  {string.Join(Environment.NewLine, Columns.Select(E => E.ToDefineString()))}
}}
";
      public string ToSerializeString() =>
$@"
  
";
      public string ToDeserializeString() =>
$@"
  
";
    }
    class CSVColumnSyntax {
      public string HeaderName;
      public string ModelFullName;
      public string FieldPath;
      public int Order;
      public bool IsStruct;
      public string ValueType;
      public string ToDefineString() =>
$@"

";
      public string ToSerializeString() =>
$@"
internal static string ToCSVRaw({(IsStruct ? "in " : "")}{ModelFullName} This){{
  return This.{FieldPath}.ToString();
}} 
";
      public string ToDeserializeString() =>
$@"
internal static void ToCSVModel({(IsStruct ? "ref " : "")}{ModelFullName} This){{
  This.{FieldPath} = Convert.ChangeType(,typeof(ValueType));
}} 
";
    }

    public void Execute(GeneratorExecutionContext context) {
      context.AddSource("CSVGeneratedAttrs", SourceText.From(ModelAttrCode + ColumnAttrCode, new UTF8Encoding(false)));
      if (context.SyntaxReceiver is CSVAttrReveiver Receiver) {
        CSharpParseOptions Options = (context.Compilation as CSharpCompilation).SyntaxTrees[0].Options as CSharpParseOptions;
        var AttributeTree = CSharpSyntaxTree.ParseText(SourceText.From(ModelAttrCode, new UTF8Encoding(false)), Options);
        var Compilation = context.Compilation.AddSyntaxTrees(AttributeTree);
        var Symbol = Compilation.GetTypeByMetadataName("CSV.CSVTableAttribute");
        foreach (var CS in Receiver.Models) {
          SemanticModel Model = Compilation.GetSemanticModel(CS.SyntaxTree);
          if (CS is ClassDeclarationSyntax CDS) {
            if (Model.GetDeclaredSymbol(CDS) is ITypeSymbol TSymbol) {
              var Attrs = TSymbol.GetAttributes().Where(E => E.AttributeClass.Equals(Symbol, SymbolEqualityComparer.Default));
              foreach (var Attr in Attrs) {
                var TableName = Attr.NamedArguments.FirstOrDefault(E => E.Key == "Name").Value.Value?.ToString() ?? "default";
                //CDS.Members.Where(E => E.AttributeLists.Count > 0).


              }
            }
          }
        }

      }

    }
  }

  [Generator]
  public class CSVAttrReveiver : ISyntaxReceiver {
    public List<TypeDeclarationSyntax> Models { get; } = new List<TypeDeclarationSyntax>();
    public void OnVisitSyntaxNode(SyntaxNode syntaxNode) {
      Debug.WriteLine(syntaxNode.GetType());
      if (syntaxNode is ClassDeclarationSyntax CDS && CDS.AttributeLists.Count > 0) {
        Models.Add(CDS);
      }
      else if (syntaxNode is StructDeclarationSyntax SDS && SDS.AttributeLists.Count > 0) {
        Models.Add(SDS);
      }
    }
  }
}

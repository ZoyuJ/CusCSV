namespace CSVHelperGenerator {
  using Microsoft.CodeAnalysis;
  using Microsoft.CodeAnalysis.CSharp;
  using Microsoft.CodeAnalysis.CSharp.Syntax;
  using Microsoft.CodeAnalysis.Text;

  using System;
  using System.Collections.Generic;
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

    /// <summary>
    /// Display global::Namespace.Class/Struct
    /// </summary>
    /// <param name="This"></param>
    /// <returns></returns>
    public static string FullDeclaration(this ITypeSymbol This) {
      //=> This.ToDisplayString(new SymbolDisplayFormat(SymbolDisplayGlobalNamespaceStyle.Included));
      var Res = This.Name;
      var Np = This.ContainingSymbol;
      while (!(Np as INamespaceSymbol)?.IsGlobalNamespace ?? false) {
        Res = Np.Name + "." + Res;
        Np = Np.ContainingSymbol;
      }
      return "global::" + Res;
    }

  }



  [Generator]
  public class CSVModelAttributeGenerator : ISourceGenerator {
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
#if DEBUG
      //if (!Debugger.IsAttached) { Debugger.Launch(); }
#endif
      context.RegisterForSyntaxNotifications(() => new CSVAttrReveiver());
    }

    class TableDefineComperar : IEqualityComparer<AttributeData> {
      public bool Equals(AttributeData x, AttributeData y) {
        return EqualityComparer<string>.Default.Equals(x.NamedArguments.FirstOrDefault(E => E.Key == "Name").Value.Value as string, y.NamedArguments.FirstOrDefault(E => E.Key == "Name").Value.Value as string);
      }

      public int GetHashCode(AttributeData obj) {
        return obj.GetHashCode();
      }
    }

    class CSVModelSyntax {
      public readonly string NameSpace;
      public readonly string Model;
      public readonly bool IsStruct;
      public readonly List<CSVTableSyntax> Tables;
      public CSVModelSyntax() { }
      protected CSVModelSyntax(ITypeSymbol Symbol) {
        NameSpace = Symbol.FullDeclaration();
        Tables = Symbol.GetAttributes().Where(E => E.AttributeClass.FullDeclaration() == "global::CSV.CSVTableAttribute")
       .Distinct(new TableDefineComperar())
       .Select(E => new CSVTableSyntax(E, Symbol))
       .ToList();
      }
      public CSVModelSyntax(SemanticModel Model, ClassDeclarationSyntax CDS) : this(Model.GetDeclaredSymbol(CDS) as ITypeSymbol) {
        IsStruct = false;
      }
      public CSVModelSyntax(SemanticModel Model, StructDeclarationSyntax SDS) : this(Model.GetDeclaredSymbol(SDS) as ITypeSymbol) {
        IsStruct = true;
      }
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

      public CSVTableSyntax() { }
      public CSVTableSyntax(AttributeData Attr, ITypeSymbol TypeSymbol) {
        TableName = Attr.NamedArguments.FirstOrDefault(E => E.Key == "Name").Value.Value?.ToString() ?? "default";
      
      }

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
      public string ConverterToCSVMethod;
      public string ConverterFromCSVMethod;
      public string FieldPath;
      public int Order;
      public bool IsStruct;
      public string ValueType;

      public CSVColumnSyntax() { }
      public CSVColumnSyntax(string TableName, bool IsStruct, string ModelFullName, AttributeData Attr, ISymbol Member) {
        this.IsStruct = IsStruct;
        this.ModelFullName = ModelFullName;
        if (Member is IFieldSymbol FMember) {
          HeaderName = (Attr.NamedArguments.FirstOrDefault(E => E.Key == "HeaderText").Value.Value as string) ?? FMember.Name;
          Order = (int)(Attr.NamedArguments.FirstOrDefault(E => E.Key == "Order").Value.Value);
          ConverterToCSVMethod = (Attr.NamedArguments.FirstOrDefault(E => E.Key == "HeaderText").Value.Value as string);
        }
        else if (Member is IPropertySymbol PMember) {

        }
      }

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
      if (context.SyntaxReceiver is CSVAttrReveiver Receiver) {
        foreach (var CS in Receiver.Models) {
          SemanticModel Model = context.Compilation.GetSemanticModel(CS.SyntaxTree);
          if (CS is ClassDeclarationSyntax CDS) {
            new CSVModelSyntax(Model, CDS);
          }
        }

      }

    }

    //private CSVTableSyntax OneTable(string TableName, SemanticModel Model, ITypeSymbol TSymbol) {
    //  var Table = new CSVTableSyntax();
    //  var Cols = TSymbol.GetMembers()
    //    .Where(E => !E.IsStatic && (E.Kind == SymbolKind.Field || E.Kind == SymbolKind.Field))
    //    .Select(E => (E, E.GetAttributes().FirstOrDefault(EE => EE.AttributeClass.Name == "CSV.CSVColumnAttribute" && (EE.NamedArguments.FirstOrDefault(EEE => EEE.Key == "TableName").Value.Value as string == TableName))))
    //    .Where(E => E.Item2 != null)
    //    .Select(E => new CSVColumnSyntax());

    //  return Table;
    //}

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

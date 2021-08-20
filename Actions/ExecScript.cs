using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;

namespace XInstall.Core.Actions {
  /// <summary>
  /// Summary description for ExecScript.
  /// </summary>

  public class ExecScript : ActionElement, IAction {
    string _Language = String.Empty;

    [Action("runscript")]
    public ExecScript() { }

    public override void ParseActionElement() {}

    [Action("language", Needed=false, Default="C#")]
    public string Language {

      get {
        return this._Language;
      }

      set {
        this._Language = value;

        switch ( this._Language ) {
          case "C#":
            break;

          case "CSharp":
            break;

          case "J#":
            break;

          case "VBNet":
            break;

          default :
            base.FatalErrorMessage( ".", String.Format( @"unknown language specified: {0}", this._Language ), 1660, -99 );
            break;
        }
      }
    }

#region IAction Members

    public override void Execute() {
      base.Execute();
    }

    public new bool IsComplete {
      get {
        return base.IsComplete;
      }
    }

    public new string ExitMessage {
      get {
        return null;
      }
    }

    public new string Name {
      get {
        return this.GetType().Name;
      }
    }

    public new int ExitCode {
      get {
        return 0;
      }
    }

#endregion
  }

  internal class CodeRunner {

    private static
      CodeDomProvider CodeProvider = null;

    private CodeRunner() {}

    private CodeDomProvider GetLangProvider( string Lang ) {

      CodeDomProvider CodeProvider = null;

      switch ( Lang ) {
        case "C#":
          CodeProvider = new Microsoft.CSharp.CSharpCodeProvider();
          break;
        case "CSharp":
          CodeProvider = new Microsoft.CSharp.CSharpCodeProvider();
          break;
        case "VBNet":
          CodeProvider = new Microsoft.VisualBasic.VBCodeProvider();
          break;
        case "VB":
          CodeProvider = new Microsoft.VisualBasic.VBCodeProvider();
          break;
      }

      return CodeProvider;
    }

    private string CreateSkeletenCode( string Lang ) {
      CodeProvider                     = this.GetLangProvider( Lang );
      CodeNamespace       Namespace    = new CodeNamespace( @"RunScript" );
      CodeTypeDeclaration CodeTypeDecl = new CodeTypeDeclaration();

      // now import necessary namespace
      CodeNamespaceImport[] NamespaceImports = {
        new CodeNamespaceImport( @"System" ),
        new CodeNamespaceImport( @"System.Collections" ),
        new CodeNamespaceImport( @"System.Data" ),
        new CodeNamespaceImport( @"System.Data.SqlClient" ),
        new CodeNamespaceImport( @"System.IO" ),
        new CodeNamespaceImport( @"System.Xml" ),
      };
      Namespace.Imports.AddRange( NamespaceImports );

      // create namespace
      CodeTypeDecl.Name = @"RunScript";
      Namespace.Types.Add( CodeTypeDecl );
      CodeTypeDecl.IsClass = true;

      // create constructor
      CodeConstructor CCtor = new CodeConstructor();
      CCtor.Attributes      = MemberAttributes.Public;
      CodeTypeDecl.Members.Add( CCtor );

      // now create an entry point method which is public static void Main
      CodeMemberMethod MainMethod = new CodeMemberMethod();
      MainMethod.Name = @"Main";
      MainMethod.Attributes = MemberAttributes.Public |
        MemberAttributes.Static;
      MainMethod.ReturnType = new CodeTypeReference( typeof(void) );

      // build the body of Main Method
      CodeMethodInvokeExpression InvokeRunScript =
        new CodeMethodInvokeExpression( new CodeTypeReferenceExpression( @"RunScript"), @"ExecuteScript", new CodeExpression[0] );
      MainMethod.Statements.Add( new CodeExpressionStatement( InvokeRunScript ) );

      // add public static void Main() into class
      CodeTypeDecl.Members.Add( MainMethod );

      // now, we need to generate the actual code
      // first create a compiler base on passed in code provider
      // and feed in proper options for generating code
      ICodeGenerator CodeGenerator    = CodeProvider.CreateGenerator();
      CompilerParameters cp           = new CompilerParameters();
      CodeGeneratorOptions GenOpt     = new CodeGeneratorOptions();
      cp.GenerateInMemory             = true;
      GenOpt.BlankLinesBetweenMembers = true;
      GenOpt.BracingStyle             = @"C";
      GenOpt.ElseOnClosing            = true;

      StringWriter CodeBuffer = new StringWriter();
      CodeGenerator.GenerateCodeFromNamespace( Namespace, CodeBuffer, GenOpt );

      return CodeBuffer.GetStringBuilder().ToString();
    }

    private string ReadScriptSource( string FileName ) {
      string Code = String.Empty;

      try {
        using( StreamReader sr = new StreamReader( FileName ) ) {
          Code = sr.ReadToEnd();
        }

      } catch ( FileNotFoundException FileNotFound ) {
        throw FileNotFound;

      } catch ( Exception e ) {
        throw e;
      }

      return Code;
    }

    private string ReadScriptSource( XmlNode ScriptNode ) {
      string Code = String.Empty;

      if ( ScriptNode.NodeType == XmlNodeType.CDATA ) {
        Code = ScriptNode.Value;
      }

      return Code;
    }

    private Assembly CompileAssembly( string SourceCode, params object[] parameters ) {
      ICodeCompiler CodeCompiler = CodeProvider.CreateCompiler();
      CompilerParameters CParams = new CompilerParameters();
      CParams.ReferencedAssemblies.AddRange( new string[] { @"System.dll", @"System.Xml.dll", @"System.Data.dll" } );
      CParams.GenerateInMemory = true;
      CompilerResults cr       = CodeCompiler.CompileAssemblyFromSource( CParams, SourceCode );

      if ( cr.Errors.Count > 0 ) {
        IEnumerator Enumerator = cr.Errors.GetEnumerator();

        while ( Enumerator.MoveNext() ) {
          CompilerError Error = (CompilerError) Enumerator.Current;
          Console.Error.WriteLine( Error );
          Environment.Exit( -1 );
        }
      }

      return cr.CompiledAssembly;
    }
  }
}











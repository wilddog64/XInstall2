using System;
using System.IO;
using System.Reflection;
using System.Runtime.Remoting;
using System.Xml;

namespace XInstall.Core.Actions
{
    /// <summary>
    /// Summary description for nant.
    /// </summary>
    public class Nant : ActionElement, IAction
    {
	    XmlNode   _xn        = null;
	    string[] _DefSymbols = null;
	    string   _BuildFile  = String.Empty;
	    string   _LogFile    = String.Empty;
	    string   _CodeBase   = String.Empty;
	    string   _Target     = String.Empty;

	    [Action("nant")]
	    public Nant( XmlNode ActionNode )
	    {
		    this._xn = ActionNode;
	    }

	    #region nant attributes

	    [Action("codebase", Needed=true)]
	    public string CodeBase
	    {
		    get
		    {
			    return this._CodeBase;
		    }
		    set
		    {
			    this._CodeBase = String.Format( @"file://{0}", value );
		    }
	    }

	    [Action("buildfile", Needed=false)]
	    public string BuildFile
	    {
		    get
		    {
			    return String.Format( @"-buildfile:{0}", this._BuildFile );
		    }
		    set
		    {
			    this._BuildFile = value;
			    if ( !File.Exists( this._BuildFile ) )
			    {
				    base.FatalErrorMessage(
					".",
					String.Format( @"file {0} is not found!", this._BuildFile ),
					1660, -1 );
			    }
		    }
	    }

	    [Action("target", Needed=false)]
	    public string Target
	    {
		    get
		    {
			    return this._Target;
		    }
		    set
		    {
			    this._Target = value;
		    }
	    }

	    [Action("logfile", Needed=false)]
	    public string LogFile
	    {
		    get
		    {
			    return this._LogFile;
		    }
		    set
		    {
			    this._LogFile = value;
		    }
	    }

	    [Action("symbols", Needed=false)]
	    public string DefSymbol
	    {
		    set
		    {
			    string Symbol = value;
			    if ( Symbol.IndexOf( @"," ) > -1 )
			    {
				    this._DefSymbols = Symbol.Split( new char[] { ',' } );
			    }
			    else
			    {
				    this._DefSymbols = new string[1] { Symbol };
			    }
		    }
	    }

	    /// <summary>
	    /// set flag to indicate if object is going to skip any error
	    /// </summary>
	    [Action("skiperror", Needed=false, Default="false")]
	    public new string SkipError
	    {
		    set
		    {
			    base.SkipError = bool.Parse( value );
		    }
	    }
	    #endregion

	    public override void ParseActionElement()
	    {
		    Console.WriteLine( @"Current Directory Is: {0}", Environment.CurrentDirectory);
		    base.ParseActionElement();
		    this.ExecuteNant( this.BuildFile, this.Target );
	    }

	    public override string ObjectName
	    {
		    get
		    {
			    return this.Name;
		    }
	    }


	    #region IAction Members

	    public override void Execute()
	    {
		    // TODO:  Add Nant.Execute implementation
		    base.Execute();
		    base.IsComplete = true;
	    }

	    public new bool IsComplete
	    {
		    get
		    {
			    return base.IsComplete;
		    }
	    }

	    public new string ExitMessage
	    {
		    get
		    {
			    return null;
		    }
	    }

	    public new string Name
	    {
		    get
		    {
			    return this.GetType().Name;
		    }
	    }

	    public new int ExitCode
	    {
		    get
		    {
			    return 0;
		    }
	    }

	    #endregion

	    #region private methods/properties

	    private void ExecuteNant( params string[] parameters )
	    {
		    AssemblyName an       = new AssemblyName();
		    an.CodeBase           = this.CodeBase;
		    Assembly     assembly = Assembly.Load( an );
		    Type         t        = assembly.EntryPoint.ReflectedType;
		    MethodInfo   mi       = t.GetMethod(
						@"Main", BindingFlags.Public | BindingFlags.Static,
						null, new Type[] { typeof( string[] ) }, null);
		    object       obj      = Activator.CreateInstance( t, true );

		    mi.Invoke( obj, new object[1] { parameters } );
	    }

	    private AppDomain LoadNant2AppDomain( string ApplicationBase )
	    {
		    AppDomainSetup AppSetupInfo  = new AppDomainSetup();
		    AppSetupInfo.ApplicationBase = ApplicationBase;
		    AppDomain thisAppDomain      = AppDomain.CreateDomain( @"Nant", null, AppSetupInfo );

		    return thisAppDomain;
	    }
	    #endregion
    }
}

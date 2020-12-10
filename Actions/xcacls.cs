using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Xml;

namespace XInstall.Core.Actions
{
    /// <summary>
    /// wraps xcacls command
    /// </summary>
    public class xcacls : ExternalPrg
    {
	    private XmlNode _ActionNode     = null;
	    private StringBuilder _Args     = new StringBuilder();

	    private readonly string _xcacls = @"xcacls.exe";
	    private string _ProgramPath     = String.Empty;
	    private string _Container       = String.Empty;
	    private string _Owner           = String.Empty;
	    private bool   _AddNewOwner     = true;

	    public enum _ContainerType
	    {
		    FILE = 0,
		    DIRECTORY,
	    }

	    _ContainerType _Type = _ContainerType.FILE;

	    [Action("xcacls")]
	    public xcacls( XmlNode xn )
	    {
		    this._ActionNode = xn;
		    this._Args.Append( "/G " );
	    }

	    #region xcalcs public properties
	    [Action("runnable", Needed=false, Default="true")]
	    public new string Runnable
	    {
		    set
		    {
			    base.Runnable = bool.Parse( value );
		    }
	    }

	    [Action("progpath", Needed=false, Default="")]
	    public string ProgramPath
	    {
		    get
		    {
			    return Path.Combine( this._ProgramPath, this._xcacls );
		    }
		    set
		    {
			    if ( value.Length == 0 )
				    this._ProgramPath =
					Path.Combine( Environment.GetEnvironmentVariable( @"WINDIR" ),
						      @"System32" );
			    else
			    {
				    this._ProgramPath = value;
				    if ( !Directory.Exists( this._ProgramPath ) )
				    {
					    throw new DirectoryNotFoundException(
						String.Format( @"Cannot find directory: {0}",
							       this._ProgramPath ) );
				    }
			    }
		    }
	    }

	    [Action("newowner", Needed=false, Default="true")]
	    public string AddNewOwner
	    {
		    set
		    {
			    this._AddNewOwner = bool.Parse( value );
			    if ( this._AddNewOwner )
			    {
				    this._Args.Insert( 0, "/E " );
			    }
		    }
	    }

	    [Action("type", Needed=true)]
	    public string ContainerType
	    {
		    set
		    {
			    switch ( value.ToUpper() )
			    {
			    case "FILE":
				    this._Type = _ContainerType.FILE;
				    break;
			    case "DIRECTORY":
				    this._Type = _ContainerType.DIRECTORY;
				    break;
			    }
		    }
	    }

	    [Action("container", Needed=true)]
	    public string Container
	    {
		    get
		    {
			    return this._Container;
		    }
		    set
		    {
			    this._Container = value;

			    switch ( this._Type )
			    {
			    case _ContainerType.FILE:
				    if ( !File.Exists( this._Container ) )
					    throw new FileNotFoundException(
						String.Format( "cannot find file: {0}",
							       this._Container ) );
				    break;
			    case _ContainerType.DIRECTORY:
				    if ( !Directory.Exists ( this._Container ) )
				    {
					    Directory.CreateDirectory( this._Container );
				    }
				    break;
			    }
		    }
	    }

	    [Action("permissions", Needed=true)]
	    public string Permissions
	    {
		    set
		    {
			    switch ( value.ToUpper() )
			    {
			    case "READ":
				    this._Args.Append(":R");
				    break;
			    case "WRITE":
				    this._Args.Append(":W");
				    break;
			    case "READWRITE":
				    this._Args.Append(":RW");
				    break;
			    case "FULL":
				    this._Args.Append(":F");
				    break;
			    case "MODIFY":
				    this._Args.Append(":C");
				    break;
			    default:
				    throw new ArgumentException(
					String.Format(
					    "unknown permission arguments {0}", value),
					"Permissions");
			    }
		    }
	    }

	    [Action("owner", Needed=true)]
	    public string Ownwer
	    {
		    set
		    {
			    this._Owner = value;
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

	    #region IAction Members

	    protected override string GetArguments()
	    {
		    string Args = this._Args.ToString();

		    // if UseWinFolder is set to true then container will
		    // be created under the windows folder.
		    Args = this.Container                                +
			   " "                                           +
			   Args.Insert( Args.IndexOf(":"), this._Owner ) +
			   " /Y";

		    return Args;
	    }

	    protected override void ParseActionElement()
	    {
		    string Program =
			Path.Combine( this._ProgramPath,
				      this._xcacls) + " ";
		    base.ProgramName           = Program;
		    base.ProgramRedirectOutput = "true";
		    base.ParseActionElement();
	    }

	    protected override string ObjectName
	    {
		    get
		    {
			    return this.Name;
		    }
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
    }
}

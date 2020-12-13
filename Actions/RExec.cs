using System;
using System.IO;
using System.Management;
using System.Xml;

namespace XInstall.Core.Actions {
    /// <summary>
    /// Remote Execute a process using WMI, S
    /// </summary>
    public class RExec : ActionElement {
	    // private ManagementScope[] _ManagementScopes = null;

	    private string            _RemoteMachine    = null;
	    private string[]          _MachineList      = null;

	    [Action("rexec")]
	    public RExec( XmlNode ActionNode ) : base( ActionNode ) {}


	    #region protected properties
	    protected override object ObjectInstance {
		    get {
			    return this;
		    }
	    }

	    protected override string ObjectName {
		    get {
			    return GetType().Name;
		    }
	    }
	    #endregion


	    #region public properties
	    [Action("allowgenerateexception", Needed=false, Default="false")]
	    public new string AllowGenerateException {
		    set {
			    base.AllowGenerateException = bool.Parse( value );
		    }
	    }

	    [Action("runnable", Needed=false, Default="true")]
	    public new string Runnable {
		    set {
			    base.Runnable = bool.Parse( value );
		    }
	    }

	    [Action("machinelist", Needed=false, Default="")]
	    public string InputFileSource {
		    set {
			    this.SplitSourceFile( value );
		    }
	    }

	    [Action("machinename", Needed=false, Default="")]
	    public string RemoteMachine {
		    get {
			    return this._RemoteMachine;
		    }
		    set {
			    this._RemoteMachine = value;
		    }
	    }
	    #endregion


	    #region private methods
	    private void SplitSourceFile( string InputFile ) {
		    this._MachineList = this.SplitSourceFile( InputFile, '\n' );
		    // return this.SplitSourceFile( InputFile, '\n' );
	    }

	    private string[] SplitSourceFile( string InputFile, char Delim ) {
		    string[] MachineList = null;
		    if ( File.Exists( InputFile ) ) {
			    using( StreamReader sr = new StreamReader( InputFile, System.Text.Encoding.Default ) ) {
				    string OneLine = sr.ReadToEnd();
				    if ( OneLine.Length > 0 )
					    MachineList = OneLine.Split( new char[] { Delim } );
			    }
		    }
		    else {
			    string Message = @"{0}: File {1} does not exist!";
			    base.FatalErrorMessage( ".", Message, 1660, true );
		    }

		    return MachineList;
	    }
	    #endregion

    }
}

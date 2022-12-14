using System;
using System.Text;
using System.Xml;

namespace XInstall.Core.Actions
{
    /// <summary>
    /// Summary description for psexec.
    /// </summary>
    public class psexec : ExternalPrg
    {
	    private readonly string PSEXEC          = @"psexec.exe";
	    private readonly string DEF_PSEXEC_PATH = @"c:\tools\bin";

	    private XmlNode _ActionNode = null;

	    private string _ComputerName    = string.Empty;
	    private string _MachineListFile = string.Empty;
	    private string _Cmd2Exec        = string.Empty;
	    private string _UserName        = string.Empty;
	    private string _Password        = string.Empty;
	    private string _WorkingDir      = string.Empty;
	    private string _BasePath        = string.Empty;

	    private bool   _ForceCopy           = false;
	    private bool   _CopyProg            = false;
	    private bool   _CopyHighVerProg     = false;
	    private bool   _InteractWithDesktop = false;

	    [Action("psexec")]
	    public psexec( XmlNode ActionNode ) : base( ActionNode ) {
		    base.ProgramName           = PSEXEC;
		    base.ProgramRedirectOutput = "true";
		    this._ActionNode           = ActionNode;
	    }


	    [Action("machine", Needed=false, Default="")]
	    public string ComputerName {
		    get {
			    return this._ComputerName;
		    }
		    set
		    {
			    this._ComputerName = value;
		    }
	    }


	    [Action("machinelistfile", Needed=false, Default="")]
	    public string MachineListFile {
		    get {
			    return this._MachineListFile;
		    }
		    set {
			    this._MachineListFile = value;
		    }
	    }


	    [Action("cmd2exec", Needed=true)]
	    public string Cmd2Exec {
		    get {
			    return this._Cmd2Exec;
		    }
		    set {
			    this._Cmd2Exec = value;
		    }
	    }


	    [Action("username", Needed=false, Default="")]
	    public string UserName {
		    get {
			    return this._UserName;
		    }
		    set {
			    this._UserName = value;
		    }
	    }


	    [Action("password", Needed=false, Default="")]
	    public string Password {
		    get {
			    return this._Password;
		    }
		    set {
			    this._Password = value;
		    }
	    }


	    [Action("forcecopy", Needed=false, Default="false")]
	    public string ForceCopy {
		    set {
			    this._ForceCopy = bool.Parse( value );
		    }
	    }


	    [Action("rmtworkdir", Needed=false, Default="false")]
	    public string WorkingDirectory {
		    get {
			    return this._WorkingDir;
		    }
		    set {
			    this._WorkingDir = value;
		    }
	    }


	    [Action("copyprog", Needed=false, Default="false")]
	    public string CopyProgram {
		    set {
			    this._CopyProg = bool.Parse( value );
		    }
	    }


	    [Action("copyhighverprog", Needed=false, Default="false")]
	    public string CopyHighVerProg {
		    set {
			    this._CopyHighVerProg = bool.Parse( value );
		    }
	    }


	    [Action("interact", Needed=false, Default="false")]
	    public string InteractWithDesktop {
		    set {
			    this._InteractWithDesktop = bool.Parse( value );
		    }
	    }


	    [Action("basepath", Needed=false, Default="c:\tools\bin")]
	    public override string BasePath {
		    get {
			    return this._BasePath;
		    }
		    set {
			    this._BasePath = value;
		    }
	    }


	    protected override string ObjectName {
		    get {
			    return this.GetType().Name;
		    }
	    }

	    protected override object ObjectInstance {
		    get {
			    return this;
		    }
	    }

	    protected override string GetArguments() {
		    StringBuilder Arguments = new StringBuilder();

		    base.ProgramWorkingDirectory = DEF_PSEXEC_PATH;
		    string Message = String.Empty;

		    bool LengthOK = this.ComputerName.Length == 0    ||
				    this.MachineListFile.Length == 0;
		    if ( !LengthOK ) {
			    Message = "{0}: you have to provide ComputerName or MachineListFile";
			    base.FatalErrorMessage( ".", string.Format(Message, ObjectName), 1660 );
		    }

		    if ( this.ComputerName.Length > 0 && this.MachineListFile.Length > 0 ) {
			    Message = "{0}: you can only provide either ComputerName or MachineListFile";
			    base.FatalErrorMessage( ".", string.Format(Message, ObjectName), 1660 );
		    }

		    if ( this.ComputerName.Length > 0 )
			    Arguments.AppendFormat( @"\\{0} ", this.ComputerName );
		    else if (this.MachineListFile.Length > 0 )
			    Arguments.AppendFormat( "@{0}", this.MachineListFile );

		    bool UsrPwdOK = (this.UserName.Length > 0 && this.Password.Length == 0) ||
				    (this.Password.Length > 0 && this.UserName.Length == 0 );

		    if ( this.UserName.Length > 0 && this.Password.Length > 0 )
			    Arguments.AppendFormat( " -u {0} -p {1} ", this.UserName, this.Password );


		    if ( this._CopyProg )
          Arguments.AppendFormat( "-c " );

		    if ( this._ForceCopy )
          Arguments.AppendFormat( "-f " );

		    if ( this._CopyHighVerProg )
          Arguments.AppendFormat( "-v " );

		    if ( this._InteractWithDesktop == true )
          Arguments.AppendFormat( "-i " );

		    if ( this.WorkingDirectory.Length > 0 )
          Arguments.AppendFormat(  "-w {0}", this.WorkingDirectory );

		    if ( this.Cmd2Exec.Length > 0 )
          Arguments.AppendFormat( " {0}", this.Cmd2Exec );

		    return Arguments.ToString();
	    }

	    protected override void ParseActionElement() {
		    base.ParseActionElement ();
	    }

	    public void OnProcessCompleted( object Sender, ProcessCompletedEventArgs e ) {
		    string Message = String.Empty;
		    if ( e.ReturnCode == 0 )
          Message = String.Format( "{0}: execute command against {1}, {2} is complete successfully with return code {3}", 
              this.ObjectName, this.ComputerName, this.Cmd2Exec, e.ReturnCode );
		    else
          Message = String.Format( "{0}: execute command against {1}, {2} failed.",
              this.ObjectName, this.ComputerName, this.Cmd2Exec );
		    base.LogItWithTimeStamp( Message );
	    }
    }
}

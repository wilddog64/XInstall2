using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace XInstall.Core.Actions {
    /// <summary>
    /// Summary description for FxCop.
    /// </summary>
    public class FxCop : ActionElement, IAction {
	    // member variables
	    private string _Message      = String.Empty;
	    private string _Location     = String.Empty;
	    private string _Project      = String.Empty;
	    private string _FxCopBin     = @"fxcopcmd.exe";
	    private string _CodeBase     = String.Empty;
	    private string _FxOutputFile = String.Empty;

	    // error handling related variables
	    private enum FXCOP_OPR_CODE {
		    FXCOP_OPR_SUCCESS,
		    FXCOP_OPR_DIRECTORY_NOT_EXIST,
		    FXCOP_OPR_BINARY_NOT_EXIST,
		    FXCOP_OPR_PROJECT_FILE_NOT_EXIST,
		    FXCOP_OPR_CANNOT_INSTANICIATE_OBJECT,
		    FXCOP_OPR_INVALID_OUTPUT_FORMAT,

	    }
	    private FXCOP_OPR_CODE _FxCopOprCode = FXCOP_OPR_CODE.FXCOP_OPR_SUCCESS;
	    private string[] _MessageTable       = {
		    "{0}: {1} - operation successfully complete",
		    "{0}: {1} - provided directory {1} does not exist!",
		    "{0}: {1} - cannot find {2} image!",
		    "{0}: {1} - project file {2} does not exist!",
		    "{0}: {1} - cannot instanciate the fxcop object!",
		    "{0}: {1} - specified output format is not valid. Only console and consolexsl are accepted!",
	    };

	    /// <summary>
	    /// the consturctor for the FxCopCmd
	    /// </summary>
	    /// <remarks>
	    ///     The FxCop constructor is a required even nothing
	    ///     needs to be put in it.  It servers as the element tag
	    ///     in the Config.Xml
	    /// </remarks>
	    [Action("fxcop")]
	    public FxCop() { }

	    /// <summary>
	    /// get/set the location of the .Net exe or dll to be loaded
	    /// </summary>
	    /// <remarks>
	    ///     CodeBase will accept the regular path format and tranlate
	    ///     it to the url format: file://...
	    /// </remarks>
	    [Action("codebase", Needed=true)]
	    public string CodeBase {
		    get {
			    return String.Format( @"file://{0}/{1}", this._CodeBase, this._FxCopBin );
		    }
		    set {
			    this._CodeBase = value;
			    if ( !Directory.Exists( this._CodeBase ) ) {
				    this.SetExitMessage( FXCOP_OPR_CODE.FXCOP_OPR_DIRECTORY_NOT_EXIST, this.Name, @"CodeBase", this._CodeBase);
				    base.FatalErrorMessage( ".", this.ExitMessage, 1660 );
			    }
		    }
	    }

	    /// <summary>
	    /// get/set the OutputFile for the fxcop
	    /// </summary>
	    /// <remarks>
	    ///     OutputFile is an optional attribute.  If not provided,
	    ///     the default value for it is auto, which means the fxcop
	    ///     will automatically generate the output file for you.
	    /// </remarks>
	    [Action("OutputFile", Needed=false, Default="auto")]
	    public string FxCopOutputTo {
		    get {
			    return String.Format( @"/out:{0}", this._FxOutputFile);
		    }
		    set {
			    this._FxOutputFile = value;
			    if ( this._FxOutputFile.ToLower().Equals( @"auto" ) ) {
				    this._FxOutputFile = Path.GetFileNameWithoutExtension( Environment.GetCommandLineArgs()[0]) + ".xml";
			    }
		    }
	    }

	    /// <summary>
	    /// get/set the FxCop project that needs to be run.
	    /// </summary>
	    /// <remarks></remarks>
	    [Action("project", Needed=false)]
	    public string FxCopProject {
		    get {
			    return String.Format( @"/project:{0}", this._Project );
		    }
		    set {
			    this._Project = value;
			    if (!File.Exists( this._Project ) ) {
				    this.SetExitMessage( FXCOP_OPR_CODE.FXCOP_OPR_PROJECT_FILE_NOT_EXIST, this.Name, @"FxCopProject", this._Project);
				    base.FatalErrorMessage( ".", this.ExitMessage, 1660, this.ExitCode );
			    }
		    }
	    }

	    /// <summary>
	    /// set a flag to indicate if the action should be run or not
	    /// </summary>
	    [Action("runnable", Needed=false, Default="true")]
	    public new string Runnable {
		    set {
			    base.Runnable = bool.Parse( value );
		    }
	    }

	    [Action("skiperror", Needed=false, Default="false")]
	    public new string SkipError {
		    set {
			    base.SkipError = bool.Parse( value );
		    }
	    }

	    public override void ParseActionElement() {

		    base.ParseActionElement();
		    AssemblyName assemblyName = new AssemblyName();
		    assemblyName.CodeBase     = this.CodeBase;
		    Assembly assembly         = Assembly.Load( assemblyName );

		    // here we obtain the type information from the loaded module
		    // and retrive the entry point method, which is Main.
		    // We also need to create the instance from loaded module by using
		    // Activator.CreateInstance module. The second parameter, nonPublic
		    // is requred for parameterless constructor.
		    Type t        = assembly.EntryPoint.ReflectedType;
		    MethodInfo mi = t.GetMethod( @"Main",
						 BindingFlags.Public | BindingFlags.Static,
						 null,
						 new Type[] { typeof( string[] ) },
						 null);
		    object obj = Activator.CreateInstance( t, true );

		    // now setup the parameters that needs to pass to FxCopCmd.exe
		    string FxProject = this.FxCopProject;        // the project we need to run
		    string FxOutput  = this.FxCopOutputTo;       // output to console
		    string[] p       = { FxProject, FxOutput };
		    object[] args    = new object[1] { p };

		    try {
			    mi.Invoke( obj, args );
			    string line = null;
			    while ( (line = Console.ReadLine() ) != null ) {
				    base.LogItWithTimeStamp( line );
			    }
		    }
		    catch ( ArgumentException ae ) {
			    throw ae;
		    }
		    catch ( Exception e ) {
			    throw e;
		    }


	    }

	    #region private utility methods/properties
	    private void SetExitMessage( FXCOP_OPR_CODE FxCopOprCode, params object[] Parameters ) {
		    this._FxCopOprCode = FxCopOprCode;
		    this._Message      = String.Format( this._MessageTable[ this.ExitCode ], Parameters );
	    }

	    private string GetProjectDirectory() {
		    string ProjectFullPath = this.FxCopProject;

		    string ProjectDirectory = ProjectFullPath.Substring( ProjectFullPath.IndexOf( @":", 0 ),
					      ProjectFullPath.LastIndexOf( @"/" ) );

		    return ProjectDirectory;
	    }

	    #endregion

	    #region IAction Members

	    public override void Execute() {
		    base.Execute();
		    base.IsComplete = true;
	    }

	    public new bool IsComplete {
		    get {
			    return base.IsComplete;
		    }
	    }

	    public new string ExitMessage {
		    get {
			    return this._Message;
		    }
	    }

	    public new string Name {
		    get {
			    return this.GetType().Name;
		    }
	    }

	    public new int ExitCode {
		    get {
			    return (int) this._FxCopOprCode;
		    }
	    }

	    public override string ObjectName {
		    get {
			    return this.Name;
		    }
	    }

	    #endregion
    }
}

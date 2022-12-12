using System;
using System.Diagnostics;
using System.Xml;

using XInstall.Util.Log;


/*
 * Class Name    : ExternalPrg
 * Inherient     : ActionElement
 * Functionality : A base class for any class that need to spawn an
 *                 external process to inheritant
 *
 * Created Date  : May 2003
 * Created By    : mliang
 *
 * Change Log    :
 *
 * who              when            what
 * --------------------------------------------------------------------
 * mliang           05/01/2003      Initial creation
 * mliang           01/27/2005      Added an overloaded contructor that
 *                                  will accept an XmlNode type
 *                                  parameter so that it can communicate
 *                                  with ActionElement oboject
 */

namespace XInstall.Core {

    public class ProcessCompletedEventArgs : EventArgs {
        private int    _ReturnCode = 0;
        private string _Msg        = String.Empty;

        public ProcessCompletedEventArgs( int ReturnCode, string Msg ) {
            this._ReturnCode = ReturnCode;
            this._Msg        = Msg;
        }

        public int ReturnCode {
            get { return this._ReturnCode; }
        }

        public string Message {
            get { return this._Msg; }
        }
    }


    public delegate void ProcessCompletedHandler( object Sender, ProcessCompletedEventArgs e );


    /// <summary>
    /// ExternalPrg class provides a mean to execute
    /// external program and capture that program's
    /// output. The class also inherits Logger to log
    /// output information into a given file.
    /// </summary>
    public class ExternalPrg : ActionElement {
#region process related variables
        private Process _thisProcess             = null;
        private ProcessStartInfo _psiProcessInfo = null;
#endregion

#region some variable used by property methods
        private string _strExtProgName       = null;
        private string _strExtProgWorkingDir = null;
        private string _strExtProgOutputFile = null;
        private string _strExtProgArgs       = null;
        private string _strExtProgOutput     = null;
        private bool   _bRedirectStandOutput = true;
        private bool   _bProcessHasExited    = false;
        private bool   _bAllowException      = false;
        private bool   _EchoBlank            = false;
        private int    _iProcessExitCode     = 0;

        private DateTime _dtProcessStartTime = new DateTime();
        private DateTime _dtProcessExitTime  = new DateTime();
#endregion

        // public event ProcessCompletedHandler ProcessComplete;

#region Event Handler
        //protected virtual void OnProcessComplete( ProcessCompletedEventArgs e )
        //{
        //    if ( ProcessComplete != null )
        //        ProcessComplete( this, e );
        //}
#endregion


#region public constructors

        /// <summary>
        /// public constructor ExternalPrg -
        ///     initialize process information for later use.
        ///     It also instanciate the Logger class
        /// </summary>
        public ExternalPrg() : base() {
            //
            // Initialize the objects
            // _thisProcess points to the newly created process
            // that is associated with current process and
            // _psiProcessInfo is for entering various process information
            // before it is launched.
            _thisProcess      = Process.GetCurrentProcess();
            _psiProcessInfo   = new ProcessStartInfo();
            base.OutToFile    = true;
            base.OutToConsole = true;
        }

        public ExternalPrg( XmlNode ActionNode ) : base( ActionNode ) {
            // Initialize the objects
            // _thisProcess points to the newly created process
            // that is associated with current process and
            // _psiProcessInfo is for entering various process information
            // before it is launched.
            _thisProcess      = Process.GetCurrentProcess();
            _psiProcessInfo   = new ProcessStartInfo();
            base.OutToFile    = true;
            base.OutToConsole = true;
        }

        /// <summary>
        /// ExternalPrg an overload constructor that takes
        /// the following parameters:
        /// </summary>
        /// <param name="strProgName">external program to be called</param>
        /// <param name="strProgArgs">external program's arguments if any</param>
        /// <param name="strOutputFile">where do we write the output to</param>

        public ExternalPrg( string strProgName, string strProgArgs, string strOutputFile ) {
            base.OutToConsole = true;
            base.OutToFile    = true;

            _thisProcess      = new Process();
            _psiProcessInfo   = new ProcessStartInfo();

            // fill in the required parameters
            this.ProgramName           = strProgName;
            this.ProgramArguments      = strProgArgs;
            this.ProgramRedirectOutput = _bRedirectStandOutput.ToString();
            this.ProgramOutputFile     = strOutputFile;

        }


        /// <summary>
        /// ExternalPrg - an overloaded constructor that takes the
        ///     follow two parameters
        /// </summary>
        /// <param name="strProgArgs">arguments for the external program</param>
        /// <param name="strOutputFile">output file to write external program's
        ///         output</param>
        public ExternalPrg( string strProgArgs, string strOutputFile ) {

            _thisProcess    = new Process();
            _psiProcessInfo = new ProcessStartInfo();

            this.ProgramArguments      = strProgArgs;
            this.ProgramRedirectOutput = _bRedirectStandOutput.ToString();
            this.ProgramOutputFile     = strOutputFile;
        }


#endregion

#region public property methods
        /// <summary>
        /// property ProgramName -
        ///     gets/sets program to be executed
        ///
        ///     An external program to be execute.
        ///     Provide a read/write accessors.
        ///
        ///     When external program extension is not executable
        ///     it will call cmd.exe to execute it by using environment
        ///     variable %comspec%.
        /// </summary>
        public string ProgramName {
            get { return _strExtProgName; }
            set {
                // gets external program and extract the extension
                // information
                this._strExtProgName    = value;
                string strProgExtension = System.IO.Path.GetExtension( _strExtProgName );

                bool isMac = (this.PlatformID == 4 || this.PlatformID == 128) ? true : false;
                // if extension is one of the following:
                // .vbs, .js, .cmd, or .bat, then construct the execute file as %comspec%
                // and assign /c ... to _psiProcessInfo.Arguments property;
                // otherwise, we simply assign the program to _psiProcessInfo.FileName property
                if ( !isMac ) {
                  if ( strProgExtension.Equals(".vbs") ||
                       strProgExtension.Equals(".js")  ||
                       strProgExtension.Equals(".cmd") ||
                       strProgExtension.Equals(".bat") ) {
                      _psiProcessInfo.FileName  = _psiProcessInfo.EnvironmentVariables["comspec"];
                      _psiProcessInfo.Arguments = "/c " + String.Format("{0}", _strExtProgName);
                  }
                  else
                      if ( !this.EchoBlank )
                          _psiProcessInfo.FileName = _strExtProgName;
                      else
                          _psiProcessInfo.FileName = Environment.GetEnvironmentVariable("comspec");
                }
                else {
                      _psiProcessInfo.FileName  = "bash";
                      _psiProcessInfo.Arguments = " -c " + String.Format("{0}", _strExtProgName);
                }
            }
        }

        /// <summary>
        /// get/set a flag to indicate if need to echo a blank
        /// to a spawned external program.
        /// </summary>
        protected bool EchoBlank {
            get { return this._EchoBlank; }
            set { this._EchoBlank = value; }
        }

        protected int PlatformID {
          get {
                // check to see if we are running under Mac
                return (int) Environment.OSVersion.Platform;
          }
        }

        /// <summary>
        /// property ProgramWorkingDirectory -
        ///     gets/sets Working Directory
        /// </summary>
        public string ProgramWorkingDirectory {
            get { return _strExtProgWorkingDir; }

            set {
                _strExtProgWorkingDir = value;
                System.IO.DirectoryInfo di = new System.IO.DirectoryInfo( _strExtProgWorkingDir );
                if ( ! di.Exists ) {
                    throw new Exception( String.Format("start directory {0} is not found", di.FullName) );
                }
                _psiProcessInfo.WorkingDirectory = _strExtProgWorkingDir;
            }
        }


        /// <summary>
        /// property ProgramArguments -
        ///     gets/sets execution program's arguments
        /// </summary>
        public string ProgramArguments {
            get { return _strExtProgArgs; }
            set {
                // if the Arguments property of _psiProcessInfo is
                // not null and we have arguments, then append the
                // new arguments to the current Arguments property
                // of _psiProcessInfo; otherwise, simply assign it
                // directly
                _strExtProgArgs = value;
                if ( _strExtProgArgs != null )
                    if (_psiProcessInfo.Arguments.Length != 0)
                        _psiProcessInfo.Arguments += _strExtProgArgs;
                    else
                        _psiProcessInfo.Arguments = _strExtProgArgs;
            }
        }


        /// <summary>
        /// propety ProgramRedirectOutput -
        ///    gets/sets if the external program's output
        ///    needed to be redirected.
        /// </summary>
        public string ProgramRedirectOutput {
            get { return this._bRedirectStandOutput.ToString(); }

            set {
                this._bRedirectStandOutput = bool.Parse( value.ToString() );
                _psiProcessInfo.RedirectStandardOutput = this._bRedirectStandOutput;

                // if we need to capture the external program's output,
                // we'll have to turn of the UseShellExecute and turn on
                // RedirectStandardOutput properties of _psiProcessInfo
                if ( this._psiProcessInfo.RedirectStandardOutput ) {
                    this._psiProcessInfo.UseShellExecute        = false;
                    this._psiProcessInfo.RedirectStandardOutput = true;
                    this._psiProcessInfo.CreateNoWindow         = true;
                }
            }
        }

        /// <summary>
        /// Property ProgramOutputFile
        ///     gets/sets the output file for exteranl program
        /// </summary>
        public string ProgramOutputFile {
            get { return _strExtProgOutputFile; }
            set {
                // Initialize log file.
                base.FileName = value;

                // if we need to capture the external program's output,
                // we'll have to turn of the UseShellExecute and turn on
                // RedirectStandardOutput properties of _psiProcessInfo
                this._psiProcessInfo.UseShellExecute        = false;
                this._psiProcessInfo.RedirectStandardOutput = true;
                this._psiProcessInfo.WindowStyle            = ProcessWindowStyle.Normal;
            }
        }


        /// <summary>
        /// protected property ProgramOutput -
        ///     get/set the output from the external program
        /// </summary>
        protected string ProgramOutput {
            get { return this._strExtProgOutput; }
        }


        /// <summary>
        /// property ProgramStartTime -
        ///     gets external program start time
        /// </summary>
        public DateTime ProgramStartTime {
            get {
                if ( this._bProcessHasExited )
                    _dtProcessStartTime = _thisProcess.StartTime;
                return _dtProcessStartTime;
            }
        }


        /// <summary>
        /// property ProgramExitTime -
        ///     gets the time when external program exits
        /// </summary>
        public DateTime ProgramExitTime {
            get {
                if ( this._bProcessHasExited )
                    _dtProcessExitTime = _thisProcess.ExitTime;
                return _dtProcessExitTime;
            }
        }


        /// <summary>
        /// property ProgramExitCode -
        ///     gets the exit code from external program
        ///     when it finished.
        /// </summary>
        public int ProgramExitCode {
            get { return this._iProcessExitCode; }
        }

#endregion

#region protected property methods
        /// <summary>
        /// protected property AllowGenerateException -
        ///     get/set a flag that tells the object
        ///     should generate an exception automatically.
        /// </summary>
        protected new bool AllowGenerateException {
            get { return this._bAllowException; }
            set { this._bAllowException = value; }
        }

        public virtual string BasePath {
            get { return System.IO.Directory.GetCurrentDirectory(); }
            set {}
        }

#endregion

#region public methods

        protected override void ParseActionElement() {
            base.ParseActionElement();

            string Arguments = this.GetArguments();
            if ( Arguments.Length > 0 )
                this.ProgramArguments = Arguments;

            // do we need to generate an exception automatically?
            if ( this.AllowGenerateException ) {
                this.AllowGenerateException = false;
                throw new Exception( "required an exception to be generated!" );
            }

            // log what we've got for the exteranl program and
            // its arguments
            base.LogItWithTimeStamp( String.Format("{0}: program - {1} {2}", this.Name, _psiProcessInfo.FileName, _psiProcessInfo.Arguments));

            string strErrorMessage = null;
            try {
                string strOldCurrDir = System.IO.Directory.GetCurrentDirectory();
                System.IO.Directory.SetCurrentDirectory( this.BasePath );

                // set process start information and start process
                _thisProcess.StartInfo = _psiProcessInfo;
                _thisProcess.Start();
                System.IO.Directory.SetCurrentDirectory( strOldCurrDir );

                // get output from process's standard output
                string Message = string.Empty;
                if ( this._bRedirectStandOutput ) {
                    while ( ( Message = this._thisProcess.StandardOutput.ReadLine() ) != null )
                        base.LogItWithTimeStamp( String.Format( "{0}: {1}", this.ObjectName, Message ) );
                }

                // wait for process exit and check it's return code
                _thisProcess.WaitForExit();
                if ( _thisProcess.HasExited ) {
                    this._iProcessExitCode                        = _thisProcess.ExitCode;
                    this._bProcessHasExited                       = true;
                    ProcessCompletedEventArgs ProcessCompleteInfo = new ProcessCompletedEventArgs( this._thisProcess.ExitCode, this.ExitMessage );
                    this.OnProcessComplete( ProcessCompleteInfo );
                }
                _psiProcessInfo.Arguments = null;
            }
            catch ( System.InvalidOperationException ioe ) {
                strErrorMessage = String.Format("Error Happened, source {0} - message {1}", ioe.Source, ioe.Message);
                throw new Exception( strErrorMessage );
            }
            catch ( System.ComponentModel.Win32Exception eWin32 ) {
                strErrorMessage = String.Format("Win32 Source: {0}, Error Code {1}, Message {2}!", eWin32.Source, eWin32.ErrorCode, eWin32.Message);
                throw new Exception( strErrorMessage );
            }
            catch ( System.Exception e ) {
                strErrorMessage = String.Format("Unhandle error: {0}", e.Message);
                throw new Exception( strErrorMessage );
            }
            base.IsComplete = true;
        }

        protected virtual string GetArguments() {
            return String.Empty;
        }

#endregion

#region ICleanUp Members

#endregion
    }
}

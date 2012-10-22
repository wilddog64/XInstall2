using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Reflection;


using System.Security.Permissions;
using System.Security.Principal;

using XInstall.Util;

namespace XInstall.Util.Log {

    public class LogMessageEventArg : EventArgs {
        private LEVEL  _Level      = LEVEL.INFORMATION;
        private string _LogMessage = string.Empty;

        public LogMessageEventArg( LEVEL Level, string MessageToLog ) {
            this._Level      = Level;
            this._LogMessage = MessageToLog;
        }


        public string LogMessage
        {
            get { return this._LogMessage; }
        }

    }


    public delegate void LogMessageEventHandler( object Sender, LogMessageEventArg LogMessage );

    /// <summary>
    /// A very simple logging class
    /// that logs information into
    /// a desired file
    /// </summary>
    // [Serializable()]
    public class Logger {
        private ArrayList              _Information    = new ArrayList();
        private static ErrorCollection _Errors         = new ErrorCollection();
        private StreamWriter           _swLogFile      = null;
        private static ISendLogMessage _SendLogMessage = null;

        private string          _strFileName      = null;
        private bool            _bOutputToConsole = false;
        private bool            _bOutputToFile    = true;
        private UInt32          _MsgID            = 0;

        #region constructors
        /// <summary>
        /// Constructor Logger - initlaize variables and
        /// call Init function to create a file stream
        /// for writting data into a given file
        /// </summary>
        /// <param name="strFileName"></param>
        /// <param name="bAppend"></param>
        public Logger(string strFileName, string bAppend) {
            //
            // TODO: possible enhancement is to inherit from System.Diagnostics
            //       to have more complete logging system
            //
            _strFileName = strFileName;

            try {
                Init();
            } 
            catch ( Exception ) {
                this.FatalErrorMessage( ".", String.Format( "{0}: error Initializes", this.Name ), 1660, true);
            }
        }


        /// <summary>
        /// an empty constuctor - do nothing but use
        /// to instance a Logger object
        /// </summary>
        public Logger () {}

        public Logger( ISendLogMessage SendLogMessageIF ) {
            _SendLogMessage = SendLogMessageIF;
        }
        #endregion

#region private utility functions
        /// <summary>
        /// private function Init is used to initialize FileStream
        /// for writting data
        /// </summary>
        private void Init () {
            // get file path information

            this._swLogFile        = this.LogFileStream;
            this._bOutputToConsole = true;
            this._bOutputToFile    = true;

        }
#endregion

#region protected property methods

        /// <summary>
        /// a protected property that gets the file stream.
        /// </summary>
        /// <remarks>
        ///     this property will return an open stream of the
        ///     log file that has been assigned to the class.
        /// </remarks>
        protected StreamWriter LogFileStream
        {
            get {
                FileIOPermission IOPermDenyRoot = new FileIOPermission( FileIOPermissionAccess.NoAccess, new string[] { "c:\\", Environment.SystemDirectory } );

                FileIOPermission IOPerm = new FileIOPermission( FileIOPermissionAccess.Write, new string[] { @"c:\temp" } );
                IOPermDenyRoot.Union( IOPerm ).Demand();

                string strThisFile     = this.FileName;
                FileStream   fsLogFile = null;
                StreamWriter swLogFile = null;
                if ( strThisFile != null || strThisFile != String.Empty ) {
                    FileInfo fi = new FileInfo( strThisFile );

                    if ( fi.Exists ) {
                        DateTime LMD = fi.LastAccessTime;
                        TimeSpan dt  = DateTime.Today - LMD;
                        if ( dt.Days > 1 ) {
                            string Extension = DateTime.Now.ToString( "ddMMyyyy" );
                            string NewFileName = Path.ChangeExtension( fi.FullName, Extension + ".bak" );
                            fi.MoveTo( NewFileName );
                        }
                    }

                    fsLogFile = fi.Open( FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite );
                    swLogFile = new StreamWriter( fsLogFile );
                    swLogFile.AutoFlush = true;
                }
                return swLogFile;
            }
        }


#endregion

#region public property methods

        /// <summary>
        /// Gets the name of object.
        /// </summary>
        public string Name
        {
            get {
                return this.GetType().Name;
            }
        }

        /// <summary>
        ///  public string FileName - get/set the log file name
        /// </summary>
        /// <remarks>
        /// FileName will get/set the file that is used to write
        /// a log message to.  If the file name is not provide,
        /// the property will create one for you and name will be
        /// your program + .log extension.  The file will be created
        /// under current directory's logs folder.  It will create
        /// the sub-directory if the one is not existed!
        /// </remarks>
        public string FileName
        {
            get {
                if ( this._strFileName == null || this._strFileName == String.Empty ) {
                    string strProgName = Path.GetFileNameWithoutExtension( Environment.GetCommandLineArgs()[0]);

                    string LogDirectory = Environment.GetEnvironmentVariable( "temp" );
                    _strFileName        = LogDirectory                 +
                                          Path.DirectorySeparatorChar  +
                                          "logs"                       +
                                          Path.DirectorySeparatorChar  +
                                          strProgName                  + 
                                          ".log";
                }
                this.CheckFilePath();
                return this._strFileName;
            }


            set {
                if ( value.ToLower().Equals("auto") )
                    value = Path.GetFileNameWithoutExtension( Environment.GetCommandLineArgs()[0] );
                this._strFileName = value;
            }
        }


        /// <summary>
        /// A boolean variable that write message to the console
        /// when set to true.
        /// </summary>
        /// <remarks>
        /// This is how you control if you want you message to
        /// be written to a console.
        /// </remarks>
        public bool OutToConsole
        {
            set { this._bOutputToConsole = bool.Parse( value.ToString() ); }
        }


        /// <summary>
        /// A boolean variable that write message to the file
        /// when set to true.
        /// </summary>
        /// <remarks>
        /// This property is used to control a given message should be
        /// written to a file or not.
        /// </remarks>
        public bool OutToFile
        {
            set { this._bOutputToFile = bool.Parse( value.ToString() ); }
        }


        /// <summary>
        /// gets a logged information
        /// </summary>
        public ArrayList LogInformation
        {
            get { return this._Information; }
        }


#endregion

#region public methods

        public void SetSendMessageInterface( ISendLogMessage SendMessageInterface ) {
            if ( SendMessageInterface != null )
                _SendLogMessage = SendMessageInterface;
        }

        public void LogItWithTimeStamp( string Message ) {
            this.LogItWithTimeStamp( LEVEL.INFORMATION, Message );
        }

        /// <summary>
        /// Log message with date/time stamp to log file
        /// </summary>
        /// <param name="strMessage">a Message to be written</param>
        /// <remarks>
        /// LogItWithTimeStamp will logs message to console, file, or
        /// both depending on how you set the properties of OutToConsole or
        /// OutToFile.
        /// </remarks>
        /// <example>
        /// The following statments will have LogItWithTimeStamp writes message
        /// "Hello World!" to both console and file:
        ///
        ///     object.OutToConsole = true;
        ///     object.OutToFile    = ture;
        ///     object.LogItWithTimeStamp( "Hello World!" );
        ///
        /// </example>
        public void LogItWithTimeStamp( LEVEL Level, string strMessage ) {
            this.LogItWithTimeStamp( Level, SERVIRITY.NORMAL, strMessage );
        }


        public void LogItWithTimeStamp( LEVEL Level, SERVIRITY Servirity, string strMessage ) {
            if ( this._bOutputToFile && !this._bOutputToConsole )
                this.WriteToFile( Level, strMessage );
            else if ( this._bOutputToConsole && ! this._bOutputToFile )
                this.WriteToConsole( Level, strMessage );
            else if ( this._bOutputToConsole && this._bOutputToFile ) {
                this.WriteToFile( Level, strMessage );
                this.WriteToConsole( Level, strMessage );
            }
            string ObjectName = strMessage.Substring( 0, strMessage.IndexOf( ":" ) );
            if ( ObjectName == null || ObjectName.Length == 0 )
                ObjectName = "Unknown";
            Error AnError = new Error( ObjectName, Level, Servirity, strMessage );
            _Errors.Add( AnError );
            this.LogMessage( AnError );

        }


        /// <summary>
        /// public void Close - close the open file
        /// </summary>
        public void Close () {
            // ignoring the ObjectDisposedException
            // when called by the dispose method. this
            // need to resolve in order to avoid unknown
            // problem
            try {
                if ( _swLogFile != null )
                    _swLogFile.Close();
            } catch ( System.ObjectDisposedException ) {}
        }


#endregion

#region protected methods

        /// <summary>
        /// Log the fatal error message returns from
        /// the external program to an event database
        /// on a given machine and exit the program
        /// immediately.
        /// </summary>
        /// <param name="strMachineName">
        ///        name of a machine that host the event database</param>
        /// <param name="strMessage">
        /// a message to be written to event database
        /// </param>
        /// <param name="iEventLogID">an id of a written message</param>
        /// <param name="iExitCode">an exit code from the program</param>
        /// <remarks>
        ///     FatalErrorMessage reports the message to a given machine's
        ///     event database and exit the program immediately.  The message
        ///     written to the event
        /// </remarks>
        protected void FatalErrorMessage( string strMachineName, string strMessage, int iEventLogID ) {
            this.FatalErrorMessage( strMachineName, strMessage, iEventLogID, false );
        }


        /// <summary>
        ///     an overloaded method that provides same
        ///     functionality as logging message to a
        ///     machine's event database.  This one differ by
        ///     not taking the exit code from the parameter list.
        /// </summary>
        /// <param name="strMachineName"></param>
        /// <param name="strMessage"></param>
        /// <param name="iEventLogID"></param>
        protected void FatalErrorMessage( string strMachineName, string strMessage, int iEventLogID, bool Abort ) {
            //base.EventLogMachine = strMachineName;
            //base.EventLogType    = "Error";
            //base.EventLogID      = iEventLogID;
            //base.EventLogMessage = strMessage;
            //base.ReportEvent();
            this.LogItWithTimeStamp( LEVEL.FATAL, SERVIRITY.FATAL, strMessage );
            if ( Abort )
                Environment.Exit( -1 );
        }


        protected void LogMessage( Error AnError ) {
            if ( _SendLogMessage != null )
                _SendLogMessage.SendLogMessage( AnError );
        }


#endregion

#region protected properties
        protected ErrorCollection Errors
        {
            get { return _Errors; }
        }


        protected UInt32 MsgID
        {
            get { return this._MsgID; }
        }


#endregion

#region private methods

        /// <summary>
        /// private void CheckFilePath() -
        ///     checks a given file path exists.
        ///     If one does not exist, then create it
        /// </summary>
        private void CheckFilePath() {
            string strFilePath = Path.GetFullPath ( _strFileName );
            string strPathInfo = strFilePath.Substring (0, strFilePath.LastIndexOf (Path.DirectorySeparatorChar));

            // check to see if given directory exists in the current
            // directory; if not, create one.
            DirectoryInfo di = new DirectoryInfo ( strPathInfo );
            if ( !di.Exists )
                Directory.CreateDirectory( strPathInfo );
        }


        /// <summary>
        /// private void WriteToFile -
        ///     writes log message to a file
        /// </summary>
        /// <param name="strMessage">a message to be written to a file</param>
        private void WriteToFile(LEVEL level, string strMessage) {
            // if StreamWrite is not initialized, call Init() i
            // internal function
            if ( _swLogFile == null )
                Init();

            // get current date/time string
            // and create a formated message
            string strCurrDateTime = DateTime.Now.ToString();
            string LogMessage      = String.Format ("[{0}] - {1} {2}", strCurrDateTime, level.ToString(), strMessage);
            this._Information.Add( LogMessage );

            // always write to the end of file and flush
            // data in buffer into a log file.
            if ( Trace.TraceOn ) {
                _swLogFile.BaseStream.Seek(0, SeekOrigin.End);
                _swLogFile.WriteLine ( LogMessage );
            }
        }


        private void WriteToFile( string Message ) {
            if ( Trace.TraceOn )
                this.WriteToFile( LEVEL.INFORMATION, Message );
        }


        /// <summary>
        /// private void WriteToConsole -
        ///     writes log message to a console
        /// </summary>
        /// <param name="strMessage">a message to be written to console</param>
        private void WriteToConsole( LEVEL level, string strMessage ) {
            if ( Trace.TraceOn ) {
                string strCurrDateTime = DateTime.Now.ToString();
                string LogMessage      = String.Format ("[{0}] - {1} {2}", strCurrDateTime, level.ToString(), strMessage);
                Console.WriteLine( LogMessage );
            }
        }


        private void WriteToConsole( string Message ) {
            if ( this._Information.IndexOf( Message ) > -1 )
                this._Information.Add( Message );

            this.WriteToConsole( LEVEL.INFORMATION, Message );
        }


    #endregion
    }
}

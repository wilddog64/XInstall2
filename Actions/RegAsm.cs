using System;
using System.IO;
using System.Text;

using Microsoft.Win32;

namespace XInstall.Core.Actions {
    /// <summary>
    /// public class RegAsm -
    ///     a class to wrap the external RegAsm.exe to
    ///     provide an ability to register an assembly
    ///     as a com+ object.
    /// </summary>
    public class RegAsm : ExternalPrg, IAction, ICleanUp {
        private readonly string _rostrNetRoot;
        private readonly string _rostrRegAsm    = "RegAsm.exe";
        private string _strAction               = null;
        private string _strAssemblyFile         = null;
        private string _strTlb                  = null;
        private string _strRegfile              = null;
        private bool   _bNoLogo                 = true;
        private bool   _bSlientMode             = true;

        /// <summary>
        /// public RegAsm() -
        ///     public constructor that initialize the
        ///     RegAsm object.
        /// </summary>
        /// <remarks>
        ///     The constructor will perform the following initialization:
        ///
        ///        . initialize all the message print to console and file
        ///        . setup the path point to the path of RegAsm.exe
        /// </remarks>
        [Action("regasm")]
        public RegAsm() {
            base.OutToConsole  = true;
            base.OutToFile     = true;
            this._rostrNetRoot = this.GetNetInstallRoot();
            base.ProgramName   = this._rostrNetRoot          +
                     Path.DirectorySeparatorChar +
                     this._rostrRegAsm;
        }

        /// <summary>
        /// property AssemblyFile -
        ///     get/set the name of assembly file to be registered.
        /// </summary>
        [Action("assemblyfile", Needed=true)]
        public string AssemblyFile {
            get {
                // check if a given file does exist;
                // otherwise, show an error message and abort
                if ( !File.Exists( this._strAssemblyFile ) )
                    base.FatalErrorMessage(".",
                               String.Format("{0}: Assembly file:{1} does not exist!",
                               this.Name, this._strAssemblyFile),
                               1660, 1);
                return this._strAssemblyFile;
            }
            set {
                this._strAssemblyFile = value;
            }
        }

        /// <summary>
        /// property TypeLibrary -
        ///     get/set the type library to be
        ///     generated for a given dll file.
        /// </summary>
        [Action("tlbname", Needed=false)]
        public string TypeLibrary {
            get {
                return this._strTlb;
            }
            set {
                this._strTlb = value;
            }
        }

        /// <summary>
        /// property RegistryFile -
        ///     get/set the registry file to be
        ///     generated for the dll file to be
        ///     registered.
        /// </summary>
        [Action("regfile", Needed=false)]
        public string RegistryFile {
            get {
                return this._strRegfile;
            }
            set {
                this._strRegfile = value;
            }
        }

        /// <summary>
        /// property NoLogo -
        ///     set a boolean flag to have regasm not
        ///     to show the logo message.
        /// </summary>
        [Action("nologo", Needed=false, Default="false")]
        public string NoLogo {
            set {
                this._bNoLogo = bool.Parse( value.ToString() );
            }
        }

        /// <summary>
        /// property slient -
        ///     sets a flag to tell whether regasm should operate
        ///     in a slient mode.
        /// </summary>
        [Action("slientmode", Needed=false)]
        public string Slient {
            set {
                this._bSlientMode = bool.Parse( value.ToString() );
            }
        }

        /// <summary>
        /// property AllowGenerateException -
        ///     set a flag that tells the RoboCopy object
        ///     should generate an exception or not.
        /// </summary>
        [Action("generateexception", Needed=false, Default="false")]
        public new string AllowGenerateException {
            set {
                try {
                    base.AllowGenerateException =
                    bool.Parse( value.ToString() );
                }
                catch ( Exception ) {
                    base.FatalErrorMessage( ".", String.Format("{0}: boolean variable parsing error", this.Name), 1660, 4);
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

        #region private methods
        private string GetArguments() {
            StringBuilder sbProgArgs = new StringBuilder();

            sbProgArgs.AppendFormat(" (0}", this.AssemblyFile );

            switch ( this._strAction ) {
            case "regfile":
                sbProgArgs.AppendFormat(" /regfile:{0}", this.RegistryFile);
                break;
            case "typelib":
                sbProgArgs.AppendFormat(" /tlb:{0}", this.TypeLibrary);
                break;
            case "unregister":
                sbProgArgs.Append(" /unregister");
                break;
            default:
                base.FatalErrorMessage( ".",
                            String.Format( "{0}: invalid action {1} specified! Valid actions are /regfile and /tlb",
                            this.Name, this._strAction), 1660, 3);
                break;
            }
            if ( this._bNoLogo ) {
                sbProgArgs.Append(" /nologo");
            }
            if ( this._bSlientMode ) {
                sbProgArgs.Append(" /slient");
            }

            return sbProgArgs.ToString();

        }

        #endregion

        #region IAction Members

        /// <summary>
        /// public override void Execute() -
        ///     an override method that carries out
        ///     the regasm function
        /// </summary>
        public override void Execute() {
            base.IsComplete       = false;
            base.ProgramArguments = this.GetArguments();
            base.Execute();
        }


        /// <summary>
        /// property IsComplete -
        ///     get the state of regasm's execution
        /// </summary>
        public new bool IsComplete {
            get {
                return base.IsComplete;
            }
        }

        /// <summary>
        /// property Action -
        ///     sets an action to be executed
        ///     by regasm.
        /// </summary>
        [Action("action", Needed=false, Default="")]
        public string Action {
            set {
                this._strAction = value;
            }
        }

        /// <summary>
        /// property ExitMessage -
        ///     get the message that corresponding
        ///     to the exit code.
        /// </summary>
        /// <remarks>
        ///     this interface is not impletement
        ///     but present here for the requirement
        ///     by the IAction interface.
        /// </remarks>
        public new string ExitMessage {
            get {
                // TODO:  Add RegAsm.ExitMessage getter implementation
                return null;
            }
        }

        /// <summary>
        /// property Name -
        ///     gets the name of this class
        /// </summary>
        public new string Name {
            get {
                return this.GetType().Name;
            }
        }

        /// <summary>
        /// property ExitCode -
        ///     gets the exit code from the execution
        ///     of regasm.exe
        /// </summary>
        public new int ExitCode {
            get {
                return base.ProgramExitCode;
            }
        }

        #endregion

        #region ICleanUp Members

        /// <summary>
        /// public void RemoveIt() -
        ///     provide a cleanup for what regasm
        ///     has done so far.
        /// </summary>
        /// <remarks>
        ///     this is an inteface method that derives
        ///     from ICleanUp inteface to provide an
        ///     ability to unregister the assembly that
        ///     regasm has registered into COM+ manager.
        /// </remarks>
        public new void RemoveIt() {
            this.Action           = "unregister";
            base.ProgramArguments = this.GetArguments();
            base.Execute();
        }

        /// <summary>
        /// private string GetNetInstallRoot() -
        ///     this private method will lookup in
        ///     the registry database in the machine
        ///     where it runs and return the install
        ///     path for the DotNet framework v1.1
        /// </summary>
        /// <returns>string variable contains path to the DotNet Framework v1.1</returns>
        private string GetNetInstallRoot() {
            string strValue = null;
            try {
                RegistryKey rkLocalMachine =
                RegistryKey.OpenRemoteBaseKey( RegistryHive.LocalMachine, Environment.MachineName );

                RegistryKey rk = rkLocalMachine.OpenSubKey(@"Software\Microsoft\.NETFramework" );
                if ( rk != null ) {
                    strValue = (string) rk.GetValue( @"sdkInstallRootv1.1" );
                }
            }
            catch ( Exception e ) {
                base.FatalErrorMessage(".", e.Message, 1660);
            }

            return strValue;
        }
        #endregion
    }
}

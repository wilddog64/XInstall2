using System;
using System.Collections;
using System.Management;
using System.Xml;

namespace XInstall.Core {
    /// <summary>
    /// Summary description for WmiBase.
    /// </summary>
    public class WmiBase : ActionElement {
        private ConnectionOptions _ConnectionOptions = new ConnectionOptions();
        private ManagementPath    _ManagementPath    = new ManagementPath();
        private ManagementObject  _ManagementObject  = null;

//        private string _Key          = String.Empty;
//        private string _Location     = String.Empty;
//        private string _MachineName  = String.Empty;
//        private string _RelativePath = String.Empty;
//        private string _UserName     = String.Empty;
//        private string _UserPass     = String.Empty;

        public WmiBase( XmlNode ActionNode ) : base( ActionNode ) {}

#region protected virtual properties

        public virtual string UserName
        {
            get { return ""; }
            set {}
        }

        public virtual string UserPass
        {
            get { return ""; }
            set {}
        }

        public virtual string MachineName
        {
            get { return ""; }
            set {}
        }

        public virtual string Key
        {
            get { return ""; }
            set {}
        }

        protected virtual string Location
        {
            get { return ""; }
            set {}
        }

        public virtual string RelativePath
        {
            get { return ""; }
            set {}
        }

        public virtual string NamespacePath
        {
            get { return ""; }
            set {}
        }

        public virtual string ClassName
        {
            get { return ""; }
            set { }
        }


#endregion

#region protected Methods

        protected ManagementObject GetManagementObject() {
            return this._ManagementObject;
        }

        protected ManagementObject GetNewInstance() {
            return this.GetNewInstance( this.ClassName );
        }

        protected ManagementObject GetNewInstance( string QueryPath ) {
            ManagementScope Scope = null;
            ManagementPath  Path  = new ManagementPath();
            // Path.ClassName        = ClassName;
            Path.NamespacePath    = this.NamespacePath;
            Path.Path = QueryPath;


            Scope             = new ManagementScope( Path, this.GetConnectionOptions() );
            Scope.Path.Server = this.MachineName;

            ManagementClass Class = new ManagementClass( Scope, Path, null );

            return Class.CreateInstance();
        }

        protected ManagementObject Connect() {
            // setup connection options


            bool UseImpersonate = this.UserName.Length == 0 &&
                                  this.UserPass.Length == 0;

            if ( UseImpersonate ) {
                this._ConnectionOptions.Impersonation = ImpersonationLevel.Impersonate;
                base.LogItWithTimeStamp(
                    String.Format( "{0} - connecting to {1} using impersonation",
                                   this.ObjectName, this.MachineName ) );
            } else {
                this._ConnectionOptions.Username = this.UserName;
                this._ConnectionOptions.Password = this.UserPass;
                base.LogItWithTimeStamp(
                    String.Format( "{0} connecting to {1} using username and password pair",
                                   this.ObjectName, this.MachineName ) );
            }

            // setup management path
            this._ManagementPath.NamespacePath = this.NamespacePath;

            this._ManagementPath.Server = this.MachineName;

            if ( this.RelativePath.Length > 0 )
                this._ManagementPath.RelativePath  = this.RelativePath;



            ManagementScope Scope = new ManagementScope( this._ManagementPath, this._ConnectionOptions );
            if ( !Scope.IsConnected ) {
                Scope.Connect();
                base.LogItWithTimeStamp( String.Format( "{0} connected to server {1}",
                                                        this.ObjectName, this.MachineName ) );
            }
            return this._ManagementObject = new ManagementObject( Scope, this._ManagementPath, null );
        }

#endregion

#region private methods

        private ConnectionOptions GetConnectionOptions() {
            return this._ConnectionOptions;
        }

#endregion

#region public override methods

        public override void Execute() {
            base.Execute();
            this.Connect();
        }

#endregion

    }
}

using System;
using System.Collections;
using System.Data;
using System.Data.SqlClient;
using System.Xml;

using XInstall.Core;
using XInstall.Util;
using XInstall.Util.Log;

namespace XInstall.CustomTestActions {
    /// <summary>
    /// Summary description for SQLTest.
    /// </summary>
    public class SQLTest :  ISqlInfo {
        private DBAccess             _DB                = new DBAccess();
        private StoredProcCollection _StoredProcs       = new StoredProcCollection();
        private XmlNode              _ActionNode        = null;
        private Hashtable            _StoredProcResults = new Hashtable();

        private bool   _AboveThreshold = false;
        private bool   _UnderThreshold = false;
        private bool   _Enable         = true;

        private string _DBServer   = string.Empty;
        private string _DBDatabase = string.Empty;
        private string _UserName   = string.Empty;
        private string _UserPass   = string.Empty;

        public SQLTest( XmlNode ActionNode ) {
            if ( ActionNode.Name == "SQLTest" )
                this._ActionNode = ActionNode;
            else
                throw new XmlException(
                    String.Format( "{0} is not recognized", ActionNode.Name ) );

        }


        [SQL("DBServer", Required=true)]
        public string DBServer
        {
            get {
                return this._DB.DataSource;
            }
            set {
                this._DB.DataSource = value;
            }
        }


        [SQL("Database", Required=true)]
        public string DBCategory
        {
            get {
                return this._DB.InitCatlog;
            }
            set {
                this._DB.InitCatlog = value;
            }
        }


        [SQL("UserName", Required=false, Default="")]
        public string DBUserName
        {
            get {
                return this._DB.UserName;
            }
            set {
                this._DB.UserName = value;
            }
        }

        [SQL("TrustedConnection", Required=false, Default="true")]
        public string TrustedConnection
        {
            set {
                bool Trusted = Convert.ToBoolean( value );

                if (Trusted)
                    this._DB.IntegratedSecurity = "SSPI";
                else
                    this._DB.IntegratedSecurity = "NO";
            }
        }


        public bool AboveThreshold
        {
            get {
                return this._AboveThreshold;
            }
        }


        public bool UnderThreshold
        {
            get {
                return this._UnderThreshold;
            }
        }


        [SQL("UserPass", Required=false, Default="")]
        public string DBUserPass
        {
            get {
                return this._DB.UserPassword;
            }
            set {
                this._DB.UserPassword = value;
            }
        }


        [SQL("Enable", Required=false, Default="true")]
        public string Enable
        {
            set {
                this._Enable = bool.Parse(value);
            }
        }


        public StoredProcCollection StoredProcs
        {
            get {
                return this._StoredProcs;
            }
        }

        protected string ObjectName
        {
            get {
                return this.GetType().Name;
            }
        }


        public void Execute() {
            try {
                this._DB.Connect();

                XmlNodeList StoredProcNodes = this._ActionNode.SelectNodes( "StoredProc" );
                if ( StoredProcNodes != null ) {
                    foreach( XmlNode StoredProcNode in StoredProcNodes ) {
                        StoredProc ThisStoredProc = new StoredProc( this._DB, StoredProcNode );
                        this._StoredProcs.Add( ThisStoredProc );
                    }
                }

                if ( this._StoredProcs != null ) {

                    foreach( StoredProc ThisStoredProc in this._StoredProcs ) {
                        ThisStoredProc.Execute();
                    }
                }
            } catch ( SqlException ) {
                throw;
            } finally {
                this._DB.Close();
            }
        }


#region ISqlInfo Members

        public XmlNodeList SqlParams
        {
            get {
                return this._ActionNode.SelectNodes( "Params" );
            }
        }


        public XmlNode SqlExpectedResult
        {
            get {
                return this._ActionNode.SelectSingleNode( "ExpectedResults" );
            }
        }

#endregion
    }
}

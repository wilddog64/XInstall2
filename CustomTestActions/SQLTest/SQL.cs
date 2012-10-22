using System;
using System.Collections;
using System.Xml;

using XInstall.Core;
using XInstall.Util;
using XInstall.Util.Log;

namespace XInstall.CustomTestActions {
    /// <summary>
    /// Summary description for SQL.
    /// </summary>
    public class SQL : ActionElement {
        private const string NODE_NAME        = "SQLTest";
        private const string PARENT_NODE_NAME = "SQL";

        private SQLTestCollection _SQLTests    = new SQLTestCollection();
        private Hashtable         _TestResults = new Hashtable();

        [Action("SQL")]
        public SQL( XmlNode ActionNode ) : base( ActionNode ) {}

        protected override string ObjectName
        {
            get {
                return this.GetType().Name;
            }
        }

        [Action("runnable", Needed=false, Default="true")]
        public string runnable
        {
            set {
                base.Runnable = bool.Parse( value );
            }
        }

        public Hashtable TestResults
        {
            get {
                return this._TestResults;
            }
        }

        public SQLTestCollection SQLTests
        {
            get {
                return this._SQLTests;
            }
        }

        protected override void ParseActionElement() {
            base.ParseActionElement();
            this.ProcessSQLTestNode();

            foreach ( SQLTest MySQLTest in this._SQLTests ) {
                MySQLTest.Execute();
                if ( !this._TestResults.ContainsKey( MySQLTest.DBServer ) )
                    this._TestResults.Add( MySQLTest.DBServer, MySQLTest.StoredProcs );
            }
        }

        protected override object ObjectInstance
        {
            get {
                return this;
            }
        }

        private void ProcessSQLTestNode() {
            XmlNode ActionNode       = base.ActionNode;
            XmlNodeList SQLTestNodes = ActionNode.SelectNodes( NODE_NAME );

            foreach ( XmlNode SQLTestNode in SQLTestNodes ) {
                SQLTest MySQLTest = new SQLTest( SQLTestNode );
                XmlAttributeCollection SQLTestNodeAttribs = SQLTestNode.Attributes;

                XmlNode SQLTestNodeDBSrvAttrib = SQLTestNodeAttribs.GetNamedItem( "DBServer" );
                string ErrorMessage = String.Empty;
                if ( SQLTestNodeDBSrvAttrib != null &&
                        SQLTestNodeDBSrvAttrib.Value.Length > 0 )
                    MySQLTest.DBServer = SQLTestNodeDBSrvAttrib.Value;
                else
                    ErrorMessage = String.Format(@"{0}: Attribute DBServer is required!", 
                                                 this.ObjectName);

                XmlNode SQLTestNodeDatabaseAttrib = SQLTestNodeAttribs.GetNamedItem( "Database" );
                if (SQLTestNodeDatabaseAttrib != null &&
                        SQLTestNodeDatabaseAttrib.Value.Length > 0 )
                    MySQLTest.DBCategory = SQLTestNodeDatabaseAttrib.Value;
                else
                    ErrorMessage = String.Format(
                                       "{0}: Attribute Database is required", this.ObjectName );

                XmlNode SQLTestNodeTrusted = SQLTestNodeAttribs.GetNamedItem( "TrustedConnection" );
                if ( SQLTestNodeTrusted != null &&
                        SQLTestNodeTrusted.Value.Length > 0 ) {
                    bool Trusted = XmlConvert.ToBoolean( SQLTestNodeTrusted.Value );
                    MySQLTest.TrustedConnection = Trusted.ToString();
                } else
                    MySQLTest.TrustedConnection = "true";

                XmlNode SQLTestNodeDBUserName = SQLTestNodeAttribs.GetNamedItem( "UserName" );
                XmlNode SQLTestNodeDBUserPass = SQLTestNodeAttribs.GetNamedItem( "UserPass" );

                bool UseSQLID = (SQLTestNodeDBUserName != null &&
                                 SQLTestNodeDBUserPass != null);
                if (UseSQLID) {
                    string UserName = SQLTestNodeDBUserName.Value;
                    string UserPass = SQLTestNodeDBUserPass.Value;

                    bool BadSQLID = ( UserName.Length == 0 ||
                                      UserPass.Length == 0 );
                    if ( !BadSQLID ) {
                        MySQLTest.DBUserName = UserName;
                        MySQLTest.DBUserPass = UserPass;
                    } else
                        ErrorMessage = String.Format(
                                           @"{0}: you have to provide both DBUserName and DBUserPass if you want to use SQL standard security!" );
                }

                if ( ErrorMessage.Length > 0 )
                    throw new XmlException( ErrorMessage );

                this._SQLTests.Add( MySQLTest );
            }
        }
    }
}

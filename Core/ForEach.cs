using System;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Xml;

// using Ops.ITCMLocalLB.Struct;
// using Ops.Proxy;

namespace XInstall.Core {
    public enum AllowTypes {
        File         = 0,
        Line,
        F5NodeGroup,
    }

    sealed class PORTDEF {
        public long port;
        public string address;
    }

    /// <summary>
    /// The foreach class simulate the foreach statement
    /// in VB script, which will base on the condition
    /// to repeat the content a number of times.  The foreach
    /// is presenting in the following format:
    ///
    ///    <foreach item="..." type="..." in="...">
    ///         ...
    ///         ...
    ///         ...
    ///    </foreach>
    /// </summary>
    public class ForEach : ActionElement {
        private XmlNode    _ActionNode    = null;
        private Hashtable  _ActionObjects = null;
        // private iControl   _BigIP         = null;

        private string _Item            = String.Empty;
        private string _Type            = String.Empty;
        private string _In              = String.Empty;
        private string _Delim           = String.Empty;
        private string _PoolName        = String.Empty;
        private string _BigIPHost       = string.Empty;
        private string _ResolveHostName = "true";

        private bool   _UseThread       = false;
        private bool   _Resolve         = true;


        /// <summary>
        /// The foreach tag constructor that accepts the
        /// following parameters:
        ///
        ///    1. Node is an xml node that represent foreach
        ///       itself.
        ///    2.
        /// </summary>
        /// <param name="Node"></param>
        /// <param name="ActionObjects"></param>
        [Action("foreach")]
        public ForEach( XmlNode Node, Hashtable ActionObjects ) : base( Node ) {
            this._ActionNode    = Node;
            this._ActionObjects = ActionObjects;

        }

#region public properties
        /// <summary>
        /// set a flag to indicate whether a given
        /// class should be executed or not
        /// </summary>
        [Action("runnable", Needed=false, Default="true")]
        public new string Runnable {
            set { base.Runnable = bool.Parse( value ); }
        }

        /// <summary>
        /// set a item that will be passed to the content of
        /// foreach node
        /// </summary>
        [Action("item", Needed=true)]
        public string Item {
            get { return ActionVariables.ScanVariable( this._Item ); }
            set {
                this._Item = value;
                ActionVariables.Add( this._Item, "", true );
            }
        }

        /// <summary>
        /// Set a type that foreach node will work on.
        /// Currently the supported types are:
        ///     File - foreach will process file and
        ///            the content of file has to be one
        ///            item per line.
        ///
        ///     Line - foreach will process a single line
        ///            and the line by default is comma delima.
        ///
        ///     F5NodeGroup - foreach will process pool defined
        ///                   in the BigIP.
        /// </summary>
        [Action("type", Needed=true)]
        public string Type {
            get { return this._Type; }
            set { this._Type = value; }
        }

        /// <summary>
        /// set an object to be processed depending on
        /// a given type
        /// </summary>
        [Action("in", Needed=true)]
        public string In {
            get { return this._In; }
            set { this._In = value; }
        }

        /// <summary>
        /// an optional proerty that sets the delima for process
        /// line.  By default, this is a comma; but you can set
        /// it to a different one.
        /// </summary>
        [Action("delim", Needed=false, Default=@",")]
        public string Delim {
            get { return this._Delim; }
            set { this._Delim = value; }
        }

        [Action("bigiphost", Needed=false, Default="")]
        public string BigIPHost {
            get { return this._BigIPHost; }
            set { this._BigIPHost = value; }
        }

        /// <summary>
        ///  returns the name of an object
        /// </summary>
        public new string Name {
            get { return this.GetType().Name; }
        }

        [Action("usethread", Needed=false, Default="false")]
        public string UseThread {
            get { return this._UseThread.ToString(); }
            set { this._UseThread = bool.Parse( value ); }
        }

        /// <summary>
        /// get/set a flag to indicate if ip should resolve to
        /// a hostname/machine name.
        /// </summary>
        [Action("resolvehostname", Needed=false, Default="true")]
        public string ResolveHostName {
            get { return this._ResolveHostName; }
            set {
                this._ResolveHostName = value;
                this._Resolve         = bool.Parse( this._ResolveHostName );
            }
        }
#endregion

#region protected properties

        /// <summary>
        /// returns the name of a object
        /// </summary>
        protected override string ObjectName {
            get { return this.Name; }
        }

        /// <summary>
        /// an override property that returns the foreach class
        /// instance
        /// </summary>
        protected override object ObjectInstance {
            get { return this; }
        }

#endregion

#region protected methods

        /// <summary>
        /// ParseActionElement() takes in charge of dispatching
        /// different action depending on a given type attribute
        /// input from foreach tage
        /// </summary>
        protected override void ParseActionElement() {
            // need to call base class's ParseActionElement()
            // methods; otherwise, the object won't work
            base.ParseActionElement ();
            switch ( this.GetThisType() ) {
                    // process BigIP
                //case AllowTypes.F5NodeGroup:
                //    DoF5NodeGroup( this._ActionNode );
                //    break;

                    // process file
                case AllowTypes.File:
                    DoFile( this.In, this._ActionNode );
                    break;

                    // process line
                case AllowTypes.Line:
                    DoLine( this._ActionNode );
                    break;
            }

        }
#endregion

#region private methods

        //private iControl BigIP
        //{
        //    get {
        //        return this._BigIP;
        //    }
        //    set {
        //        this._BigIP = value;
        //    }
        //}

        // GetThisType() translate input string type
        // into a enumeration type in the class
        private AllowTypes GetThisType() {
            // assign a default value, which is a directory
            AllowTypes ThisType = AllowTypes.File;

            // start converting
            // mP>U3sr!
            // N0def@ult$
            switch ( this.Type ) {
                case "File":
                    ThisType = AllowTypes.File;
                    break;
                //case "F5NodeGroup":
                //    ThisType = AllowTypes.F5NodeGroup;
                //    if ( this.BigIPHost.Length == 0 )
                //        throw new
                //        ArgumentNullException(
                //            "when type is F5NodeGroup, BigIPHost cannot be an empty value" );
                //    this.BigIP = new iControl( this.BigIPHost, "rn56weg7", "4%ee3$5twR" );
                //    // this.BigIP = new iControl( this.BigIPHost, "mliang", "Admin#1" );
                //    break;
                case "Line":
                    ThisType = AllowTypes.Line;
                    break;
            }

            return ThisType;
        }

        // ProcessDirectory( DirectoryInfo di ) - this function
        // process a given directory recusively.
        private void ProcessDirectory( DirectoryInfo di ) {
            try {
                foreach( DirectoryInfo Directory in di.GetDirectories() )
                this.ProcessDirectory( Directory );

                foreach( FileInfo File in di.GetFiles() )
									ActionVariables.Add( this.Item, File.Name, true );
            } 
						catch ( System.IO.DirectoryNotFoundException d ) {
                base.SetExitMessage( "object {0}: method {1} reports error - {2}", this.Name, "ProcessDirectory", d.Message );
                throw;
            } 
						catch ( System.IO.FileNotFoundException f ) {
                base.SetExitMessage( "object {0}: method {1} reports error - {2}", this.Name, "ProcessDirectory", f.Message );
                throw;
            } 
						catch ( Exception ex ) {
                base.SetExitMessage( "object {0}: method {1} reports error - {2}", this.Name, "ProcessDirectory", ex.Message );
                throw;
            }
        }

        // DoF5NodeGroup() - this is used to process F5 BigIP
        //private void DoF5NodeGroup( XmlNode xn ) {
        //    try {
        //        PORTDEF[] PoolMembers = PoolMembers =
        //                                    GetPoolMembers( this.In );
        //        foreach ( PORTDEF PoolMember in PoolMembers ) {
        //            string HostName = this._Resolve                      ?
        //                              iControl.GetHostByAddress( PoolMember.address )  :
        //                              PoolMember.address;

        //            ActionVariables.Add( this.Item, HostName, true );
        //            this.ProcessChildNodes( xn.ChildNodes );
        //        }
        //    } catch ( Exception ex ) {
        //        base.SetExitMessage( "object {0}: method {1} reports error, {2}",
        //                             this.Name, "DoF5NodeGroup", ex.Message );
        //        throw;

        //    }
        //}

        private PORTDEF GetPortDefs( object Port ) {
            PORTDEF PortDef = new PORTDEF();

            Type t = Port.GetType();
            FieldInfo p = t.GetField( "port" );
            FieldInfo adr = t.GetField( "address" );

            if ( p != null && adr != null) {
                PortDef.port = (long) p.GetValue( Port );
                PortDef.address = (string) adr.GetValue( Port );
            }

            return PortDef;
        }

        // DoFile( string FileName, XmlNode xn ) - this function
        // is used to process file content
        private void DoFile( string FileName, XmlNode xn ) {
            try {
                using( StreamReader FileReader = new StreamReader( FileName, System.Text.Encoding.Default ) ) {
                    while ( FileReader.Peek() > 0 ) {
                        ActionVariables.Add( this.Item, FileReader.ReadLine(), true );
                        if ( xn.HasChildNodes )
                            this.ProcessChildNodes( xn.ChildNodes );
                    }
                }
            } 
						catch ( System.IO.FileNotFoundException f ) {
                base.SetExitMessage( "object {0}: method {1} reports error, {2}", this.Name, "DoFile", f.Message );
                throw;
            } 
						catch ( Exception ex ) {
                base.SetExitMessage( "object {0}: method {1} reports error, {2}", this.Name, "ProcessDirectory", ex.Message );
                throw;
            }
        }

        // DoLine( XmlNode xn ) is used to process line input.
        private void DoLine( XmlNode xn ) {
            if ( this.Delim.Length != 0 ) {
                string[] Items = this.In.Split( this.Delim.ToCharArray() );
                foreach ( string Item in Items ) {
                    ActionVariables.Add( this.Item, Item, true );
                    if ( xn.HasChildNodes )
                        this.ProcessChildNodes( xn.ChildNodes );
                }
            }
        }

        // ProcessChildNodes( XmlNodeList ChildNode ) will execute
        // the content in the foreach node if a gvien node inside it
        // can be found from the _ActionObjects hash table
        private void ProcessChildNodes( XmlNodeList ChildNodes ) {
            foreach ( XmlNode ChildNode in ChildNodes ) {
                object ObjInstance              = base.CreateObject( ChildNode.Name, ChildNode, this._ActionObjects );
                ActionElement ThisActionElement = ( ObjInstance as ActionElement );
                if ( ThisActionElement != null )
                    ThisActionElement.Execute();
            }
        }

        //private PORTDEF[] GetPoolMembers( string PoolName ) {
        //    object[] PortList = (object[]) this.BigIP.Dispatch( "lbpool", "get_member_list", PoolName );
        //    PORTDEF[] PortDefs = new PORTDEF[ PortList.Length ];

        //    int i = 0;
        //    foreach ( object Port in PortList ) {
        //        PORTDEF PortDef = this.GetPortDefs( Port );
        //        PortDefs[ i++ ] = PortDef;
        //    }

        //    return PortDefs;
        //}

#endregion
    }
}

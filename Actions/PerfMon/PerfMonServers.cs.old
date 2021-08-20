using System;
using System.Collections;
using System.Diagnostics;
using System.Xml;

namespace XInstall.Core.Actions {
    /// <summary>
    /// Summary description for PerfMonServers.
    /// </summary>
    internal class PerfMonServer : ActionElement {
        private const string MY_NODE_NAME   = @"PerfMonCheckServer";
        private const string MY_PARENT_NODE = @"perfmon";

        private Hashtable CounterInfo = new Hashtable();
        private float  _MaxValue      = 0;
        private float  _MinValue      = 0;
        private float  _CounterValue  = 0;

        private string _MachineName     = String.Empty;
        private string _CounterCategory = String.Empty;
        private string _ServerName      = String.Empty;

        [Action("PerfMonCheck")]
        public PerfMonServer( XmlNode ActionNode ) : base( ActionNode ) {
            if ( ActionNode.ParentNode.Name != MY_PARENT_NODE )
                throw new Exception( String.Format( "{0} cannot be a child of {1}", MY_NODE_NAME, MY_PARENT_NODE ) );

            if ( ActionNode.Name != MY_NODE_NAME )
                throw new ArgumentException( "invalid element!", "perfmon" );
        }

#region public properties
        [Action("MachineName", Needed=true)]
        public string MachineName {
            get {
                return this._MachineName;
            }

            set {
                this._MachineName = value;
            }
        }

        [Action("Category", Needed=true)]
        public string CounterCategory {
            get {
                return this._CounterCategory;
            }

            set {
                this._CounterCategory = value;
            }
        }


#endregion

#region protected properties

        protected float CounterValue {
            get {
                return this._CounterValue;
            }

            set {
                this._CounterValue = value;
            }
        }


        protected bool InRange {
            get {
                return this._CounterValue >= this._MinValue &&
                       this._CounterValue <= this._MaxValue;
            }
        }


        protected override object ObjectInstance {
            get {
                return this;
            }
        }

#endregion

#region protected methods
        protected override void ParseActionElement() {
            base.ParseActionElement();

            XmlNode ActionNode   = base.ActionNode;
            XmlNodeList CategoryNodes = ActionNode.SelectNodes( "Category" );

            foreach ( XmlNode CategoryNode in CategoryNodes ) {
                if ( CategoryNode.HasChildNodes ) {
                    XmlNode CategoryNodeName = CategoryNode.Attributes.GetNamedItem( "name" );
                    if ( CategoryNodeName != null )
                        ProcessCategory( CategoryNodeName.Value, this.MachineName, CategoryNode.ChildNodes );
                    else
                        throw
                        new ArgumentNullException( "Name", "countercategoryname can't be null or empty" );
                }
            }
        }
#endregion

        private void ProcessCategory( string CounterCategoryName, string MachineName, XmlNodeList CategoryChildNodes ) {
            PerformanceCounterCategory PerfMonCounterCat = new PerformanceCounterCategory( CounterCategoryName, MachineName );

            foreach( XmlNode CategoryChild in CategoryChildNodes ) {
                if ( CategoryChild.Name == "Counter" ) {
                    XmlAttributeCollection ChildAttribs = CategoryChild.Attributes;
                    if ( ChildAttribs.Count > 0 ) {
                        ArrayList CounterInfoArray  = new ArrayList();
                        XmlNode CounterNameNode     = ChildAttribs.GetNamedItem( "Name" );
                        XmlNode InstanceNameNode    = ChildAttribs.GetNamedItem( "InstanceName" );
                        XmlNode CounterMaxValueNode = ChildAttribs.GetNamedItem( "MaxValue" );
                        XmlNode CounterMinValueNode = ChildAttribs.GetNamedItem( "MinValue" );

                        if ( CounterNameNode == null ) {
                            CounterNameNode     = CategoryChild.SelectSingleNode( "Name" );
                            string InstanceName = String.Empty;

                            if ( CounterMaxValueNode == null )
                                CounterMaxValueNode = CategoryChild.SelectSingleNode( "MaxValue" );
                            else
                                throw new ArgumentNullException( "MaxValue", "maxvalue can't be null" );

                            if ( CounterMinValueNode == null )
                                CounterMinValueNode = CategoryChild.SelectSingleNode( "MinValue" );
                            else
                                throw new ArgumentNullException( "MinValue", "minvalue can't be null" );

                            if ( InstanceNameNode != null ) {
                                InstanceName = InstanceNameNode.Value;
                                if ( PerfMonCounterCat.InstanceExists( InstanceName ) ) {
                                    PerformanceCounter PerfMonCounter =
                                        new PerformanceCounter( CounterCategoryName,
                                                                CounterNameNode.Value,
                                                                InstanceName,
                                                                MachineName );
                                    PerfMonCounter.NextValue();
                                }
                            }
                        } 
                        else
                            throw new ArgumentNullException( "Name", "counter name can't be null" );

                    }
                }
            }
        }
    }
}

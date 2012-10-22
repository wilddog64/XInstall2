using System;
using System.Collections;
using System.Diagnostics;
using System.Xml;

namespace XInstall.CustomTestActions {
    /// <summary>
    /// Summary description for PerfMonCounterCategory.
    /// </summary>
    public class PerfMonCounterCategory {
        private PerformanceCounterCategory _PerfMonCountCategory = null;
        private PerfMonCounterCollection   _PerfMonCounters      = new PerfMonCounterCollection();

        private string _MachineName  = String.Empty;
        private string _CategoryName = String.Empty;

        public PerfMonCounterCategory( XmlNode ActionNode ) {
            ProcessCounterCategoryNode( ActionNode );
        }

        public PerfMonCounterCategory( string MachineName,
                                       string CategoryName ) {
            this._PerfMonCountCategory =
                new PerformanceCounterCategory( CategoryName, MachineName );
        }


        public PerfMonCounterCategory( string MachineName,
                                       string CategoryName,
                                       params string[] Counters ) {
            this._MachineName  = MachineName;
            this._CategoryName = CategoryName;

            this._PerfMonCountCategory = new PerformanceCounterCategory(
                                             this._MachineName, this._CategoryName );

            if ( Counters.Length > 0 ) {
                foreach( string Counter in Counters ) {
                    string[] Items = Counter.Split( new char[]{ ':' } );
                    if (Items.Length > 0) {
                        if ( this._PerfMonCountCategory.CounterExists( Items[0]  ) &&
                                this._PerfMonCountCategory.InstanceExists( Items[1] ) ) {
                            PerfMonCounter MyPerfMonCounter =
                                new PerfMonCounter( MachineName,
                                                    CategoryName,
                                                    Items[0],
                                                    Items[1],
                                                    (float) Convert.ToDouble( Items[2] ),
                                                    (float) Convert.ToDouble( Items[3] ) );
                            this._PerfMonCounters.Add( MyPerfMonCounter );

                        }
                    }
                }
            }
        }


        public void Add( PerfMonCounter MyPerfMonCounter ) {
            this._PerfMonCounters.Add( MyPerfMonCounter );
        }


        public void Add( string CounterName, string InstanceName,
                         float MaxValue, float MinValue ) {
            PerfMonCounter MyPerfMonCounter =
                new PerfMonCounter( this.MachineName,
                                    this.CategoryName,
                                    CounterName,
                                    InstanceName,
                                    MaxValue,
                                    MinValue );
            this._PerfMonCounters.Add( MyPerfMonCounter );
        }


        public void Add( string CounterName, string InstanceName,
                         string MaxValue, string MinValue ) {
            float MaxVal = (float) Convert.ToDouble( MaxValue );
            float MinVal = (float) Convert.ToDouble( MinValue );

            this.Add( CounterName, InstanceName, MaxVal, MinVal );
        }


        public PerfMonCounter this[ string CounterName ]
        {
            get {
                return this._PerfMonCounters[ CounterName ];
            }
        }


        public string MachineName
        {
            get {
                return this._MachineName;
            }
            set {
                this._MachineName = value;
            }
        }


        public string CategoryName
        {
            get {
                return this._CategoryName;
            }
            set {
                this._CategoryName = value;
            }
        }


        public PerfMonCounterCollection PerfMonCounters
        {
            get {
                return this._PerfMonCounters;
            }
        }

        private void ProcessCounterCategoryNode( XmlNode ActionNode ) {
            XmlAttributeCollection CounterCategoryAttribs = ActionNode.Attributes;

            XmlNode MachineNameNode         = CounterCategoryAttribs.GetNamedItem( "MachineName" );
            XmlNode CounterCategoryNameNode = CounterCategoryAttribs.GetNamedItem( "CounterCategoryName" );

            if ( MachineNameNode != null &&
                    MachineNameNode.Value.Length > 0) {
                this.MachineName = MachineNameNode.Value;
            } else {
                throw new ArgumentNullException( "MachineName Attribute",
                                                 "MachineName attribute node missing" );
            }

            if ( CounterCategoryNameNode != null &&
                    CounterCategoryNameNode.Value.Length > 0 ) {
                this.CategoryName = CounterCategoryNameNode.Value;
            } else {
                throw new ArgumentNullException( "CounterCategoryName Attribute",
                                                 "CounterCategoryName attribute node missing" );
            }

            if ( ActionNode.HasChildNodes ) {

                ProcessCounterCategoryChildNode( ActionNode.ChildNodes );
            }
        }


        private void ProcessCounterCategoryChildNode( XmlNodeList CounterNodes ) {
            foreach( XmlNode CounterNode in CounterNodes ) {
                XmlAttributeCollection CounterAttribs = CounterNode.Attributes;
                if ( CounterNode.ParentNode.Name == "CounterCategory" &&
                        CounterNode.Name            == "Counter" ) {
                    PerfMonCounter MyPerfMonCounter = new PerfMonCounter( CounterNode );
                    this.Add( MyPerfMonCounter );
                }
            }
        }
    }
}

using System;
using System.Diagnostics;
using System.Xml;

namespace XInstall.CustomTestActions {
    /// <summary>
    /// Summary description for PerfMonAttribute.
    /// </summary>
    public class PerfMonCounter {
        private const string CHILD_NAME        = @"Counter";
        private const string PARENT_NAME       = @"CounterCategory";
        private const string GRAND_PARENT_NAME = @"PerfMon";

        private PerformanceCounter _PerfMonCounter = new PerformanceCounter();

        private bool   _CounterEnable       = true;

        private string _MachineName         = string.Empty;
        private string _CounterCategoryName = string.Empty;
        private string _CounterInstanceName = string.Empty;
        private string _CounterName         = string.Empty;
        private string _DisplayName         = string.Empty;

        private float  _MaxValue            = 0.0F;
        private float  _MinValue            = 0.0F;

        public PerfMonCounter() {}

        public PerfMonCounter( XmlNode ActionNode ) {
            ProcessPerfMonCounterNode( ActionNode );
        }


        public PerfMonCounter( string MachineName,
                               string CategoryName,
                               string CounterName,
                               string InstanceName,
                               float  MaxValue,
                               float  MinValue) {
            this._MachineName         = MachineName;
            this._CounterCategoryName = CategoryName;
            this._CounterName         = CounterName;
            this._MaxValue            = MaxValue;
            this._MinValue            = MinValue;

            this._PerfMonCounter.MachineName  = this._MachineName;
            this._PerfMonCounter.CategoryName = this._CounterCategoryName;
            this._PerfMonCounter.CounterName  = this._CounterName;

            if ( PerformanceCounterCategory.InstanceExists(
                        InstanceName,
                        CategoryName,
                        MachineName ) ) {
                this._CounterInstanceName         = InstanceName;
                this._PerfMonCounter.InstanceName = InstanceName;
            } else {
                throw new ArgumentException(
                    string.Format( @"instance name {0} does not exist for this counter {1}
                                   with this category {2} on this machine {3}",
                                   InstanceName, CounterName, CategoryName, MachineName ) );
            }

        }


        public string UniqCounterName
        {
            get {
                string UniqName =
                    String.Format( "{0}_{1}_{2}_{3}",
                                   this.MachineName,
                                   this._CounterCategoryName,
                                   this._CounterName,
                                   this._CounterInstanceName );

                return UniqName;
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


        public string CounterCategoryName
        {
            get {
                return this._CounterCategoryName;
            }
            set {
                this._CounterCategoryName = value;
            }
        }


        public string CounterName
        {
            get {
                return this._CounterName;
            }
            set {
                this._CounterName = value;
            }
        }


        public string CounterInstanceName
        {
            get {
                return this._CounterInstanceName;
            }
            set {
                this._CounterInstanceName = value;
            }
        }


        public float MaxValue
        {
            get {
                return this._MaxValue;
            }
            set {
                this._MaxValue = value;
            }
        }


        public float MinValue
        {
            get {
                return this._MinValue;
            }
            set {
                this._MinValue = value;
            }
        }


        public float CounterValue
        {
            get {
                return this._PerfMonCounter.NextValue();
            }
        }


        public bool InRange
        {
            get {
                float CounterValue = this._PerfMonCounter.NextValue();
                return ( CounterValue >= this._MinValue &&
                         CounterValue <= this._MaxValue );
            }
        }


        public bool CounterEnable
        {
            get {
                return this._CounterEnable;
            }
        }


        public string DisplayName
        {
            get {
                return this._DisplayName;
            }
            set {
                this._DisplayName = value;
            }
        }

        private void ProcessPerfMonCounterNode( XmlNode ActionNode ) {

            if ( ActionNode.Name                       == CHILD_NAME  &&
                    ActionNode.ParentNode.Name            == PARENT_NAME &&
                    ActionNode.ParentNode.ParentNode.Name == GRAND_PARENT_NAME ) {
                XmlNode CounterCategoryNode = ActionNode.ParentNode;
                XmlAttributeCollection CounterCategoryAttribs = CounterCategoryNode.Attributes;
                XmlNode MachineNameAttrib                     = CounterCategoryAttribs.GetNamedItem( "MachineName" );
                XmlNode CounterCategoryNameAttrib             = CounterCategoryAttribs.GetNamedItem( "CounterCategoryName" );
                this.MachineName         = MachineNameAttrib.Value;
                this.CounterCategoryName = CounterCategoryNameAttrib.Value;

                XmlAttributeCollection CounterAttribs = ActionNode.Attributes;
                XmlNode CounterNameAttrib         = CounterAttribs.GetNamedItem( "Name" );
                XmlNode CounterInstanceNameAttrib = CounterAttribs.GetNamedItem( "InstanceName" );
                XmlNode CounterMaxValueAttrib     = CounterAttribs.GetNamedItem( "HighThreshold" );
                XmlNode CounterMinValueAttrib     = CounterAttribs.GetNamedItem( "LowThreshold" );
                XmlNode CounterEnableAttrib       = CounterAttribs.GetNamedItem( "Enable" );
                XmlNode CounterDisplayNameAttrib  = CounterAttribs.GetNamedItem( "DisplayName" );


                if ( CounterDisplayNameAttrib != null &&
                        CounterDisplayNameAttrib.Value.Length > 0) {
                    this.DisplayName = CounterDisplayNameAttrib.Value;
                }

                this.CounterName         = CounterNameAttrib.Value;
                this.CounterInstanceName = CounterInstanceNameAttrib.Value;
                this.MaxValue            = (float) Convert.ToDouble(CounterMaxValueAttrib.Value);
                this.MinValue            = (float) Convert.ToDouble(CounterMinValueAttrib.Value);

                if ( CounterEnableAttrib != null )
                    this._CounterEnable = bool.Parse( CounterEnableAttrib.Value );

                if ( this.CounterEnable ) {
                    this._PerfMonCounter = new PerformanceCounter();
                    this._PerfMonCounter.MachineName = this.MachineName;
                    this._PerfMonCounter.CategoryName = this.CounterCategoryName;
                    this._PerfMonCounter.CounterName = this.CounterName;
                    if ( this._CounterInstanceName.Length > 0 )
                        this._PerfMonCounter.InstanceName = this.CounterInstanceName;
                }
            }
        }
    }
}

using System;
using System.Collections;
using System.Xml;

using XInstall.Core;
using XInstall.Util;
using XInstall.Util.Log;

namespace XInstall.CustomTestActions {
    /// <summary>
    /// Summary description for Class1.
    /// </summary>
    public class PerfMon : ActionElement {

        private PerfMonCounterCategoryCollection _PerfMonCounterCategories = null;

        [Action("PerfMon")]
        public PerfMon( XmlNode ActionNode ) : base( ActionNode ) {
            foreach ( XmlNode CounterCategoryNode in ActionNode.ChildNodes ) {
                PerfMonCounterCategory MyPerfMonCounterCategory =
                    new PerfMonCounterCategory( CounterCategoryNode );
                this._PerfMonCounterCategories.Add( MyPerfMonCounterCategory );
            }
        }


        [Action("runnable", Needed=false, Default="true")]
        public new string Runnable
        {
            set {
                base.Runnable = bool.Parse( value );
            }
        }

        [Action("skiperror", Needed=false, Default="false")]
        public new string SkipError
        {
            set {
                base.SkipError = bool.Parse( value );
            }
        }

#region protected properties
        protected override object ObjectInstance
        {
            get {
                return this;
            }
        }

        protected override string ObjectName
        {
            get {
                return this.GetType().Name;
            }
        }
#endregion

#region protected methods
        protected override void ParseActionElement() {
            base.ParseActionElement ();
            XmlNode ActionNode = base.ActionNode;

            foreach( PerfMonCounterCategory MyPerfMonCounterCategory in
                     this._PerfMonCounterCategories ) {}
        }
#endregion

#region private methods
        private PerfMonCounterCategory ProcessCounterCategory( XmlNode ChildNode ) {
            XmlAttributeCollection ChildNodeAttribs  = ChildNode.Attributes;
            XmlNode MachineNameNode         = ChildNodeAttribs.GetNamedItem( "MachineName" );
            XmlNode CounterCategoryNameNode = ChildNodeAttribs.GetNamedItem( "CounterCategoryName" );

            string MachineName         = MachineNameNode.Value;
            string CounterCategoryName = CounterCategoryNameNode.Value;
            bool HasCounterCategoryInfo = MachineName.Length > 0 &&
                                          CounterCategoryName.Length > 0;

            if ( ChildNode.HasChildNodes ) {
                XmlAttributeCollection CounterNodeAttribs = ChildNode.Attributes;

                foreach( XmlNode CounterNode in ChildNode.ChildNodes ) {
                    if ( CounterNode.Name == "Counter" ) {
                        XmlNode CounterNameNode         = CounterNodeAttribs.GetNamedItem( "Name" );
                        XmlNode CounterInstanceNameNode = CounterNodeAttribs.GetNamedItem( "InstanceName" );
                        XmlNode MaxValueNode            = CounterNodeAttribs.GetNamedItem( "MaxValue" );
                        XmlNode MinValueNode            = CounterNodeAttribs.GetNamedItem( "MinValue" );

                        string CounterParams =
                            String.Format( "{0}:{1}:{2}:{3}",
                                           CounterNameNode.Value,
                                           CounterInstanceNameNode.Value,
                                           MaxValueNode.Value,
                                           MinValueNode.Value );


                    }
                }
            }

            return null;
        }
#endregion
    }
}
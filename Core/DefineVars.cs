using System;
using System.Xml;

namespace XInstall.Core {
    /// <summary>
    /// Summary description for DefineVars.
    /// </summary>
    public class DefineVar : ActionElement {
        private XmlNode _Var = null;
        private string  _VarName = string.Empty;
        private string  _VarValue = string.Empty;

        [Action("defvar")]
        public DefineVar ( XmlNode VarNode ) : base( VarNode ) {
            this._Var = VarNode;
        }


        [Action("name", Needed=false)]
        public string VarName
        {
            get { return this._VarName; }
            set { this._VarName = value; }
        }


        [Action("value", Needed=false, Default="")]
        public string VarValue
        {
            get { return this._VarValue; }
            set { this._VarValue = value; }
        }


        protected override void ParseActionElement() {
            if ( this.VarName.Length  != 0 && this.VarValue.Length != 0 )
                ActionVariables.Add( this.VarName, this.VarValue, true );
            else if ( this._Var.HasChildNodes ) {
                XmlNodeList Vars = this._Var.ChildNodes;
                foreach ( XmlNode Var in Vars ) {
                    if ( Var.Name.Equals( "var" ) ) {
                        XmlNode VarName  = Var.Attributes.GetNamedItem( "name" );
                        XmlNode VarValue = Var.Attributes.GetNamedItem( "value" );
                        if ( VarName.Value.Length != 0 && VarValue.Value.Length != 0 )
                            ActionVariables.Add( VarName.Value, VarValue.Value, true );

                    }
                }
            }
        }

    }
}

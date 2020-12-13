using System;

namespace XInstall.Core {
    /// <summary>
    /// Summary description for ADSIAttributes.
    /// </summary>
    [AttributeUsage( AttributeTargets.Property )]
    public class ADSIAttribute : Attribute {
	    private string _Name = String.Empty;
	    public ADSIAttribute( string Name ) {
		    this._Name = Name;
	    }

	    public string Name {
		    get {
			    return this._Name;
		    }
		    set {
			    this._Name = value;
		    }
	    }

    }
}

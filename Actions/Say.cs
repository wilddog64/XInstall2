using System;

namespace XInstall.Core.Actions
{
    /// <summary>
    /// Summary description for Say.
    /// </summary>
    public class Say : ActionElement, IAction
    {
	    [Action("say")]
	    public Say()
	    {
		    //
		    // TODO: Add constructor logic here
		    //
	    }
	    #region IAction Members

	    public override void Execute()
	    {
		    base.LogItWithTimeStamp( "hello world!" );
	    }

	    public new bool IsComplete
	    {
		    get
		    {
			    // TODO:  Add Say.IsComplete getter implementation
			    return false;
		    }
	    }

	    public new string ExitMessage
	    {
		    get
		    {
			    // TODO:  Add Say.ExitMessage getter implementation
			    return null;
		    }
	    }

	    public new string Name
	    {
		    get
		    {
			    // TODO:  Add Say.Name getter implementation
			    return this.GetType().Name;
		    }
	    }

	    public new int ExitCode
	    {
		    get
		    {
			    // TODO:  Add Say.ExitCode getter implementation
			    return 0;
		    }
	    }

	    #endregion
    }
}

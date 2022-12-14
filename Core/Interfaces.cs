using System;
using System.Collections;
using System.Xml;

namespace XInstall.Core {
    using System.Collections;


    /// <summary>
    /// An interface that provides the ability to
    /// remove what the object has done so far. It
    /// provides only one interface method, RemoveIt
    /// </summary>
    public interface ICleanUp {
        /// <summary>
        /// The method that provide an ability
        /// to remove what object has done so far
        /// </summary>
        /// <remarks>
        /// This interface should be implemented by
        /// all the action objects so that when required,
        /// the object has ability to remove what it has
        /// done so far.
        /// </remarks>
        void RemoveIt();
    }


    /// <summary>
    /// The common interface that provide an action object
    /// to be executed.
    /// </summary>
    public interface IAction {
        /// <summary>
        /// The interface property that returns the exit code
        /// from the current object
        /// </summary>
        int ExitCode {
            get;
        }

        /// <summary>
        /// The interface property that retrieves the message
        /// from the action object.
        /// </summary>
        string ExitMessage {
            get;
        }

        /// <summary>
        /// The interface property that gets the name of the
        /// action object.
        /// </summary>
        string Name {
            get;
        }

        /// <summary>
        /// The interface property that gets the state of
        /// an action object.
        /// </summary>
        bool IsComplete {
            get;
        }

        /// <summary>
        /// The interface method that execute the Action object.
        /// </summary>
        void Execute();
    }

    public interface IActionElement {
        void ParseActionElement();
        string ObjectName { get; }
    }
}

using System;

namespace XInstall.Util {
    /// <summary>
    /// Summary description for Trace.
    /// </summary>
    public class Trace {
        private static bool  _TraceOn = true;

        private Trace()	{}

        public static bool TraceOn {
            get { return _TraceOn; }
            set { _TraceOn = value; }
        }
    }
}

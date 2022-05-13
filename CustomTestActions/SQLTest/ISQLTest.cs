using System;
using System.Xml;

namespace XInstall.CustomTestActions {
    public interface ISqlAction {
        void Execute();
    }

    public interface ISqlInfo {
        XmlNodeList SqlParams
        {
            get;
            }

            XmlNode SqlExpectedResult
            {
                get;
                }
            }
}

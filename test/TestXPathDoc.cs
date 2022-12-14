using System;
using System.IO;
using System.Xml;
using System.Xml.XPath;

namespace TestXPathDoc {
    class TestIt {
        public static void Main() {
            XPathDocument XPathDoc = new XPathDocument();
            XmlTextReader Xtr      = new XmlTextReader( @"..\conf\Config.xml");
            XPathDoc.ReadXml( Xtr );

            XPathNavigator PathNavigator = XPathDoc.CreateNavigator();
            XPathNodeIterator NodeIter   = PathNavigator.Select( @"//setup/*" );
            WalkTree( NodeIter );
            // while( NodeIter.MoveNext() ) {
            //      Console.WriteLine( "Node Type: {0}, Node Name: {1}",
            //                NodeIter.Current.NodeType,
            //                NodeIter.Current.LocalName );
            // }
        }

        public static void WalkTree( XPathNodeIterator Nav ) {

            while ( Nav.MoveNext() ) {
                if ( Nav.Current.NodeType == XPathNodeType.Comment )
                    continue;
                if ( Nav.Current.NodeType == XPathNodeType.Element )
                    Console.WriteLine( "Node Name: {0}", Nav.Current.LocalName );
                if ( Nav.Current.NodeType == XPathNodeType.Text )
                    Console.WriteLine( "Node Value: {0}", Nav.Current.Value );
                if ( Nav.Current.HasAttributes ) {
                    XPathNavigator CurrentNode = Nav.Current.Clone();
                    if ( CurrentNode.MoveToFirstAttribute() ) {
                        do {
                            Console.WriteLine( "\t {0} -> {1}", CurrentNode.LocalName, CurrentNode.Value );
                        } while ( CurrentNode.MoveToNextAttribute() );
                        Console.WriteLine();
                    }
                }
                if ( Nav.Current.HasChildren ) {
                    XPathNodeIterator ChildNodes = Nav.Current.SelectChildren( XPathNodeType.Element );
                    WalkTree( ChildNodes );
                }
            }
        }
    }
}

using System.Collections.Generic;
using System.Xml;

namespace XeroScreencast.Helpers
{
    public static class StringExtensions
    {

        public static string ReadSingleNode(this string xml, string nodeXPath)
        {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(xml);

            XmlNode node = xmlDocument.SelectSingleNode(nodeXPath);
            return (node == null) ? string.Empty : node.InnerText;
        }

        public static IEnumerable<string> ReadNodes(this string xml, string nodeXPath)
        {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(xml);

            foreach (XmlNode node in xmlDocument.SelectNodes(nodeXPath))
            {
                yield return node.InnerText;
            }
        }

    }
}

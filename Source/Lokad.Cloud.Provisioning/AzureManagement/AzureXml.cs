using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace Lokad.Cloud.Provisioning.AzureManagement
{
    internal static class AzureXml
    {
        private static readonly XNamespace _AzureNS = @"http://schemas.microsoft.com/windowsazure";

        public static XElement Element(string name, params object[] content)
        {
            return new XElement(_AzureNS + name, content);
        }

        public static XElement AzureElement(this XContainer element, string elementName)
        {
            return element.Element(_AzureNS + elementName);
        }

        public static IEnumerable<XElement> AzureElements(this XContainer element, string parentElementName, string itemElementName)
        {
            return element.Element(_AzureNS + parentElementName).Elements(_AzureNS + itemElementName);
        }

        public static string AzureValue(this XContainer element, string elementName)
        {
            return element.Element(_AzureNS + elementName).Value;
        }

        public static string AzureEncodedValue(this XContainer element, string elementName)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(element.Element(_AzureNS + elementName).Value));
        }

        public static string AttributeValue(this XElement element, string attributeName)
        {
            return element.Attribute(attributeName).Value;
        }

        public static XElement Configuration(XDocument config)
        {
            return new XElement(_AzureNS + "Configuration", Convert.ToBase64String(Encoding.UTF8.GetBytes(config.ToString(SaveOptions.OmitDuplicateNamespaces))));
        }

        public static XDocument AzureConfiguration(this XContainer element)
        {
            // Even though the XML is declared as UTF-16 it is actually encoded in UTF-8
            return XDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(element.Element(_AzureNS + "Configuration").Value)));
        }
    }
}

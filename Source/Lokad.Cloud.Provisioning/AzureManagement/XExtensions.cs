using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace Lokad.Cloud.Provisioning.AzureManagement
{
    internal static class XExtensions
    {
        private static readonly XNamespace _AzureNS = @"http://schemas.microsoft.com/windowsazure";
        private static readonly XNamespace _ServiceConfigNS = @"http://schemas.microsoft.com/ServiceHosting/2008/10/ServiceConfiguration";

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

        public static string AzureBase64Value(this XContainer element, string elementName)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(element.Element(_AzureNS + elementName).Value));
        }

        public static XElement ServiceConfigElement(this XContainer element, string elementName)
        {
            return element.Element(_ServiceConfigNS + elementName);
        }

        public static IEnumerable<XElement> ServiceConfigElements(this XContainer element, string parentElementName, string itemElementName)
        {
            return element.Element(_ServiceConfigNS + parentElementName).Elements(_ServiceConfigNS + itemElementName);
        }

        public static string ServiceConfigValue(this XContainer element, string elementName)
        {
            return element.Element(_ServiceConfigNS + elementName).Value;
        }

        public static string AttributeValue(this XElement element, string attributeName)
        {
            return element.Attribute(attributeName).Value;
        }
    }
}

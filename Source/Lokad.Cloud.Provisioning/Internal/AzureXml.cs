#region Copyright (c) Lokad 2010-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace Lokad.Cloud.Provisioning.Internal
{
    /// <summary>
    /// <c>Xml</c> Traversing and factory helpers for the Windows Azure Management API schema.
    /// </summary>
    internal static class AzureXml
    {
        private static readonly XNamespace _azureNs = @"http://schemas.microsoft.com/windowsazure";

        public static XElement CreateElement(string name, params object[] content)
        {
            return new XElement(_azureNs + name, content);
        }

        public static XElement AzureElement(this XContainer container, string elementName)
        {
            var element = container.Element(_azureNs + elementName);
            if (element == null)
            {
                throw new ArgumentException(string.Format("Azure Management XML: element '{0}' has no child element '{1}'", container, elementName));
            }

            return element;
        }

        public static IEnumerable<XElement> AzureElements(this XContainer container, string parentElementName, string itemElementName)
        {
            var parentElement = container.Element(_azureNs + parentElementName);
            if (parentElement == null)
            {
                return new XElement[0];
            }

            return parentElement.Elements(_azureNs + itemElementName);
        }

        public static string AzureValue(this XContainer container, string elementName)
        {
            return AzureElement(container, elementName).Value;
        }

        public static string AzureEncodedValue(this XContainer container, string elementName)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(AzureElement(container, elementName).Value));
        }

        public static string AttributeValue(this XElement container, string attributeName)
        {
            var attribute = container.Attribute(attributeName);
            if (attribute == null)
            {
                throw new ArgumentException(string.Format("Azure Management XML: element '{0}' has no attribute '{1}'", container, attributeName));
            }

            return attribute.Value;
        }

        public static XElement CreateConfiguration(XDocument config)
        {
            return new XElement(_azureNs + "Configuration", Convert.ToBase64String(Encoding.UTF8.GetBytes(config.ToString(SaveOptions.OmitDuplicateNamespaces))));
        }

        public static XDocument AzureConfiguration(this XContainer container)
        {
            // Even though the XML is declared as UTF-16 it is actually encoded in UTF-8
            return XDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(AzureElement(container, "Configuration").Value)));
        }
    }
}

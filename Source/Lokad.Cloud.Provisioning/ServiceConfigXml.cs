#region Copyright (c) Lokad 2010-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Collections.Generic;
using System.Xml.Linq;

namespace Lokad.Cloud.Provisioning
{
    internal static class ServiceConfigXml
    {
        private static readonly XNamespace _ServiceConfigNS = @"http://schemas.microsoft.com/ServiceHosting/2008/10/ServiceConfiguration";

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
    }
}

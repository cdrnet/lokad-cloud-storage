#region Copyright (c) Lokad 2010-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Lokad.Cloud.Provisioning
{
    internal static class ServiceConfigXml
    {
        private static readonly XNamespace _serviceConfigNs = @"http://schemas.microsoft.com/ServiceHosting/2008/10/ServiceConfiguration";

        public static XElement ServiceConfigElement(this XContainer container, string elementName)
        {
            var element = container.Element(_serviceConfigNs + elementName);
            if (element == null)
            {
                throw new ArgumentException(string.Format("Azure Service Config XML: element '{0}' has no child element '{1}'", container, elementName));
            }

            return element;
        }

        public static IEnumerable<XElement> ServiceConfigElements(this XContainer container, string parentElementName, string itemElementName)
        {
            return ServiceConfigElement(container, parentElementName).Elements(_serviceConfigNs + itemElementName);
        }
    }
}

#region Copyright (c) Lokad 2010-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Lokad.Cloud.Provisioning.Internal
{
    /// <summary>
    /// <c>Xml</c> Traversing and factory helpers for the Windows Azure service configuration schema.
    /// </summary>
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
            var parentElement = container.Element(_serviceConfigNs + parentElementName);
            if (parentElement == null)
            {
                return new XElement[0];
            }

            return parentElement.Elements(_serviceConfigNs + itemElementName);
        }
    }
}

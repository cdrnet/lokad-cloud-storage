#region Copyright (c) Lokad 2010-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Lokad.Cloud.Provisioning.Info
{
    [DataContract(Namespace = "http://schemas.lokad.com/lokad-cloud/provisioning/1.2"), Serializable]
    public class RoleInfo
    {
        [DataMember(IsRequired = true)]
        public string RoleName { get; set; }

        [DataMember(IsRequired = true)]
        public int ActualInstanceCount { get; set; }

        [DataMember(IsRequired = true)]
        public int ConfiguredInstanceCount { get; set; }

        [DataMember(IsRequired = true)]
        public Dictionary<string, string> Settings { get; set; }
    }
}

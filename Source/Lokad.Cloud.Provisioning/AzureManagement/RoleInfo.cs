using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Lokad.Cloud.Provisioning.AzureManagement
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

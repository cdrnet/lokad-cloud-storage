#region Copyright (c) Lokad 2010-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Runtime.Serialization;

namespace Lokad.Cloud.Provisioning.Info
{
    [DataContract(Namespace = "http://schemas.lokad.com/lokad-cloud/provisioning/1.2"), Serializable]
    public enum DeploymentStatus
    {
        [EnumMember]
        Running,

        [EnumMember]
        Suspended,

        [EnumMember]
        RunningTransitioning,

        [EnumMember]
        SuspendedTransitioning,

        [EnumMember]
        Starting,

        [EnumMember]
        Suspending,

        [EnumMember]
        Deploying,

        [EnumMember]
        Deleting,
    }
}
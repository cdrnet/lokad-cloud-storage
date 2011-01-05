#region Copyright (c) Lokad 2010, Microsoft
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
//
// Based on Microsoft Sample Code from http://code.msdn.microsoft.com/azurecmdlets
//---------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.  
//
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
// EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES 
// OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE. 
//---------------------------------------------------------------------------------
#endregion

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Lokad.Cloud.Management.Azure.Entities
{
	/// <summary>
	/// Role Instance
	/// </summary>
	[DataContract(Namespace = ApiConstants.XmlNamespace)]
	public class Role
	{
		[DataMember(Order = 1)]
		public string RoleName { get; set; }

		[DataMember(Order = 2)]
		public string OperatingSystemVersion { get; set; }

		public ExtensionDataObject ExtensionData { get; set; }
	}

	/// <summary>
	/// List of role instances
	/// </summary>
	[CollectionDataContract(Name = "RoleList", ItemName = "Role", Namespace = ApiConstants.XmlNamespace)]
	public class RoleList : List<Role>
	{
		public RoleList()
		{
		}

		public RoleList(IEnumerable<Role> roles)
			: base(roles)
		{
		}
	}
}
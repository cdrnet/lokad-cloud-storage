#region Copyright (c) Lokad 2009-2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Linq;

namespace Lokad.Cloud.Console.WebRole.Behavior
{
    public static class Users
    {
        public static bool IsAdministrator(string identifier)
        {
            return CloudConfiguration.Admins
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Contains(identifier);
        }
    }
}
#region (c)2009-2011 Lokad - New BSD license
// Company: http://www.lokad.com
// This code is released under the terms of the new BSD licence
#endregion

using System;

namespace Lokad.Cloud.Storage.Shared.Policies
{
    /// <summary> This delegate represents <em>catch</em> block.</summary>
    /// <param name="ex">Exception to handle.</param>
    /// <returns><em>true</em> if we can handle exception.</returns>
    public delegate bool ExceptionHandler(Exception ex);
}

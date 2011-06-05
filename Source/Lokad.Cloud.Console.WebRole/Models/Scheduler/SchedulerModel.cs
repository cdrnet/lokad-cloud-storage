#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Lokad.Cloud.Management;

namespace Lokad.Cloud.Console.WebRole.Models.Scheduler
{
    public class SchedulerModel
    {
        public CloudServiceSchedulingInfo[] Schedules { get; set; }
    }
}
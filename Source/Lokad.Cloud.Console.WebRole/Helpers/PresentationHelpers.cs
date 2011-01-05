#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using Lokad.Cloud.Management.Api10;

namespace Lokad.Cloud.Console.WebRole.Helpers
{
    public static class PresentationHelpers
    {
        public static string PrettyFormat(this TimeSpan timeSpan)
        {
            // TODO: Reuse Lokad.Shared FormatUtil, once it supports this scenario (implemented but currently internal)

            const int second = 1;
            const int minute = 60 * second;
            const int hour = 60 * minute;
            const int day = 24 * hour;
            const int month = 30*day;

            double delta = timeSpan.TotalSeconds;

            if (delta < 1) return timeSpan.Milliseconds + " ms";
            if (delta < 1 * minute) return timeSpan.Seconds == 1 ? "one second" : timeSpan.Seconds + " seconds";
            if (delta < 2 * minute) return "a minute";
            if (delta < 50 * minute) return timeSpan.Minutes + " minutes";
            if (delta < 70 * minute) return "an hour";
            if (delta < 2 * hour) return (int)timeSpan.TotalMinutes + " minutes";
            if (delta < 24 * hour) return timeSpan.Hours + " hours";
            if (delta < 48 * hour) return (int)timeSpan.TotalHours + " hours";
            if (delta < 30 * day) return timeSpan.Days + " days";

            if (delta < 12 * month)
            {
                var months = (int)Math.Floor(timeSpan.Days / 30.0);
                return months <= 1 ? "one month" : months + " months";
            }

            var years = (int)Math.Floor(timeSpan.Days / 365.0);
            return years <= 1 ? "one year" : years + " years";
        }

        public static string PrettyFormatLease(CloudServiceSchedulingInfo info)
        {
            if (!info.LeasedSince.HasValue || !info.LeasedUntil.HasValue)
            {
                return "available";
            }

            var now = DateTimeOffset.UtcNow;

            if (info.LeasedUntil.Value < now)
            {
                return "expired";
            }

            if (!info.LeasedBy.HasValue || String.IsNullOrEmpty(info.LeasedBy.Value))
            {
                return String.Format(
                    "{0} ago, expires in {1}",
                    now.Subtract(info.LeasedSince.Value).PrettyFormat(),
                    info.LeasedUntil.Value.Subtract(now).PrettyFormat());
            }

            return String.Format(
                "by {0} {1} ago, expires in {2}",
                info.LeasedBy.Value,
                now.Subtract(info.LeasedSince.Value).PrettyFormat(),
                info.LeasedUntil.Value.Subtract(now).PrettyFormat());
        }
    }
}

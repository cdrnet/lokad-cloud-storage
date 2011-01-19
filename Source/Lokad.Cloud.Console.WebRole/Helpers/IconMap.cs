#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

namespace Lokad.Cloud.Console.WebRole.Helpers
{
    public static class IconMap
    {
        public const string Loading = "icon-loading";

        public const string GoodBad = "icon-GoodBad";
        public static string GoodBadOf(bool isGood)
        {
            return string.Concat(GoodBad, (isGood ? "-Good" : "-VeryBad"));
        }

        public const string LogLevels = "icon-LogLevels";
        public static string LogLevelsOf(LogLevel level)
        {
            return string.Concat(LogLevels, "-", level.ToString());
        }
        public static string LogLevelsOf(string level)
        {
            return string.Concat(LogLevels, "-", level);
        }

        public const string OkCancel = "icon-OkCancel";
        public static string OkCancelOf(bool isOk)
        {
            return string.Concat(OkCancel, (isOk ? "-Ok" : "-Cancel"));
        }

        public const string PlusMinus = "icon-PlusMinus";
        public static string PlusMinusOf(bool isOk)
        {
            return string.Concat(PlusMinus, (isOk ? "-Plus" : "-Minus"));
        }

        public const string StartStop = "icon-StartStop";
        public static string StartStopOf(StartStopState level)
        {
            return string.Concat(StartStop, "-", level.ToString());
        }
        public static string StartStopOf(bool isStart)
        {
            return StartStopOf(isStart ? StartStopState.Start : StartStopState.Stop);
        }
    }

    public enum StartStopState
    {
        Start,
        Stop,
        Pause,
        Standby
    }
}
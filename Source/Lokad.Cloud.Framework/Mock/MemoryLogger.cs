#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;

namespace Lokad.Cloud.Mock
{
    public class MemoryLogger : Storage.Shared.Logging.ILog
	{
        public bool IsEnabled(Storage.Shared.Logging.LogLevel level)
		{
			return false;
		}

        public void Log(Storage.Shared.Logging.LogLevel level, Exception ex, object message)
		{
			//do nothing
		}

        public void Log(Storage.Shared.Logging.LogLevel level, object message)
		{
			Log(level, null, message);
		}
	}
}

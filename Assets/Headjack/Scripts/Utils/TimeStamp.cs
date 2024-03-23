// Copyright (c) Headjack (part of Purple Pill VR B.V.), All rights reserved.

using System;

namespace Headjack.Utils
{
    /// <summary>
    /// Helper for Unix epoch timestamp
    /// </summary>
    public class TimeStamp
    {
        private static readonly DateTime epochStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static double GetMs()
        {
            return (DateTime.UtcNow - epochStart).TotalMilliseconds;
        }

        public static long GetMsLong()
        {
            return (DateTime.UtcNow.Ticks - epochStart.Ticks) / 10000;
        }
    }
}

using System;

namespace Utility
{
    public class SimpleTimeSource : ITimeSource
    {
        public DateTime UTCNow 
        {
            get
            {
                return DateTime.UtcNow;
            }
        }
    }
}
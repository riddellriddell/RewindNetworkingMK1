using System;

namespace Utility
{
    //Defines the api boundary for getting a time value
    public interface ITimeSource
    {
        DateTime UTCNow { get; }
    }
}
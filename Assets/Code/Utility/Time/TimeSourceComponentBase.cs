using System;
using UnityEngine;

namespace Utility
{
    public class TimeSourceComponentBase : MonoBehaviour, ITimeSource
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
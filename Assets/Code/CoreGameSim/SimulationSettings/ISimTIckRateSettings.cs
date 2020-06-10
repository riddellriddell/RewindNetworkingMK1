using FixedPointy;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ISimTickRateSettings
{
    int TicksPerSecond { get; }
    long SimTickLength { get; }
    Fix SecondsPerTick { get; }
}

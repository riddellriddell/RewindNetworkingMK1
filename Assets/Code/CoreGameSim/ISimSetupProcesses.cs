using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sim
{
    public interface ISimSetupProcesses<TFrameData, TSettingsData>
    {
        int Priority { get; }

        string ProcessName { get; }

        bool ApplySetupProcess(uint iTick, in TSettingsData sdaSettingsData, long lFirstPeerID, ref TFrameData fdaFrameData);
    }
}

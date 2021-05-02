using Sim;
using SimDataInterpolation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameStateView
{
    public interface IGameStateView
    {
        void SetupConstDataViewEntities(ConstData cdaConstData);
        void UpdateView(InterpolatedFrameDataGen ifdInterpolatedFrameData, SimProcessorSettings sdaSettingsData);
    }
}
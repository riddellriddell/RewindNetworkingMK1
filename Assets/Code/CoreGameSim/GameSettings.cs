using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FixedPointy;

[CreateAssetMenu(fileName = "SimGlobalSettings", menuName = "Simulation/Settings", order = 1)]
public class GameSettings : ScriptableObject
{
    //serialization format of variables 
    public FixValueUnityInterface TickDelta;

    public FixVec2ValueUnityInterface ChararcterSize;

    public FixValueUnityInterface MovementSpeed;

    public FixVec2ValueUnityInterface GameFieldSize;

    public FixValueUnityInterface TargetQueueSize;

    //the internal time step in milliseconds 
    public Fix m_fixTickDelta;

    public FixVec2 m_v2iCharacterSize;

    public Fix m_fixMoveSpeed;

    public FixVec2 m_v2iGameFieldExtents;

    //the target length of the queue in seconds 
    public Fix m_fixTargetQueueLength;


    public void Deserialize()
    {
        Debug.Log("Awake");

        //deserialize values into fixed point values 

        m_fixTickDelta = TickDelta.FixValue;

        m_v2iCharacterSize = ChararcterSize.FixValue;

        m_fixMoveSpeed = MovementSpeed.FixValue;

        m_v2iGameFieldExtents = GameFieldSize.FixValue;

        m_fixTargetQueueLength = TargetQueueSize.FixValue;
    }
}

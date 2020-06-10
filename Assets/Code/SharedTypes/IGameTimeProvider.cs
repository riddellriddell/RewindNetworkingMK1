using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SharedTypes
{
    //gets the current time of the simulation
    //if this is a multiplayer game it is the agrred upon time synchronied across peers
    //it it is a single player game then this value is just date time utc
    public interface ISimTimeProvider 
    {
        DateTime GetCurrentSimTime();
    }
}

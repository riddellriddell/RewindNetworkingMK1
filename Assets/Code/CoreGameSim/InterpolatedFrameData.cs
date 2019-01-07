using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sim
{
    //the class is a non determanistic representation of the game state given a target time and time offset for each player 
    public class InterpolatedFrameData  
    {
        public enum State : byte
        {
            Standing,
            Moving,
            FastAttack,
            SlowAttack,
            Blocking,
            Stunned,
            Dead
        }

        [FlagsAttribute]
        public enum Direction : byte
        {
            None = 0,
            Up = 1,
            Down = 2,
            Left = 4,
            Right = 8
        }

        public int PlayerCount
        {
            get
            {
                return m_iPlayerHealths.Count;
            }
        }


        //the game time interpolated to for each player
        public List<float> m_fInterpolationTime;

        //list of all the player healths
        public List<int> m_iPlayerHealths;

        //list of all the player positions 
        public List<Vector2> m_vecPosition;

        //list of all the player directions 
        public List<Direction> m_dirFaceDirection;

        //list of all the player states
        public List<State> m_staPlayerState;

        //list of all action cooldowns
        public List<float> m_fStateEventTime;

        //list of all the player scores 
        public List<int> m_iScore;

        public InterpolatedFrameData(int iPlayerNumber)
        {

            //initalise all list lengths 
            m_fInterpolationTime = new List<float>(iPlayerNumber);
            m_iPlayerHealths = new List<int>(iPlayerNumber);
            m_vecPosition = new List<Vector2>(iPlayerNumber);
            m_dirFaceDirection = new List<Direction>(iPlayerNumber);
            m_staPlayerState = new List<State>(iPlayerNumber);
            m_fStateEventTime = new List<float>(iPlayerNumber);
            m_iScore = new List<int>(iPlayerNumber);

        }
    }
}
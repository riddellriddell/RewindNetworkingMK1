using System;
using Codice.Client.Common.EventTracking;
using Unity.Collections;
using UnityEngine;

namespace Utility
{
    public class TimeSourceComponentBase : MonoBehaviour, ITimeSource
    {
        public enum StepOptions
        {
            MatchUTC,
            StepOnCommand,
            TimedStep,
            //RecordStep,
            //ReplayStep,
        }
        
        public StepOptions m_stoStepOptions = StepOptions.MatchUTC;
        public bool m_bStepCommand = false;
        public bool m_bPause = false;
        public float m_fStepsPerSecond = 20.0f;
        public float m_fTimeBetweenSteps = 1.0f;
        public bool m_bPauseAfterTime = false;
        public float m_fTimeToPauseAt = 0.0f;
        
        public string m_strStartTime;

        [ReadOnly, SerializeField]
        public float m_fRunTime = 0.0f;

        public bool m_bLog = false;
        
        private DateTime m_dtmTime;
        private DateTime m_dtmStartTime;
        private float m_fTimeSinceLastStep = 0.0f;
        private float m_fTotalStepedTime = 0.0f;
        private bool m_bInitialized  = false;
        public DateTime UTCNow
        {
            get
            {
                switch (m_stoStepOptions)
                {
                    case StepOptions.MatchUTC:
                        return DateTime.UtcNow;
                        break;
                    case StepOptions.StepOnCommand:
                        case StepOptions.TimedStep:

                            if (!m_bInitialized)
                            {
                                //if start time is empty
                                if (string.IsNullOrWhiteSpace(m_strStartTime))
                                {
                                    m_dtmTime = DateTime.UtcNow;
                                    m_dtmStartTime = m_dtmTime;
                                }
                                else
                                {
                                    m_dtmTime = GetTimeFromStartValues();
                                    m_dtmStartTime = m_dtmTime;
                                }

                                m_bInitialized = true;
                            }
                            
                            return m_dtmTime;

                            break;
                }
                
                return DateTime.UtcNow;
                
            }
            
        }

        private void Start()
        {
            if (!m_bInitialized)
            {
                //if start time is empty
                if (string.IsNullOrWhiteSpace(m_strStartTime))
                {
                    m_dtmTime = DateTime.UtcNow;
                    m_dtmStartTime = m_dtmTime;
                }
                else
                {
                    m_dtmTime = GetTimeFromStartValues();
                    m_dtmStartTime = m_dtmTime;
                }

                m_bInitialized = true;
            }
        }

        private void Update()
        {
            switch (m_stoStepOptions)
            {
                case StepOptions.StepOnCommand:
                    if (m_bStepCommand)
                    {
                        m_bStepCommand = false;
                        if (m_fStepsPerSecond > 0)
                        {
                            m_dtmTime += TimeSpan.FromSeconds(1.0f / m_fStepsPerSecond);
                        }
                    }
                    break;
                case StepOptions.TimedStep:
                    
                    m_fTimeSinceLastStep += Time.deltaTime;

                    if (m_fTimeSinceLastStep >= m_fTimeBetweenSteps)
                    {
                        m_fTimeSinceLastStep -= m_fTimeBetweenSteps;

                        if (m_fTotalStepedTime >= m_fTimeToPauseAt && m_bPauseAfterTime)
                        {
                            m_bPause = true;
                        }
                        
                        if (m_fStepsPerSecond > 0 && !m_bPause)
                        {
                            float fStepTimeDelta = 1.0f / m_fStepsPerSecond;
                            m_fTotalStepedTime += fStepTimeDelta;
                            m_dtmTime += TimeSpan.FromSeconds(fStepTimeDelta);
                        }
                    }

                    break;
            }
            
            //update the run time
            m_fRunTime = m_fTotalStepedTime;
        }

        private DateTime GetTimeFromStartValues()
        {
            DateTime dtmStartTime = DateTime.Parse(m_strStartTime, null, System.Globalization.DateTimeStyles.AdjustToUniversal).ToUniversalTime();
            
            if (m_bLog)
            {
                Debug.Log($"Time Source Start Time {dtmStartTime} in time zone {dtmStartTime.Kind} from time string: {m_strStartTime}"); 
            }
            
            return dtmStartTime;
        }
    }
}
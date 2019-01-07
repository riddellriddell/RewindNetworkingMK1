
using System.Collections;
using System.Collections.Generic;

namespace Sim
{
	public class InterpolatedFrameDataGen
	{
		public List<System.Int32> m_sPlayerHealths; 
				
																																									
		public List<System.Single> m_sPlayerHealthsErrorOffset; 

		public List<System.Int32> m_sPlayerHealthsErrorAdjusted;

		public List<UnityEngine.Vector2> m_v2iPosition; 
				
																																									
		public List<UnityEngine.Vector2> m_v2iPositionErrorOffset; 

		public List<UnityEngine.Vector2> m_v2iPositionErrorAdjusted;

		public List<System.Byte> m_bFaceDirection; 
				
		public List<System.Byte> m_bPlayerState; 
				
		public List<System.Single> m_sStateEventTick; 
				
		public List<System.Int32> m_bScore; 
				

		public int PlayerCount
        {
            get
            {
                return m_sPlayerHealths.Count;
            }
        }

		public InterpolatedFrameDataGen(int iPlayerCount)
		{
			
			m_sPlayerHealths = new List<System.Int32>();  
			m_sPlayerHealthsErrorOffset = new List<System.Single>(); 

			m_sPlayerHealthsErrorAdjusted = new List<System.Int32>(); 

			for(int i = 0 ; i < iPlayerCount; i++)
			{
				m_sPlayerHealths.Add(default(System.Int32));
				m_sPlayerHealthsErrorOffset.Add(default(System.Single));

				m_sPlayerHealthsErrorAdjusted.Add(default(System.Int32));
			}
			
			m_v2iPosition = new List<UnityEngine.Vector2>();  
			m_v2iPositionErrorOffset = new List<UnityEngine.Vector2>(); 

			m_v2iPositionErrorAdjusted = new List<UnityEngine.Vector2>(); 

			for(int i = 0 ; i < iPlayerCount; i++)
			{
				m_v2iPosition.Add(default(UnityEngine.Vector2));
				m_v2iPositionErrorOffset.Add(default(UnityEngine.Vector2));

				m_v2iPositionErrorAdjusted.Add(default(UnityEngine.Vector2));
			}
			
			m_bFaceDirection = new List<System.Byte>();  
			for(int i = 0 ; i < iPlayerCount; i++)
			{
				m_bFaceDirection.Add(default(System.Byte));
			}
			
			m_bPlayerState = new List<System.Byte>();  
			for(int i = 0 ; i < iPlayerCount; i++)
			{
				m_bPlayerState.Add(default(System.Byte));
			}
			
			m_sStateEventTick = new List<System.Single>();  
			for(int i = 0 ; i < iPlayerCount; i++)
			{
				m_sStateEventTick.Add(default(System.Single));
			}
			
			m_bScore = new List<System.Int32>();  
			for(int i = 0 ; i < iPlayerCount; i++)
			{
				m_bScore.Add(default(System.Int32));
			}
		}	
	}
}
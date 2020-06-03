
using System.Collections;
using System.Collections.Generic;

namespace Sim
{
	//GENERATED CLASS DO NOT EDIT!!!!!

	public class InterpolatedFrameDataGen
	{
		public System.Single[] m_lPeersAssignedToSlot;
		public System.Int32[] m_bInput;
		public System.Byte[] m_bShipHealth;
																																									
		public System.Single[] m_bShipHealthErrorOffset; 

		public System.Byte[] m_bShipHealthErrorAdjusted;

		public System.Single[] m_fixShipHealDelayTimeOut;
																																									
		public System.Single[] m_fixShipHealDelayTimeOutErrorOffset; 

		public System.Single[] m_fixShipHealDelayTimeOutErrorAdjusted;

		public System.Byte[] m_bShipLastDamagedBy;
		public System.Single[] m_fixShipPosX;
																																									
		public System.Single[] m_fixShipPosXErrorOffset; 

		public System.Single[] m_fixShipPosXErrorAdjusted;

		public System.Single[] m_fixShipPosY;
																																									
		public System.Single[] m_fixShipPosYErrorOffset; 

		public System.Single[] m_fixShipPosYErrorAdjusted;

		public System.Single[] m_fixShipVelocityX;
																																									
		public System.Single[] m_fixShipVelocityXErrorOffset; 

		public System.Single[] m_fixShipVelocityXErrorAdjusted;

		public System.Single[] m_fixShipVelocityY;
																																									
		public System.Single[] m_fixShipVelocityYErrorOffset; 

		public System.Single[] m_fixShipVelocityYErrorAdjusted;

		public System.Single[] m_fixShipBaseAngle;
																																									
		public System.Single[] m_fixShipBaseAngleErrorOffset; 

		public System.Single[] m_fixShipBaseAngleErrorAdjusted;


		public void MatchFormat(FrameData fdaFrameData)
		{
			
			if(m_lPeersAssignedToSlot == null || m_lPeersAssignedToSlot.Length != fdaFrameData.m_lPeersAssignedToSlot.Length)
			{
				m_lPeersAssignedToSlot = new System.Single[fdaFrameData.m_lPeersAssignedToSlot.Length] ;

			}

			
			if(m_bInput == null || m_bInput.Length != fdaFrameData.m_bInput.Length)
			{
				m_bInput = new System.Int32[fdaFrameData.m_bInput.Length] ;

			}

			
			if(m_bShipHealth == null || m_bShipHealth.Length != fdaFrameData.m_bShipHealth.Length)
			{
				m_bShipHealth = new System.Byte[fdaFrameData.m_bShipHealth.Length] ;

																																									
				m_bShipHealthErrorOffset = new System.Single[fdaFrameData.m_bShipHealth.Length] ;

				m_bShipHealthErrorAdjusted = new System.Byte[fdaFrameData.m_bShipHealth.Length] ;
			
			}

			
			if(m_fixShipHealDelayTimeOut == null || m_fixShipHealDelayTimeOut.Length != fdaFrameData.m_fixShipHealDelayTimeOut.Length)
			{
				m_fixShipHealDelayTimeOut = new System.Single[fdaFrameData.m_fixShipHealDelayTimeOut.Length] ;

																																									
				m_fixShipHealDelayTimeOutErrorOffset = new System.Single[fdaFrameData.m_fixShipHealDelayTimeOut.Length] ;

				m_fixShipHealDelayTimeOutErrorAdjusted = new System.Single[fdaFrameData.m_fixShipHealDelayTimeOut.Length] ;
			
			}

			
			if(m_bShipLastDamagedBy == null || m_bShipLastDamagedBy.Length != fdaFrameData.m_bShipLastDamagedBy.Length)
			{
				m_bShipLastDamagedBy = new System.Byte[fdaFrameData.m_bShipLastDamagedBy.Length] ;

			}

			
			if(m_fixShipPosX == null || m_fixShipPosX.Length != fdaFrameData.m_fixShipPosX.Length)
			{
				m_fixShipPosX = new System.Single[fdaFrameData.m_fixShipPosX.Length] ;

																																									
				m_fixShipPosXErrorOffset = new System.Single[fdaFrameData.m_fixShipPosX.Length] ;

				m_fixShipPosXErrorAdjusted = new System.Single[fdaFrameData.m_fixShipPosX.Length] ;
			
			}

			
			if(m_fixShipPosY == null || m_fixShipPosY.Length != fdaFrameData.m_fixShipPosY.Length)
			{
				m_fixShipPosY = new System.Single[fdaFrameData.m_fixShipPosY.Length] ;

																																									
				m_fixShipPosYErrorOffset = new System.Single[fdaFrameData.m_fixShipPosY.Length] ;

				m_fixShipPosYErrorAdjusted = new System.Single[fdaFrameData.m_fixShipPosY.Length] ;
			
			}

			
			if(m_fixShipVelocityX == null || m_fixShipVelocityX.Length != fdaFrameData.m_fixShipVelocityX.Length)
			{
				m_fixShipVelocityX = new System.Single[fdaFrameData.m_fixShipVelocityX.Length] ;

																																									
				m_fixShipVelocityXErrorOffset = new System.Single[fdaFrameData.m_fixShipVelocityX.Length] ;

				m_fixShipVelocityXErrorAdjusted = new System.Single[fdaFrameData.m_fixShipVelocityX.Length] ;
			
			}

			
			if(m_fixShipVelocityY == null || m_fixShipVelocityY.Length != fdaFrameData.m_fixShipVelocityY.Length)
			{
				m_fixShipVelocityY = new System.Single[fdaFrameData.m_fixShipVelocityY.Length] ;

																																									
				m_fixShipVelocityYErrorOffset = new System.Single[fdaFrameData.m_fixShipVelocityY.Length] ;

				m_fixShipVelocityYErrorAdjusted = new System.Single[fdaFrameData.m_fixShipVelocityY.Length] ;
			
			}

			
			if(m_fixShipBaseAngle == null || m_fixShipBaseAngle.Length != fdaFrameData.m_fixShipBaseAngle.Length)
			{
				m_fixShipBaseAngle = new System.Single[fdaFrameData.m_fixShipBaseAngle.Length] ;

																																									
				m_fixShipBaseAngleErrorOffset = new System.Single[fdaFrameData.m_fixShipBaseAngle.Length] ;

				m_fixShipBaseAngleErrorAdjusted = new System.Single[fdaFrameData.m_fixShipBaseAngle.Length] ;
			
			}

		}
	}
}
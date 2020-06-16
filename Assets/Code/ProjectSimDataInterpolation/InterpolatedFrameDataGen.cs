
using System.Collections;
using System.Collections.Generic;
using Sim;

namespace SimDataInterpolation
{
	//GENERATED CLASS DO NOT EDIT!!!!!

	public class InterpolatedFrameDataGen : IInterpolatedFrameData<FrameData>
	{
		public System.Int64[] m_lPeersAssignedToSlot;
		public System.Int32[] m_bInput;
		public System.Byte[] m_fixShipHealth;
																																									
		public System.Single[] m_fixShipHealthErrorOffset; 

		public System.Byte[] m_fixShipHealthErrorAdjusted;

		public System.Single[] m_fixShipHealDelayTimeOut;
																																									
		public System.Single[] m_fixShipHealDelayTimeOutErrorOffset; 

		public System.Single[] m_fixShipHealDelayTimeOutErrorAdjusted;

		public System.Byte[] m_bShipLastDamagedBy;
		public System.Single[] m_fixTimeUntilRespawn;
																																									
		public System.Single[] m_fixTimeUntilRespawnErrorOffset; 

		public System.Single[] m_fixTimeUntilRespawnErrorAdjusted;

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

		public System.Byte[] m_bLazerFireIndex;
		public System.Single[] m_fixTimeUntilNextFire;
																																									
		public System.Single[] m_fixTimeUntilNextFireErrorOffset; 

		public System.Single[] m_fixTimeUntilNextFireErrorAdjusted;

		public System.Byte[] m_bLazerOwner;
		public System.Single[] m_fixLazerLifeRemaining;
																																									
		public System.Single[] m_fixLazerLifeRemainingErrorOffset; 

		public System.Single[] m_fixLazerLifeRemainingErrorAdjusted;

		public System.Single[] m_fixLazerPositionX;
																																									
		public System.Single[] m_fixLazerPositionXErrorOffset; 

		public System.Single[] m_fixLazerPositionXErrorAdjusted;

		public System.Single[] m_fixLazerPositionY;
																																									
		public System.Single[] m_fixLazerPositionYErrorOffset; 

		public System.Single[] m_fixLazerPositionYErrorAdjusted;

		public System.Single[] m_fixLazerVelocityX;
																																									
		public System.Single[] m_fixLazerVelocityXErrorOffset; 

		public System.Single[] m_fixLazerVelocityXErrorAdjusted;

		public System.Single[] m_fixLazerVelocityY;
																																									
		public System.Single[] m_fixLazerVelocityYErrorOffset; 

		public System.Single[] m_fixLazerVelocityYErrorAdjusted;


		public void MatchFormat(FrameData fdaFrameData)
		{
			
			if(m_lPeersAssignedToSlot == null || m_lPeersAssignedToSlot.Length != fdaFrameData.m_lPeersAssignedToSlot.Length)
			{
				m_lPeersAssignedToSlot = new System.Int64[fdaFrameData.m_lPeersAssignedToSlot.Length] ;

			}

			
			if(m_bInput == null || m_bInput.Length != fdaFrameData.m_bInput.Length)
			{
				m_bInput = new System.Int32[fdaFrameData.m_bInput.Length] ;

			}

			
			if(m_fixShipHealth == null || m_fixShipHealth.Length != fdaFrameData.m_fixShipHealth.Length)
			{
				m_fixShipHealth = new System.Byte[fdaFrameData.m_fixShipHealth.Length] ;

																																									
				m_fixShipHealthErrorOffset = new System.Single[fdaFrameData.m_fixShipHealth.Length] ;

				m_fixShipHealthErrorAdjusted = new System.Byte[fdaFrameData.m_fixShipHealth.Length] ;
			
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

			
			if(m_fixTimeUntilRespawn == null || m_fixTimeUntilRespawn.Length != fdaFrameData.m_fixTimeUntilRespawn.Length)
			{
				m_fixTimeUntilRespawn = new System.Single[fdaFrameData.m_fixTimeUntilRespawn.Length] ;

																																									
				m_fixTimeUntilRespawnErrorOffset = new System.Single[fdaFrameData.m_fixTimeUntilRespawn.Length] ;

				m_fixTimeUntilRespawnErrorAdjusted = new System.Single[fdaFrameData.m_fixTimeUntilRespawn.Length] ;
			
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

			
			if(m_bLazerFireIndex == null || m_bLazerFireIndex.Length != fdaFrameData.m_bLazerFireIndex.Length)
			{
				m_bLazerFireIndex = new System.Byte[fdaFrameData.m_bLazerFireIndex.Length] ;

			}

			
			if(m_fixTimeUntilNextFire == null || m_fixTimeUntilNextFire.Length != fdaFrameData.m_fixTimeUntilNextFire.Length)
			{
				m_fixTimeUntilNextFire = new System.Single[fdaFrameData.m_fixTimeUntilNextFire.Length] ;

																																									
				m_fixTimeUntilNextFireErrorOffset = new System.Single[fdaFrameData.m_fixTimeUntilNextFire.Length] ;

				m_fixTimeUntilNextFireErrorAdjusted = new System.Single[fdaFrameData.m_fixTimeUntilNextFire.Length] ;
			
			}

			
			if(m_bLazerOwner == null || m_bLazerOwner.Length != fdaFrameData.m_bLazerOwner.Length)
			{
				m_bLazerOwner = new System.Byte[fdaFrameData.m_bLazerOwner.Length] ;

			}

			
			if(m_fixLazerLifeRemaining == null || m_fixLazerLifeRemaining.Length != fdaFrameData.m_fixLazerLifeRemaining.Length)
			{
				m_fixLazerLifeRemaining = new System.Single[fdaFrameData.m_fixLazerLifeRemaining.Length] ;

																																									
				m_fixLazerLifeRemainingErrorOffset = new System.Single[fdaFrameData.m_fixLazerLifeRemaining.Length] ;

				m_fixLazerLifeRemainingErrorAdjusted = new System.Single[fdaFrameData.m_fixLazerLifeRemaining.Length] ;
			
			}

			
			if(m_fixLazerPositionX == null || m_fixLazerPositionX.Length != fdaFrameData.m_fixLazerPositionX.Length)
			{
				m_fixLazerPositionX = new System.Single[fdaFrameData.m_fixLazerPositionX.Length] ;

																																									
				m_fixLazerPositionXErrorOffset = new System.Single[fdaFrameData.m_fixLazerPositionX.Length] ;

				m_fixLazerPositionXErrorAdjusted = new System.Single[fdaFrameData.m_fixLazerPositionX.Length] ;
			
			}

			
			if(m_fixLazerPositionY == null || m_fixLazerPositionY.Length != fdaFrameData.m_fixLazerPositionY.Length)
			{
				m_fixLazerPositionY = new System.Single[fdaFrameData.m_fixLazerPositionY.Length] ;

																																									
				m_fixLazerPositionYErrorOffset = new System.Single[fdaFrameData.m_fixLazerPositionY.Length] ;

				m_fixLazerPositionYErrorAdjusted = new System.Single[fdaFrameData.m_fixLazerPositionY.Length] ;
			
			}

			
			if(m_fixLazerVelocityX == null || m_fixLazerVelocityX.Length != fdaFrameData.m_fixLazerVelocityX.Length)
			{
				m_fixLazerVelocityX = new System.Single[fdaFrameData.m_fixLazerVelocityX.Length] ;

																																									
				m_fixLazerVelocityXErrorOffset = new System.Single[fdaFrameData.m_fixLazerVelocityX.Length] ;

				m_fixLazerVelocityXErrorAdjusted = new System.Single[fdaFrameData.m_fixLazerVelocityX.Length] ;
			
			}

			
			if(m_fixLazerVelocityY == null || m_fixLazerVelocityY.Length != fdaFrameData.m_fixLazerVelocityY.Length)
			{
				m_fixLazerVelocityY = new System.Single[fdaFrameData.m_fixLazerVelocityY.Length] ;

																																									
				m_fixLazerVelocityYErrorOffset = new System.Single[fdaFrameData.m_fixLazerVelocityY.Length] ;

				m_fixLazerVelocityYErrorAdjusted = new System.Single[fdaFrameData.m_fixLazerVelocityY.Length] ;
			
			}

		}
	}
}
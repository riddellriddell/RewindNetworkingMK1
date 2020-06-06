using FixedPointy;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using Utility;

namespace Sim
{
    public class FrameData : IFrameData, IShipPositions, IPeerSlotAssignmentFrameData, IPeerInputFrameData, IShipRespawnFrameData, IShipHealthframeData
    {
        #region PeerAssignment

        public int MaxPlayerCount
        {
            get
            {
                return m_lPeersAssignedToSlot.Length;
            }
        }
               
        [FrameDataInterpilationTypeAttribute(typeof(float), FrameDataInterpilationTypeAttribute.InterpolationType.None)]
        public long[] m_lPeersAssignedToSlot;

        #region IPeerSlotAssignmentFrameData
        public long[] PeerSlotAssignment { get => m_lPeersAssignedToSlot; set => m_lPeersAssignedToSlot = value; }
        #endregion
        #endregion

        #region Input
        [FrameDataInterpilationTypeAttribute(typeof(int), FrameDataInterpilationTypeAttribute.InterpolationType.None)]
        public byte[] m_bInput;

        #region IPeerInputFrameData
        public byte[] PeerInput { get => m_bInput; set => m_bInput = value; }
        #endregion
        #endregion

        #region Health

        //list of all the ship Healths 
        [FrameDataInterpilationTypeAttribute(typeof(byte))]
        public Fix[] m_fixShipHealth;

        [FrameDataInterpilationTypeAttribute(typeof(float))]
        public Fix[] m_fixShipHealDelayTimeOut;

        [FrameDataInterpilationTypeAttribute(typeof(byte), FrameDataInterpilationTypeAttribute.InterpolationType.None)]
        public byte[] m_bShipLastDamagedBy;

        #region IShipHealthframeData
        public Fix[] ShipHealth { get => m_fixShipHealth; set => m_fixShipHealth = value; }
        public Fix[] ShipHealDelayTimeOut { get => m_fixShipHealDelayTimeOut; set => m_fixShipHealDelayTimeOut = value; }
        public byte[] ShipLastDamagedBy { get => m_bShipLastDamagedBy; set => m_bShipLastDamagedBy = value; }

        #endregion

        #region ShipSpawn
        [FrameDataInterpilationTypeAttribute(typeof(float))]
        public Fix[] m_fixTimeUntilRespawn;
        public Fix[] TimeUntilRespawn { get => m_fixTimeUntilRespawn; set => m_fixTimeUntilRespawn = value; }
        #endregion

        #endregion

        #region ShipMovement 
        //list of all the player positions 
        [FrameDataInterpilationTypeAttribute(typeof(float))]
        public Fix[] m_fixShipPosX;

        [FrameDataInterpilationTypeAttribute(typeof(float))]
        public Fix[] m_fixShipPosY;

        [FrameDataInterpilationTypeAttribute(typeof(float))]
        public Fix[] m_fixShipVelocityX;

        [FrameDataInterpilationTypeAttribute(typeof(float))]
        public Fix[] m_fixShipVelocityY;

        [FrameDataInterpilationTypeAttribute(typeof(float))]
        public Fix[] m_fixShipBaseAngle;

        #region IShipPositions

        public Fix[] ShipPositionX { get => m_fixShipPosX; set => m_fixShipPosX = value; }
        public Fix[] ShipPositionY { get => m_fixShipPosY; set => m_fixShipPosY = value; }
        public Fix[] ShipVelocityX { get => m_fixShipVelocityX; set => m_fixShipVelocityX = value; }
        public Fix[] ShipVelocityY { get => m_fixShipVelocityY; set => m_fixShipVelocityY = value; }
        public Fix[] ShipBaseAngle { get => m_fixShipBaseAngle; set => m_fixShipBaseAngle = value; }




        #endregion

        #endregion



        public FrameData()
        {

        }

        public bool ResetToState(IFrameData fdaFrameDataToResetTo)
        {
            FrameData fdaTargetData = fdaFrameDataToResetTo as FrameData;

            #region IPeerSlotAssignmentFrameData

            if (m_lPeersAssignedToSlot == null || m_lPeersAssignedToSlot.Length != fdaTargetData.m_lPeersAssignedToSlot.Length)
            {
                m_lPeersAssignedToSlot = fdaTargetData.m_lPeersAssignedToSlot.Clone() as long[];
            }
            else
            {
                Array.Copy(fdaTargetData.m_lPeersAssignedToSlot, m_lPeersAssignedToSlot, fdaTargetData.m_lPeersAssignedToSlot.Length);
            }

            #endregion

            #region IPeerInputFrameData
            if (m_bInput == null || m_bInput.Length != fdaTargetData.m_lPeersAssignedToSlot.Length)
            {
                m_bInput = fdaTargetData.m_bInput.Clone() as byte[];
            }
            else
            {
                Array.Copy(fdaTargetData.m_bInput, m_bInput, fdaTargetData.m_lPeersAssignedToSlot.Length);
            }
            #endregion

            #region IShipRespawnFrameData
            if (m_fixTimeUntilRespawn == null || m_fixTimeUntilRespawn.Length != fdaTargetData.m_fixTimeUntilRespawn.Length)
            {
                m_fixTimeUntilRespawn = fdaTargetData.m_fixTimeUntilRespawn.Clone() as Fix[];
            }
            else
            {
                Array.Copy(fdaTargetData.m_fixTimeUntilRespawn, m_fixTimeUntilRespawn, fdaTargetData.m_fixTimeUntilRespawn.Length);
            }
            #endregion

            #region IShipHealthframeData

            if (m_fixShipHealth == null || m_fixShipHealth.Length != fdaTargetData.m_fixShipHealth.Length)
            {
                m_fixShipHealth = fdaTargetData.m_fixShipHealth.Clone() as Fix[];
                m_fixShipHealDelayTimeOut = fdaTargetData.m_fixShipHealDelayTimeOut.Clone() as Fix[];
                m_bShipLastDamagedBy = fdaTargetData.m_bShipLastDamagedBy.Clone() as byte[];
            }
            else
            {
                Array.Copy(fdaTargetData.m_fixShipHealth, m_fixShipHealth, fdaTargetData.m_fixShipHealth.Length);
                Array.Copy(fdaTargetData.m_fixShipHealDelayTimeOut, m_fixShipHealDelayTimeOut, fdaTargetData.m_fixShipHealDelayTimeOut.Length);
                Array.Copy(fdaTargetData.m_bShipLastDamagedBy, m_bShipLastDamagedBy, fdaTargetData.m_bShipLastDamagedBy.Length);
            }

            #endregion

            #region IShipPositions 

            if (m_fixShipPosX == null || m_fixShipPosX.Length != fdaTargetData.m_fixShipPosX.Length)
            {
                m_fixShipPosX = fdaTargetData.m_fixShipPosX.Clone() as Fix[];
                m_fixShipPosY = fdaTargetData.m_fixShipPosY.Clone() as Fix[];
                m_fixShipVelocityX = fdaTargetData.m_fixShipVelocityX.Clone() as Fix[];
                m_fixShipVelocityY = fdaTargetData.m_fixShipVelocityY.Clone() as Fix[];
                m_fixShipBaseAngle = fdaTargetData.m_fixShipBaseAngle.Clone() as Fix[];
            }
            else
            {
                Array.Copy(fdaTargetData.m_fixShipPosX, m_fixShipPosX, fdaTargetData.m_fixShipPosX.Length);
                Array.Copy(fdaTargetData.m_fixShipPosY, m_fixShipPosY, fdaTargetData.m_fixShipPosY.Length);
                Array.Copy(fdaTargetData.m_fixShipVelocityX, m_fixShipVelocityX, fdaTargetData.m_fixShipVelocityX.Length);
                Array.Copy(fdaTargetData.m_fixShipVelocityY, m_fixShipVelocityY, fdaTargetData.m_fixShipVelocityY.Length);
                Array.Copy(fdaTargetData.m_fixShipBaseAngle, m_fixShipBaseAngle, fdaTargetData.m_fixShipBaseAngle.Length);
            }

            #endregion


            return true;
        }

        public bool Encode(WriteByteStream wbsByteStream)
        {
            bool bWasASuccess = true;

            #region IPeerSlotAssignmentFrameData

            bWasASuccess &= ByteStream.Serialize(wbsByteStream, ref m_lPeersAssignedToSlot);

            #endregion

            #region IPeerInputFrameData

            bWasASuccess &= ByteStream.Serialize(wbsByteStream, ref m_bInput, m_lPeersAssignedToSlot.Length);

            #endregion

            #region IShipRespawnFrameData
            bWasASuccess &= FixSerialization.Serialize(wbsByteStream, ref m_fixTimeUntilRespawn, m_lPeersAssignedToSlot.Length);
            #endregion

            #region IShipHealthframeData

            //list of all the ship Healths 
            bWasASuccess &= FixSerialization.Serialize(wbsByteStream, ref m_fixShipHealth, m_lPeersAssignedToSlot.Length);

            bWasASuccess &= FixSerialization.Serialize(wbsByteStream, ref m_fixShipHealDelayTimeOut, m_lPeersAssignedToSlot.Length);

            bWasASuccess &= ByteStream.Serialize(wbsByteStream, ref m_bShipLastDamagedBy, m_lPeersAssignedToSlot.Length);

            #endregion

            #region IShipPositions 

            bWasASuccess &= FixSerialization.Serialize(wbsByteStream, ref m_fixShipPosX, m_lPeersAssignedToSlot.Length);

            bWasASuccess &= FixSerialization.Serialize(wbsByteStream, ref m_fixShipPosY, m_lPeersAssignedToSlot.Length);

            bWasASuccess &= FixSerialization.Serialize(wbsByteStream, ref m_fixShipVelocityX, m_lPeersAssignedToSlot.Length);

            bWasASuccess &= FixSerialization.Serialize(wbsByteStream, ref m_fixShipVelocityY, m_lPeersAssignedToSlot.Length);

            bWasASuccess &= FixSerialization.Serialize(wbsByteStream, ref m_fixShipBaseAngle, m_lPeersAssignedToSlot.Length);

            #endregion

            return bWasASuccess;
        }

        public bool Decode(ReadByteStream rbsReadByteStream)
        {

            bool bWasASuccess = true;

            #region IPeerSlotAssignmentFrameData

            bWasASuccess &= ByteStream.Serialize(rbsReadByteStream, ref m_lPeersAssignedToSlot);

            #endregion

            #region IPeerInputFrameData

            bWasASuccess &= ByteStream.Serialize(rbsReadByteStream, ref m_bInput, m_lPeersAssignedToSlot.Length);

            #endregion

            #region IShipRespawnFrameData
            bWasASuccess &= FixSerialization.Serialize(rbsReadByteStream, ref m_fixTimeUntilRespawn, m_lPeersAssignedToSlot.Length);
            #endregion


            #region IShipHealthframeData

            //list of all the ship Healths 
            bWasASuccess &= FixSerialization.Serialize(rbsReadByteStream, ref m_fixShipHealth, m_lPeersAssignedToSlot.Length);

            bWasASuccess &= FixSerialization.Serialize(rbsReadByteStream, ref m_fixShipHealDelayTimeOut, m_lPeersAssignedToSlot.Length);

            bWasASuccess &= ByteStream.Serialize(rbsReadByteStream, ref m_bShipLastDamagedBy, m_lPeersAssignedToSlot.Length);

            #endregion

            #region IShipPositions 

            bWasASuccess &= FixSerialization.Serialize(rbsReadByteStream, ref m_fixShipPosX, m_lPeersAssignedToSlot.Length);

            bWasASuccess &= FixSerialization.Serialize(rbsReadByteStream, ref m_fixShipPosY, m_lPeersAssignedToSlot.Length);

            bWasASuccess &= FixSerialization.Serialize(rbsReadByteStream, ref m_fixShipVelocityX, m_lPeersAssignedToSlot.Length);

            bWasASuccess &= FixSerialization.Serialize(rbsReadByteStream, ref m_fixShipVelocityY, m_lPeersAssignedToSlot.Length);

            bWasASuccess &= FixSerialization.Serialize(rbsReadByteStream, ref m_fixShipBaseAngle, m_lPeersAssignedToSlot.Length);

            #endregion

            return bWasASuccess;
        }

        public int GetSize()
        {
            int iSize = 0;

            #region IPeerSlotAssignmentFrameData

            iSize += ByteStream.DataSize( m_lPeersAssignedToSlot);

            #endregion

            #region IPeerInputFrameData

            iSize += ByteStream.DataSize(m_bInput, m_lPeersAssignedToSlot.Length);

            #endregion

            #region IShipRespawnFrameData
            iSize += FixSerialization.DataSize(m_fixTimeUntilRespawn, m_lPeersAssignedToSlot.Length);
            #endregion


            #region IShipHealthframeData

            //list of all the ship Healths 
            iSize += FixSerialization.DataSize(m_fixShipHealth, m_lPeersAssignedToSlot.Length);

            iSize += FixSerialization.DataSize(m_fixShipHealDelayTimeOut, m_lPeersAssignedToSlot.Length);

            iSize += ByteStream.DataSize(m_bShipLastDamagedBy, m_lPeersAssignedToSlot.Length);

            #endregion

            #region IShipPositions 

            iSize += FixSerialization.DataSize(m_fixShipPosX, m_lPeersAssignedToSlot.Length);

            iSize += FixSerialization.DataSize(m_fixShipPosY, m_lPeersAssignedToSlot.Length);

            iSize += FixSerialization.DataSize(m_fixShipVelocityX, m_lPeersAssignedToSlot.Length);

            iSize += FixSerialization.DataSize(m_fixShipVelocityY, m_lPeersAssignedToSlot.Length);

            iSize += FixSerialization.DataSize(m_fixShipBaseAngle, m_lPeersAssignedToSlot.Length);

            #endregion

            return iSize;
        }
               
        //generate a hash of all the values 
        public void GetHash(byte[] bOutput)
        {
            WriteByteStream wbsWriteStream = new WriteByteStream(GetSize());

            Encode(wbsWriteStream);

            MD5 md5 = MD5.Create();

            byte[] bHash = md5.ComputeHash(wbsWriteStream.GetData());

            for (int i = 0; i < bOutput.Length; i++)
            {
                bOutput[i] = bHash[i % bHash.Length];
            }
        }

    }
}

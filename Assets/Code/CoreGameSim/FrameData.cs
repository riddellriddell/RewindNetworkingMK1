using FixedPointy;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using Utility;

namespace Sim
{
    public class FrameData : IFrameData
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

        #endregion

        #region Input
        [FrameDataInterpilationTypeAttribute(typeof(int), FrameDataInterpilationTypeAttribute.InterpolationType.None)]
        public byte[] m_bInput;

        #endregion

        #region Health

        //list of all the ship Healths 
        [FrameDataInterpilationTypeAttribute(typeof(byte))]
        public byte[] m_bShipHealth;

        [FrameDataInterpilationTypeAttribute(typeof(float))]
        public Fix[] m_fixShipHealDelayTimeOut;

        [FrameDataInterpilationTypeAttribute(typeof(byte), FrameDataInterpilationTypeAttribute.InterpolationType.None)]
        public byte[] m_bShipLastDamagedBy;

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

        #endregion



        public FrameData()
        {

        }

        public bool ResetToState(IFrameData fdaFrameDataToResetTo)
        {
            FrameData fdaTargetData = fdaFrameDataToResetTo as FrameData;

            #region PeerAssignment

            if (m_lPeersAssignedToSlot == null || m_lPeersAssignedToSlot.Length != fdaTargetData.m_lPeersAssignedToSlot.Length)
            {
                m_lPeersAssignedToSlot = fdaTargetData.m_lPeersAssignedToSlot.Clone() as long[];
            }
            else
            {
                Array.Copy(fdaTargetData.m_lPeersAssignedToSlot, m_lPeersAssignedToSlot, fdaTargetData.m_lPeersAssignedToSlot.Length);
            }

            #endregion

            #region ShipHealth

            if (m_bShipHealth == null || m_bShipHealth.Length != fdaTargetData.m_bShipHealth.Length)
            {
                m_bShipHealth = fdaTargetData.m_bShipHealth.Clone() as byte[];
                m_fixShipHealDelayTimeOut = fdaTargetData.m_fixShipHealDelayTimeOut.Clone() as Fix[];
                m_bShipLastDamagedBy = fdaTargetData.m_bShipLastDamagedBy.Clone() as byte[];
            }
            else
            {
                Array.Copy(fdaTargetData.m_bShipHealth, m_bShipHealth, fdaTargetData.m_bShipHealth.Length);
                Array.Copy(fdaTargetData.m_fixShipHealDelayTimeOut, m_fixShipHealDelayTimeOut, fdaTargetData.m_fixShipHealDelayTimeOut.Length);
                Array.Copy(fdaTargetData.m_bShipLastDamagedBy, m_bShipLastDamagedBy, fdaTargetData.m_bShipLastDamagedBy.Length);
            }

            #endregion

            #region ShipMovement 

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
            
            #region PeerAssignment

            bWasASuccess &= ByteStream.Serialize(wbsByteStream, ref m_lPeersAssignedToSlot);
            
            #endregion

            #region Input

            bWasASuccess &= ByteStream.Serialize(wbsByteStream, ref m_bInput, m_lPeersAssignedToSlot.Length);

            #endregion

            #region Health

            //list of all the ship Healths 
            bWasASuccess &= ByteStream.Serialize(wbsByteStream, ref m_bShipHealth, m_lPeersAssignedToSlot.Length);

            bWasASuccess &= FixSerialization.Serialize(wbsByteStream, ref m_fixShipHealDelayTimeOut, m_lPeersAssignedToSlot.Length);

            bWasASuccess &= ByteStream.Serialize(wbsByteStream, ref m_bShipLastDamagedBy, m_lPeersAssignedToSlot.Length);

            #endregion

            #region ShipMovement 

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

            #region PeerAssignment

            bWasASuccess &= ByteStream.Serialize(rbsReadByteStream, ref m_lPeersAssignedToSlot);

            #endregion

            #region Input

            bWasASuccess &= ByteStream.Serialize(rbsReadByteStream, ref m_bInput, m_lPeersAssignedToSlot.Length);

            #endregion

            #region Health

            //list of all the ship Healths 
            bWasASuccess &= ByteStream.Serialize(rbsReadByteStream, ref m_bShipHealth, m_lPeersAssignedToSlot.Length);

            bWasASuccess &= FixSerialization.Serialize(rbsReadByteStream, ref m_fixShipHealDelayTimeOut, m_lPeersAssignedToSlot.Length);

            bWasASuccess &= ByteStream.Serialize(rbsReadByteStream, ref m_bShipLastDamagedBy, m_lPeersAssignedToSlot.Length);

            #endregion

            #region ShipMovement 

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

            #region PeerAssignment

            iSize += ByteStream.DataSize( m_lPeersAssignedToSlot);

            #endregion

            #region Input

            iSize += ByteStream.DataSize(m_bInput, m_lPeersAssignedToSlot.Length);

            #endregion

            #region Health

            //list of all the ship Healths 
            iSize += ByteStream.DataSize(m_bShipHealth, m_lPeersAssignedToSlot.Length);

            iSize += FixSerialization.DataSize(m_fixShipHealDelayTimeOut, m_lPeersAssignedToSlot.Length);

            iSize += ByteStream.DataSize(m_bShipLastDamagedBy, m_lPeersAssignedToSlot.Length);

            #endregion

            #region ShipMovement 

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

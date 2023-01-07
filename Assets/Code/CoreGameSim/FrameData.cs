using FixedPointy;
using System;
using System.Security.Cryptography;
using Utility;

namespace Sim
{
    public class FrameData :
        IFrameData,
        IShipPositions,
        IPeerSlotAssignmentFrameData,
        IPeerInputFrameData,
        IShipRespawnFrameData,
        IShipHealthframeData,
        IShipWeaponFrameData,
        ILazerFrameData
    {
        #region PeerAssignment

        public int MaxPlayerCount
        {
            get
            {
                return m_lPeersAssignedToSlot.Length;
            }
        }

        [FrameDataInterpilationTypeAttribute(typeof(long), FrameDataInterpilationTypeAttribute.InterpolationType.None)]
        public long[] m_lPeersAssignedToSlot;

        #region IPeerSlotAssignmentFrameData
        public long[] PeerSlotAssignment { get => m_lPeersAssignedToSlot; set => m_lPeersAssignedToSlot = value; }
        #endregion
        #endregion

        #region Input
        [FrameDataInterpilationTypeAttribute(typeof(int), FrameDataInterpilationTypeAttribute.InterpolationType.None)]
        public byte[] m_bInput;

        [FrameDataInterpilationTypeAttribute(typeof(int), FrameDataInterpilationTypeAttribute.InterpolationType.None)]
        public byte[] m_iInputHash;

        #region IPeerInputFrameData
        public byte[] PeerInput { get => m_bInput; set => m_bInput = value; }

        public byte[] InputHash { get => m_iInputHash; set => m_iInputHash = value; }
        #endregion
        #endregion

        #region Health

        //list of all the ship Healths 
        [FrameDataInterpilationTypeAttribute(typeof(float), FrameDataInterpilationTypeAttribute.InterpolationType.Linear)]
        [FrameDataInterpolationBreak(FrameDataInterpolationBreakAttribute.DataSource.OldFrameData,"m_fixShipHealth[i]","<=",FrameDataInterpolationBreakAttribute.DataSource.CustomData,"Fix.Zero",true)]
        public Fix[] m_fixShipHealth;

        [FrameDataInterpilationTypeAttribute(typeof(float))]
        [FrameDataInterpolationBreak(FrameDataInterpolationBreakAttribute.DataSource.OldFrameData, "m_fixShipHealth[i]", "<=", FrameDataInterpolationBreakAttribute.DataSource.CustomData, "Fix.Zero", true)]
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
        [FrameDataInterpolationBreak(FrameDataInterpolationBreakAttribute.DataSource.OldFrameData, "m_fixShipHealth[i]", "<=", FrameDataInterpolationBreakAttribute.DataSource.CustomData, "Fix.Zero", true)]
        public Fix[] m_fixTimeUntilRespawn;
        public Fix[] TimeUntilRespawn { get => m_fixTimeUntilRespawn; set => m_fixTimeUntilRespawn = value; }
        #endregion

        #endregion

        #region ShipMovement 
        //list of all the player positions 
        [FrameDataInterpilationTypeAttribute(typeof(float))]
        [FrameDataInterpolationBreak(FrameDataInterpolationBreakAttribute.DataSource.OldFrameData, "m_fixShipHealth[i]", "<=", FrameDataInterpolationBreakAttribute.DataSource.CustomData, "Fix.Zero", true)]
        public Fix[] m_fixShipPosX;

        [FrameDataInterpilationTypeAttribute(typeof(float))]
        [FrameDataInterpolationBreak(FrameDataInterpolationBreakAttribute.DataSource.OldFrameData, "m_fixShipHealth[i]", "<=", FrameDataInterpolationBreakAttribute.DataSource.CustomData, "Fix.Zero", true)]
        public Fix[] m_fixShipPosY;

        [FrameDataInterpilationTypeAttribute(typeof(float))]
        [FrameDataInterpolationBreak(FrameDataInterpolationBreakAttribute.DataSource.OldFrameData, "m_fixShipHealth[i]", "<=", FrameDataInterpolationBreakAttribute.DataSource.CustomData, "Fix.Zero", true)]
        public Fix[] m_fixShipVelocityX;

        [FrameDataInterpilationTypeAttribute(typeof(float))]
        [FrameDataInterpolationBreak(FrameDataInterpolationBreakAttribute.DataSource.OldFrameData, "m_fixShipHealth[i]", "<=", FrameDataInterpolationBreakAttribute.DataSource.CustomData, "Fix.Zero", true)]
        public Fix[] m_fixShipVelocityY;

        [FrameDataInterpilationTypeAttribute(typeof(float), FrameDataInterpilationTypeAttribute.InterpolationType.Circular)]
        [FrameDataInterpolationBreak(FrameDataInterpolationBreakAttribute.DataSource.OldFrameData, "m_fixShipHealth[i]", "<=", FrameDataInterpolationBreakAttribute.DataSource.CustomData, "Fix.Zero", true)]
        public Fix[] m_fixShipBaseAngle;

        #region IShipPositions

        public Fix[] ShipPositionX { get => m_fixShipPosX; set => m_fixShipPosX = value; }
        public Fix[] ShipPositionY { get => m_fixShipPosY; set => m_fixShipPosY = value; }
        public Fix[] ShipVelocityX { get => m_fixShipVelocityX; set => m_fixShipVelocityX = value; }
        public Fix[] ShipVelocityY { get => m_fixShipVelocityY; set => m_fixShipVelocityY = value; }
        public Fix[] ShipBaseAngle { get => m_fixShipBaseAngle; set => m_fixShipBaseAngle = value; }

        #endregion

        #endregion

        #region IShipWeaponFrameData
        [FrameDataInterpilationTypeAttribute(typeof(byte), FrameDataInterpilationTypeAttribute.InterpolationType.None)]
        public byte[] m_bLazerFireIndex;

        [FrameDataInterpilationTypeAttribute(typeof(float))]
        [FrameDataInterpolationBreak(FrameDataInterpolationBreakAttribute.DataSource.OldFrameData, "m_fixShipHealth[i]", "<=", FrameDataInterpolationBreakAttribute.DataSource.CustomData, "Fix.Zero", true)]
        public Fix[] m_fixTimeUntilLaserFire;

        [FrameDataInterpilationTypeAttribute(typeof(float))]
        [FrameDataInterpolationBreak(FrameDataInterpolationBreakAttribute.DataSource.OldFrameData, "m_fixShipHealth[i]", "<=", FrameDataInterpolationBreakAttribute.DataSource.CustomData, "Fix.Zero", true)]
        public Fix[] m_fixTimeUntilNextFire;

        public byte[] LazerFireIndex { get => m_bLazerFireIndex; set => m_bLazerFireIndex = value; }
        
        public Fix[] TimeUntilLaserFire { get => m_fixTimeUntilLaserFire; set => m_fixTimeUntilLaserFire = value; }


        public Fix[] TimeUntilLaserReset { get => m_fixTimeUntilNextFire; set => m_fixTimeUntilNextFire = value; }

        #endregion

        #region ILazerFrameData

        [FrameDataInterpilationTypeAttribute(typeof(byte), FrameDataInterpilationTypeAttribute.InterpolationType.None)]
        public byte[] m_bLazerOwner;

        [FrameDataInterpilationTypeAttribute(typeof(float))]
        [FrameDataInterpolationBreak(FrameDataInterpolationBreakAttribute.DataSource.OldFrameData, "m_fixLazerLifeRemaining[i]", "<=", FrameDataInterpolationBreakAttribute.DataSource.NewFrameData, "m_fixLazerLifeRemaining[i]", true)]
        public Fix[] m_fixLazerLifeRemaining;

        [FrameDataInterpilationTypeAttribute(typeof(float))]
        [FrameDataInterpolationBreak(FrameDataInterpolationBreakAttribute.DataSource.OldFrameData, "m_fixLazerLifeRemaining[i]", "<=", FrameDataInterpolationBreakAttribute.DataSource.NewFrameData, "m_fixLazerLifeRemaining[i]", true)]
        public Fix[] m_fixLazerPositionX;

        [FrameDataInterpilationTypeAttribute(typeof(float))]
        [FrameDataInterpolationBreak(FrameDataInterpolationBreakAttribute.DataSource.OldFrameData, "m_fixLazerLifeRemaining[i]", "<=", FrameDataInterpolationBreakAttribute.DataSource.NewFrameData, "m_fixLazerLifeRemaining[i]", true)]
        public Fix[] m_fixLazerPositionY;

        [FrameDataInterpilationTypeAttribute(typeof(float))]
        [FrameDataInterpolationBreak(FrameDataInterpolationBreakAttribute.DataSource.OldFrameData, "m_fixLazerLifeRemaining[i]", "<=", FrameDataInterpolationBreakAttribute.DataSource.NewFrameData, "m_fixLazerLifeRemaining[i]", true)]
        public Fix[] m_fixLazerVelocityX;

        [FrameDataInterpilationTypeAttribute(typeof(float))]
        [FrameDataInterpolationBreak(FrameDataInterpolationBreakAttribute.DataSource.OldFrameData, "m_fixLazerLifeRemaining[i]", "<=", FrameDataInterpolationBreakAttribute.DataSource.NewFrameData, "m_fixLazerLifeRemaining[i]", true)]
        public Fix[] m_fixLazerVelocityY;

        public byte[] LazerOwner { get => m_bLazerOwner; set => m_bLazerOwner = value; }
        public Fix[] LazerLifeRemaining { get => m_fixLazerLifeRemaining; set => m_fixLazerLifeRemaining = value; }
        public Fix[] LazerPositionX { get => m_fixLazerPositionX; set => m_fixLazerPositionX = value; }
        public Fix[] LazerPositionY { get => m_fixLazerPositionY; set => m_fixLazerPositionY = value; }
        public Fix[] LazerVelocityX { get => m_fixLazerVelocityX; set => m_fixLazerVelocityX = value; }
        public Fix[] LazerVelocityY { get => m_fixLazerVelocityY; set => m_fixLazerVelocityY = value; }
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
            if (m_bInput == null || m_bInput.Length != fdaTargetData.m_bInput.Length)
            {
                m_bInput = fdaTargetData.m_bInput.Clone() as byte[];
            }
            else
            {
                Array.Copy(fdaTargetData.m_bInput, m_bInput, fdaTargetData.m_bInput.Length);
            }


            if (m_iInputHash == null || m_iInputHash.Length != fdaTargetData.m_iInputHash.Length)
            {
                m_iInputHash = fdaTargetData.m_iInputHash.Clone() as byte[];
            }
            else
            {
                Array.Copy(fdaTargetData.m_iInputHash, m_iInputHash, fdaTargetData.m_iInputHash.Length);
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

            #region IShipWeaponFrameData
            if (m_bLazerFireIndex == null || m_bLazerFireIndex.Length != fdaTargetData.m_bLazerFireIndex.Length)
            {

                m_bLazerFireIndex = fdaTargetData.m_bLazerFireIndex.Clone() as byte[];
                m_fixTimeUntilNextFire = fdaTargetData.m_fixTimeUntilNextFire.Clone() as Fix[];
                m_fixTimeUntilLaserFire = fdaTargetData.m_fixTimeUntilLaserFire.Clone() as Fix[];
            }
            else
            {
                Array.Copy(fdaTargetData.m_bLazerFireIndex, m_bLazerFireIndex, fdaTargetData.m_bLazerFireIndex.Length);
                Array.Copy(fdaTargetData.m_fixTimeUntilNextFire, m_fixTimeUntilNextFire, fdaTargetData.m_fixTimeUntilNextFire.Length);
                Array.Copy(fdaTargetData.m_fixTimeUntilLaserFire, m_fixTimeUntilLaserFire, fdaTargetData.m_fixTimeUntilLaserFire.Length);
            }
            #endregion

            #region ILazerFrameData
            if (m_bLazerOwner == null || m_bLazerOwner.Length != fdaTargetData.m_bLazerOwner.Length)
            {
                m_bLazerOwner = fdaTargetData.m_bLazerOwner.Clone() as byte[];
                m_fixLazerLifeRemaining = fdaTargetData.m_fixLazerLifeRemaining.Clone() as Fix[];
                m_fixLazerPositionX = fdaTargetData.m_fixLazerPositionX.Clone() as Fix[];
                m_fixLazerPositionY = fdaTargetData.m_fixLazerPositionY.Clone() as Fix[];
                m_fixLazerVelocityX = fdaTargetData.m_fixLazerVelocityX.Clone() as Fix[];
                m_fixLazerVelocityY = fdaTargetData.m_fixLazerVelocityY.Clone() as Fix[];
            }
            else
            {
                Array.Copy(fdaTargetData.m_bLazerOwner, m_bLazerOwner, fdaTargetData.m_bLazerOwner.Length);
                Array.Copy(fdaTargetData.m_fixLazerLifeRemaining, m_fixLazerLifeRemaining, fdaTargetData.m_fixLazerLifeRemaining.Length);
                Array.Copy(fdaTargetData.m_fixLazerPositionX, m_fixLazerPositionX, fdaTargetData.m_fixLazerPositionX.Length);
                Array.Copy(fdaTargetData.m_fixLazerPositionY, m_fixLazerPositionY, fdaTargetData.m_fixLazerPositionY.Length);
                Array.Copy(fdaTargetData.m_fixLazerVelocityX, m_fixLazerVelocityX, fdaTargetData.m_fixLazerVelocityX.Length);
                Array.Copy(fdaTargetData.m_fixLazerVelocityY, m_fixLazerVelocityY, fdaTargetData.m_fixLazerVelocityY.Length);
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

            bWasASuccess &= ByteStream.Serialize(wbsByteStream, ref m_iInputHash, 8);

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

            #region IShipWeaponFrameData
            bWasASuccess &= ByteStream.Serialize(wbsByteStream, ref m_bLazerFireIndex, m_lPeersAssignedToSlot.Length);
            bWasASuccess &= FixSerialization.Serialize(wbsByteStream, ref m_fixTimeUntilLaserFire, m_lPeersAssignedToSlot.Length);
            bWasASuccess &= FixSerialization.Serialize(wbsByteStream, ref m_fixTimeUntilNextFire, m_lPeersAssignedToSlot.Length);
            #endregion

            #region ILazerFrameData
            bWasASuccess &= ByteStream.Serialize(wbsByteStream, ref m_bLazerOwner);
            bWasASuccess &= FixSerialization.Serialize(wbsByteStream, ref m_fixLazerLifeRemaining, m_bLazerOwner.Length);
            bWasASuccess &= FixSerialization.Serialize(wbsByteStream, ref m_fixLazerPositionX, m_bLazerOwner.Length);
            bWasASuccess &= FixSerialization.Serialize(wbsByteStream, ref m_fixLazerPositionY, m_bLazerOwner.Length);
            bWasASuccess &= FixSerialization.Serialize(wbsByteStream, ref m_fixLazerVelocityX, m_bLazerOwner.Length);
            bWasASuccess &= FixSerialization.Serialize(wbsByteStream, ref m_fixLazerVelocityY, m_bLazerOwner.Length);
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

            bWasASuccess &= ByteStream.Serialize(rbsReadByteStream, ref m_iInputHash, 8);

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

            #region IShipWeaponFrameData
            bWasASuccess &= ByteStream.Serialize(rbsReadByteStream, ref m_bLazerFireIndex, m_lPeersAssignedToSlot.Length);

            bWasASuccess &= FixSerialization.Serialize(rbsReadByteStream, ref m_fixTimeUntilLaserFire, m_lPeersAssignedToSlot.Length);

            bWasASuccess &= FixSerialization.Serialize(rbsReadByteStream, ref m_fixTimeUntilNextFire, m_lPeersAssignedToSlot.Length);
            #endregion

            #region ILazerFrameData
            bWasASuccess &= ByteStream.Serialize(rbsReadByteStream, ref m_bLazerOwner);
            bWasASuccess &= FixSerialization.Serialize(rbsReadByteStream, ref m_fixLazerLifeRemaining, m_bLazerOwner.Length);
            bWasASuccess &= FixSerialization.Serialize(rbsReadByteStream, ref m_fixLazerPositionX, m_bLazerOwner.Length);
            bWasASuccess &= FixSerialization.Serialize(rbsReadByteStream, ref m_fixLazerPositionY, m_bLazerOwner.Length);
            bWasASuccess &= FixSerialization.Serialize(rbsReadByteStream, ref m_fixLazerVelocityX, m_bLazerOwner.Length);
            bWasASuccess &= FixSerialization.Serialize(rbsReadByteStream, ref m_fixLazerVelocityY, m_bLazerOwner.Length);
            #endregion

            return bWasASuccess;
        }

        public int GetSize()
        {
            int iSize = 0;

            #region IPeerSlotAssignmentFrameData

            iSize += ByteStream.DataSize(m_lPeersAssignedToSlot);

            #endregion

            #region IPeerInputFrameData

            iSize += ByteStream.DataSize(m_bInput, m_lPeersAssignedToSlot.Length);

            iSize += ByteStream.DataSize(m_iInputHash, 8);

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

            #region IShipWeaponFrameData
            iSize += ByteStream.DataSize(m_bLazerFireIndex, m_lPeersAssignedToSlot.Length);

            iSize += FixSerialization.DataSize(m_fixTimeUntilNextFire, m_lPeersAssignedToSlot.Length);
            #endregion

            #region ILazerFrameData
            iSize += ByteStream.DataSize(m_bLazerOwner);
            iSize += FixSerialization.DataSize(m_fixLazerLifeRemaining, m_bLazerOwner.Length);
            iSize += FixSerialization.DataSize(m_fixLazerPositionX, m_bLazerOwner.Length);
            iSize += FixSerialization.DataSize(m_fixLazerPositionY, m_bLazerOwner.Length);
            iSize += FixSerialization.DataSize(m_fixLazerVelocityX, m_bLazerOwner.Length);
            iSize += FixSerialization.DataSize(m_fixLazerVelocityY, m_bLazerOwner.Length);
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

            HashTools.MergeHashes(ref bOutput, bHash);
        }

    }
}

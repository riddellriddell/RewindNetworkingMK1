using FixedPointy;

namespace Sim
{
    public interface IShipHealthframeData
    {
        Fix[] ShipHealth { get; set; }
        Fix[] ShipHealDelayTimeOut { get; set; }
        byte[] ShipLastDamagedBy { get; set; }
    }

    public interface IShipHealthSettingsData
    {
        Fix ShipMaxHealth { get; }
        Fix ShipHealDelayTime { get; }
        Fix ShipHealRate { get; }

    }

    public class ProcessShipHealth<TFrameData, TConstData, TSettingsData> :
        ISimProcess<TFrameData, TConstData, TSettingsData>,
        ISimSetupProcesses<TFrameData, TSettingsData>
        where TFrameData : IShipHealthframeData, IPeerSlotAssignmentFrameData, IFrameData, new()
        where TSettingsData : IPeerSlotAssignmentSettingsData, IShipHealthSettingsData, ISimTickRateSettings
    {
        public int Priority { get; } = 3;

        public string ProcessName { get; } = "Ship Health Process";

        public static void DamageShip(TFrameData fdaFrameData,TSettingsData sdaSettingsData, int iIndex, Fix fixDamage, byte bAttackingPeerIndex = byte.MaxValue)
        {
            fdaFrameData.ShipHealth[iIndex] = fdaFrameData.ShipHealth[iIndex] - fixDamage;
            fdaFrameData.ShipHealDelayTimeOut[iIndex] = sdaSettingsData.ShipHealDelayTime;

            if (bAttackingPeerIndex != byte.MaxValue)
            {
                fdaFrameData.ShipLastDamagedBy[iIndex] = bAttackingPeerIndex;
            }
        }

        public static void OnShipRespawn(TFrameData fdaFrameData, TSettingsData sdaSettinsData, int iIndex)
        {
            fdaFrameData.ShipHealth[iIndex] = sdaSettinsData.ShipMaxHealth;
            fdaFrameData.ShipHealDelayTimeOut[iIndex] = Fix.Zero;
            fdaFrameData.ShipLastDamagedBy[iIndex] = byte.MaxValue;
        }

        public bool ApplySetupProcess(uint iTick, in TSettingsData sdaSettingsData, long lFirstPeerID, ref TFrameData fdaFrameData)
        {
            fdaFrameData.ShipHealth = new Fix[sdaSettingsData.MaxPlayers];
            fdaFrameData.ShipHealDelayTimeOut = new Fix[sdaSettingsData.MaxPlayers];
            fdaFrameData.ShipLastDamagedBy = new byte[sdaSettingsData.MaxPlayers];

            for (int i = 0; i < fdaFrameData.ShipLastDamagedBy.Length; i++)
            {
                fdaFrameData.ShipLastDamagedBy[i] = byte.MaxValue;
            }

            return true;
        }

        public bool ProcessFrameData(uint iTick, in TSettingsData staSettingsData, in TConstData cdaConstantData, in TFrameData fdaInFrameData, in object[] objInputs, ref TFrameData fdaOutFrameData)
        {
            //loop through all ships 
            for (int i = 0; i < staSettingsData.MaxPlayers; i++)
            {
                //check if peer exists in game 
                if (fdaOutFrameData.PeerSlotAssignment[i] != long.MinValue)
                {
                    //check if ship is dead
                    if (fdaOutFrameData.ShipHealth[i] <= Fix.Zero)
                    {
                        fdaOutFrameData.ShipHealDelayTimeOut[i] = Fix.Zero;
                    }
                    //check if healing is still timed out 
                    else if (fdaOutFrameData.ShipHealDelayTimeOut[i] > Fix.Zero)
                    {
                        //reduce amount of time until ship can start healing 
                        fdaOutFrameData.ShipHealDelayTimeOut[i] = fdaOutFrameData.ShipHealDelayTimeOut[i] - staSettingsData.SecondsPerTick;
                    }
                    else
                    {
                        //add health to ship
                        fdaOutFrameData.ShipHealth[i] = fdaOutFrameData.ShipHealth[i] + (staSettingsData.ShipHealRate * staSettingsData.SecondsPerTick);

                        //check if reached max health
                        if (fdaOutFrameData.ShipHealth[i] > staSettingsData.ShipMaxHealth)
                        {
                            fdaOutFrameData.ShipHealth[i] = staSettingsData.ShipMaxHealth;

                            //clear any damage flags
                            fdaOutFrameData.ShipLastDamagedBy[i] = byte.MaxValue;
                        }
                    }
                }
                else
                {
                    fdaOutFrameData.ShipHealth[i] = Fix.Zero;
                    fdaOutFrameData.ShipLastDamagedBy[i] = byte.MaxValue;
                    fdaOutFrameData.ShipHealDelayTimeOut[i] = Fix.Zero;
                }

            }

            return true;
        }
    }
}

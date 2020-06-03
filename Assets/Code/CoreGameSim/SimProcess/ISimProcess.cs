namespace Sim
{
    public interface ISimProcess<TFrameData, TConstData, TSettingsData>
    {
        int Priotity { get; }

        string ProcessName { get; }

        bool ProcessFrameData(uint iTick, in TSettingsData staSettingsData, in TConstData cdaConstantData, in TFrameData fdaInFrameData, in object[] objInputs, ref TFrameData fdaOutFrameData);

    }
}

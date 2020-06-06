namespace Sim
{
    public interface ISimProcess<TFrameData, TConstData, TSettingsData>
    {
        int Priority { get; }

        string ProcessName { get; }

        bool ProcessFrameData(uint iTick, in TSettingsData sdaSettingsData, in TConstData cdaConstantData, in TFrameData fdaInFrameData, in object[] objInputs, ref TFrameData fdaOutFrameData);

    }
}

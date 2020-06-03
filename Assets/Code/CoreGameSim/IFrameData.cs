using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utility;

namespace Sim
{
    public interface IFrameData
    {
        int MaxPlayerCount { get; }
        bool Encode(WriteByteStream wbsWriteBytStream);
        bool Decode(ReadByteStream rbsReadByteStream);
        int GetSize();
        bool ResetToState(IFrameData fdaFrameDataToResetTo);
        void GetHash(byte[] bHashBytes);
    }
}
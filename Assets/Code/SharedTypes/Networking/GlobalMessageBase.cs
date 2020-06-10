using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utility;

namespace SharedTypes
{
    public abstract class GlobalMessageBase
    {
        public abstract int TypeNumber { get; set; }

        public abstract void Serialize(ReadByteStream rbsByteStream);

        public abstract void Serialize(WriteByteStream rbsByteStream);

        public abstract int DataSize();

    }
}
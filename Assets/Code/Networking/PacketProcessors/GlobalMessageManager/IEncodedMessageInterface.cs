using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utility;

namespace Networking
{
    public interface IEncriptedMessageInterface
    {
        //how many bytes will this message take up
        int GetSize();

        //uses the key manager to encript a piece of data
        //only works for messages encripted for local peer
        bool Encript(WriteByteStream wbsByteStream, GlobalMessageKeyManager mkmKeyManager);

        //uses the key manager to decript message using the public keys
        //stored in the key manager, if the message needs a key that does not exist in the
        //key manager or another error is encountered then this funciton will return false
        //and include a list of the missing keys 
        bool TryDecript(ReadByteStream rbsByteStream, GlobalMessageKeyManager mkmKeyManager, HashSet<long> lMissingKeys);

        //the byte array of this objects data fully encripted and ready to sent to peers
        byte[] EncriptedData { get; set; }
    }
}

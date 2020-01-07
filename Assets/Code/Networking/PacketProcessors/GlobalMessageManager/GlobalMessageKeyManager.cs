using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

namespace Networking
{
    //this class manages the public keys for all the peers in the swarm
    public class GlobalMessageKeyManager 
    {
        public class PublicKey
        {
            //the user id this key belongs to
            public long m_lPeerID;

            // public key used to decode messages and verify signatures
            public RSAParameters m_rprPublickey;

            //signature from the server confirming this is the users public key
            public Byte[] m_bServerSignature;

            //is this key validated by the server and for the correct person
            public bool m_bIsValid;

            //generate hash for signing
            public byte[] GenerateHash()
            {
                byte[] bOutput;


                using (SHA256 mySHA256 = SHA256.Create())
                {
                    //generate hash of key
                    bOutput = mySHA256.ComputeHash(m_rprPublickey.Exponent);

                    //replace first 8 bytes with peer id
                    byte[] bIDBytes = BitConverter.GetBytes(m_lPeerID);

                    for (int i = 0; i < bIDBytes.Length; i++)
                    {
                        bOutput[i] = bIDBytes[i];
                    }
                }

                return bOutput;
            }

            public void Decode(ReadByteStream rbsByteStream, RSAParameters rprServerKeyInfo)
            {
                //get signature from server
                ByteStream.Serialize(rbsByteStream, ref m_bServerSignature);
                               
                //get public key
                ByteStream.Serialize(rbsByteStream, ref m_rprPublickey.Exponent);

                //Create a new instance of RSACryptoServiceProvider.
                using (RSACryptoServiceProvider rcpCriptoProvider = new RSACryptoServiceProvider())
                {
                    //Import the RSA Key information. This only needs
                    //to include the public key information.
                    rcpCriptoProvider.ImportParameters(rprServerKeyInfo);

                    //decript server signature
                    byte[] bDecriptedData = rcpCriptoProvider.Decrypt(m_bServerSignature, false);

                    //extract the peer id
                    m_lPeerID = BitConverter.ToInt64(bDecriptedData, 0);

                    //check if the hashes match up
                    byte[] bHash = GenerateHash();

                    for(int i = 0; i < bHash.Length; i++)
                    {
                        if(bHash[i] != bDecriptedData[i])
                        {
                            m_bIsValid = false;
                            break;
                        }
                    }
                    m_bIsValid = true;
                }
            }

            //cant encode server sig (only createable by server)
            public void Encode(WriteByteStream wbsByteStream,RSAParameters rprServerKeyInfo)
            {                
                //serialize signature from server
                ByteStream.Serialize(wbsByteStream, ref m_bServerSignature);

                //serialize public key
                ByteStream.Serialize(wbsByteStream, ref m_rprPublickey.Exponent);
            }
        }

        //local peer id (not sure if data duplication in this way is a good thing, maybe should replace)
        public long m_lLocalPeerID;

        //local peer key data
        public RSAParameters m_rprKeyData;

        //the signature of the local peers public key by the server
        public Byte[] m_bServerSignature;

        //server public key
        public RSAParameters m_rprServerPublicKey;

        //list of all keys
        public Dictionary<long,PublicKey> m_plkPublicKeys;

        //add key to list
        public void AddKey(PublicKey plkKey)
        {
            //check if key is valid
            if (plkKey == null || plkKey.m_bIsValid == false)
            {
                //discard non valid key
                return;

            }

            //get public key peer id
            long lPeerID = plkKey.m_lPeerID;

            //check if peer already exists 
            if(m_plkPublicKeys.TryGetValue(lPeerID, out PublicKey plkExistingKey))
            {
                //check that keys match
                Byte[] bExistingHash = plkExistingKey.GenerateHash();
                Byte[] bNewKeyHash = plkKey.GenerateHash();

                bool bMatch = true;

                for(int i = 0; i < bExistingHash.Length; i++)
                {
                    if(bExistingHash[i] != bNewKeyHash[i])
                    {
                        bMatch = false;

                        break;
                    }
                }

                if(bMatch == false)
                {
                    //should not be here this means someone hacked the server
                }
            }
        }

        //create public key for local peer
        public PublicKey CreateLocalKey()
        {
            PublicKey pkyPublicKey = new PublicKey();

            pkyPublicKey.m_bServerSignature = m_bServerSignature;

            pkyPublicKey.m_lPeerID = m_lLocalPeerID;

            pkyPublicKey.m_rprPublickey = new RSAParameters();

            pkyPublicKey.m_rprPublickey.Modulus = m_rprServerPublicKey.Modulus;

            pkyPublicKey.m_rprPublickey.Exponent = m_rprServerPublicKey.Exponent;

            return pkyPublicKey;
        }
    }
}

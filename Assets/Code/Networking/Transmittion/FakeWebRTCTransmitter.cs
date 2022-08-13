using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Networking
{
    public class FakeWebRTCTransmitter : IPeerTransmitter
    {
        [Serializable]
        public class NegotiationMessage
        {
            public enum Type
            {
                Offer,
                Reply,
                Ice,
            }

            [SerializeField]
            public int m_iType;

            [SerializeField]
            public int m_iSender;

            [SerializeField]
            public string m_strExtraPadding;

        }

        public static Dictionary<int, FakeWebRTCTransmitter> TransmitterRegistery { get; } = new Dictionary<int, FakeWebRTCTransmitter>();

        public static float s_fOfferCreateTime = 0.05f;

        public static float s_fIceCreateTime = 0.05f;

        public static int s_iNumberOfIceCandidates = 5;

        public static int s_iConnectOnReturnIceCandidate = 3;

        public static int s_iDataPaddingMax = 1000;

        public static int s_iDataPaddingMin = 400;

        protected int m_iTransmitterID = int.MinValue;

        protected int m_iTargetID = int.MinValue;

        public bool m_bMakingOffer = false;

        protected bool m_bSessionDescriptionFinished = false;

        public int m_iIceCandidatesRecieved = 0;

        public FakeWebRTCTransmitter()
        {
            //m_iTransmitterID = 0 - TransmitterRegistery.Count;
            m_iTransmitterID = Random.Range(int.MinValue, int.MaxValue);

            while (TransmitterRegistery.ContainsKey(m_iTargetID))
            {
                Debug.Log("Collision for transmitter id detected calculating new id");
                m_iTransmitterID = Random.Range(int.MinValue, int.MaxValue);
            }

            TransmitterRegistery[m_iTransmitterID] = this;
        }

        protected void OnDataRecieved(byte[] bData)
        {
            //check if "Connected" 
            if (State != PeerTransmitterState.Connected)
            {
                return;
            }

            OnDataReceive?.Invoke(bData);
        }

        protected void MakeConnection()
        {
            //check if already disconnected 
            if(State != PeerTransmitterState.Negotiating)
            {
                return;
            }

            State = PeerTransmitterState.Connected;

            OnConnectionEstablished?.Invoke();
        }

        protected IEnumerator MakeOffer()
        {
            //check state 
            if (State != PeerTransmitterState.New)
            {
                yield break;
            }

            //change state to negotiating 
            State = PeerTransmitterState.Negotiating;

            Debug.Log($"making offer negotiation message");

            yield return new WaitForSeconds(s_fOfferCreateTime);

            NegotiationMessage nmsMessage = new NegotiationMessage()
            {
                m_iSender = m_iTransmitterID,
                m_iType = (int)NegotiationMessage.Type.Offer,
                m_strExtraPadding = GenerateRandomDataPadding()
            };

            string strOffer = JsonUtility.ToJson(nmsMessage);

            m_bSessionDescriptionFinished = true;

            OnNegotiationMessageCreated?.Invoke(strOffer);

            int iIceCandidatesSent = 0;

            //make all the ice candidates
            while (State == PeerTransmitterState.Negotiating && iIceCandidatesSent < s_iNumberOfIceCandidates)
            {
                yield return InternetConnectionSimulator.Instance.StartCoroutine(MakeIce());

                iIceCandidatesSent++;
            }

            yield return null;
        }

        protected IEnumerator MakeReply()
        {
            //check state 
            if (State != PeerTransmitterState.New && State != PeerTransmitterState.Negotiating)
            {
                Debug.Log("Error started making reply in non negotiation state");
                yield break;
            }

            State = PeerTransmitterState.Negotiating;

            Debug.Log($"connection {m_iTransmitterID} making reply negotiation message to {m_iTargetID}");

            yield return new WaitForSeconds(s_fOfferCreateTime);

            NegotiationMessage nmsMessage = new NegotiationMessage()
            {
                m_iSender = m_iTransmitterID,
                m_iType = (int)NegotiationMessage.Type.Reply,
                m_strExtraPadding = GenerateRandomDataPadding()
            };

            string strOffer = JsonUtility.ToJson(nmsMessage);

            m_bSessionDescriptionFinished = true;

            OnNegotiationMessageCreated?.Invoke(strOffer);
        }

        protected IEnumerator MakeIce()
        {
            //wait for session description to finish
            if (m_bSessionDescriptionFinished)
            {
                yield return null;
            }

            yield return new WaitForSeconds(s_fIceCreateTime);

            if (State != PeerTransmitterState.Negotiating)
            {
                yield break;
            }

            Debug.Log($"making ice negotiation message");

            NegotiationMessage nmsMessage = new NegotiationMessage()
            {
                m_iSender = m_iTransmitterID,
                m_iType = (int)NegotiationMessage.Type.Ice,
                m_strExtraPadding = GenerateRandomDataPadding()
            };

            string strOffer = JsonUtility.ToJson(nmsMessage);

            OnNegotiationMessageCreated?.Invoke(strOffer);

            yield return null;
        }

        // ----------------------- IPeerTransmitter interface -----------------------------
        #region
        public PeerTransmitterState State { get; private set; }

        public Action<string> OnNegotiationMessageCreated { get; set; }
        public Action<byte[]> OnDataReceive { get; set; }

        public Action OnConnectionEstablished { get; set; }
        public Action OnConnectionLost { get; set; }

        public bool ProcessNegotiationMessage(string strMessage)
        {
            //check in negotiation state 
            if(State != PeerTransmitterState.Negotiating && State != PeerTransmitterState.New)
            {
                return false;
            }

            NegotiationMessage nmsNegotiationMessage = JsonUtility.FromJson<NegotiationMessage>(strMessage);

            Debug.Log($"Processing message {strMessage} Sender: {nmsNegotiationMessage.m_iSender}, Type: {nmsNegotiationMessage.m_iType}");

            if (nmsNegotiationMessage.m_iType == (int)NegotiationMessage.Type.Offer ||
                nmsNegotiationMessage.m_iType == (int)NegotiationMessage.Type.Reply)
            {
                Debug.Log($" Target Transmitter ID:{nmsNegotiationMessage.m_iSender}");
                m_iTargetID = nmsNegotiationMessage.m_iSender;

                //if message was offer start making reply
                if (nmsNegotiationMessage.m_iType == (int)NegotiationMessage.Type.Offer)
                {
                    InternetConnectionSimulator.Instance.StartCoroutine(MakeReply());
                }
            }
            else if (m_bMakingOffer)
            {
                //update the number of ice candidates recieved
                m_iIceCandidatesRecieved++;

                Debug.Log($"{s_iConnectOnReturnIceCandidate - m_iIceCandidatesRecieved} Left to create connection from {m_iTransmitterID}  to {m_iTargetID} !!!");

                //if enough ice candidates have been recieved make connection
                if (m_iIceCandidatesRecieved >= s_iConnectOnReturnIceCandidate)
                {
                    if (m_iTargetID != int.MinValue)
                    {
                        if (TransmitterRegistery.TryGetValue(m_iTargetID, out FakeWebRTCTransmitter fwtTarget))
                        {
                            if(fwtTarget.State == PeerTransmitterState.Negotiating)
                            {
                                Debug.Log("Transmitter connection established!!!!");

                                State = PeerTransmitterState.Connected;
                                fwtTarget.MakeConnection();
                                OnConnectionEstablished?.Invoke();
                            }
                            else
                            {
                                Debug.Log("Target Failed Connection Process");
                            }
                           
                        }
                        else
                        {
                            Debug.Log($"Unable to find target {m_iTargetID} on Transmitter {m_iTransmitterID}");
                        }
                    }
                    else
                    {
                        Debug.Log($"Error target id never recieved on Transmitter {m_iTransmitterID}");
                    }
                }
            }
            else
            {
                //update the number of ice candidates recieved for th e
                m_iIceCandidatesRecieved++;

                //make reply ice
                InternetConnectionSimulator.Instance.StartCoroutine(MakeIce());
            }

            return true;
        }

        public void Disconnect()
        {

            if (TransmitterRegistery.ContainsKey(m_iTransmitterID))
            {
                TransmitterRegistery.Remove(m_iTransmitterID);
            }

            State = PeerTransmitterState.Disconnected;
            OnDisconnect();
        }

        public bool SentData(byte[] data)
        {
            //check if connected 
            if (State != PeerTransmitterState.Connected)
            {
                return false;
            }

            //try get target
            if (TransmitterRegistery.TryGetValue(m_iTargetID, out FakeWebRTCTransmitter fwtTransmitter))
            {
                //send the data through a fake unreliable connection
                InternetConnectionSimulator.Instance.SendPacket(data, fwtTransmitter.OnDataRecieved);
            }

            return true;
        }

        public void StartNegotiation()
        {
            m_bMakingOffer = true;
            m_iIceCandidatesRecieved = 0;
            InternetConnectionSimulator.Instance.StartCoroutine(MakeOffer());
        }

        protected string GenerateRandomDataPadding()
        {
            int iCharacterNumber = Random.Range(s_iDataPaddingMin, s_iDataPaddingMax);

            const string chars = "abcdefghijklmnopABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 |:_,.";
            return new string(Enumerable.Repeat(chars, iCharacterNumber)
              .Select(s => s[Random.Range(0, s.Length)]).ToArray());
        }

        protected void OnDisconnect()
        {
            OnConnectionLost?.Invoke();
        }

        public void OnCleanup()
        {
        }
        #endregion
    }
}
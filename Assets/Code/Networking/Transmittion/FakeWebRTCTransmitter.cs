using System;
using System.Collections;
using System.Collections.Generic;
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

            public int m_iType;
            public int m_iSender;

        }

        public static Dictionary<int, FakeWebRTCTransmitter> TransmitterRegistery { get; } = new Dictionary<int, FakeWebRTCTransmitter>();

        public static float s_fOfferCreateTime = 1f;

        public static float s_fIceCreateTime = 0.5f;

        public static int s_iNumberOfIceCandidates = 5;

        public static int s_iConnectOnReturnIceCandidate = 3;

        protected int m_iTransmitterID = int.MinValue;

        protected int m_iTargetID = int.MinValue;

        protected bool m_bMakingOffer = false;

        protected int m_iIceCandidatesRecieved = 0;

        public FakeWebRTCTransmitter()
        {
            //m_iTransmitterID = TransmitterRegistery.Count;
            m_iTransmitterID = Random.Range(int.MinValue, int.MaxValue);

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
            State = PeerTransmitterState.Connected;

            OnConnectionEstablished?.Invoke();
        }

        protected IEnumerator MakeOffer()
        {
            //check state 
            if(State != PeerTransmitterState.New )
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
                m_iType = (int)NegotiationMessage.Type.Offer
            };

            string strOffer = JsonUtility.ToJson(nmsMessage);

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
                yield break;
            }

            State = PeerTransmitterState.Negotiating;

            Debug.Log($"making reply negotiation message");

            yield return new WaitForSeconds(s_fOfferCreateTime);

            NegotiationMessage nmsMessage = new NegotiationMessage()
            {
                m_iSender = m_iTransmitterID,
                m_iType = (int)NegotiationMessage.Type.Reply
            };

            string strOffer = JsonUtility.ToJson(nmsMessage);

            OnNegotiationMessageCreated?.Invoke(strOffer);
        }

        protected IEnumerator MakeIce()
        {
            yield return new WaitForSeconds(s_fIceCreateTime);

            if (State != PeerTransmitterState.Negotiating)
            {
                yield break;
            }

            Debug.Log($"making ice negotiation message");

            NegotiationMessage nmsMessage = new NegotiationMessage()
            {
                m_iSender = m_iTransmitterID,
                m_iType = (int)NegotiationMessage.Type.Ice
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

        public bool ProcessNegotiationMessage(string strMessage)
        {
            Debug.Log($"Processing message {strMessage}");

            NegotiationMessage nmsNegotiationMessage = JsonUtility.FromJson<NegotiationMessage>(strMessage);

            if(nmsNegotiationMessage.m_iType == (int)NegotiationMessage.Type.Offer ||
                nmsNegotiationMessage.m_iType == (int)NegotiationMessage.Type.Reply)
            {
                Debug.Log($" Target Transmitter ID:{nmsNegotiationMessage.m_iSender}");
                m_iTargetID = nmsNegotiationMessage.m_iSender;

                //if message was offer start making reply
                if(nmsNegotiationMessage.m_iType == (int)NegotiationMessage.Type.Offer)
                {
                    InternetConnectionSimulator.Instance.StartCoroutine(MakeReply());
                }
            }
            else if(m_bMakingOffer && State != PeerTransmitterState.Connected)
            {
                //update the number of ice candidates recieved
                m_iIceCandidatesRecieved++;

                Debug.Log($"{s_iConnectOnReturnIceCandidate - m_iIceCandidatesRecieved} Left to create connection !!!");

                //if enough ice candidates have been recieved make connection
                if(m_iIceCandidatesRecieved >= s_iConnectOnReturnIceCandidate)
                {
                    if(m_iTargetID != int.MinValue)
                    {
                        if(TransmitterRegistery.TryGetValue(m_iTargetID,out FakeWebRTCTransmitter fwtTarget))
                        {
                            Debug.Log("Transmitter connection established!!!!");

                            State = PeerTransmitterState.Connected;
                            fwtTarget.MakeConnection();
                            OnConnectionEstablished?.Invoke();
                        }
                    }
                }
            }
            else
            {
                //make reply ice
                InternetConnectionSimulator.Instance.StartCoroutine(MakeIce());
            }

            return true;
        }

        public void Disconnect()
        {
            State = PeerTransmitterState.Disconnected;
            if (TransmitterRegistery.ContainsKey(m_iTransmitterID))
            {
                TransmitterRegistery.Remove(m_iTransmitterID);
            }
        }

        public bool SentData(byte[] data)
        {
            //check if connected 
            if (State != PeerTransmitterState.Connected)
            {
                return false;
            }

            //try get target
            if (TransmitterRegistery.TryGetValue(m_iTargetID,out FakeWebRTCTransmitter fwtTransmitter))
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
            
        #endregion
    }
}
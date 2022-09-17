#if UNITY_WEBGL
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Html5WebRTC;

namespace Networking
{
    public class Html5WebRTCTransmitter : IPeerTransmitter
    {
        [Serializable]
        private struct RTCIceCandidateWrapper
        {
            public string candidate;
            public string sdpMid;
            public int sdpMLineIndex;
        }

        [Serializable]
        private struct RTCSessionDescriptionWrapper
        {
            public int type; //RTCSdpType
            public string sdp;
        }

        private static string s_strOfferID = "OFR";
        private static string s_strAnswerID = "ANS";
        private static string s_strIceID = "ICE";

        private static int s_iActiveTransmitters = 0;
        private static bool s_bWebRTCSetup = false;

        public PeerTransmitterState State { get; private set; } = PeerTransmitterState.New;

        public Action<string> OnNegotiationMessageCreated { get; set; }
        public Action OnConnectionEstablished { get; set; }
        public Action OnConnectionLost { get; set; }
        public Action<byte[]> OnDataReceive { get; set; }

        private WebRTCConnection m_pcnPeerConnection;
        private WebRTCDataChannel m_dchDataChannel;
        private MonoBehaviour m_monCoroutineExecutionObject;
        private string m_strLocalSessionDescription;
        private string m_strRemoteSessionDescription;
        private bool m_bAlive = true;


        public Html5WebRTCTransmitter(MonoBehaviour monCoroutineExecutionObject)
        {
            s_iActiveTransmitters++;

            if (s_iActiveTransmitters == 1 && s_bWebRTCSetup == false)
            {
                Debug.Log("Initialize WebRTC");
                s_bWebRTCSetup = true;
                WebRTC.Initialize();
            }

            m_monCoroutineExecutionObject = monCoroutineExecutionObject;

            //get config
            RTCConnectionConfig ccfConfig = SettupConnectionConfigStruct();

            //create peer connection
            m_pcnPeerConnection = new WebRTCConnection(ccfConfig);

            m_pcnPeerConnection.OnDataChannel = OnDataChannelRecieved;
            m_pcnPeerConnection.OnIceCandidate = OnIceCandidate;

            m_monCoroutineExecutionObject.StartCoroutine(Update());
        }

        protected IEnumerator Update()
        {
            while (m_bAlive == true)
            {
                m_pcnPeerConnection?.Update();
                m_dchDataChannel?.Update();

                yield return null;
            }
        }

        #region IPeerTransmitterInterface

        public void Disconnect()
        {
            if (State == PeerTransmitterState.Connected)
            {
                State = PeerTransmitterState.Disconnected;

                OnConnectionLost?.Invoke();
                OnConnectionLost = null;
            }
            else
            {
                State = PeerTransmitterState.Disconnected;
            }

            //clean up
            m_dchDataChannel?.Close();
            m_dchDataChannel?.Dispose();
            m_dchDataChannel = null;

            m_pcnPeerConnection.Close();
            m_pcnPeerConnection.Dispose();
            m_bAlive = false;
        }

        public bool ProcessNegotiationMessage(string strMessage)
        {
            //check if in the correct state to process messages
            if (State == PeerTransmitterState.Disconnected)
            {
                return false;
            }

            int iLength = strMessage.Length;

            //check that negotiation message at least has enough characters to work out message type
            if (iLength < 3)
            {
                Debug.LogError("Negotiation message too small");
                return false;
            }

            //get last 3 letters and try and work out what type of message it is 
            string strMessageType = strMessage.Substring(strMessage.Length - 3);

            strMessage = strMessage.Remove(strMessage.Length - 3);


            if (strMessageType == s_strOfferID)
            {
                m_monCoroutineExecutionObject.StartCoroutine(ProcessOffer(strMessage));
                return true;
            }
            else if (strMessageType == s_strAnswerID)
            {
                m_monCoroutineExecutionObject.StartCoroutine(ProcessAnswer(strMessage));
                return true;
            }
            else if (strMessageType == s_strIceID)
            {
                ProcessIceCandidate(strMessage);

                return true;
            }
            Debug.LogError("Negotiation message did not match any known message types");

            return false;
        }

        public bool SentData(byte[] data)
        {
            if (State != PeerTransmitterState.Connected)
            {
                Debug.LogError($"Cant send data in non conencted state Data:{data}");

                return false;
            }

            if (m_dchDataChannel == null)
            {
                Debug.LogError($"Cant send data due to null data channel Data:{data}");

                return false;
            }

            m_dchDataChannel.Send(data);

            return true;
        }

        public void StartNegotiation()
        {
            //check that the transmitter is in the right state 
            if (State != PeerTransmitterState.New)
            {
                //cant start negotiation on an already negotiated connection
                Debug.LogError("cant start negotiation on an already negotiated WebRTC connection");

                return;
            }

            State = PeerTransmitterState.Negotiating;

            //setup unreliable data channel
            //RTCDataChannelInit dciDataChannelInit = new RTCDataChannelInit(false);
            //dciDataChannelInit.maxRetransmits = 0;
            //dciDataChannelInit.maxRetransmitTime = 0;
            //dciDataChannelInit.ordered = false;
            //dciDataChannelInit.reliable = false;

            //create data channel and link all callbacks 
            OnDataChannelCreated(m_pcnPeerConnection.CreateDataChannel("data",false));

            //start negotiation process 
            m_monCoroutineExecutionObject.StartCoroutine(CreateOfferCoroutine());

        }

        public void OnCleanup()
        {
            s_iActiveTransmitters--;

            if(s_iActiveTransmitters < 0)
            {
                s_iActiveTransmitters = 0;

                if (s_bWebRTCSetup == true)
                {
                    Debug.Log("Finalize WebRTC");
                    s_bWebRTCSetup = false;
                    m_bAlive = false;
                    WebRTC.Dispose();
                }
            }           
        }

        #endregion

        protected void ProcessIceCandidate(string strIceCandidate)
        {
            //deserialize ice candedate 
            RTCIceCandidateWrapper icwWrapper = JsonUtility.FromJson<RTCIceCandidateWrapper>(strIceCandidate);
            RTCIceCandidate iceIceCandidate = new RTCIceCandidate()
            {
                candidate = icwWrapper.candidate,
                sdpMid = icwWrapper.sdpMid,
                sdpMLineIndex = icwWrapper.sdpMLineIndex
            };

            m_pcnPeerConnection.AddIceCandidate(iceIceCandidate);
        }

        protected IEnumerator ProcessOffer(string strOffer)
        {
            //check that the transmitter is in the right state 
            if (State != PeerTransmitterState.New)
            {
                //cant start negotiation on an already negotiated connection
                //Debug.LogError("cant procecss an offer message on an already negotiated WebRTC connection");

                yield break;
            }

            State = PeerTransmitterState.Negotiating;

            m_strRemoteSessionDescription = strOffer;

            //set the remote description 
            yield return m_monCoroutineExecutionObject.StartCoroutine(SetRemoteDescriptionCoroutine());

            //check that we are still negotiating connected and nothing has gone wrong
            if (State != PeerTransmitterState.Negotiating)
            {
                yield break;
            }

            //start building reply 
            yield return m_monCoroutineExecutionObject.StartCoroutine(CreateAnswerCoroutine());
        }

        protected IEnumerator ProcessAnswer(string strAnswer)
        {
            //check that the transmitter is in the right state 
            if (State != PeerTransmitterState.Negotiating)
            {
                //cant start negotiation on an already negotiated connection
                Debug.LogError("cant procecss an offer message on an already negotiated WebRTC connection");

                yield break;
            }

            m_strRemoteSessionDescription = strAnswer;

            //set the local description 
            yield return m_monCoroutineExecutionObject.StartCoroutine(SetRemoteDescriptionCoroutine());
        }

        protected void OnDataChannelRecieved(WebRTCDataChannel dchDataChannel)
        {
            Debug.Log("C# Data Channel Recived");

            OnDataChannelCreated(dchDataChannel);

            OnDataChannelOpen();
        }

        protected void OnDataChannelCreated(WebRTCDataChannel dchDataChannel)
        {
            m_dchDataChannel = dchDataChannel;
            m_dchDataChannel.OnClose = OnDataChannelClose;
            m_dchDataChannel.OnOpen = OnDataChannelOpen;
            m_dchDataChannel.OnMessage = OnMessage;
        }

        protected void OnDataChannelOpen()
        {
            if (State != PeerTransmitterState.Negotiating)
            {
                Debug.LogError($"Should Not be transitioning from non negotiating state {State} to connection to a connected state");

                return;
            }

            State = PeerTransmitterState.Connected;

            OnConnectionEstablished?.Invoke();
        }

        protected void OnDataChannelClose()
        {
            if (State != PeerTransmitterState.Connected)
            {
                Debug.LogError($"Should Not be transitioning from a non negotiating connection state {State} to a disconnected state");

                return;
            }

            State = PeerTransmitterState.Disconnected;

            OnConnectionLost?.Invoke();
        }

        protected void OnIceCandidate(RTCIceCandidate​ icdIceCandidate)
        {
            RTCIceCandidateWrapper icwIceCandidateWrapper = new RTCIceCandidateWrapper()
            {
                candidate = icdIceCandidate.candidate,
                sdpMid = icdIceCandidate.sdpMid,
                sdpMLineIndex = icdIceCandidate.sdpMLineIndex
            };

            OnNegotiationMessageCreated?.Invoke(JsonUtility.ToJson(icwIceCandidateWrapper) + s_strIceID);
        }

        protected void OnMessage(byte[] bytes)
        {
            if (State != PeerTransmitterState.Connected)
            {
                Debug.LogError($"Can not recieve data in state {State} can only recieve in the connected state. Data: {bytes.ToString()}");

                return;
            }

            Debug.LogError($"Handling Message C# side Message Bytes:{bytes.ToString()}");

            OnDataReceive?.Invoke(bytes);
        }

        protected IEnumerator CreateOfferCoroutine()
        {
            //start creating offerr
            RTCSessionDescriptionAsyncOperation sdoAsyncOpperation = m_pcnPeerConnection.CreateOffer();

            yield return sdoAsyncOpperation;

            //check if there was an error creating offer
            if (sdoAsyncOpperation.IsError == false)
            {
                //check still trying to connect 
                if (State == PeerTransmitterState.Negotiating)
                {
                    //set the offer description
                    m_strLocalSessionDescription = sdoAsyncOpperation.m_strSessionDescription;
                    yield return m_monCoroutineExecutionObject.StartCoroutine(SetLocalDescriptionCoroutine());
                }
            }
            else
            {
                Debug.LogError($"Error occured during WebRTC Create Offer. Error:{sdoAsyncOpperation.Error}");

                //change state 
                State = PeerTransmitterState.Disconnected;

                //clean up as best as possible
                m_dchDataChannel?.Dispose();

                //tell listeners that the conenction failed 
                OnConnectionLost?.Invoke();
            }

            //if an error was not encountered 
            if (State == PeerTransmitterState.Negotiating)
            {
                //send negotiation message 
                OnNegotiationMessageCreated?.Invoke(m_strLocalSessionDescription + s_strOfferID);
            }
        }

        protected IEnumerator CreateAnswerCoroutine()
        {
            //start creating answer
            RTCSessionDescriptionAsyncOperation sdoAsyncOpperation = m_pcnPeerConnection.CreateAnswer();

            yield return sdoAsyncOpperation;

            //check if there was an error creating offer
            if (sdoAsyncOpperation.IsError == false)
            {
                //check still trying to connect 
                if (State == PeerTransmitterState.Negotiating)
                {
                    //set the offer description
                    m_strLocalSessionDescription = sdoAsyncOpperation.m_strSessionDescription;
                    yield return m_monCoroutineExecutionObject.StartCoroutine(SetLocalDescriptionCoroutine());
                }
            }
            else
            {
                Debug.LogError($"Error occured during WebRTC Create Answer. Error:{sdoAsyncOpperation.Error}");

                //change state 
                State = PeerTransmitterState.Disconnected;

                //clean up as best as possible
                m_dchDataChannel?.Dispose();

                //tell listeners that the conenction failed 
                OnConnectionLost?.Invoke();
            }

            //if an error was not encountered 
            if (State == PeerTransmitterState.Negotiating)
            {
                //send negotiation message 
                OnNegotiationMessageCreated?.Invoke(m_strLocalSessionDescription + s_strAnswerID);
            }
        }

        protected IEnumerator SetLocalDescriptionCoroutine()
        {
            RTCSetSessionDescriptionAsyncOperation ssdAsyncOpperation = m_pcnPeerConnection.SetLocalDescription(m_strLocalSessionDescription);

            yield return ssdAsyncOpperation;

            if (ssdAsyncOpperation.IsError == false)
            {
                Debug.Log($"Set local description Succeded for {m_pcnPeerConnection}");

                m_pcnPeerConnection.m_bLocalDescriptionSet = true;
            }
            else
            {
                Debug.Log($"Failed to set local description {m_strLocalSessionDescription}. Error {ssdAsyncOpperation.Error} ");

                //change state 
                State = PeerTransmitterState.Disconnected;

                //clean up as best as possible
                m_dchDataChannel?.Dispose();

                //tell listeners that the conenction failed 
                OnConnectionLost?.Invoke();
            }

        }

        protected IEnumerator SetRemoteDescriptionCoroutine()
        {
            RTCSetSessionDescriptionAsyncOperation ssdAsyncOpperation = m_pcnPeerConnection.SetRemoteDescription(m_strRemoteSessionDescription);

            yield return ssdAsyncOpperation;

            if (ssdAsyncOpperation.IsError == false)
            {
                Debug.Log($"Set Remote description Succeded for {m_pcnPeerConnection}");
            }
            else
            {
                Debug.Log($"Failed to set Remote description {m_strRemoteSessionDescription}. Error {ssdAsyncOpperation.Error} ");

                //change state 
                State = PeerTransmitterState.Disconnected;

                //clean up as best as possible
                m_dchDataChannel?.Dispose();

                //tell listeners that the conenction failed 
                OnConnectionLost?.Invoke();
            }
        }

        protected RTCConnectionConfig SettupConnectionConfigStruct()
        {
            RTCConnectionConfig ccfConfig = new RTCConnectionConfig();

            RTCIceServer isrStunServer = new RTCIceServer();
            isrStunServer.urls = "stun:3.26.25.12:3478";
            isrStunServer.username = "";
            isrStunServer.credential = "";

            RTCIceServer isrTurnServer = new RTCIceServer();
            isrTurnServer.urls = "turn:3.26.25.12:3478";
            isrTurnServer.username = "USERNAME";
            isrTurnServer.credential = "PASSWORD";

            RTCIceServer isrBackupServer = new RTCIceServer();
            isrBackupServer.urls = "stun:stun.l.google.com:19302";
            isrBackupServer.username = "";
            isrBackupServer.credential = "";

            ccfConfig.iceServers = new RTCIceServer[]{ isrStunServer, isrTurnServer, isrBackupServer };

            return ccfConfig;
        }
    }
}
#endif
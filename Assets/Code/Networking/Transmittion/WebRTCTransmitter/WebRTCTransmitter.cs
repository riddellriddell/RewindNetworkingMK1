using System;
using System.Collections;
using Unity.WebRTC;
using UnityEngine;

namespace Networking
{
    public class WebRTCTransmitter : IPeerTransmitter
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

        private RTCPeerConnection m_pcnPeerConnection;
        private RTCDataChannel m_dchDataChannel;
        private MonoBehaviour m_monCoroutineExecutionObject;
        private RTCSessionDescription m_sdcLocalSessionDescription;
        private RTCSessionDescription m_sdcRemoteSessionDescription;

        private RTCOfferOptions m_ofoOfferOptions = new RTCOfferOptions
        {
            iceRestart = false,
            offerToReceiveAudio = true,
            offerToReceiveVideo = false
        };

        private RTCAnswerOptions m_afoAnswerOptions = new RTCAnswerOptions
        {
            iceRestart = false,
        };

        public WebRTCTransmitter(MonoBehaviour monCoroutineExecutionObject)
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
            RTCConfiguration cnfConfig = GetSelectedSdpSemantics();

            //create peer connection
            m_pcnPeerConnection = new RTCPeerConnection(ref cnfConfig);

            m_pcnPeerConnection.OnDataChannel = OnDataChannelRecieved;
            m_pcnPeerConnection.OnIceCandidate = OnIceCandidate;
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

            if (strMessageType == s_strIceID)
            {
                ProcessIceCandidate(strMessage);

                return true;
            }
            else if (strMessageType == s_strOfferID)
            {
                m_monCoroutineExecutionObject.StartCoroutine(ProcessOffer(strMessage));
                return true;
            }
            else if (strMessageType == s_strAnswerID)
            {
                m_monCoroutineExecutionObject.StartCoroutine(ProcessAnswer(strMessage));
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
            RTCDataChannelInit dciDataChannelInit = new RTCDataChannelInit(false);
            //dciDataChannelInit.maxRetransmits = 0;
            //dciDataChannelInit.maxRetransmitTime = 0;
            //dciDataChannelInit.ordered = false;
            //dciDataChannelInit.reliable = false;

            //create data channel and link all callbacks 
            OnDataChannelCreated(m_pcnPeerConnection.CreateDataChannel("data", ref dciDataChannelInit));

            //start negotiation process 
            m_monCoroutineExecutionObject.StartCoroutine(CreateOfferCoroutine());

        }

        public void OnCleanup()
        {
            s_iActiveTransmitters--;

            if (s_iActiveTransmitters <= 0 && s_bWebRTCSetup == true)
            {
                Debug.Log("Finalize WebRTC");
                s_bWebRTCSetup = false;
                WebRTC.Finalize();
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

            m_pcnPeerConnection.AddIceCandidate(ref iceIceCandidate);
        }

        protected IEnumerator ProcessOffer(string strOffer)
        {
            //check that the transmitter is in the right state 
            if (State != PeerTransmitterState.New)
            {
                //cant start negotiation on an already negotiated connection
                Debug.LogError("cant procecss an offer message on an already negotiated WebRTC connection");

                yield break;
            }

            State = PeerTransmitterState.Negotiating;

            RTCSessionDescriptionWrapper sdwSessionDescriptionWrapper = JsonUtility.FromJson<RTCSessionDescriptionWrapper>(strOffer);

            m_sdcRemoteSessionDescription = new RTCSessionDescription()
            {
                sdp = sdwSessionDescriptionWrapper.sdp,
                type = (RTCSdpType)sdwSessionDescriptionWrapper.type
            };

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

            RTCSessionDescriptionWrapper sdwSessionDescriptionWrapper = JsonUtility.FromJson<RTCSessionDescriptionWrapper>(strAnswer);

            m_sdcRemoteSessionDescription = new RTCSessionDescription()
            {
                sdp = sdwSessionDescriptionWrapper.sdp,
                type = (RTCSdpType)sdwSessionDescriptionWrapper.type
            };

            //set the local description 
            yield return m_monCoroutineExecutionObject.StartCoroutine(SetRemoteDescriptionCoroutine());
        }

        protected void OnDataChannelRecieved(RTCDataChannel dchDataChannel)
        {
            OnDataChannelCreated(dchDataChannel);

            OnDataChannelOpen();
        }

        protected void OnDataChannelCreated(RTCDataChannel dchDataChannel)
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
                Debug.LogError("Should Not be transitioning from a non negotiating connection to a connected state");

                return;
            }

            State = PeerTransmitterState.Connected;

            OnConnectionEstablished?.Invoke();
        }

        protected void OnDataChannelClose()
        {
            if (State != PeerTransmitterState.Connected)
            {
                Debug.LogError("Should Not be transitioning from a non negotiating connection to a disconnected state");

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
                Debug.LogError($"Cand recieve data when not in the connected state Data: {bytes}");

                return;
            }

            OnDataReceive?.Invoke(bytes);
        }

        protected IEnumerator CreateOfferCoroutine()
        {
            //start creating offerr
            RTCSessionDescriptionAsyncOperation sdoAsyncOpperation = m_pcnPeerConnection.CreateOffer(ref m_ofoOfferOptions);

            yield return sdoAsyncOpperation;

            //check if there was an error creating offer
            if (sdoAsyncOpperation.isError == false)
            {
                //check still trying to connect 
                if (State == PeerTransmitterState.Negotiating)
                {
                    //set the offer description
                    m_sdcLocalSessionDescription = sdoAsyncOpperation.desc;
                    yield return m_monCoroutineExecutionObject.StartCoroutine(SetLocalDescriptionCoroutine());
                }
            }
            else
            {
                Debug.LogError($"Error occured during WebRTC Create Offer. Error:{sdoAsyncOpperation.error}");

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
                //fill in wrapper class for serialization
                RTCSessionDescriptionWrapper sdwWrapper = new RTCSessionDescriptionWrapper()
                {
                    sdp = m_sdcLocalSessionDescription.sdp,
                    type = (int)m_sdcLocalSessionDescription.type
                };

                //send negotiation message 
                OnNegotiationMessageCreated?.Invoke(JsonUtility.ToJson(sdwWrapper) + s_strOfferID);
            }
        }

        protected IEnumerator CreateAnswerCoroutine()
        {
            //start creating answer
            RTCSessionDescriptionAsyncOperation sdoAsyncOpperation = m_pcnPeerConnection.CreateAnswer(ref m_afoAnswerOptions);

            yield return sdoAsyncOpperation;

            //check if there was an error creating offer
            if (sdoAsyncOpperation.isError == false)
            {
                //check still trying to connect 
                if (State == PeerTransmitterState.Negotiating)
                {
                    //set the offer description
                    m_sdcLocalSessionDescription = sdoAsyncOpperation.desc;
                    yield return m_monCoroutineExecutionObject.StartCoroutine(SetLocalDescriptionCoroutine());
                }
            }
            else
            {
                Debug.LogError($"Error occured during WebRTC Create Answer. Error:{sdoAsyncOpperation.error}");

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
                //fill in wrapper class for serialization
                RTCSessionDescriptionWrapper sdwWrapper = new RTCSessionDescriptionWrapper()
                {
                    sdp = m_sdcLocalSessionDescription.sdp,
                    type = (int)m_sdcLocalSessionDescription.type
                };

                //send negotiation message 
                OnNegotiationMessageCreated?.Invoke(JsonUtility.ToJson(sdwWrapper) + s_strAnswerID);
            }
        }

        protected IEnumerator SetLocalDescriptionCoroutine()
        {
            RTCSessionDescriptionAsyncOperation sdoAsyncOpperation = m_pcnPeerConnection.SetLocalDescription(ref m_sdcLocalSessionDescription);

            yield return sdoAsyncOpperation;

            if (sdoAsyncOpperation.isError == false)
            {
                Debug.Log($"Set local description Succeded for {m_pcnPeerConnection}");
            }
            else
            {
                Debug.Log($"Failed to set local description {m_sdcLocalSessionDescription.sdp}. Error {sdoAsyncOpperation.error} ");

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
            RTCSessionDescriptionAsyncOperation sdoAsyncOpperation = m_pcnPeerConnection.SetRemoteDescription(ref m_sdcRemoteSessionDescription);

            yield return sdoAsyncOpperation;

            if (sdoAsyncOpperation.isError == false)
            {
                Debug.Log($"Set Remote description Succeded for {m_pcnPeerConnection}");
            }
            else
            {
                Debug.Log($"Failed to set Remote description {m_sdcRemoteSessionDescription.sdp}. Error {sdoAsyncOpperation.error} ");

                //change state 
                State = PeerTransmitterState.Disconnected;

                //clean up as best as possible
                m_dchDataChannel?.Dispose();

                //tell listeners that the conenction failed 
                OnConnectionLost?.Invoke();
            }
        }

        protected RTCConfiguration GetSelectedSdpSemantics()
        {
            RTCConfiguration cnfConfig = default;
            cnfConfig.iceServers = new RTCIceServer[]
            {
            new RTCIceServer { urls = new string[] { "stun:stun.l.google.com:19302" } }
            };

            return cnfConfig;
        }
    }
}

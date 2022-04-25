using System.Collections;
using System.Collections.Generic;
using Unity.Html5WebRTC;
using UnityEngine;
using UnityEngine.UI;

public class NativeFunctionsTest : MonoBehaviour
{
    public Text m_txtNativeFunctionOut;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(Test2());
    }
    

    public IEnumerator Test1()
    {
        Debug.Log("Starting Test");

        yield return null;

        Debug.Log("Initalizing");

        //setup webrtc
        NativeFunctions.Initialize();

        Debug.Log("Creating new connection");

        string strIceConnectionUrl = "stun:stun.l.google.com:19302";
        
        int iConnectionPtr = NativeFunctions.NewConnection(strIceConnectionUrl);      

        m_txtNativeFunctionOut.text += $", Connection ptr:{iConnectionPtr}";

        Debug.Log("Creating Data channel");

        //create data channel
        int iChannelPtr = NativeFunctions.CreateDataChannel(iConnectionPtr, "data", false);
        
        m_txtNativeFunctionOut.text += $", Channel ptr:{iChannelPtr}";

        //create offer
        Debug.Log("Creating Offer");

        int iCreateOfferAsyncPtr = NativeFunctions.CreateOffer(iConnectionPtr);
        
        RTCSessionDescriptionAsyncOperation sdaAsyncOpperation = new RTCSessionDescriptionAsyncOperation(iCreateOfferAsyncPtr);
        
        m_txtNativeFunctionOut.text += $", Starting async opperation";
        
        yield return sdaAsyncOpperation;
        
        m_txtNativeFunctionOut.text += $", async opperation complete";
        
        m_txtNativeFunctionOut.text += $", SDC: {sdaAsyncOpperation.m_strSessionDescription}";

        Debug.Log($", SDC: {sdaAsyncOpperation.m_strSessionDescription}");

        int iSetLocalDescriptionPtr = NativeFunctions.SetLocalDescription(iConnectionPtr, JsonUtility.ToJson(sdaAsyncOpperation.m_strSessionDescription));

        RTCSetSessionDescriptionAsyncOperation ssdSetSessionDescriptionAsync = new RTCSetSessionDescriptionAsyncOperation(iSetLocalDescriptionPtr);

        Debug.Log("Setting local description");

        yield return ssdSetSessionDescriptionAsync;

        Debug.Log("local description set");

        NativeFunctions.Dispose();

        Debug.Log("Finished");
    }

    public IEnumerator Test2()
    {
        yield return null;

        Debug.Log("Starting Test");
        //setup
        WebRTC.Initialize();

        string strIceConnectionUrl = "stun:stun2.l.google.com:19302";

        Debug.Log("CreatingConneciton");

        WebRTCConnection conSendConnection = new WebRTCConnection(strIceConnectionUrl);

        WebRTCConnection conReplyConnection = new WebRTCConnection(strIceConnectionUrl);

        conSendConnection.OnIceCandidate = (RTCIceCandidate icdIceCandidate) =>
        {
            Debug.Log($"Send Ice Candidate- Candidate:{icdIceCandidate.candidate} SDPMid:{icdIceCandidate.sdpMid} SDPMLineIndex{icdIceCandidate.sdpMLineIndex}");
            conReplyConnection.AddIceCandidate(icdIceCandidate);
        };
        conReplyConnection.OnIceCandidate = (RTCIceCandidate icdIceCandidate) =>
        {
            Debug.Log($"Recieve Ice Candidate- Candidate:{icdIceCandidate.candidate} SDPMid:{icdIceCandidate.sdpMid} SDPMLineIndex{icdIceCandidate.sdpMLineIndex}");
            conSendConnection.AddIceCandidate(icdIceCandidate);
        };
    
        //creating data channel
        Debug.Log("Creating DataChannel");

        WebRTCDataChannel dchSendDataChannel = conSendConnection.CreateDataChannel("Test", false);

        WebRTCDataChannel dchRecieverDataChannel = null;

        conReplyConnection.OnDataChannel = (WebRTCDataChannel dchChannel) =>
        {
            Debug.Log("Data Channel Opened On Reciever");
            dchRecieverDataChannel = dchChannel;

            dchRecieverDataChannel.OnMessage = (byte[] bData) =>
            {
                Debug.Log("Message Recieved On reciever");
                for (int i = 0; i < bData.Length; i++)
                {
                    Debug.Log(bData[i]);
                }
            };

            byte[] TestData = { 0, 1, 255 };
            dchRecieverDataChannel.Send(TestData);
            TestData[0] = 1;
            dchRecieverDataChannel.Send(TestData);
            TestData[0] = 2;
            dchRecieverDataChannel.Send(TestData);
            TestData[0] = 3;
            dchRecieverDataChannel.Send(TestData);
        };

        dchSendDataChannel.OnOpen = () => {
            Debug.Log("Data Channel Open on Sender");
            byte[] TestData = { 0, 1, 255 };

            dchSendDataChannel.Send(TestData);
        };

        dchSendDataChannel.OnMessage = (byte[] bData) => {
            Debug.Log("Message Recieved On Sender");
            for (int i =0; i < bData.Length; i++)
            {
                Debug.Log(bData[i]);
            }
        };

        Debug.Log("Creating Offer");

        RTCSessionDescriptionAsyncOperation sdaOfferAsyncOpperation = conSendConnection.CreateOffer();

        yield return sdaOfferAsyncOpperation;

        yield return new WaitForSeconds(1.0f);

        Debug.Log($"OfferResult {sdaOfferAsyncOpperation.m_strSessionDescription} Was Error: {sdaOfferAsyncOpperation.IsError} ");

        string strOffer = JsonUtility.ToJson(sdaOfferAsyncOpperation.m_strSessionDescription);

        Debug.Log($"Setting Local Description {strOffer} of Send Connection");

        yield return conSendConnection.SetLocalDescription(sdaOfferAsyncOpperation.m_strSessionDescription);

        yield return new WaitForSeconds(1.0f);

        Debug.Log($"Setting Remote Description {strOffer} of recieve Connection");

        RTCSetSessionDescriptionAsyncOperation ssdSetRecieveRemoteDesc = conReplyConnection.SetRemoteDescription(sdaOfferAsyncOpperation.m_strSessionDescription);

        yield return ssdSetRecieveRemoteDesc;

        yield return new WaitForSeconds(1.0f);

        Debug.Log($"Set Remote Description Result is error:{ssdSetRecieveRemoteDesc.IsError}");

        Debug.Log("Creating Answer");

        RTCSessionDescriptionAsyncOperation sdaAnswerAsyncOpperation = conReplyConnection.CreateAnswer();

        yield return sdaAnswerAsyncOpperation;

        yield return new WaitForSeconds(1.0f);

        Debug.Log($" AnswerResult: {sdaAnswerAsyncOpperation.m_strSessionDescription} Was Error: {sdaAnswerAsyncOpperation.IsError} ");

        Debug.Log("Setting local description of recieve channel");

        yield return conReplyConnection.SetLocalDescription(sdaAnswerAsyncOpperation.m_strSessionDescription);

        yield return new WaitForSeconds(1.0f);

        Debug.Log("Setting remote description of send channel");

        RTCSetSessionDescriptionAsyncOperation ssdSetSendRemoteDesc = conSendConnection.SetRemoteDescription(sdaAnswerAsyncOpperation.m_strSessionDescription);

        yield return ssdSetSendRemoteDesc;

        Debug.Log($"Set Remote Description Result is error:{ssdSetSendRemoteDesc.IsError}");

        yield return new WaitForSeconds(1f);

        for (int i = 0; i < 60; i++)
        {
            Debug.Log("Updating to deect Ice Candidates");

            yield return new WaitForSeconds(0.25f);

            conSendConnection.Update();
            conReplyConnection.Update();

            dchSendDataChannel?.Update();
            dchRecieverDataChannel?.Update();
        }

        yield return new WaitForSeconds(0.5f);

        Debug.Log($"Data channel status:{((dchSendDataChannel != null && dchRecieverDataChannel != null) ? "setup" : "broken")} , Sending Data");

        byte[] testData = {0,1,69,255};

        dchSendDataChannel?.Send(testData);

        yield return new WaitForSeconds(5);

        Debug.Log("Disposing");

        dchSendDataChannel?.Close();
        dchRecieverDataChannel?.Close();

        dchSendDataChannel?.Dispose();
        dchRecieverDataChannel?.Dispose();

        conSendConnection.Dispose();
        conReplyConnection.Dispose();

        WebRTC.Dispose();
    }
}

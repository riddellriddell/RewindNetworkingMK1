mergeInto(LibraryManager.library, {

  Initialize:function()
  {
    //Dictionary holding all connections
    iDataHeadPtr = 1;
    mapData = new Map();

  },  

  Dispose:function()
  {
    delete mapData;
  },

  MapDataNew:function(objData)
  {
    iDataHeadPtr++;

    mapData.set(iDataHeadPtr,objData);

    return iDataHeadPtr;
  },

  MapDataDelete:function(iDataPtr)
  {
    mapData.delete(iDataPtr);
  },

  //------------------------------- Connection ------------------------------------------
  
  
  NewConnection__deps: ['SetupDataChannel'],
  NewConnection__deps: ['MapDataNew'],
  NewConnection:function(strIceServerURL)
  {
    //connection settings 
    const config =  JSON.parse(Pointer_stringify(strIceServerURL)); 
    
    const exampleConfig = {
      iceServers: [
          {
              username: 'myuser',
              credential: 'userpassword',
              urls: 'turn:public_ip_address:3478?transport=tcp'
          }
      ]
  }

    const oldConfig ={iceServers: [{urls: "stun:stun.1.google.com:19302"}]};
    
    console.log("oldCandidate: " + JSON.stringify(oldConfig) + " new candidate: " + JSON.stringify(config)  + " example config: " + JSON.stringify(exampleConfig));

    var conConnection = new RTCPeerConnection(config);

    var iDataPtr = _MapDataNew(conConnection);

    
    //an event object for all events 
    //boolean values are set to true if there is an event of this type that needs to be handdled 
    conConnection.objEvents = 
    {
      bOnIceCandidate: false,
      iDataChannelPtr: -1
    };

    //array holding all the ice candidates
    conConnection.objIceCandidates = {strCandidates: []}

    // setup function to handle ice candidates 
    function OnIceCandidate(iceCandidate)
    {
      if(iceCandidate.candidate != undefined && iceCandidate.candidate != null && iceCandidate.candidate != "")
      {
        var jsnIceCandidate = JSON.stringify( iceCandidate.candidate);

        console.log("RTC_Lib on ice candidate" + jsnIceCandidate);

        conConnection.objIceCandidates.strCandidates.push(jsnIceCandidate);
        conConnection.objEvents.bOnIceCandidate = true;
      }
      else
      {
        console.log("RTC_Lib on ice candidate EMPTY");
        //conConnection.objIceCandidates.strCandidates.push("");
      }
    };

    function OnDataChannel(evtEvent)
    {
      console.log("RTC_Lib OnDataChannel");
      var dchDataChannel = evtEvent.channel;
      conConnection.objEvents.iDataChannelPtr = _MapDataNew(dchDataChannel);
      _SetupDataChannel(dchDataChannel); 
    };

    conConnection.onicecandidate = OnIceCandidate;
    conConnection.ondatachannel = OnDataChannel;

    return iDataPtr;
  },

  //gets a list of all the events that have occured 
  GetConnectionEvents:function(iConnectionPtr)
  {
    var conConnection = mapData.get(iConnectionPtr);

    var strReturn = JSON.stringify(conConnection.objEvents);

    //resets event list 
    conConnection.objEvents.bOnIceCandidate = false;
    conConnection.objEvents.iDataChannelPtr = -1;

    var bufferSize = lengthBytesUTF8(strReturn) + 1;
    var buffer = _malloc(bufferSize);
    stringToUTF8(strReturn, buffer, bufferSize);
    return buffer;
  },

  //gets and clears unhandeld ice candidate events 
  GetConnectionIceCandidateEvents:function(iConnectionPtr)
  {
    var conConnection = mapData.get(iConnectionPtr);

    var strReturn = JSON.stringify(conConnection.objIceCandidates);

    conConnection.objIceCandidates.strCandidates = [];

    var bufferSize = lengthBytesUTF8(strReturn) + 1;
    var buffer = _malloc(bufferSize);
    stringToUTF8(strReturn, buffer, bufferSize);
    return buffer;
  },

  GetConnectionOnDataChannelEvents:function(iConnectionPtr)
  {
    var conConnection = mapData.get(iConnectionPtr);

    return conConnection.iDataChannelPtr;
  },

  CloseConnection:function(iConnectionPtr)
  {
    var conConnection = mapData.get(iConnectionPtr);

    conConnection.close();
  },

  DisposeConnection__deps: ['MapDataDelete'],
  DisposeConnection:function(iPeerConnectionPtr)
  {
    _MapDataDelete(iPeerConnectionPtr);
  },

  HandleSendChannelStatusChange:function(event)
  {
    console.log(event);
  },
  
  CreateDataChannel__deps: ['SetupDataChannel'],
  CreateDataChannel__deps: ['MapDataNew'],
  CreateDataChannel__deps: ['HandleSendChannelStatusChange'],
  CreateDataChannel:function(iPeerConnectionPtr, strLabel, bIsReliable)
  {
    console.log("RTC_Lib CreateDataChannel")

    var ObjectCreationSettings = {};

    ObjectCreationSettings.ordered = bIsReliable;

    var strStringLabel = Pointer_stringify(strLabel);

    var conConnection = mapData.get(iPeerConnectionPtr);

    var dchNewDataChannel =  conConnection.createDataChannel("SendChannel");

    var iDataChannelPtr = _MapDataNew(dchNewDataChannel);

    _SetupDataChannel(dchNewDataChannel);
    
    return iDataChannelPtr;
  },

  

  //starts request for offer and returns "pointer" to async status tracker  
  CreateOffer__deps: ['MapDataNew'],
  CreateOffer:function(iPeerConnectionPtr)
  {
    var conConnection = mapData.get(iPeerConnectionPtr);

    var objAsync = 
    {
      bIsFinished: false
    };

    function ProcessOffer(offer)
    {
      objAsync.bIsError = false; 
      objAsync.strDescription = JSON.stringify(offer);
      objAsync.bIsFinished = true;

      console.log(offer)
    };

    function ProcessError(error)
    {
      objAsync.bIsError = true; 
      objAsync.strDescription = "Error" + JSON.stringify(error);  
      objAsync.bIsFinished = true;
    }

    conConnection.createOffer().then(ProcessOffer).catch(ProcessError);
        
    var iAsyncDataPtr = _MapDataNew(objAsync);

    return iAsyncDataPtr;
  },

  CreateAnswer__deps: ['MapDataNew'],
  CreateAnswer:function(iPeerConnectionPtr)
  {
    var conConnection = mapData.get(iPeerConnectionPtr);

    var objAsync = 
    {
      bIsFinished: false
    };

    function ProcessOffer(offer)
    {
      objAsync.bIsError = false; 
      objAsync.strDescription = JSON.stringify(offer);
      objAsync.bIsFinished = true;

      console.log(offer)
    };

    function ProcessError(error)
    {
      objAsync.bIsError = true; 
      objAsync.strDescription = "Error" + JSON.stringify(error);  
      objAsync.bIsFinished = true;
    };

    conConnection.createAnswer().then(ProcessOffer).catch(ProcessError);
        
    var iAsyncDataPtr = _MapDataNew(objAsync);

    return iAsyncDataPtr;
  },

  IsAsyncActionComplete:function(iAsyncItemPtr)
  {
    var objAsync = mapData.get(iAsyncItemPtr);

    if(objAsync.bIsFinished)
    {
      return true;
    }
    return false;
  },

  GetAsyncResult__deps: ['MapDataDelete'],
  GetAsyncResult:function(iAsyncItemPtr)
  {
    var objAsync = mapData.get(iAsyncItemPtr);

    var strReturn = JSON.stringify(objAsync);

    //clean up async items
    _MapDataDelete(iAsyncItemPtr);

    var bufferSize = lengthBytesUTF8(strReturn) + 1;
    var buffer = _malloc(bufferSize);
    stringToUTF8(strReturn, buffer, bufferSize);
    return buffer;
  },
  
  SetLocalDescription__deps: ['MapDataNew'],
  SetLocalDescription:function(iConnectionPtr,strDescriptionJson)
  {
    var objSDP = JSON.parse( Pointer_stringify(strDescriptionJson));

    var conConnection = mapData.get(iConnectionPtr);

    var objAsync = {};

    objAsync.bIsFinished = false;
    objAsync.bIsError = false;

    function ProcessSetLocalDescription()
    {
      objAsync.bIsFinished = true;
    };

    function ProcessSetLocalDescriptionError()
    {
      objAsync.bIsFinished = true; 
      objAsync.bIsError = true;
    };

    conConnection.setLocalDescription(objSDP).then(ProcessSetLocalDescription).catch(ProcessSetLocalDescriptionError);

    var iDataPtr = _MapDataNew(objAsync);
    return iDataPtr;
  },
  
  SetRemoteDescription__deps: ['MapDataNew'],
  SetRemoteDescription:function(iConnectionPtr, strDescriptionJson)
  {
    var objSDP = JSON.parse(Pointer_stringify(strDescriptionJson));

    var conConnection = mapData.get(iConnectionPtr);

    var objAsync = {};

    objAsync.bIsFinished = false;
    objAsync.bIsError = false;

    function ProcessSetLocalDescription()
    {
      objAsync.bIsFinished = true;
    };

    function ProcessSetLocalDescriptionError()
    {
      objAsync.bIsFinished = true; 
      objAsync.bIsError = true;
    };

    conConnection.setRemoteDescription(objSDP).then(ProcessSetLocalDescription).catch(ProcessSetLocalDescriptionError);

    var iDataPtr = _MapDataNew(objAsync);
    return iDataPtr;
  },

  AddIceCandidate:function(iConnectionPtr, strIceCandidateJson)
  {
    var conConnection = mapData.get(iConnectionPtr);

    function OnAddIceCandidateError(error)
    {
      console.log(`Failure during addIceCandidate(): ${error.name}`);
    };

    conConnection.addIceCandidate(JSON.parse(Pointer_stringify(strIceCandidateJson))).catch(OnAddIceCandidateError);
  },

  ///-------------------------------------------- Data Channel ----------------------------------------

  SetupDataChannel__deps: ['DataChannelEventsReset'],
  SetupDataChannel__deps:['WriteMessageToDataChannelBuffer'],
  SetupDataChannel:function(dchDataChannel)
  {    
    _DataChannelEventsReset(dchDataChannel);

    //set data type
    dchDataChannel.binaryType = "arraybuffer";

    function OnOpen()
    {
      console.log("Data Channel Opened!!!");

      dchDataChannel.bIsOpen = true;
      dchDataChannel.objEvents.bOnOpen = true;
    };

    function OnClose()
    {
      console.log("Data Channel Closed!!!");
      dchDataChannel.objEvents.bOnClose = true;
    };

    function OnMessage(bMessage)
    {
      //make sure channel is marked as open
      //before sending a message 
      if(dchDataChannel.bIsOpen = false)
      {
        console.log("Data channel not marked as open, forcing channel open!");
        dchDataChannel.bIsOpen = true;
        dchDataChannel.objEvents.bOnOpen = true;
      }

      if(dchDataChannel.bMessageBuffer != undefined)
      {        
        _WriteMessageToDataChannelBuffer(dchDataChannel,bMessage);
      }
      else
      {
        console.log("Message Buffer Undefined! unable to store message!!!!!");
      }
    };

    dchDataChannel.bIsOpen = false;
    dchDataChannel.bIsMessageBufferSetup = false;
    dchDataChannel.onopen = OnOpen;
    dchDataChannel.onclose = OnClose;
    dchDataChannel.onmessage = OnMessage;
  },

  DataChannelEventsReset:function(dchDataChannel)
  {
    dchDataChannel.objEvents = 
    {
      bOnOpen: false,
      bOnClose: false,
      strSerializedCorrectly: "True",
    };
  },

  DataChannelSetupMessageBuffer:function(iDataChannelPtr,bMessageByteArray,iByteArraySize,bMessageIndexArray,iMessageIndexArraySize)
  {
    var dchDataChannel = mapData.get(iDataChannelPtr);

    if( dchDataChannel != undefined && dchDataChannel.bMessageBuffer != undefined && dchDataChannel.bMessageBuffer.length != 0)
    {
      //console.log("message buffer already setup");
      return;
    }

    var bMessagesSharedArray = new Uint8Array(buffer, bMessageByteArray, iByteArraySize);

    var iMessageIndexArray =  new Int32Array(buffer, bMessageIndexArray,iMessageIndexArraySize);

    dchDataChannel.bIsMessageBufferSetup = true;
    dchDataChannel.bMessageBuffer = bMessagesSharedArray;
    dchDataChannel.iMessageIndexBuffer = iMessageIndexArray;
  },

  WriteMessageToDataChannelBuffer:function(dchDataChannel,Message)
  {
    //check that data channel is setup and ready 
    if(dchDataChannel.bIsMessageBufferSetup === undefined || dchDataChannel.bIsMessageBufferSetup == false)
    {
      return;
    }

    if( dchDataChannel.bMessageBuffer.length == 0)
    {
      console.log("message buffer was detached unable to write message");
      return;
    }

    var bData = new Uint8Array(Message.data);

    var iSize = bData.length; 

    //get the total number of messages in the buffer 
     var iTotalNumberOfMessages = dchDataChannel.iMessageIndexBuffer[0];

     //get the index to write the new message index to 
     var iIndexIndex = iTotalNumberOfMessages + 1;

     //check if too many messages have been recieved 
     if(iIndexIndex >= dchDataChannel.iMessageIndexBuffer.length)
     {
        throw("Recieved" +  iIndexIndex + "messages which is too many messages only a max of" + dchDataChannel.iMessageIndexBuffer.length + "Messages can be processed befor message buffer is full");
     }

     //get the index to write from
     var iWriteIndex = 0;
 
     if(iIndexIndex != 1)
     {
       iWriteIndex = dchDataChannel.iMessageIndexBuffer[iTotalNumberOfMessages];
     }

     var iWriteEndIndex = iWriteIndex + iSize;

     if(iWriteEndIndex >= dchDataChannel.bMessageBuffer.length)
     {
       //error this message will overflow the message buffer 
       throw("Message buffer overflow");
     }
 
     //set the write to index 
     dchDataChannel.iMessageIndexBuffer[iIndexIndex] = iWriteEndIndex;
     
     //update the number of messages in the buffer
     dchDataChannel.iMessageIndexBuffer[0] = dchDataChannel.iMessageIndexBuffer[0] + 1;

     //write the data 
     dchDataChannel.bMessageBuffer.set(bData,iWriteIndex);
  },

  CloseDataChannel :function(iDataChannelPtr)
  {
    var dchDataChannel = mapData.get(iDataChannelPtr);

    dchDataChannel.close();
  },

  DisposeDataChannel__deps: ['MapDataDelete'],
  DisposeDataChannel :function(iDataChannelPtr)
  {
    _MapDataDelete(iDataChannelPtr);
  },

  GetDataChannelEvents__deps: ['DataChannelEventsReset'],
  GetDataChannelEvents:function(iDataChannelPtr)
  {
    var dchDataChannel = mapData.get(iDataChannelPtr);

    var strEventString = JSON.stringify(dchDataChannel.objEvents);

    _DataChannelEventsReset(dchDataChannel);

    var bufferSize = lengthBytesUTF8(strEventString) + 1;
    var buffer = _malloc(bufferSize);
    stringToUTF8(strEventString, buffer, bufferSize);
    return buffer;
  },

  SendByteArray:function(iDataChannelPtr,bSendArray,iSendArrayLength)
  {  
    var dchDataChannel = mapData.get(iDataChannelPtr);

    //get array view of message 
    var bSendData = new Uint8Array(buffer,bSendArray,iSendArrayLength);

    console.log("Sending Data " + bSendData + "Through WebRTC data channel:" + dchDataChannel);

    //send data 
    dchDataChannel.send(bSendData);
  }
});
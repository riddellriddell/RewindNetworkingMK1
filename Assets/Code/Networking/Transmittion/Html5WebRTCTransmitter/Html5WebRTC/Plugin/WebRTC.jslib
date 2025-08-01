
var iDataHeadPtr = 1;
var mapData = new Map();

mergeInto(LibraryManager.library, {

  Initialize:function()
  {
    //Dictionary holding all connections
    iDataHeadPtr = 1;
    mapData = new Map();
  },  

  Dispose:function()
  {
    mapData.clear();
    mapData = null;;
  },

  MapDataNew:function(objData)
  {
    iDataHeadPtr++;

    mapData.set(iDataHeadPtr,objData);

    return iDataHeadPtr;
  },

  MapDataDelete:function(iDataPtr)
  {
    if (mapData.has(iDataPtr)) 
    {
        mapData.delete(iDataPtr);
    }
    else
    {
        console.log("trying to delete data for data pointer that does not exist. Data Pointer: " + iDataPtr)
    }
  },

  //------------------------------- Connection ------------------------------------------
  
  
  NewConnection__deps: ['SetupDataChannel','MapDataNew'],
  NewConnection:function(strIceServerURL)
  {
    //connection settings 
    const config =  JSON.parse(UTF8ToString(strIceServerURL)); 

    var conConnection;

    try
    {
        conConnection = new RTCPeerConnection(config);
    }
    catch(e)
    {
        console.error("Failed to create RTCPeerConnection", e);
        return 0;
    }

    var iDataPtr = _MapDataNew(conConnection);

    
    //an event object for all events 
    //boolean values are set to true if there is an event of this type that needs to be handled 
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

    var strReturn = JSON.stringify({...conConnection.objEvents});

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
    
        if (!conConnection)
        {
          console.log("Connection object not found for connection pointer:" + iConnectionPtr);
          return -1;
        }

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
    
      if (!conConnection)
      {
          console.log("Connection object not found for connection pointer:" + iConnectionPtr);
          return -1;
      }

    return conConnection.iDataChannelPtr;
  },

  CloseConnection:function(iConnectionPtr)
  {
    var conConnection = mapData.get(iConnectionPtr);
    
    if (!conConnection)
    {
        console.log("Connection object not found for connection pointer:" + iConnectionPtr);
        return;
    }

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
  
  CreateDataChannel__deps: ['SetupDataChannel', 'MapDataNew','HandleSendChannelStatusChange'],
  CreateDataChannel:function(iPeerConnectionPtr, strLabel, bIsReliable)
  {
    console.log("RTC_Lib CreateDataChannel")

    var ObjectCreationSettings = {};

    ObjectCreationSettings.ordered = bIsReliable;

    var strStringLabel = UTF8ToString(strLabel);

    var conConnection = mapData.get(iPeerConnectionPtr);

    var dchNewDataChannel =  conConnection.createDataChannel(strStringLabel);

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
    
    var iAsyncDataPtr = _MapDataNew(objAsync);

    conConnection.createOffer().then(ProcessOffer).catch(ProcessError);
        
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
    var objSDP = JSON.parse( UTF8ToString(strDescriptionJson));

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
  
    console.log(`Setting remote description ${strDescriptionJson}`);
  
    var objSDP = JSON.parse(UTF8ToString(strDescriptionJson));

    var conConnection = mapData.get(iConnectionPtr);

    var objAsync = {};

    objAsync.bIsFinished = false;
    objAsync.bIsError = false;

    function ProcessSetLocalDescription()
    {
      objAsync.bIsFinished = true;
    };

    function ProcessSetLocalDescriptionError(error)
    {
      console.error(`Error setting remote description ${JSON.stringify(error)}`);
      objAsync.bIsFinished = true; 
      objAsync.bIsError = true;
    };

    conConnection.setRemoteDescription(objSDP).then(ProcessSetLocalDescription).catch(ProcessSetLocalDescriptionError);

    var iDataPtr = _MapDataNew(objAsync);
    return iDataPtr;
  },

  AddIceCandidate:function(iConnectionPtr, strIceCandidateJson)
  {
    var strJson = UTF8ToString(strIceCandidateJson);
    console.log(`Adding ice candidate: ${strJson}`);
  
    var conConnection = mapData.get(iConnectionPtr);

    function OnAddIceCandidateError(error)
    {
      console.log(`Failure during addIceCandidate(): ${JSON.stringify(error)}`);
    };

    conConnection.addIceCandidate(JSON.parse(strJson)).catch(OnAddIceCandidateError);
  },

  ///-------------------------------------------- Data Channel ----------------------------------------

  SetupDataChannel__deps: ['DataChannelEventsReset', 'WriteMessageToDataChannelBuffer'],
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
      if(dchDataChannel.bIsOpen === false)
      {
        console.log("Data channel not marked as open, forcing channel open!");
        dchDataChannel.bIsOpen = true;
        dchDataChannel.objEvents.bOnOpen = true;
      }

      if(dchDataChannel.bMessageBufferOffset != undefined)
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

  DataChannelSetupMessageBuffer:function(iDataChannelPtr,bMessageByteArrayOffset,iByteArraySize,bMessageIndexArrayOffset,iMessageIndexArraySize)
  {
    var dchDataChannel = mapData.get(iDataChannelPtr);

    dchDataChannel.bIsMessageBufferSetup = true;
    dchDataChannel.bMessageBufferOffset = bMessageByteArrayOffset;
    dchDataChannel.iMessageBufferSize = iByteArraySize;
    
    
    dchDataChannel.bMessageIndexBufferOffset = bMessageIndexArrayOffset;
    dchDataChannel.iMessageIndexBufferSize = iMessageIndexArraySize;
  },

  WriteMessageToDataChannelBuffer:function(dchDataChannel,Message)
  {
    //check that data channel is setup and ready 
    if(dchDataChannel.bIsMessageBufferSetup === undefined || dchDataChannel.bIsMessageBufferSetup == false)
    {
        console.log("message buffer was not setup unable to write message");
        return;
    }

    if( dchDataChannel.iMessageIndexBufferSize == 0)
    {
        console.log("message buffer was detached unable to write message");
        return;
    }
    
    //re create the buffers using offset and size
    var bMessageBuffer = new Uint8Array(HEAPU8.buffer, dchDataChannel.bMessageBufferOffset, dchDataChannel.iMessageBufferSize);
    var iMessageIndexBuffer =  new Int32Array(HEAPU8.buffer,  dchDataChannel.bMessageIndexBufferOffset, dchDataChannel.iMessageIndexBufferSize * 4);
        

    var bData = new Uint8Array(Message.data);

    var iSize = bData.length; 

    //get the total number of messages in the buffer 
     var iTotalNumberOfMessages = iMessageIndexBuffer[0];

     //get the index to write the new message index to 
     var iIndexIndex = iTotalNumberOfMessages + 1;

     //check if too many messages have been received 
     if(iIndexIndex >= iMessageIndexBuffer.length)
     {
        throw("Received" +  iIndexIndex + "messages which is too many messages only a max of" + iMessageIndexBuffer.length + "Messages can be processed befor message buffer is full");
     }

     //get the index to write from
     var iWriteIndex = 0;
 
     if(iIndexIndex != 1)
     {
       iWriteIndex = iMessageIndexBuffer[iTotalNumberOfMessages];
     }

     var iWriteEndIndex = iWriteIndex + iSize;

     if(iWriteEndIndex >= bMessageBuffer.length)
     {
       //error this message will overflow the message buffer 
       throw("Message buffer overflow");
     }
 
     //set the write to index 
     iMessageIndexBuffer[iIndexIndex] = iWriteEndIndex;
     
     //update the number of messages in the buffer
     iMessageIndexBuffer[0] = iMessageIndexBuffer[0] + 1;

     //write the data 
     bMessageBuffer.set(bData,iWriteIndex);
  },

  CloseDataChannel :function(iDataChannelPtr)
  {
    var dchDataChannel = mapData.get(iDataChannelPtr);

    dchDataChannel.close();
  },

  DisposeDataChannel__deps: ['MapDataDelete'],
  DisposeDataChannel:function(iDataChannelPtr)
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
    var bSendData = new Uint8Array(HEAPU8.buffer,bSendArray,iSendArrayLength);

    console.log("Sending Data " + bSendData + "Through WebRTC data channel:" + dchDataChannel);

    //send data 
    dchDataChannel.send(bSendData);
  },
  
  // Add a free function Unity can call
  FreePtr:function(ptr) 
  {
      _free(ptr);
  }
});
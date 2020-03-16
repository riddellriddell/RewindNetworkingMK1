import * as functions from 'firebase-functions';
import * as admin from 'firebase-admin';

admin.initializeApp();

 function MakeID(length:number):string 
 {
    let result           = '';
    let characters       = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789';
    let charactersLength = characters.length;
    for ( let i = 0; i < length; i++ ) {
       result += characters.charAt(Math.floor(Math.random() * charactersLength));
    }
    return result;
 }

function SetupNewPeer(UserKey : admin.database.Reference, AccessKey : string): Promise<void>
{
    const PromiseArray = [];

    //the time of the last activity
    PromiseArray.push(UserKey.child('LastActivity').set(admin.database.ServerValue.TIMESTAMP));
    PromiseArray.push(UserKey.child('AccessKey').set(AccessKey));

    return Promise.all(PromiseArray).then();
}

function SetupNewUdidMapping(UdidKey : admin.database.Reference, UserKey :string, AccessKey : string): Promise<void>
{
    const PromiseArray = [];

    //the time of the last activity
    PromiseArray.push(UdidKey.child('UserKey').set(UserKey));
    PromiseArray.push(UdidKey.child('AccessKey').set(AccessKey));

    return Promise.all(PromiseArray).then();
}

 export const GetPeerIDForUDID = functions.https.onRequest((request, response) =>
 {   
    console.log(request.body);

     //get the device udid
    const strUniqueDeviceID = request.body.udid;

    console.log(strUniqueDeviceID);
    
    //check for null empty or undifined request
    if(!strUniqueDeviceID)
    {
        response.status(400).json(
            {                  
                message: 'Unique ID Not Included in request' + strUniqueDeviceID
            }
        );
    }

    const UdidMappingAddress = admin.database().ref('UdidToPeerID').child(strUniqueDeviceID);

    //chech to see if unique id has map to peer id
    UdidMappingAddress.once('value')
    .then((DataSnapshot) =>
    {
        console.log(DataSnapshot);

        //on unique id found 
        if(DataSnapshot.val() !== null)
        {           
            return DataSnapshot.val();
        }
        else
        {
            //create new user 
            const NewUserAddress = admin.database().ref('Users').push();
            
            const NewUserID = String(NewUserAddress.key)
            const NewUserAccessKey = MakeID(10);
            
            const PromiseArray = [];

            //push new user to the server 
            PromiseArray.push( SetupNewPeer(NewUserAddress,NewUserAccessKey));
            PromiseArray.push( SetupNewUdidMapping(UdidMappingAddress,NewUserID,NewUserAccessKey));
            

            return Promise.all(PromiseArray).then(() =>
            {
                const UserIDValues = {
                    UserKey: NewUserID,
                    AccessKey: NewUserAccessKey
                };

               return UserIDValues
            })
        }
    })
    .then((result) =>
    {
        response.status(200).json(
            {
                UserID: result
            }                    
        );
    })
    .catch((error) =>
    {
        response.status(500).send(error);
    });

    //response.send("Failed to find peer id for udid");
 });

 //sends a message from one peer to another
export const SendMessageToPeer = functions.https.onRequest((request, response) =>
{
    //check that request was fully formed 
    const lFromID = request.body.m_lFromID;
    const lToID = request.body.m_lToID;
    const iType = request.body.m_iType;
    const strMessage = request.body.m_strMessage;
    
    let bIsValidRequest = true;

    // validate request types
    if((typeof lFromID).localeCompare('string')!== 0 || !lFromID)
    {
        bIsValidRequest = false;
    }
    if((typeof lToID).localeCompare('string')!== 0 || !lToID)
    {
        bIsValidRequest = false;
    }
    
    if((typeof iType).localeCompare('number')!== 0)
    {
        bIsValidRequest = false;
    }
    
    if((typeof strMessage).localeCompare('string')!== 0 || !strMessage)
    {
        bIsValidRequest = false;
    }

    if(bIsValidRequest === false)
    {
        response.status(400).json(
            {                  
                message: 'Bad Request data in request FromID:' + lFromID + ' ToID:' + lToID + ' Type:' + iType + ' Message:' + strMessage
            }
        );

        return;
    }

    //get target directory 
    const UserMessageAddress = admin.database().ref('Users').child(lToID).child('Messages').push();

    //create message
    const MessageCreatePromises = []

    MessageCreatePromises.push(UserMessageAddress.child('Date').set(admin.database.ServerValue.TIMESTAMP));
    MessageCreatePromises.push(UserMessageAddress.child('From').set(lFromID));
    MessageCreatePromises.push(UserMessageAddress.child('Type').set(iType));
    MessageCreatePromises.push(UserMessageAddress.child('Message').set(strMessage));

    Promise.all(MessageCreatePromises).then((res)=>
    {
        response.status(200).send('success');
        
    }).catch((error) =>
    {
        response.status(500).json(
            {                  
                message: 'Error in sending message' + error
            }
        );
    });
});

//get the messages for a user 
export const GetMessagesForPeer = functions.https.onRequest((request, response) =>
{
    //get user id
    const strUserID = request.body.m_strUserID;
    const strAccesKey = request.body.m_strAccessKey;

    if(!strUserID || !strAccesKey)
    {
        response.status(400).json(
            {                  
                message: 'Bad Request data in request UserID:' + strUserID + ' AccessKey:' + strAccesKey
            }
        );

        return;
    }

    //try get user messages
    const UserAccessKeyAddress = admin.database().ref('Users').child(strUserID).child('AccessKey');
    const UserMessageAddress = admin.database().ref('Users').child(strUserID).child('Messages');

    const MessageCreatePromises = []

     //message object
     interface MessageDetials
     {
        Key: string,
        Value: any
     }
    const Messages = new Array<MessageDetials>();
    let bIsValidReqest = true;

    MessageCreatePromises.push(UserAccessKeyAddress.once('value').then((ReturnValues) =>
    {
        if(ReturnValues === null )
        {
            //check if peer existed at all
            response.status(404).json(
                {                  
                    message: 'User Does Not Exist UserID:' + strUserID
                }
            );

            bIsValidReqest = false;

            return;
        }
        else if (ReturnValues.val() !== strAccesKey)
        {
             //check if peer existed at all
             response.status(500).json(
                {                  
                    message: 'Incorrect User Key  Access Key:' + ReturnValues.val() + ' Passed Key:' + strAccesKey + ' UserKey:' + strUserID 
                }               
            );

            bIsValidReqest = false;

            return;
        }

        bIsValidReqest = true

        return;
    }));

    //get messages 
    MessageCreatePromises.push(UserMessageAddress.once('value').then((ReturnValue) =>
    {      
        //check if there was any messages 
        if(ReturnValue === null)
        {
           return;
        }
        else
        {
            ReturnValue.forEach((messageSnapshot) =>
            {
                const MessageDetails = 
                {
                    Key: String(messageSnapshot.key),
                    Value: messageSnapshot.val()
                };

                Messages.push(MessageDetails)
            }); 

            return;
        }        
    }));

    //wait for all promises to complete
    Promise.all(MessageCreatePromises).then(() =>
    {
        if(bIsValidReqest === false)
        {
            return null;
        }   
        
        interface ReplyMessage
        {
            m_lFromUser: string,
            m_lTimeOfMessage: number,
            m_iMessageType: number,
            m_strMessage: string
        }

        //build reply message
        const ReplyMessage = new Array<ReplyMessage>();
            
        //the min time for a message to be valid
        const MinValidMessageAge = Date.now() - (1000 * 20);

        const DeletePromisies = []

        for (let i = 0; i < Messages.length; i++) 
        {
            const MessageCreationTime = Number(Messages[i].Value.Date);
            if(MessageCreationTime > MinValidMessageAge)
            {
                let UserMessage = {
                    m_lFromUser: Messages[i].Value.From,
                    m_lTimeOfMessage: Messages[i].Value.Date,
                    m_iMessageType: Messages[i].Value.Type,
                    m_strMessage: Messages[i].Value.Message
                };

                ReplyMessage.push(UserMessage);
            }

            DeletePromisies.push(UserMessageAddress.child(Messages[i].Key).remove())
        };

        //wait for messages to finish deleting 
        return Promise.all(DeletePromisies).then(()=>
        {
            //return the compiled reply message
            return ReplyMessage;
        } );

    }).then((result) =>
    {
        if(result != null)
        {
            //send reply message 
            response.status(200).json(
            {                  
                UserMessage: result
            });
        }

    }).catch((error)=>
    {
        //send reply message 
        response.status(500).json(
        {                  
            Error: error
        });
    });

});

  
interface GatewayState
{
    m_iRemainingSlots: number  
}

interface GatewayDetails
{
    m_iLastActiveTime:number
    m_strUserID: string
    m_strAccessKey: string
    m_staGameState: GatewayState 
}


//sets a gateway 
export const SetGateway = functions.https.onRequest((request, response) =>
{
    //get owning player and owning player key
    const OldestValidGateTime =Number(Date.now());
    const strUserID:string = String(request.body.m_strUserID);
    const strAccessKey:string = String(request.body.m_strAccessKey);
    const staGameState: GatewayState = request.body.m_staGameState;

    const GateDetaild: GatewayDetails =
    {
        m_iLastActiveTime: OldestValidGateTime,
        m_strUserID: strUserID,
        m_strAccessKey: strAccessKey,
        m_staGameState: staGameState
    }

    //validate inputs 
    if(!strUserID || !strAccessKey || staGameState === null || staGameState === undefined || staGameState.m_iRemainingSlots === undefined)
    {
        response.status(400).json(
            {                  
                message: 'Bad Request data in request UserID:' + strUserID + ' AccessKey:' + strAccessKey + ' Game State' + staGameState
            }
        );

        return;
    }

    //check if already exists 
    const GatewayAccessKeyAddress = admin.database().ref('Gateways').child(strUserID).child('AccessKey');
    
    //get gateway access key
    GatewayAccessKeyAddress.once('value').then((result) => 
    {
        console.log('Result from get gateway for user' + result.val());

        if(result.val() === null || result.val() === strAccessKey)
        {
            const GatewayAddress = admin.database().ref('Gateways').child(strUserID);

            //let GatewayConnectPromise = []

            //GatewayConnectPromise.push(GatewayAddress.child('LastActivity').set(admin.database.ServerValue.TIMESTAMP))
            //GatewayConnectPromise.push(GatewayAddress.child('AccessKey').set(strAccessKey))
            //GatewayConnectPromise.push(GatewayAddress.child('GameState').set(staGameState))
 
            //return Promise.all(GatewayConnectPromise).then(() => 
            //{
            //    return true;
            //});

            return GatewayAddress.set(GateDetaild).then(()=>
            {
                return true;
            })
        }
        else
        {
            return false;
        }
    }).then((returnValue) =>
    {
        if(returnValue === true)
        {
            response.status(200).json(
                {                  
                    message: 'Success'
                }
            );
        }
        else
        {
            response.status(400).json(
                {                  
                    message: 'Incorrect Key, Passed key:' + strAccessKey
                }
            );
        }

    }).catch((error)=>
    {
        //send reply message 
        response.status(500).json(
        {                  
            Error: error
        });
    });
});

//sets a gateway 
export const GetGateway = functions.https.onRequest((request, response) =>
{
    //in the future get the peer details and find a game with 
    //matching world location / other deets 

    //get all gateways
    const Gateways = admin.database().ref('Gateways');
  
    const ActiveGateways = new Array<GatewayDetails>()
    const DeadGateways = new Array<string>()
    
    //get gateway details
    Gateways.once('value').then((result) =>
    {        
        if(result.val() !== null)
        {
            //the oldest time for an active gateway 
            const OldestValidGateTime:number = Date.now() - (1000 * 20);

            result.forEach((gateway) =>
            {
                console.log('GetGatewayValue: last active time:' + gateway.val().m_iLastActiveTime + ' oldest Valid Time:' + OldestValidGateTime)

                if(Number(gateway.val().m_iLastActiveTime) < OldestValidGateTime)
                {
                    DeadGateways.push(String(gateway.key));
                }
                else if(Number(gateway.val().m_staGameState.m_iRemainingSlots) > 0)
                {
                    ActiveGateways.push(gateway.val() as GatewayDetails)
                }
            })
        }
    }).then(() =>
    {
        const RemoveGatePromises = []

        for(let i = 0 ; i < DeadGateways.length; i++)
        {
            RemoveGatePromises.push(Gateways.child(DeadGateways[i]).remove())
        }

        return Promise.all(RemoveGatePromises).then();
    }).then(() =>
    {
        //if there are no gateways return a 404 and let the user create a new one
        if(ActiveGateways.length === 0)
        {
            
            response.status(404).json(
                {                  
                    message:'No active Gateways exist'
                }
            );

            return;
        };

        let BestGateway = null

        for(let i = 0 ; i < ActiveGateways.length; i++)
        {
            if(BestGateway == null)
            {
                BestGateway = ActiveGateways[i];
            }
            else if(BestGateway.m_staGameState.m_iRemainingSlots < ActiveGateways[i].m_staGameState.m_iRemainingSlots)
            {
                BestGateway = ActiveGateways[i];
            }

        }

        response.status(200).json(
            {                  
                BestGatewayOwner: BestGateway?.m_strUserID
            }
        );
       
    }).catch((error) =>
    {
        //send reply message 
        response.status(500).json(
        {                  
            Error: error
        });
    })

});
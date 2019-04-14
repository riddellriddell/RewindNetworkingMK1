using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Code.Networking
{
    //this represents a connection to another  user 
    interface IInternetConnection
    {      
       

        //called when data is received
        Action<List<byte>> OnDataReceive { get; set; }

        //send data through internet 
        bool SentData(List<byte> data);

    }
}

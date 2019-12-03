using Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sim
{
    public class GamePacketFactory : ClassWithIDFactory
    {
        protected override void SetupTypes()
        {
            AddType<ResetTickCountPacket>(ResetTickCountPacket.TypeID);
            AddType<PingPacket>(PingPacket.TypeID);
            AddType<NetTestSendPacket>(NetTestSendPacket.TypeID);
            AddType<NetTestReplyPacket>(NetTestReplyPacket.TypeID);
            AddType<InputPacket>(InputPacket.TypeID);
            AddType<StartCountDownPacket>(StartCountDownPacket.TypeID);
            AddType<NetworkLayoutPacket>(NetworkLayoutPacket.TypeID);
            AddType<ConnectionNegotiationPacket>(ConnectionNegotiationPacket.TypeID);
        }
    }
}

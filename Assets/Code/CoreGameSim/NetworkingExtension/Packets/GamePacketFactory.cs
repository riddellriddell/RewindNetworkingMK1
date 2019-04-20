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
            AddType<NetTestPacket>(NetTestPacket.TypeID);
            AddType<InputPacket>(InputPacket.TypeID);
            AddType<StartCountDownPacket>(StartCountDownPacket.TypeID);
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
    public class ConnectionPropagatorProcessor : NetworkPacketProcessor
    {
        public override int Priority
        {
            get
            {
                throw new NotImplementedException();
            }
        }
    }
}

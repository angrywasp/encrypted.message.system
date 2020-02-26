using System.Collections.Generic;
using AngryWasp.Net;

namespace EMS.TimedEvents
{
    [TimedEvent(30)]
    public class ConnectToSeedNodes : ITimedEvent
    {
        public void Execute()
        {
            if (ConnectionManager.Count == 0 && !Config.User.NoReconnect)
                Client.ConnectToSeedNodes();
        }
    }
}
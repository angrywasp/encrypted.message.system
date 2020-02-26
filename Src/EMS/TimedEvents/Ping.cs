using System.Collections.Generic;
using AngryWasp.Net;

namespace EMS.TimedEvents
{
    [TimedEvent(60)]
    public class Ping : ITimedEvent
    {
        public void Execute()
        {
            Helpers.MessageAll(AngryWasp.Net.Ping.GenerateRequest().ToArray());
        }
    }
}
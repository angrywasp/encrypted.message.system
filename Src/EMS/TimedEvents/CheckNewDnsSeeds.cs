using System.Collections.Generic;
using AngryWasp.Net;

namespace EMS.TimedEvents
{
    [TimedEvent(60 * 60)]
    public class CheckNewDnsSeeds : ITimedEvent
    {
        public void Execute()
        {
            Helpers.AddSeedFromDns();
        }
    }
}
using AngryWasp.Net;
using EMS.Commands.P2P;

namespace EMS.TimedEvents
{
    [TimedEvent(5 * 60)]
    public class CheckNewMessages : ITimedEvent
    {
        public void Execute()
        {
            Helpers.MessageAll(RequestMessagePool.GenerateRequest(true).ToArray());
        }
    }
}
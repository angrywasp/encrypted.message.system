using AngryWasp.Net;

namespace EMS.TimedEvents
{
    [TimedEvent(10 * 60)]
    public class ExchangePeerLists : ITimedEvent
    {
        public void Execute()
        {
            Helpers.MessageAll(AngryWasp.Net.ExchangePeerList.GenerateRequest(true).ToArray());
        }
    }
}
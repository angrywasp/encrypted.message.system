using System.Collections.Generic;
using AngryWasp.Net;

namespace EMS.TimedEvents
{
    [TimedEvent(10 * 60)]
    public class ExchangePeerLists : ITimedEvent
    {
        public void Execute()
        {
            List<Connection> disconnected = new List<Connection>();

            ConnectionManager.ForEach(Direction.Incoming | Direction.Outgoing, (c) =>
            {
                if (!c.Write(AngryWasp.Net.ExchangePeerList.GenerateRequest(true).ToArray()))
                    disconnected.Add(c);
            });

            foreach (Connection c in disconnected)
                ConnectionManager.Remove(c);
        }
    }
}
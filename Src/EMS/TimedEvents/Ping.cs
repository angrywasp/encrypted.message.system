using System.Collections.Generic;
using AngryWasp.Net;

namespace EMS.TimedEvents
{
    [TimedEvent(60)]
    public class Ping : ITimedEvent
    {
        public void Execute()
        {
            List<Connection> disconnected = new List<Connection>();

            ConnectionManager.ForEach(Direction.Incoming | Direction.Outgoing, (c) =>
            {
                if (!c.Write(AngryWasp.Net.Ping.GenerateRequest().ToArray()))
                    disconnected.Add(c);
            });

            foreach (var c in disconnected)
                ConnectionManager.Remove(c);
        }
    }
}
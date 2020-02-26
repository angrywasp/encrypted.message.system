using System.Collections.Generic;
using AngryWasp.Net;
using EMS.Commands.P2P;

namespace EMS.TimedEvents
{
    [TimedEvent(5 * 60)]
    public class CheckNewMessages : ITimedEvent
    {
        public void Execute()
        {
            List<Connection> disconnected = new List<Connection>();

            ConnectionManager.ForEach(Direction.Incoming | Direction.Outgoing, (c) =>
            {
                if (!c.Write(RequestMessagePool.GenerateRequest(true).ToArray()))
                    disconnected.Add(c);
            });

            foreach (var c in disconnected)
                ConnectionManager.Remove(c);
        }
    }
}
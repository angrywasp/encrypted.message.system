using System.Collections.Generic;
using AngryWasp.Net;

namespace EMS.TimedEvents
{
    [TimedEvent(2 * 60)]
    public class DeleteExpired : ITimedEvent
    {
        public void Execute()
        {
            HashSet<HashKey16> delete = new HashSet<HashKey16>();

            foreach (var m in MessagePool.Messages)
            {
                if (m.Value.IsExpired())
                    delete.Add(m.Key);
            }

            foreach (var k in delete)
            {
                Message m;
                MessagePool.Messages.TryRemove(k, out m);
                Log.WriteConsole($"Message {m.Key} expired");
                if (MessagePool.OutgoingMessages.Contains(k))
                    MessagePool.OutgoingMessages.Remove(k);
            }
        }
    }
}
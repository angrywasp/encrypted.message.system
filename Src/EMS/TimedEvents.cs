using System;
using System.Collections.Generic;
using System.Linq;
using AngryWasp.Helpers;
using AngryWasp.Net;
using EMS.Commands.P2P;

namespace EMS
{
    public class TimedEventAttribute : Attribute
    {
        private int interval;

        public int Interval => interval;

        public TimedEventAttribute(int interval)
        {
            this.interval = interval * 1000;
        }
    }

    public class TimedEvents
    {
        public static void Initialize()
        {
            var methods = typeof(TimedEvents).GetMethods()
                .Where(m => m.GetCustomAttributes(typeof(TimedEventAttribute), false).Length > 0)
                .ToArray();

            foreach (var m in methods)
            {
                TimedEventAttribute a = m.GetCustomAttributes(true).OfType<TimedEventAttribute>().FirstOrDefault();
                Action action = (Action)m.CreateDelegate(typeof(Action));
                TimedEventManager.Add(m.Name, action, a.Interval);
            }
        }

        /// <summary>
        /// Delete expired messages
        /// </summary>
        [TimedEvent(2 * 60)]
        public static void DeleteExpired()
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
        
        /// <summary>
        /// Check for new seed nodes via DNS
        /// </summary>
        [TimedEvent(60 * 60)]
        public static void CheckNewDnsSeeds()
        {
            Helpers.AddSeedFromDns();
        }

        /// <summary>
        /// Attempt connection to seed nodes if we are offline
        /// </summary>
        [TimedEvent(30)]
        public static void ConnectToSeeds()
        {
            if (ConnectionManager.Count == 0 && !Config.User.NoReconnect)
                Client.ConnectToSeedNodes();
        }

        /// <summary>
        /// Exchange peer lists with other nodes.
        /// </summary>
        [TimedEvent(10 * 60)]
        public static void ExchangePeerList()
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
    
        /// <summary>
        /// Exchange peer lists with other nodes.
        /// </summary>
        [TimedEvent(5 * 60)]
        public static void CheckNewMessages()
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

        /// <summary>
        /// Exchange peer lists with other nodes.
        /// </summary>
        [TimedEvent(60)]
        public static void Ping()
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
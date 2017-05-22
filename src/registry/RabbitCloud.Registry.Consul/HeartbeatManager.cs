﻿using Consul;
using Microsoft.Extensions.Logging;
using RabbitCloud.Rpc.Abstractions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace RabbitCloud.Registry.Consul
{
    public class HeartbeatManager : IDisposable
    {
        private readonly IList<string> _checkids = new List<string>();
        private readonly Timer _timer;

        public HeartbeatManager(IConsulClient consulClient, ILogger<HeartbeatManager> logger = null)
        {
            logger = logger ?? NullLogger<HeartbeatManager>.Instance;
            var timeSpan = ConsulConstants.TtlInterval.Subtract(TimeSpan.FromSeconds(3));
            _timer = new Timer(async s =>
            {
                string[] ids;
                lock (_checkids)
                    ids = _checkids.ToArray();

                var ndoeName = await consulClient.Agent.GetNodeName();
                foreach (var id in ids)
                {
                    for (var i = 0; i < 3; i++)
                    {
                        try
                        {
                            await consulClient.Agent.PassTTL(id, ndoeName);
                        }
                        catch (Exception exception)
                        {
                            logger.LogError(0, exception, $"pass TTL failure.id:{id},try count:{i + 1}");
                        }
                    }
                }
            }, null, TimeSpan.Zero, timeSpan);
        }

        public void AddHeartbeat(string serviceId)
        {
            lock (_checkids)
            {
                _checkids.Add("service:" + serviceId);
            }
        }

        public void RemoveHeartbeat(string serviceId)
        {
            lock (_checkids)
            {
                _checkids.Add("service:" + serviceId);
            }
        }

        #region IDisposable

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose()
        {
            _timer?.Dispose();
        }

        #endregion IDisposable
    }
}
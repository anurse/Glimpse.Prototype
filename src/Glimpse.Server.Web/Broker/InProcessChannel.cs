﻿using Glimpse.Agent;
using System;
using System.Threading.Tasks;

namespace Glimpse.Server
{
    public class InProcessChannel : IChannelSender
    {
        private readonly IServerBroker _messageBus;

        public InProcessChannel(IServerBroker messageBus)
        {
            _messageBus = messageBus;
        }

        public void PublishMessage(IMessage message)
        {
            _messageBus.SendMessage(message);
        }
    }
}
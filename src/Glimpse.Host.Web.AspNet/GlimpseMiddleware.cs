﻿using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Glimpse.Web;
using System;
using Microsoft.Framework.DependencyInjection;

namespace Glimpse.Host.Web.AspNet
{
    public class GlimpseMiddleware
    {
        private readonly RequestDelegate _innerNext;
        private readonly RequestRuntimeHost _runtime;
        private readonly ISettings _settings;
        private readonly IContextData<MessageContext> _contextData;

        public GlimpseMiddleware(RequestDelegate innerNext, IServiceProvider serviceProvider)
            : this(innerNext, serviceProvider, null)
        {
        }

        public GlimpseMiddleware(RequestDelegate innerNext, IServiceProvider serviceProvider, Func<IHttpContext, bool> shouldRun)
        {
            _innerNext = innerNext;

            var typeActivator = serviceProvider.GetService<ITypeActivator>();
             
            _runtime = typeActivator.CreateInstance<RequestRuntimeHost>(); 
            _contextData = new ContextData<MessageContext>();

            // TODO: Need to find a way/better place for 
            var settings = new Settings();
            if (shouldRun != null)
            {
                settings.ShouldProfile = context => shouldRun((HttpContext)context);
            }
            _settings = settings;
        }

        // TODO: Look at pushing the workings of this into MasterRequestRuntime
        public async Task Invoke(Microsoft.AspNet.Http.HttpContext context)
        {
            var newContext = new HttpContext(context, _settings);

            if (_runtime.Authorized(newContext))
            {
                // TODO: This is the wrong place for this, AgentRuntime isn't garenteed to execute first
                _contextData.Value = new MessageContext { Id = Guid.NewGuid(), Type = "Request" };

                _runtime.Begin(newContext);

                var handler = (IRequestHandler)null;
                if (_runtime.TryGetHandle(newContext, out handler))
                {
                    await handler.Handle(newContext);
                }
                else
                {
                    await _innerNext(context);
                }

                // TODO: This doesn't work correctly :( (headers)
                _runtime.End(newContext);
            }
            else
            {
                await _innerNext(context);
            }

            /*

            //TODO: Logic should probably be more like this.
            //      Problem is that WebCommon shouldn't know about Profile.
            //      Have to think about this more. Mayber there is a pattern here???

            var handeled = false;

            var handler = (IRequestHandler) null;
            if (_runtime.TryGetHandle(newContext, out handler)
                && _runtime.AuthorizedRequest(newContext))  // Is internal Glimpse request
            { 
                handeled = true;
                    
                await _runtime.HandleBegin(newContext); 
                await handler.Handle(newContext); 
                await _runtime.HandleEnd(newContext); 
            }
            else if (_runtime.IgnoredRequest(newContext)
            {
                handeled = true;
                    
                // TODO: This is the wrong place for this, AgentRuntime isn't garenteed to execute first
                _contextData.Value = new MessageContext {Id = Guid.NewGuid(), Type = "Request"};

                await _runtime.ProfileBegin(newContext); 
                await handler.Profile(newContext); 
                await _runtime.ProfileEnd(newContext); 
            }

            if (!handeled)
            { 
                await _innerNext(context);
            }
            */
        }
    }
}
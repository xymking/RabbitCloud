﻿using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rabbit.Cloud.Application.Abstractions;
using Rabbit.Cloud.Grpc.Fluent.ApplicationModels;
using Rabbit.Cloud.Grpc.Fluent.ApplicationModels.Internal;
using System;
using System.Threading.Tasks;

namespace Rabbit.Cloud.Server.Grpc
{
    public class ServerMethodInvokerFactory : IServerMethodInvokerFactory
    {
        private readonly GrpcServerOptions _options;
        private readonly DefaultServerMethodInvokerFactory _defaultServerMethodInvokerFactory;

        public ServerMethodInvokerFactory(IOptions<GrpcServerOptions> options, IServiceProvider services, ILogger<DefaultServerMethodInvoker> logger)
        {
            _options = options.Value;
            _defaultServerMethodInvokerFactory = new DefaultServerMethodInvokerFactory(services, logger);
        }

        #region Implementation of IServerMethodInvokerFactory

        public IServerMethodInvoker CreateInvoker(ServerMethodModel serverMethod)
        {
            var serverMethodInvoker = _defaultServerMethodInvokerFactory.CreateInvoker(serverMethod);
            return new GrpcServerMethodInvoker(serverMethodInvoker, _options.Invoker);
        }

        #endregion Implementation of IServerMethodInvokerFactory

        private class GrpcServerMethodInvoker : IServerMethodInvoker
        {
            private readonly IServerMethodInvoker _serverMethodInvoker;
            private readonly RabbitRequestDelegate _invoker;

            public GrpcServerMethodInvoker(IServerMethodInvoker serverMethodInvoker, RabbitRequestDelegate invoker)
            {
                _serverMethodInvoker = serverMethodInvoker;
                _invoker = invoker;
            }

            #region Implementation of IServerMethodInvoker

            public async Task<TResponse> UnaryServerMethod<TRequest, TResponse>(TRequest request, ServerCallContext callContext)
            {
                var context = new GrpcServerRabbitContext();
                context.Request.Request = request;
                context.Request.ServerCallContext = callContext;

                context.LogicInvoker = async () =>
                {
                    if (context.Response.Response != null)
                        return context.Response.Response;
                    return context.Response.Response =
                        await _serverMethodInvoker.UnaryServerMethod<TRequest, TResponse>(request, callContext);
                };

                await _invoker(context);

                context.Response.ResponseType = typeof(TResponse);
                return (TResponse)context.Response.Response;
            }

            public Task<TResponse> ClientStreamingServerMethod<TRequest, TResponse>(IAsyncStreamReader<TRequest> requestStream, ServerCallContext callContext)
            {
                throw new NotImplementedException();
            }

            public Task ServerStreamingServerMethod<TRequest, TResponse>(TRequest request, IServerStreamWriter<TResponse> responseStream,
                ServerCallContext callContext)
            {
                throw new NotImplementedException();
            }

            public Task DuplexStreamingServerMethod<TRequest, TResponse>(IAsyncStreamReader<TRequest> requestStream,
                IServerStreamWriter<TResponse> responseStream, ServerCallContext callContext)
            {
                throw new NotImplementedException();
            }

            #endregion Implementation of IServerMethodInvoker
        }
    }
}
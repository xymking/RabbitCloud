﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Rabbit.Cloud.Application;
using Rabbit.Cloud.Application.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Rabbit.Cloud.Client
{
    public class RabbitRequestMessage
    {
        public RabbitRequestMessage(Uri url, object body = null, IDictionary<string, StringValues> headers = null)
        {
            Scheme = url.Scheme;
            Host = url.Host;
            Path = url.PathAndQuery.Substring(0, url.PathAndQuery.Length - url.Query.Length);
            Port = url.Port;
            QueryString = url.Query;
            Body = body;
            Headers = headers != null ? new Dictionary<string, StringValues>(headers, StringComparer.OrdinalIgnoreCase) : new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase);
        }

        public RabbitRequestMessage()
        {
        }

        public string Scheme { get; set; }
        public string Host { get; set; }
        public string Path { get; set; }
        public int Port { get; set; }
        public string QueryString { get; set; }
        public IDictionary<string, StringValues> Headers { get; }
        public object Body { get; set; }
    }

    public class RabbitResponseMessage
    {
        public RabbitResponseMessage(IDictionary<string, StringValues> headers)
        {
            Headers = headers != null ? new Dictionary<string, StringValues>(headers, StringComparer.OrdinalIgnoreCase) : new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase);
        }

        public int StatusCode { get; set; }
        public object Body { get; set; }
        public IDictionary<string, StringValues> Headers { get; }
    }

    public interface IRabbitClient
    {
        Task<RabbitResponseMessage> SendAsync(RabbitRequestMessage request);
    }

    public class RabbitClient : IRabbitClient
    {
        private readonly RabbitRequestDelegate _requestDelegate;
        private readonly IServiceProvider _serviceProvider;

        public RabbitClient(RabbitRequestDelegate requestDelegate, IServiceProvider serviceProvider)
        {
            _requestDelegate = requestDelegate;
            _serviceProvider = serviceProvider;
        }

        #region Implementation of IRabbitClient

        public async Task<RabbitResponseMessage> SendAsync(RabbitRequestMessage request)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var rabbitContext = new RabbitContext
                {
                    RequestServices = scope.ServiceProvider
                };
                var rabbitRequest = rabbitContext.Request;
                rabbitRequest.Scheme = request.Scheme;
                rabbitRequest.Host = request.Host;
                rabbitRequest.Path = request.Path;
                rabbitRequest.Port = request.Port;
                rabbitRequest.QueryString = request.QueryString;
                rabbitRequest.Body = request.Body;

                await _requestDelegate(rabbitContext);

                var rabbitResponse = rabbitContext.Response;
                return new RabbitResponseMessage(rabbitResponse.Headers)
                {
                    Body = rabbitResponse.Body,
                    StatusCode = rabbitResponse.StatusCode
                };
            }
        }

        #endregion Implementation of IRabbitClient
    }

    public static class RabbitClientExtensions
    {
        public static async Task<TResponse> SendAsync<TRequest, TResponse>(this IRabbitClient rabbitClient, string url, TRequest request, IDictionary<string, StringValues> headers = null)
        {
            var response = await rabbitClient.SendAsync(new RabbitRequestMessage(new Uri(url), request, headers));
            return (TResponse)response.Body;
        }
    }
}
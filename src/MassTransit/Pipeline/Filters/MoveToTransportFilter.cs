// Copyright 2007-2015 Chris Patterson, Dru Sellers, Travis Smith, et. al.
//  
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
// 
//     http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software distributed
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.
namespace MassTransit.Pipeline.Filters
{
    using System;
    using System.Threading.Tasks;
    using Transports;


    /// <summary>
    /// Moves a message received to a transport without any deserialization
    /// </summary>
    public class MoveToTransportFilter :
        IFilter<ReceiveContext>
    {
        readonly Uri _destinationAddress;
        readonly Lazy<Task<ISendTransport>> _getDestinationTransport;
        readonly string _reason;

        public MoveToTransportFilter(Uri destinationAddress, Func<Task<ISendTransport>> getDestinationTransport, string reason)
        {
            _getDestinationTransport = new Lazy<Task<ISendTransport>>(getDestinationTransport);
            _destinationAddress = destinationAddress;
            _reason = reason ?? "Unspecified";
        }

        void IProbeSite.Probe(ProbeContext context)
        {
            ProbeContext scope = context.CreateFilterScope("move");
            scope.Add("destinationAddress", _destinationAddress);
        }

        async Task IFilter<ReceiveContext>.Send(ReceiveContext context, IPipe<ReceiveContext> next)
        {
            ISendTransport transport = await _getDestinationTransport.Value.ConfigureAwait(false);

            IPipe<SendContext> pipe = Pipe.Execute<SendContext>(sendContext =>
            {
                sendContext.Headers.Set(MessageHeaders.Reason, _reason);

                sendContext.SetHostHeaders();
            });

            await transport.Move(context, pipe).ConfigureAwait(false);

            await next.Send(context).ConfigureAwait(false);
        }
    }
}
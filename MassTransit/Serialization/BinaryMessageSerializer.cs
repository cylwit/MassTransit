// Copyright 2007-2008 The Apache Software Foundation.
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
namespace MassTransit.Serialization
{
	using System;
	using System.Collections.Generic;
	using System.IO;
    using System.Runtime.Remoting.Messaging;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Formatters.Binary;
	using Internal;
	using Util;

    /// <summary>
    /// The binary message serializer used the .NET BinaryFormatter to serialize
    /// message content. 
    /// </summary>
    public class BinaryMessageSerializer
        : IMessageSerializer
    {
		private static readonly BinaryFormatter _formatter = new BinaryFormatter();

        public void Serialize<T>(Stream output, T message)
        {
            Check.EnsureSerializable(message);

        	_formatter.Serialize(output, message, GetHeaders(message));
        }

    	private static Header[] GetHeaders<T>(T message)
    	{
			List<Header> headers = new List<Header>();

			var context = BusContext.Current.OutboundMessage();

			if(context.ReplyTo != null)
			{
				headers.Add(new Header(MessageEnvelopeHeaders.ReplyTo, context.ReplyTo));
			}

    		return headers.ToArray();
    	}

    	public object Deserialize(Stream input)
        {
            object obj = _formatter.Deserialize(input, DeserializeHeaderHandler);

            return obj;
        }

    	private static object DeserializeHeaderHandler(Header[] headers)
    	{
			if (headers == null)
				return null;

    		var context = BusContext.Current.InboundMessage();

    		for (int i = 0; i < headers.Length; i++)
    		{
				switch ( headers[i].Name)
				{
					case MessageEnvelopeHeaders.ReplyTo:
						context.SetReplyTo((Uri) headers[i].Value);
						break;
				}
    		}

    		return null;
    	}
    }

	public static class MessageEnvelopeHeaders
	{
		public const string ReplyTo = "ReplyTo";
	}
}
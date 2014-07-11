/* Copyright 2010-2014 MongoDB Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Internal;
using NUnit.Framework;

namespace MongoDB.Driver.Tests.Communication
{
    [TestFixture]
    public class MongoConnectionTests
    {
        [Test]
        public async Task TestAsynchronousOpenSendReceiveAndClose()
        {
            var mongoConnection = new MongoConnection(Configuration.TestServer.Instance);
            await mongoConnection.OpenAsync();

            Assert.AreEqual(MongoConnectionState.Open, mongoConnection.State, "Connection state should be Open");

            await mongoConnection.SendMessageAsync(new MongoQueryMessage(new BsonBinaryWriterSettings(), "admin.$cmd", QueryFlags.SlaveOk,
                mongoConnection.ServerInstance.MaxWireDocumentSize, 0, -1, new CommandDocument("ping", 1), null));
            var reply = await mongoConnection.ReceiveMessageAsync(new BsonBinaryReaderSettings(), BsonSerializer.LookupSerializer<CommandResult>());

            Assert.AreEqual(1, reply.NumberReturned, "Expecting exactly one document in reply to Ping command");
            Assert.IsTrue(reply.Documents[0].Ok, "Unexpected response received");

            await mongoConnection.CloseAsync();

            Assert.AreEqual(MongoConnectionState.Closed, mongoConnection.State, "Connection state should be Closed");
        }
    }
}

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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Communication.Security.Mechanisms;
using MongoDB.Driver.Internal;
using MongoDB.Driver.Operations;

namespace MongoDB.Driver.Communication.Security
{
    /// <summary>
    /// Authenticates credentials against MongoDB.
    /// </summary>
    internal class Authenticator
    {
        // private static fields
        private static readonly List<IAuthenticationProtocol> __clientSupportedProtocols = new List<IAuthenticationProtocol>
        {
            // when we start negotiating, MONGODB-CR should be moved to the bottom of the list...
            new MongoCRAuthenticationProtocol(),
            new X509AuthenticationProtocol(),
            new SaslAuthenticationProtocol(new GssapiMechanism()),
            new SaslAuthenticationProtocol(new PlainMechanism())
        };

        // private fields
        private readonly MongoConnection _connection;
        private readonly IEnumerable<MongoCredential> _credentials;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="Authenticator" /> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="credentials">The credentials.</param>
        public Authenticator(MongoConnection connection, IEnumerable<MongoCredential> credentials)
        {
            _connection = connection;
            _credentials = credentials;
        }

        // public methods
        /// <summary>
        /// Authenticates the specified connection.
        /// </summary>
        public void Authenticate()
        {
            if (!_credentials.Any())
            {
                return;
            }

            if (IsArbiter())
            {
                return;
            }

            foreach (var credential in _credentials)
            {
                Authenticate(credential);
            }
        }

        /// <summary>
        /// Authenticates the specified connection.
        /// </summary>
        public async Task AuthenticateAsync()
        {
            if (!_credentials.Any())
            {
                return;
            }

            if (await IsArbiterAsync().ConfigureAwait(false))
            {
                return;
            }

            foreach (var credential in _credentials)
            {
                await AuthenticateAsync(credential).ConfigureAwait(false);
            }
        }

        // private methods
        private void Authenticate(MongoCredential credential)
        {
            GetClientSupportedProtocol(credential).Authenticate(_connection, credential);
        }

        private Task AuthenticateAsync(MongoCredential credential)
        {
            return GetClientSupportedProtocol(credential).AuthenticateAsync(_connection, credential);
        }

        private static IAuthenticationProtocol GetClientSupportedProtocol(MongoCredential credential)
        {
            var clientSupportedProtocol = __clientSupportedProtocols.FirstOrDefault(protocol => protocol.CanUse(credential));

            if (clientSupportedProtocol != null)
                return clientSupportedProtocol;

            var message = string.Format("Unable to find a protocol to authenticate. The credential for source {0}, username {1} over mechanism {2} could not be authenticated.", credential.Source, credential.Username, credential.Mechanism);
            throw new MongoSecurityException(message);
        }

        private bool IsArbiter()
        {
            var operation = CreateIsMasterCommandOperation();
            var result = operation.Execute(_connection);
            return result.Response.GetValue("arbiterOnly", false).ToBoolean();
        }

        private async Task<bool> IsArbiterAsync()
        {
            var operation = CreateIsMasterCommandOperation();
            var result = await operation.ExecuteAsync(_connection).ConfigureAwait(false);
            return result.Response.GetValue("arbiterOnly", false).ToBoolean();
        }

        private static CommandOperation<CommandResult> CreateIsMasterCommandOperation()
        {
            var readerSettings = new BsonBinaryReaderSettings();
            var writerSettings = new BsonBinaryWriterSettings();
            var command = new CommandDocument("isMaster", true);
            var resultSerializer = BsonSerializer.LookupSerializer<CommandResult>();

            var commandOperation = new CommandOperation<CommandResult>(
                "admin", //databaseName
                readerSettings,
                writerSettings,
                command,
                QueryFlags.SlaveOk,
                null, // options
                null, // readPreference
                resultSerializer);
            return commandOperation;
        }
    }
}

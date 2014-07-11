﻿/* Copyright 2010-2014 MongoDB Inc.
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
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Internal;
using MongoDB.Driver.Operations;

namespace MongoDB.Driver.Communication.Security
{
    /// <summary>
    /// Authenticates a credential using the SASL protocol.
    /// </summary>
    internal class SaslAuthenticationProtocol : IAuthenticationProtocol
    {
        // private fields
        private readonly ISaslMechanism _mechanism;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="SaslAuthenticationProtocol" /> class.
        /// </summary>
        /// <param name="mechanism">The mechanism.</param>
        public SaslAuthenticationProtocol(ISaslMechanism mechanism)
        {
            _mechanism = mechanism;
        }

        // public properties
        /// <summary>
        /// Gets the name.
        /// </summary>
        public string Name
        {
            get { return _mechanism.Name; }
        }

        // public methods
        /// <summary>
        /// Authenticates the connection against the given database.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="credential">The credential.</param>
        public void Authenticate(MongoConnection connection, MongoCredential credential)
        {
            using (var conversation = new SaslConversation())
            {
                ISaslStep currentStep;
                var command = CreateSaslStartCommand(connection, credential, out currentStep);

                CommandResult result;
                do
                {
                    try
                    {
                        result = RunCommand(connection, credential.Source, command);
                    }
                    catch (MongoCommandException ex)
                    {
                        throw CreateMongoSecurityException(ex);
                    }
                } while (!CheckAuthorizationCompleted(result, conversation, ref currentStep, ref command));
            }
        }

        /// <summary>
        /// Asynchronously authenticates the connection against the given database.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="credential">The credential.</param>
        public async Task AuthenticateAsync(MongoConnection connection, MongoCredential credential)
        {
            using (var conversation = new SaslConversation())
            {
                ISaslStep currentStep;
                var command = CreateSaslStartCommand(connection, credential, out currentStep);

                CommandResult result;
                do
                {
                    try
                    {
                        result = await RunCommandAsync(connection, credential.Source, command).ConfigureAwait(false);
                    }
                    catch (MongoCommandException ex)
                    {
                        throw CreateMongoSecurityException(ex);
                    }
                } while (!CheckAuthorizationCompleted(result, conversation, ref currentStep, ref command));
            }
        }

        /// <summary>
        /// Determines whether this instance can use the specified credential.
        /// </summary>
        /// <param name="credential">The credential.</param>
        /// <returns>
        ///   <c>true</c> if this instance can use the specified credential; otherwise, <c>false</c>.
        /// </returns>
        public bool CanUse(MongoCredential credential)
        {
            return _mechanism.CanUse(credential);
        }

        // private methods
        private CommandDocument CreateSaslStartCommand(MongoConnection connection, MongoCredential credential,
            out ISaslStep currentStep)
        {
            currentStep = _mechanism.Initialize(connection, credential);

            var command = new CommandDocument
            {
                {"saslStart", 1},
                {"mechanism", _mechanism.Name},
                {"payload", currentStep.BytesToSendToServer}
            };
            return command;
        }
        
        private static MongoSecurityException CreateMongoSecurityException(MongoCommandException ex)
        {
            var message = "Unknown error occured during authentication.";
            var code = ex.CommandResult.Code;
            var errmsg = ex.CommandResult.ErrorMessage;
            if (code.HasValue && errmsg != null)
            {
                message = string.Format("Error: {0} - {1}", code, errmsg);
            }

            return new MongoSecurityException(message, ex);
        }

        private static bool CheckAuthorizationCompleted(CommandResult result, SaslConversation conversation,
            ref ISaslStep currentStep, ref CommandDocument command)
        {
            if (result.Response["done"].AsBoolean)
            {
                return true;
            }

            currentStep = currentStep.Transition(conversation, result.Response["payload"].AsByteArray);

            command = new CommandDocument
            {
                {"saslContinue", 1},
                {"conversationId", result.Response["conversationId"].AsInt32},
                {"payload", currentStep.BytesToSendToServer}
            };
            return false;
        }

        private static CommandOperation<CommandResult> CreateSaslCommandOperation(string databaseName, IMongoCommand command)
        {
            var readerSettings = new BsonBinaryReaderSettings();
            var writerSettings = new BsonBinaryWriterSettings();
            var resultSerializer = BsonSerializer.LookupSerializer<CommandResult>();

            var commandOperation = new CommandOperation<CommandResult>(
                databaseName,
                readerSettings,
                writerSettings,
                command,
                QueryFlags.SlaveOk,
                null, // options
                null, // readPreference
                resultSerializer);
            return commandOperation;
        }

        private static CommandResult RunCommand(MongoConnection connection, string databaseName, IMongoCommand command)
        {
            var commandOperation = CreateSaslCommandOperation(databaseName, command);

            return commandOperation.Execute(connection);
        }

        private static Task<CommandResult> RunCommandAsync(MongoConnection connection, string databaseName, IMongoCommand command)
        {
            var commandOperation = CreateSaslCommandOperation(databaseName, command);

            return commandOperation.ExecuteAsync(connection);
        }
    }
}
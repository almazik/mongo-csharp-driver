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
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Communication.Security;
using MongoDB.Driver.Operations;

namespace MongoDB.Driver.Internal
{
    /// <summary>
    /// Represents the state of a connection.
    /// </summary>
    public enum MongoConnectionState
    {
        /// <summary>
        /// The connection has not yet been initialized.
        /// </summary>
        Initial,
        /// <summary>
        /// The connection is open.
        /// </summary>
        Open,
        /// <summary>
        /// The connection is closed.
        /// </summary>
        Closed
    }

    /// <summary>
    /// Represents a connection to a MongoServerInstance.
    /// </summary>
    public class MongoConnection
    {
        // private fields
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);
        private readonly MongoServerInstance _serverInstance;
        private readonly MongoConnectionPool _connectionPool;
        private readonly int _generationId; // the generationId of the connection pool at the time this connection was created
        private MongoConnectionState _state;
        private TcpClient _tcpClient;
        private Stream _stream; // either a NetworkStream or an SslStream wrapping a NetworkStream
        private readonly DateTime _createdAt;
        private DateTime _lastUsedAt; // set every time the connection is Released
        private int _messageCounter;
        private int _requestId;

        // constructors
        internal MongoConnection(MongoConnectionPool connectionPool)
            : this(connectionPool.ServerInstance)
        {
            _connectionPool = connectionPool;
            _generationId = connectionPool.GenerationId;
        }

        internal MongoConnection(MongoServerInstance serverInstance)
        {
            _serverInstance = serverInstance;
            _createdAt = DateTime.UtcNow;
            _state = MongoConnectionState.Initial;
        }

        // public properties
        /// <summary>
        /// Gets the connection pool that this connection belongs to.
        /// </summary>
        public MongoConnectionPool ConnectionPool
        {
            get { return _connectionPool; }
        }

        /// <summary>
        /// Gets the DateTime that this connection was created at.
        /// </summary>
        public DateTime CreatedAt
        {
            get { return _createdAt; }
        }

        /// <summary>
        /// Gets the generation of the connection pool that this connection belongs to.
        /// </summary>
        public int GenerationId
        {
            get { return _generationId; }
        }

        /// <summary>
        /// Gets the DateTime that this connection was last used at.
        /// </summary>
        public DateTime LastUsedAt
        {
            get { return _lastUsedAt; }
            internal set { _lastUsedAt = value; }
        }

        /// <summary>
        /// Gets a count of the number of messages that have been sent using this connection.
        /// </summary>
        public int MessageCounter
        {
            get { return _messageCounter; }
        }

        /// <summary>
        /// Gets the RequestId of the last message sent on this connection.
        /// </summary>
        public int RequestId
        {
            get { return _requestId; }
        }

        /// <summary>
        /// Gets the server instance this connection is connected to.
        /// </summary>
        public MongoServerInstance ServerInstance
        {
            get { return _serverInstance; }
        }

        /// <summary>
        /// Gets the state of this connection.
        /// </summary>
        public MongoConnectionState State
        {
            get { return _state; }
        }

        // internal methods
        internal void Close()
        {
            _semaphore.Wait();
            try
            {
                CloseWithLockAcquired();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        internal async Task CloseAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                CloseWithLockAcquired();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Closes the connection assuming that lock for critical section has already been acquired. Is needed to avoid nested locks as SemaphoreSlim doesn't have such functionality.
        /// </summary>
        private void CloseWithLockAcquired()
        {
            if (_state != MongoConnectionState.Closed)
            {
                if (_stream != null)
                {
                    try { _stream.Close(); }
                    catch { } // ignore exceptions
                    _stream = null;
                }
                if (_tcpClient != null)
                {
                    if (_tcpClient.Connected)
                    {
                        // even though MSDN says TcpClient.Close doesn't close the underlying socket
                        // it actually does (as proven by disassembling TcpClient and by experimentation)
                        try { _tcpClient.Close(); }
                        catch { } // ignore exceptions
                    }
                    _tcpClient = null;
                }
                _state = MongoConnectionState.Closed;
            }
        }

        internal bool IsExpired()
        {
            var now = DateTime.UtcNow;
            return now > _createdAt + _serverInstance.Settings.MaxConnectionLifeTime
                || now > _lastUsedAt + _serverInstance.Settings.MaxConnectionIdleTime;
        }

        internal void Open()
        {
            if (_state != MongoConnectionState.Initial)
            {
                throw new InvalidOperationException("Open called more than once.");
            }

            var ipEndPoint = _serverInstance.GetIPEndPoint();
            var tcpClient = new TcpClient(ipEndPoint.AddressFamily);
            tcpClient.NoDelay = true; // turn off Nagle
            tcpClient.ReceiveBufferSize = MongoDefaults.TcpReceiveBufferSize;
            tcpClient.SendBufferSize = MongoDefaults.TcpSendBufferSize;
            tcpClient.Connect(ipEndPoint);

            var stream = (Stream)tcpClient.GetStream();
            if (_serverInstance.Settings.UseSsl)
            {
                var checkCertificateRevocation = true;
                var clientCertificateCollection = (X509CertificateCollection)null;
                var clientCertificateSelectionCallback = (LocalCertificateSelectionCallback)null;
                var enabledSslProtocols = SslProtocols.Default;
                var serverCertificateValidationCallback = (RemoteCertificateValidationCallback)null;

                var sslSettings = _serverInstance.Settings.SslSettings;
                if (sslSettings != null)
                {
                    checkCertificateRevocation = sslSettings.CheckCertificateRevocation;
                    clientCertificateCollection = sslSettings.ClientCertificateCollection;
                    clientCertificateSelectionCallback = sslSettings.ClientCertificateSelectionCallback;
                    enabledSslProtocols = sslSettings.EnabledSslProtocols;
                    serverCertificateValidationCallback = sslSettings.ServerCertificateValidationCallback;
                }

                if (serverCertificateValidationCallback == null && !_serverInstance.Settings.VerifySslCertificate)
                {
                    serverCertificateValidationCallback = AcceptAnyCertificate;
                }

                var sslStream = new SslStream(stream, false, serverCertificateValidationCallback, clientCertificateSelectionCallback);
                try
                {
                    var targetHost = _serverInstance.Address.Host;
                    sslStream.AuthenticateAsClient(targetHost, clientCertificateCollection, enabledSslProtocols, checkCertificateRevocation);
                }
                catch
                {
                    try { stream.Close(); }
                    catch { } // ignore exceptions
                    try { tcpClient.Close(); }
                    catch { } // ignore exceptions
                    throw;
                }
                stream = sslStream;
            }

            _tcpClient = tcpClient;
            _stream = stream;
            _state = MongoConnectionState.Open;

            new Authenticator(this, _serverInstance.Settings.Credentials)
                .Authenticate();
        }

        internal async Task OpenAsync()
        {
            if (_state != MongoConnectionState.Initial)
            {
                throw new InvalidOperationException("Open called more than once.");
            }

            var ipEndPoint = _serverInstance.GetIPEndPoint();
            var tcpClient = new TcpClient(ipEndPoint.AddressFamily)
            {
                NoDelay = true,
                ReceiveBufferSize = MongoDefaults.TcpReceiveBufferSize,
                SendBufferSize = MongoDefaults.TcpSendBufferSize
            };
            await tcpClient.ConnectAsync(ipEndPoint.Address, ipEndPoint.Port).ConfigureAwait(false);

            var stream = (Stream)tcpClient.GetStream();
            if (_serverInstance.Settings.UseSsl)
            {
                var checkCertificateRevocation = true;
                var clientCertificateCollection = (X509CertificateCollection)null;
                var clientCertificateSelectionCallback = (LocalCertificateSelectionCallback)null;
                var enabledSslProtocols = SslProtocols.Default;
                var serverCertificateValidationCallback = (RemoteCertificateValidationCallback)null;

                var sslSettings = _serverInstance.Settings.SslSettings;
                if (sslSettings != null)
                {
                    checkCertificateRevocation = sslSettings.CheckCertificateRevocation;
                    clientCertificateCollection = sslSettings.ClientCertificateCollection;
                    clientCertificateSelectionCallback = sslSettings.ClientCertificateSelectionCallback;
                    enabledSslProtocols = sslSettings.EnabledSslProtocols;
                    serverCertificateValidationCallback = sslSettings.ServerCertificateValidationCallback;
                }

                if (serverCertificateValidationCallback == null && !_serverInstance.Settings.VerifySslCertificate)
                {
                    serverCertificateValidationCallback = AcceptAnyCertificate;
                }

                var sslStream = new SslStream(stream, false, serverCertificateValidationCallback, clientCertificateSelectionCallback);
                try
                {
                    var targetHost = _serverInstance.Address.Host;
                    await sslStream.AuthenticateAsClientAsync(targetHost, clientCertificateCollection, enabledSslProtocols, checkCertificateRevocation).ConfigureAwait(false);
                }
                catch
                {
                    try { stream.Close(); }
                    catch { } // ignore exceptions
                    try { tcpClient.Close(); }
                    catch { } // ignore exceptions
                    throw;
                }
                stream = sslStream;
            }

            _tcpClient = tcpClient;
            _stream = stream;
            _state = MongoConnectionState.Open;

            await new Authenticator(this, _serverInstance.Settings.Credentials)
                .AuthenticateAsync().ConfigureAwait(false);
        }

        internal MongoReplyMessage<TDocument> ReceiveMessage<TDocument>(
            BsonBinaryReaderSettings readerSettings,
            IBsonSerializer<TDocument> serializer)
        {
            if (_state == MongoConnectionState.Closed) { throw new InvalidOperationException("Connection is closed."); }
            _semaphore.Wait();
            try
            {
                _lastUsedAt = DateTime.UtcNow;
                var networkStream = GetNetworkStream();
                var readTimeout = (int)_serverInstance.Settings.SocketTimeout.TotalMilliseconds;
                if (readTimeout != 0)
                {
                    networkStream.ReadTimeout = readTimeout;
                }

                using (var byteBuffer = ByteBufferFactory.LoadLengthPrefixedDataFrom(networkStream))
                using (var stream = new ByteBufferStream(byteBuffer, ownsByteBuffer: true))
                {
                    var reply = new MongoReplyMessage<TDocument>(readerSettings, serializer);
                    reply.ReadFrom(stream);
                    return reply;
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
                throw;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        internal async Task<MongoReplyMessage<TDocument>> ReceiveMessageAsync<TDocument>(
            BsonBinaryReaderSettings readerSettings,
            IBsonSerializer<TDocument> serializer)
        {
            if (_state == MongoConnectionState.Closed) { throw new InvalidOperationException("Connection is closed."); }

            await _semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                _lastUsedAt = DateTime.UtcNow;
                var networkStream = await GetNetworkStreamAsync().ConfigureAwait(false);
                var readTimeout = (int)_serverInstance.Settings.SocketTimeout.TotalMilliseconds;
                if (readTimeout != 0)
                {
                    networkStream.ReadTimeout = readTimeout;
                }

                using (var byteBuffer = await ByteBufferFactory.LoadLengthPrefixedDataFromAsync(networkStream).ConfigureAwait(false))
                using (var stream = new ByteBufferStream(byteBuffer, ownsByteBuffer: true))
                {
                    var reply = new MongoReplyMessage<TDocument>(readerSettings, serializer);
                    reply.ReadFrom(stream);
                    return reply;
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
                throw;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        internal void SendMessage(Stream stream, int requestId)
        {
            if (_state == MongoConnectionState.Closed) { throw new InvalidOperationException("Connection is closed."); }
            _semaphore.Wait();
            try
            {
                try
                {
                    _lastUsedAt = DateTime.UtcNow;
                    _requestId = requestId;

                    var networkStream = GetNetworkStream();
                    var writeTimeout = (int)_serverInstance.Settings.SocketTimeout.TotalMilliseconds;
                    if (writeTimeout != 0)
                    {
                        networkStream.WriteTimeout = writeTimeout;
                    }
                    stream.Position = 0;
                    stream.CopyTo(networkStream);
                    _messageCounter++;
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
                throw;
            }
        }

        internal async Task SendMessageAsync(Stream stream, int requestId)
        {
            if (_state == MongoConnectionState.Closed) { throw new InvalidOperationException("Connection is closed."); }

            //Have to use manual Monitor.Enter/Monitor.Exit as await does not work inside lock() statement
            await _semaphore.WaitAsync().ConfigureAwait(false);
            _lastUsedAt = DateTime.UtcNow;
            _requestId = requestId;
            try
            {
                var networkStream = await GetNetworkStreamAsync().ConfigureAwait(false);
                var writeTimeout = (int)_serverInstance.Settings.SocketTimeout.TotalMilliseconds;
                if (writeTimeout != 0)
                {
                    networkStream.WriteTimeout = writeTimeout;
                }
                stream.Position = 0;
                await stream.CopyToAsync(networkStream).ConfigureAwait(false);
                _messageCounter++;
            }
            catch (Exception ex)
            {
                HandleException(ex);
                throw;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        internal void SendMessage(MongoRequestMessage message)
        {
            using (var stream = new MemoryStream())
            {
                message.WriteTo(stream);
                SendMessage(stream, message.RequestId);
            }
        }

        internal async Task SendMessageAsync(MongoRequestMessage message)
        {
            using (var stream = new MemoryStream())
            {
                message.WriteTo(stream);
                await SendMessageAsync(stream, message.RequestId).ConfigureAwait(false);
            }
        }

        // private methods
        private bool AcceptAnyCertificate(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors
        )
        {
            return true;
        }

        private Stream GetNetworkStream()
        {
            if (_state == MongoConnectionState.Initial)
            {
                Open();
            }
            return _stream;
        }

        private async Task<Stream> GetNetworkStreamAsync()
        {
            if (_state == MongoConnectionState.Initial)
            {
                await OpenAsync().ConfigureAwait(false);
            }
            return _stream;
        }

        private void HandleException(Exception ex)
        {
            // there are three possible situations:
            // 1. we can keep using the connection
            // 2. just this one connection needs to be closed
            // 3. the whole connection pool needs to be cleared

            switch (DetermineAction(ex))
            {
                case HandleExceptionAction.KeepConnection:
                    break;
                case HandleExceptionAction.CloseConnection:
                    CloseWithLockAcquired(); //HandleException is always called from within critical section, so we don't acquire the lock
                    break;
                case HandleExceptionAction.ClearConnectionPool:
                    CloseWithLockAcquired();
                    if (_connectionPool != null)
                    {
                        _connectionPool.Clear(); //For now will use synchronous method, given that DetermineAction always returns CloseConnection
                    }
                    break;
                default:
                    throw new MongoInternalException("Invalid HandleExceptionAction");
            }

            _serverInstance.RefreshStateAsSoonAsPossible();
        }

        private enum HandleExceptionAction
        {
            KeepConnection,
            CloseConnection,
            ClearConnectionPool
        }

        private HandleExceptionAction DetermineAction(Exception ex)
        {
            // TODO: figure out when to return KeepConnection or ClearConnectionPool (if ever)

            // don't return ClearConnectionPool unless you are *sure* it is the right action
            // definitely don't make ClearConnectionPool the default action
            // returning ClearConnectionPool frequently can result in Connect/Disconnect storms

            return HandleExceptionAction.CloseConnection; // this should always be the default action
        }
    }
}

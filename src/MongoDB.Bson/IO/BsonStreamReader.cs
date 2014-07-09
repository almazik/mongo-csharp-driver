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

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MongoDB.Bson.IO
{
    /// <summary>
    /// Represents a reader that reads primitive BSON types from a Stream.
    /// </summary>
    public class BsonStreamReader
    {
        // private static fields
        private static readonly bool[] __validBsonTypes = new bool[256];

        // private fields
        private readonly Stream _stream;
        private readonly IBsonStream _bsonStream;
        private readonly UTF8Encoding _encoding;
        private readonly byte[] _buffer = new byte[32];

        // static constructor
        static BsonStreamReader()
        {
            foreach (BsonType bsonType in Enum.GetValues(typeof(BsonType)))
            {
                __validBsonTypes[(byte)bsonType] = true;
            }
        }

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="BsonStreamReader" /> class.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="encoding">The encoding.</param>
        /// <exception cref="System.ArgumentNullException">stream</exception>
        public BsonStreamReader(Stream stream, UTF8Encoding encoding)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }
            if (encoding == null)
            {
                throw new ArgumentNullException("encoding");
            }

            _stream = stream;
            _bsonStream = stream as IBsonStream;
            _encoding = encoding;
        }

        // public properties
        /// <summary>
        /// Gets the base stream.
        /// </summary>
        /// <value>
        /// The base stream.
        /// </value>
        public Stream BaseStream
        {
            get { return _stream; }
        }

        /// <summary>
        /// Gets or sets the position.
        /// </summary>
        /// <value>
        /// The position.
        /// </value>
        public long Position
        {
            get { return _stream.Position; }
            set { _stream.Position = value; }
        }

        // public methods
        /// <summary>
        /// Reads a BSON boolean from the stream.
        /// </summary>
        /// <returns>A bool.</returns>
        /// <exception cref="System.ArgumentNullException">stream</exception>
        /// <exception cref="System.IO.EndOfStreamException">Thrown if stream is empty.</exception>
        public bool ReadBoolean()
        {
            var b = _stream.ReadByte();
            if (b == -1)
            {
                throw new EndOfStreamException();
            }

            return b != 0;
        }

        // public methods
        /// <summary>
        /// Reads a BSON boolean from the stream.
        /// </summary>
        /// <returns>A bool.</returns>
        /// <exception cref="System.ArgumentNullException">stream</exception>
        /// <exception cref="System.IO.EndOfStreamException">Thrown if stream is empty.</exception>
        public async Task<bool> ReadBooleanAsync()
        {
            var b = await ReadByteAsync().ConfigureAwait(false);
            return b != 0;
        }

        /// <summary>
        /// Reads a BSON type code from the stream.
        /// </summary>
        /// <returns>A BsonType.</returns>
        /// <exception cref="System.ArgumentNullException">stream</exception>
        /// <exception cref="System.IO.EndOfStreamException">Thrown if stream is empty.</exception>
        /// <exception cref="System.FormatException"></exception>
        public BsonType ReadBsonType()
        {
            var b = ReadByte();
            if (b == -1)
            {
                throw new EndOfStreamException();
            }
            if (!__validBsonTypes[b])
            {
                string message = string.Format("Invalid BsonType: {0}.", b);
                throw new FormatException(message);
            }

            return (BsonType)b;
        }

        /// <summary>
        /// Asynchronously reads a BSON type code from the stream.
        /// </summary>
        /// <returns>A BsonType.</returns>
        /// <exception cref="System.ArgumentNullException">stream</exception>
        /// <exception cref="System.IO.EndOfStreamException">Thrown if stream is empty.</exception>
        /// <exception cref="System.FormatException"></exception>
        public async Task<BsonType> ReadBsonTypeAsync()
        {
            var b = await ReadByteAsync().ConfigureAwait(false);
            if (!__validBsonTypes[b])
            {
                string message = string.Format("Invalid BsonType: {0}.", b);
                throw new FormatException(message);
            }

            return (BsonType)b;
        }

        /// <summary>
        /// Reads a BSON CString from the stream.
        /// </summary>
        /// <returns>A string.</returns>
        public string ReadCString()
        {
            var utf8 = ReadCStringBytes();
            return Utf8Helper.DecodeUtf8String(utf8.Array, utf8.Offset, utf8.Count, Utf8Helper.StrictUtf8Encoding);
        }

        /// <summary>
        /// Asynchronously reads a BSON CString from the stream.
        /// </summary>
        /// <returns>A string.</returns>
        public async Task<string> ReadCStringAsync()
        {
            var utf8 = await ReadCStringBytesAsync().ConfigureAwait(false);
            return Utf8Helper.DecodeUtf8String(utf8.Array, utf8.Offset, utf8.Count, Utf8Helper.StrictUtf8Encoding);
        }

        /// <summary>
        /// Reads a BSON CString from the stream.
        /// </summary>
        /// <returns>An ArraySegment containing the CString bytes (without the null byte).</returns>
        public ArraySegment<byte> ReadCStringBytes()
        {
            if (_bsonStream != null)
            {
                return _bsonStream.ReadBsonCStringBytes();
            }
            else
            {
                var memoryStream = new MemoryStream(32); // override default capacity of zero

                while (true)
                {
                    var b = _stream.ReadByte();
                    if (b == -1)
                    {
                        throw new EndOfStreamException();
                    }
                    if (b == 0)
                    {
                        return new ArraySegment<byte>(memoryStream.GetBuffer(), 0, (int)memoryStream.Length); // without the null byte
                    }
                    memoryStream.WriteByte((byte)b);
                }
            }
        }

        /// <summary>
        /// Asynchronously reads a BSON CString from the stream.
        /// </summary>
        /// <returns>An ArraySegment containing the CString bytes (without the null byte).</returns>
        public async Task<ArraySegment<byte>> ReadCStringBytesAsync()
        {
            if (_bsonStream != null)
            {
                return _bsonStream.ReadBsonCStringBytes();
            }
            else
            {
                var memoryStream = new MemoryStream(32); // override default capacity of zero

                while (true)
                {
                    await FillBufferAsync(1).ConfigureAwait(false);
                    var b = _buffer[0];
                    if (b == 0)
                    {
                        return new ArraySegment<byte>(memoryStream.GetBuffer(), 0, (int)memoryStream.Length); // without the null byte
                    }
                    memoryStream.WriteByte(b);
                }
            }
        }

        /// <summary>
        /// Reads a BSON double from the stream.
        /// </summary>
        /// <returns>A double.</returns>
        /// <exception cref="System.ArgumentNullException">stream</exception>
        public double ReadDouble()
        {
            if (_bsonStream != null)
            {
                return _bsonStream.ReadBsonDouble();
            }
            else
            {
                FillBuffer(8);
                return BitConverter.ToDouble(_buffer, 0);
            }
        }

        /// <summary>
        /// Asynchronously reads a BSON double from the stream.
        /// </summary>
        /// <returns>A double.</returns>
        /// <exception cref="System.ArgumentNullException">stream</exception>
        public async Task<double> ReadDoubleAsync()
        {
            if (_bsonStream != null)
            {
                return _bsonStream.ReadBsonDouble();
            }
            else
            {
                await FillBufferAsync(8).ConfigureAwait(false);
                return BitConverter.ToDouble(_buffer, 0);
            }
        }

        /// <summary>
        /// Reads a 32-bit BSON integer from the stream.
        /// </summary>
        /// <returns>An int.</returns>
        /// <exception cref="System.ArgumentNullException">stream</exception>
        public int ReadInt32()
        {
            if (_bsonStream != null)
            {
                return _bsonStream.ReadBsonInt32();
            }
            else
            {
                FillBuffer(4);
                return _buffer[0] | (_buffer[1] << 8) | (_buffer[2] << 16) | (_buffer[3] << 24);
            }
        }

        /// <summary>
        /// Asynchronously reads a 32-bit BSON integer from the stream.
        /// </summary>
        /// <returns>An int.</returns>
        /// <exception cref="System.ArgumentNullException">stream</exception>
        public async Task<int> ReadInt32Async()
        {
            if (_bsonStream != null)
            {
                return _bsonStream.ReadBsonInt32();
            }
            else
            {
                await FillBufferAsync(4).ConfigureAwait(false);
                return _buffer[0] | (_buffer[1] << 8) | (_buffer[2] << 16) | (_buffer[3] << 24);
            }
        }

        /// <summary>
        /// Reads a 64-bit BSON integer from the stream.
        /// </summary>
        /// <returns>A long.</returns>
        /// <exception cref="System.ArgumentNullException">stream</exception>
        public long ReadInt64()
        {
            if (_bsonStream != null)
            {
                return _bsonStream.ReadBsonInt64();
            }
            else
            {
                FillBuffer(8);
                var lo = (uint)(_buffer[0] | (_buffer[1] << 8) | (_buffer[2] << 16) | (_buffer[3] << 24));
                var hi = (uint)(_buffer[4] | (_buffer[5] << 8) | (_buffer[6] << 16) | (_buffer[7] << 24));
                return (long)(((ulong)hi << 32) | (ulong)lo);
            }
        }

        /// <summary>
        /// Asynchronously reads a 64-bit BSON integer from the stream.
        /// </summary>
        /// <returns>A long.</returns>
        /// <exception cref="System.ArgumentNullException">stream</exception>
        public async Task<long> ReadInt64Async()
        {
            if (_bsonStream != null)
            {
                return _bsonStream.ReadBsonInt64();
            }
            else
            {
                await FillBufferAsync(8).ConfigureAwait(false);
                var lo = (uint)(_buffer[0] | (_buffer[1] << 8) | (_buffer[2] << 16) | (_buffer[3] << 24));
                var hi = (uint)(_buffer[4] | (_buffer[5] << 8) | (_buffer[6] << 16) | (_buffer[7] << 24));
                return (long)(((ulong)hi << 32) | (ulong)lo);
            }
        }

        /// <summary>
        /// Reads a BSON ObjectId from the stream.
        /// </summary>
        /// <returns>An ObjectId.</returns>
        /// <exception cref="System.ArgumentNullException">stream</exception>
        public ObjectId ReadObjectId()
        {
            if (_bsonStream != null)
            {
                return _bsonStream.ReadBsonObjectId();
            }
            else
            {
                FillBuffer(12);
                return new ObjectId(_buffer, 0);
            }
        }

        /// <summary>
        /// Asynchronously reads a BSON ObjectId from the stream.
        /// </summary>
        /// <returns>An ObjectId.</returns>
        /// <exception cref="System.ArgumentNullException">stream</exception>
        public async Task<ObjectId> ReadObjectIdAsync()
        {
            if (_bsonStream != null)
            {
                return _bsonStream.ReadBsonObjectId();
            }
            else
            {
                await FillBufferAsync(12).ConfigureAwait(false);
                return new ObjectId(_buffer, 0);
            }
        }

        /// <summary>
        /// Reads a BSON string from the stream.
        /// </summary>
        /// 
        /// <returns>A string.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// stream
        /// or
        /// encoding
        /// </exception>
        /// <exception cref="System.FormatException">
        /// String is missing null terminator byte.
        /// </exception>
        public string ReadString()
        {
            if (_bsonStream != null)
            {
                return _bsonStream.ReadBsonString(_encoding);
            }
            else
            {
                var length = ReadInt32();
                if (length < 1)
                {
                    var message = string.Format("Invalid string length: {0}.", length);
                    throw new FormatException(message);
                }

                var bytes = ReadBytes(length); // read the null byte also (included in length)
                if (bytes[length - 1] != 0)
                {
                    throw new FormatException("String is missing terminating null byte.");
                }

                return Utf8Helper.DecodeUtf8String(bytes, 0, length - 1, _encoding); // don't decode the null byte
            }
        }

        /// <summary>
        /// Asynchronously reads a BSON string from the stream.
        /// </summary>
        /// 
        /// <returns>A string.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// stream
        /// or
        /// encoding
        /// </exception>
        /// <exception cref="System.FormatException">
        /// String is missing null terminator byte.
        /// </exception>
        public async Task<string> ReadStringAsync()
        {
            if (_bsonStream != null)
            {
                return _bsonStream.ReadBsonString(_encoding);
            }
            else
            {
                var length = await ReadInt32Async().ConfigureAwait(false);
                if (length < 1)
                {
                    var message = string.Format("Invalid string length: {0}.", length);
                    throw new FormatException(message);
                }

                var bytes = await ReadBytesAsync(length).ConfigureAwait(false); // read the null byte also (included in length)
                if (bytes[length - 1] != 0)
                {
                    throw new FormatException("String is missing terminating null byte.");
                }

                return Utf8Helper.DecodeUtf8String(bytes, 0, length - 1, _encoding); // don't decode the null byte
            }
        }

        /// <summary>
        /// Reads a byte.
        /// </summary>
        /// <returns>A byte that was read from the stream, or <c>-1</c> if the stream is empty.</returns>
        public int ReadByte()
        {
            return _stream.ReadByte();
        }

        /// <summary>
        /// Asynchronously reads a byte.
        /// </summary>
        /// <returns>A byte.</returns>
        /// <exception cref="System.IO.EndOfStreamException">Thrown if stream is empty.</exception>
        /// <remarks>Note that behavior at the end of the stream is different to <see cref="ReadByte"/> method.</remarks>
        public async Task<int> ReadByteAsync()
        {
            await FillBufferAsync(1).ConfigureAwait(false);
            return _buffer[0];
        }

        /// <summary>
        /// Reads bytes from the stream and stores them in an existing buffer.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset in the buffer at which to start storing the bytes being read.</param>
        /// <param name="count">The count.</param>
        /// <exception cref="System.ArgumentNullException">stream</exception>
        /// <exception cref="System.ArgumentException">Thrown if <paramref name="count"/> is negative.</exception>
        /// <exception cref="System.IO.EndOfStreamException">Thrown if not enough bytes are available.</exception>
        public void ReadBytes(byte[] buffer, int offset, int count)
        {
            if (count < 0)
            {
                throw new ArgumentException("Count cannot be negative.", "count");
            }

            while (count > 0)
            {
                var read = _stream.Read(buffer, offset, count);
                if (read == 0)
                {
                    throw new EndOfStreamException();
                }
                offset += read;
                count -= read;
            }
        }

        /// <summary>
        /// Reads bytes from the stream.
        /// </summary>
        /// <param name="count">The count.</param>
        /// <returns>A byte array.</returns>
        /// <exception cref="System.ArgumentNullException">stream</exception>
        /// <exception cref="System.ArgumentException">Thrown if <paramref name="count"/> is negative.</exception>
        /// <exception cref="System.IO.EndOfStreamException">Thrown if not enough bytes are available.</exception>
        public byte[] ReadBytes(int count)
        {
            if (count < 0)
            {
                throw new ArgumentException("Count cannot be negative.", "count");
            }

            var bytes = new byte[count];
            ReadBytes(bytes, 0, count);
            return bytes;
        }

        /// <summary>
        /// Asynchronously reads bytes from the stream and stores them in an existing buffer.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset in the buffer at which to start storing the bytes being read.</param>
        /// <param name="count">The count.</param>
        /// <exception cref="System.ArgumentNullException">stream</exception>
        /// <exception cref="System.ArgumentException">Thrown if <paramref name="count"/> is negative.</exception>
        /// <exception cref="System.IO.EndOfStreamException">Thrown if not enough bytes are available.</exception>
        public async Task ReadBytesAsync(byte[] buffer, int offset, int count)
        {
            if (count < 0)
            {
                throw new ArgumentException("Count cannot be negative.", "count");
            }

            while (count > 0)
            {
                var read = await _stream.ReadAsync(buffer, offset, count).ConfigureAwait(false);
                if (read == 0)
                {
                    throw new EndOfStreamException();
                }
                offset += read;
                count -= read;
            }
        }

        /// <summary>
        /// Asynchronously reads bytes from the stream.
        /// </summary>
        /// <param name="count">The count.</param>
        /// <returns>A byte array.</returns>
        /// <exception cref="System.ArgumentNullException">stream</exception>
        /// <exception cref="System.ArgumentException">Thrown if <paramref name="count"/> is negative.</exception>
        /// <exception cref="System.IO.EndOfStreamException">Thrown if not enough bytes are available.</exception>
        public async Task<byte[]> ReadBytesAsync(int count)
        {
            if (count < 0)
            {
                throw new ArgumentException("Count cannot be negative.", "count");
            }

            var bytes = new byte[count];
            await ReadBytesAsync(bytes, 0, count).ConfigureAwait(false);
            return bytes;
        }

        /// <summary>
        /// Skips over a BSON CString positioning the stream to just after the terminating null byte.
        /// </summary>
        /// <exception cref="System.ArgumentNullException">stream</exception>
        public void SkipCString()
        {
            if (_bsonStream != null)
            {
                _bsonStream.SkipBsonCString();
            }
            else
            {
                while (true)
                {
                    var b = _stream.ReadByte();
                    if (b == -1)
                    {
                        throw new EndOfStreamException();
                    }
                    else if (b == 0)
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Asynchronously skips over a BSON CString positioning the stream to just after the terminating null byte.
        /// </summary>
        /// <exception cref="System.ArgumentNullException">stream</exception>
        public async Task SkipCStringAsync()
        {
            if (_bsonStream != null)
            {
                _bsonStream.SkipBsonCString();
            }
            else
            {
                while (true)
                {
                    await FillBufferAsync(1).ConfigureAwait(false);
                    if (_buffer[0] == 0)
                    {
                        break;
                    }
                }
            }
        }

        // private methods
        private void FillBuffer(int count)
        {
            if (count == 1)
            {
                var b = _stream.ReadByte();
                if (b == -1)
                {
                    throw new EndOfStreamException();
                }
                _buffer[0] = (byte)b;
            }
            else
            {
                var offset = 0;
                do
                {
                    var read = _stream.Read(_buffer, offset, count - offset);
                    if (read == 0)
                    {
                        throw new EndOfStreamException();
                    }
                    offset += read;
                }
                while (offset < count);
            }
        }

        // private methods
        private async Task FillBufferAsync(int count)
        {
            var offset = 0;
            do
            {
                var read = await _stream.ReadAsync(_buffer, offset, count - offset).ConfigureAwait(false);
                if (read == 0)
                {
                    throw new EndOfStreamException();
                }
                offset += read;
            }
            while (offset < count);
        }
    }
}

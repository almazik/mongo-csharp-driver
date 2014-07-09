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
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson.IO;
using NUnit.Framework;

namespace MongoDB.Bson.Tests.IO
{
    [TestFixture]
    public class BsonStreamWriterTests
    {
        private MemoryStream _memoryStream;
        private BsonStreamWriter _bsonStreamWriter;

        [SetUp]
        public void Setup()
        {
            _memoryStream = new MemoryStream();
            _bsonStreamWriter = new BsonStreamWriter(_memoryStream, Utf8Helper.StrictUtf8Encoding);
        }

        [Test]
        public void TestWriteBoolean()
        {
            _bsonStreamWriter.WriteBoolean(true);
            _bsonStreamWriter.WriteBoolean(false);
            CollectionAssert.AreEqual(new byte[] { 01, 00 }, _memoryStream.ToArray(), "Booleans were written incorrectly");
            Assert.AreEqual(2, _bsonStreamWriter.Position, "Position is incorrect");
        }

        [Test]
        public async Task TestWriteBooleanAsync()
        {
            await _bsonStreamWriter.WriteBooleanAsync(true);
            await _bsonStreamWriter.WriteBooleanAsync(false);
            CollectionAssert.AreEqual(new byte[] { 01, 00 }, _memoryStream.ToArray(), "Booleans were written incorrectly");
            Assert.AreEqual(2, _bsonStreamWriter.Position, "Position is incorrect");
        }

        [Test]
        public void TestWriteBsonType()
        {
            _bsonStreamWriter.WriteBsonType(BsonType.Document);
            CollectionAssert.AreEqual(new[] { (byte)BsonType.Document }, _memoryStream.ToArray(), "BsonType was written incorrectly");
            Assert.AreEqual(1, _bsonStreamWriter.Position, "Position is incorrect");
        }

        [Test]
        public async Task TestWriteBsonTypeAsync()
        {
            await _bsonStreamWriter.WriteBsonTypeAsync(BsonType.Document);
            CollectionAssert.AreEqual(new[] { (byte)BsonType.Document }, _memoryStream.ToArray(), "BsonType was written incorrectly");
            Assert.AreEqual(1, _bsonStreamWriter.Position, "Position is incorrect");
        }

        [Test]
        public void TestWriteByte()
        {
            _bsonStreamWriter.WriteByte(0x57);
            CollectionAssert.AreEqual(new byte[] { 0x57 }, _memoryStream.ToArray(), "Byte was written incorrectly");
            Assert.AreEqual(1, _bsonStreamWriter.Position, "Position is incorrect");
        }

        [Test]
        public async Task TestWriteByteAsync()
        {
            await _bsonStreamWriter.WriteByteAsync(0x57);
            CollectionAssert.AreEqual(new byte[] { 0x57 }, _memoryStream.ToArray(), "Byte was written incorrectly");
            Assert.AreEqual(1, _bsonStreamWriter.Position, "Position is incorrect");
        }

        [Test]
        public void TestWriteBytes()
        {
            var expected = new byte[] { 05, 04, 03, 02, 01 };
            _bsonStreamWriter.WriteBytes(expected);
            CollectionAssert.AreEqual(expected, _memoryStream.ToArray(), "Bytes were written incorrectly");
            Assert.AreEqual(5, _bsonStreamWriter.Position, "Position is incorrect");
        }

        [Test]
        public async Task TestWriteBytesAsync()
        {
            var expected = new byte[] { 05, 04, 03, 02, 01 };
            await _bsonStreamWriter.WriteBytesAsync(expected);
            CollectionAssert.AreEqual(expected, _memoryStream.ToArray(), "Bytes were written incorrectly");
            Assert.AreEqual(5, _bsonStreamWriter.Position, "Position is incorrect");
        }

        [Test]
        public void TestWriteCString()
        {
            const string value = "HelloWorld";
            var expected = Utf8Helper.StrictUtf8Encoding.GetBytes(value).Concat(new byte[] { 0 }).ToArray();

            _bsonStreamWriter.WriteCString(value);
            CollectionAssert.AreEqual(expected, _memoryStream.ToArray(), "CString was written incorrectly");
            Assert.AreEqual(expected.Length, _bsonStreamWriter.Position, "Position is incorrect");
        }

        [Test]
        public async Task TestWriteCStringAsync()
        {
            const string value = "HelloWorld";
            var expected = Utf8Helper.StrictUtf8Encoding.GetBytes(value).Concat(new byte[] { 0 }).ToArray();

            await _bsonStreamWriter.WriteCStringAsync(value);
            CollectionAssert.AreEqual(expected, _memoryStream.ToArray(), "CString were written incorrectly");
            Assert.AreEqual(expected.Length, _bsonStreamWriter.Position, "Position is incorrect");
        }

        [Test]
        public void TestWriteDouble()
        {
            _bsonStreamWriter.WriteDouble(BitConverter.Int64BitsToDouble(0x0102030405060708));
            CollectionAssert.AreEqual(new byte[] { 08, 07, 06, 05, 04, 03, 02, 01 }, _memoryStream.ToArray(), "Double was written incorrectly");
            Assert.AreEqual(8, _bsonStreamWriter.Position, "Position is incorrect");
        }

        [Test]
        public async Task TestWriteDoubleAsync()
        {
            await _bsonStreamWriter.WriteDoubleAsync(BitConverter.Int64BitsToDouble(0x0102030405060708));
            CollectionAssert.AreEqual(new byte[] { 08, 07, 06, 05, 04, 03, 02, 01 }, _memoryStream.ToArray(), "Double was written incorrectly");
            Assert.AreEqual(8, _bsonStreamWriter.Position, "Position is incorrect");
        }

        [Test]
        public void TestWriteInt32()
        {
            _bsonStreamWriter.WriteInt32(0x01020304);
            CollectionAssert.AreEqual(new byte[] { 04, 03, 02, 01 }, _memoryStream.ToArray(), "Int32 was written incorrectly");
            Assert.AreEqual(4, _bsonStreamWriter.Position, "Position is incorrect");
        }

        [Test]
        public async Task TestWriteInt32Async()
        {
            await _bsonStreamWriter.WriteInt32Async(0x01020304);
            CollectionAssert.AreEqual(new byte[] { 04, 03, 02, 01 }, _memoryStream.ToArray(), "Int32 was written incorrectly");
            Assert.AreEqual(4, _bsonStreamWriter.Position, "Position is incorrect");
        }

        [Test]
        public void TestWriteInt64()
        {
            _bsonStreamWriter.WriteInt64(0x0102030405060708);
            CollectionAssert.AreEqual(new byte[] { 08, 07, 06, 05, 04, 03, 02, 01 }, _memoryStream.ToArray(), "Int64 was written incorrectly");
            Assert.AreEqual(8, _bsonStreamWriter.Position, "Position is incorrect");
        }

        [Test]
        public async Task TestWriteInt64Async()
        {
            await _bsonStreamWriter.WriteInt64Async(0x0102030405060708);
            CollectionAssert.AreEqual(new byte[] { 08, 07, 06, 05, 04, 03, 02, 01 }, _memoryStream.ToArray(), "Int64 was written incorrectly");
            Assert.AreEqual(8, _bsonStreamWriter.Position, "Position is incorrect");
        }

        [Test]
        public void TestWriteObjectId()
        {
            var expected = new byte[] { 12, 11, 10, 09, 08, 07, 06, 05, 04, 03, 02, 01 };

            _bsonStreamWriter.WriteObjectId(new ObjectId(expected));
            CollectionAssert.AreEqual(expected, _memoryStream.ToArray(), "ObjectId was written incorrectly");
            Assert.AreEqual(12, _bsonStreamWriter.Position, "Position is incorrect");
        }

        [Test]
        public async Task TestWriteObjectIdAsync()
        {
            var expected = new byte[] { 12, 11, 10, 09, 08, 07, 06, 05, 04, 03, 02, 01 };

            await _bsonStreamWriter.WriteObjectIdAsync(new ObjectId(expected));
            CollectionAssert.AreEqual(expected, _memoryStream.ToArray(), "ObjectId were written incorrectly");
            Assert.AreEqual(12, _bsonStreamWriter.Position, "Position is incorrect");
        }

        [Test]
        public void TestWriteString()
        {
            const string value = "HelloWorld";
            var bytes = Utf8Helper.StrictUtf8Encoding.GetBytes(value);
            var expected = BitConverter.GetBytes(bytes.Length + 1).Concat(bytes).Concat(new byte[] { 0 }).ToArray();

            _bsonStreamWriter.WriteString(value);
            CollectionAssert.AreEqual(expected, _memoryStream.ToArray(), "String was written incorrectly");
            Assert.AreEqual(expected.Length, _bsonStreamWriter.Position, "Position is incorrect");
        }

        [Test]
        public async Task TestWriteStringAsync()
        {
            const string value = "HelloWorld";
            var bytes = Utf8Helper.StrictUtf8Encoding.GetBytes(value);
            var expected = BitConverter.GetBytes(bytes.Length + 1).Concat(bytes).Concat(new byte[] { 0 }).ToArray();

            await _bsonStreamWriter.WriteStringAsync(value);
            CollectionAssert.AreEqual(expected, _memoryStream.ToArray(), "String were written incorrectly");
            Assert.AreEqual(expected.Length, _bsonStreamWriter.Position, "Position is incorrect");
        }
    }
}

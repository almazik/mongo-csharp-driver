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
    public class BsonStreamReaderTests
    {
        [Test]
        public void TestReadBoolean()
        {
            var bsonStreamReader = new BsonStreamReader(new MemoryStream(new byte[] { 00, 01 }), Utf8Helper.StrictUtf8Encoding);

            Assert.AreEqual(false, bsonStreamReader.ReadBoolean(), "False value was read incorrectly");
            Assert.AreEqual(true, bsonStreamReader.ReadBoolean(), "False value was read incorrectly");
            Assert.AreEqual(2, bsonStreamReader.Position, "Position is incorrect");
        }

        [Test]
        public async Task TestReadBooleanAsync()
        {
            var bsonStreamReader = new BsonStreamReader(new MemoryStream(new byte[] { 00, 01 }), Utf8Helper.StrictUtf8Encoding);

            Assert.AreEqual(false, await bsonStreamReader.ReadBooleanAsync(), "False value was read incorrectly");
            Assert.AreEqual(true, await bsonStreamReader.ReadBooleanAsync(), "False value was read incorrectly");
            Assert.AreEqual(2, bsonStreamReader.Position, "Position is incorrect");
        }

        [Test]
        public void TestReadBsonType()
        {
            var bsonStreamReader = new BsonStreamReader(new MemoryStream(new[] { (byte)BsonType.Document }), Utf8Helper.StrictUtf8Encoding);

            Assert.AreEqual(BsonType.Document, bsonStreamReader.ReadBsonType(), "BsonType was read incorrectly");
            Assert.AreEqual(1, bsonStreamReader.Position, "Position is incorrect");
        }

        [Test]
        public async Task TestReadBsonTypeAsync()
        {
            var bsonStreamReader = new BsonStreamReader(new MemoryStream(new[] { (byte)BsonType.Document }), Utf8Helper.StrictUtf8Encoding);

            Assert.AreEqual(BsonType.Document, await bsonStreamReader.ReadBsonTypeAsync(), "BsonType was read incorrectly");
            Assert.AreEqual(1, bsonStreamReader.Position, "Position is incorrect");
        }

        [Test]
        public void TestReadCString()
        {
            const string expected = "HelloWorld";
            var bytes = Utf8Helper.StrictUtf8Encoding.GetBytes(expected).Concat(new byte[] { 0 }).ToArray();

            var bsonStreamReader = new BsonStreamReader(new MemoryStream(bytes), Utf8Helper.StrictUtf8Encoding);

            Assert.AreEqual(expected, bsonStreamReader.ReadCString(), "CString was read incorrectly");
            Assert.AreEqual(bytes.Length, bsonStreamReader.Position, "Position is incorrect");
        }

        [Test]
        public async Task TestReadCStringAsync()
        {
            const string expected = "HelloWorld";
            var bytes = Utf8Helper.StrictUtf8Encoding.GetBytes(expected).Concat(new byte[] { 0 }).ToArray();

            var bsonStreamReader = new BsonStreamReader(new MemoryStream(bytes), Utf8Helper.StrictUtf8Encoding);

            Assert.AreEqual(expected, await bsonStreamReader.ReadCStringAsync(), "CString was read incorrectly");
            Assert.AreEqual(bytes.Length, bsonStreamReader.Position, "Position is incorrect");
        }

        [Test]
        public void TestReadCStringBytes()
        {
            const string expected = "HelloWorld";
            var bytes = Utf8Helper.StrictUtf8Encoding.GetBytes(expected);

            var bsonStreamReader = new BsonStreamReader(new MemoryStream(bytes.Concat(new byte[] { 0 }).ToArray()), Utf8Helper.StrictUtf8Encoding);

            CollectionAssert.AreEqual(bytes, bsonStreamReader.ReadCStringBytes(), "CStringBytes was read incorrectly");
            Assert.AreEqual(bytes.Length + 1, bsonStreamReader.Position, "Position is incorrect"); //should advance after the null-terminator
        }

        [Test]
        public async Task TestReadCStringBytesAsync()
        {
            const string expected = "HelloWorld";
            var bytes = Utf8Helper.StrictUtf8Encoding.GetBytes(expected);

            var bsonStreamReader = new BsonStreamReader(new MemoryStream(bytes.Concat(new byte[] { 0 }).ToArray()), Utf8Helper.StrictUtf8Encoding);

            CollectionAssert.AreEqual(bytes, (await bsonStreamReader.ReadCStringBytesAsync()), "CStringBytes was read incorrectly");
            Assert.AreEqual(bytes.Length + 1, bsonStreamReader.Position, "Position is incorrect"); //should advance after the null-terminator
        }

        [Test]
        public void TestReadDouble()
        {
            var expected = BitConverter.Int64BitsToDouble(0x0102030405060708);
            var bsonStreamReader = new BsonStreamReader(new MemoryStream(new byte[] { 08, 07, 06, 05, 04, 03, 02, 01 }), Utf8Helper.StrictUtf8Encoding);

            Assert.AreEqual(expected, bsonStreamReader.ReadDouble(), "Double was read incorrectly");
            Assert.AreEqual(8, bsonStreamReader.Position, "Position is incorrect");
        }

        [Test]
        public async Task TestReadDoubleAsync()
        {
            var expected = BitConverter.Int64BitsToDouble(0x0102030405060708);
            var bsonStreamReader = new BsonStreamReader(new MemoryStream(new byte[] { 08, 07, 06, 05, 04, 03, 02, 01 }), Utf8Helper.StrictUtf8Encoding);

            Assert.AreEqual(expected, await bsonStreamReader.ReadDoubleAsync(), "Double was read incorrectly");
            Assert.AreEqual(8, bsonStreamReader.Position, "Position is incorrect");
        }

        [Test]
        public void TestReadInt32()
        {
            var bsonStreamReader = new BsonStreamReader(new MemoryStream(new byte[] { 04, 03, 02, 01 }), Utf8Helper.StrictUtf8Encoding);

            Assert.AreEqual(0x01020304, bsonStreamReader.ReadInt32(), "Int32 was read incorrectly");
            Assert.AreEqual(4, bsonStreamReader.Position, "Position is incorrect");
        }

        [Test]
        public async Task TestReadInt32Async()
        {
            var bsonStreamReader = new BsonStreamReader(new MemoryStream(new byte[] { 04, 03, 02, 01 }), Utf8Helper.StrictUtf8Encoding);

            Assert.AreEqual(0x01020304, await bsonStreamReader.ReadInt32Async(), "Int32 was read incorrectly");
            Assert.AreEqual(4, bsonStreamReader.Position, "Position is incorrect");
        }

        [Test]
        public void TestReadInt64()
        {
            var bsonStreamReader = new BsonStreamReader(new MemoryStream(new byte[] { 08, 07, 06, 05, 04, 03, 02, 01 }), Utf8Helper.StrictUtf8Encoding);

            Assert.AreEqual(0x0102030405060708, bsonStreamReader.ReadInt64(), "Int64 was read incorrectly");
            Assert.AreEqual(8, bsonStreamReader.Position, "Position is incorrect");
        }

        [Test]
        public async Task TestReadInt64Async()
        {
            var bsonStreamReader = new BsonStreamReader(new MemoryStream(new byte[] { 08, 07, 06, 05, 04, 03, 02, 01 }), Utf8Helper.StrictUtf8Encoding);

            Assert.AreEqual(0x0102030405060708, await bsonStreamReader.ReadInt64Async(), "Int64 was read incorrectly");
            Assert.AreEqual(8, bsonStreamReader.Position, "Position is incorrect");
        }

        [Test]
        public void TestReadObjectId()
        {
            var bytes = new byte[] { 01, 02, 03, 04, 05, 06, 07, 08, 09, 10, 11, 12 };
            var bsonStreamReader = new BsonStreamReader(new MemoryStream(bytes), Utf8Helper.StrictUtf8Encoding);

            Assert.AreEqual(new ObjectId(bytes), bsonStreamReader.ReadObjectId(), "ObjectId were read incorrectly");
            Assert.AreEqual(12, bsonStreamReader.Position, "Position is incorrect");
        }

        [Test]
        public async Task TestReadObjectIdAsync()
        {
            var bytes = new byte[] { 01, 02, 03, 04, 05, 06, 07, 08, 09, 10, 11, 12 };
            var bsonStreamReader = new BsonStreamReader(new MemoryStream(bytes), Utf8Helper.StrictUtf8Encoding);

            Assert.AreEqual(new ObjectId(bytes), await bsonStreamReader.ReadObjectIdAsync(), "ObjectId were read incorrectly");
            Assert.AreEqual(12, bsonStreamReader.Position, "Position is incorrect");
        }

        [Test]
        public void TestReadString()
        {
            const string expected = "HelloWorld";
            var bytes = Utf8Helper.StrictUtf8Encoding.GetBytes(expected);
            bytes = BitConverter.GetBytes(bytes.Length + 1).Concat(bytes).Concat(new byte[] { 0 }).ToArray();

            var bsonStreamReader = new BsonStreamReader(new MemoryStream(bytes), Utf8Helper.StrictUtf8Encoding);

            Assert.AreEqual(expected, bsonStreamReader.ReadString(), "String was read incorrectly");
            Assert.AreEqual(bytes.Length, bsonStreamReader.Position, "Position is incorrect"); //should advance after the null-terminator
        }

        [Test]
        public async Task TestReadStringAsync()
        {
            const string expected = "HelloWorld";
            var bytes = Utf8Helper.StrictUtf8Encoding.GetBytes(expected);
            bytes = BitConverter.GetBytes(bytes.Length + 1).Concat(bytes).Concat(new byte[] { 0 }).ToArray();

            var bsonStreamReader = new BsonStreamReader(new MemoryStream(bytes), Utf8Helper.StrictUtf8Encoding);

            Assert.AreEqual(expected, await bsonStreamReader.ReadStringAsync(), "String was read incorrectly");
            Assert.AreEqual(bytes.Length, bsonStreamReader.Position, "Position is incorrect"); //should advance after the null-terminator
        }

        [Test]
        public void TestReadByte()
        {
            var bsonStreamReader = new BsonStreamReader(new MemoryStream(new byte[] { 0x77 }), Utf8Helper.StrictUtf8Encoding);

            Assert.AreEqual(0x77, bsonStreamReader.ReadByte(), "Byte was read incorrectly");
            Assert.AreEqual(1, bsonStreamReader.Position, "Position is incorrect");
        }

        [Test]
        public async Task TestReadByteAsync()
        {
            var bsonStreamReader = new BsonStreamReader(new MemoryStream(new byte[] { 0x77 }), Utf8Helper.StrictUtf8Encoding);

            Assert.AreEqual(0x77, await bsonStreamReader.ReadByteAsync(), "Byte was read incorrectly");
            Assert.AreEqual(1, bsonStreamReader.Position, "Position is incorrect");
        }

        [Test]
        public void TestReadBytes()
        {
            var expected = Enumerable.Range(1, 100).Select(i => (byte)i).ToArray();
            var bsonStreamReader = new BsonStreamReader(new MemoryStream(expected), Utf8Helper.StrictUtf8Encoding);

            CollectionAssert.AreEqual(expected.Take(50), bsonStreamReader.ReadBytes(50), "First 50 bytes were read incorrectly");

            var actual = new byte[800]; //defining larger buffer, reading into specific place within buffer
            bsonStreamReader.ReadBytes(actual, 100, 50);
            CollectionAssert.AreEqual(expected.Skip(50), actual.Skip(100).Take(50), "Last 50 bytes were read incorrectly");

            Assert.AreEqual(100, bsonStreamReader.Position, "Position is incorrect");
        }

        [Test]
        public async Task TestReadBytesAsync()
        {
            var expected = Enumerable.Range(1, 100).Select(i => (byte)i).ToArray();
            var bsonStreamReader = new BsonStreamReader(new MemoryStream(expected), Utf8Helper.StrictUtf8Encoding);

            CollectionAssert.AreEqual(expected.Take(50), await bsonStreamReader.ReadBytesAsync(50), "First 50 bytes were read incorrectly");

            var actual = new byte[800]; //defining larger buffer, reading into specific place within buffer
            await bsonStreamReader.ReadBytesAsync(actual, 100, 50);
            CollectionAssert.AreEqual(expected.Skip(50), actual.Skip(100).Take(50), "Last 50 bytes were read incorrectly");

            Assert.AreEqual(100, bsonStreamReader.Position, "Position is incorrect");
        }

        [Test]
        public void TestSkipCString()
        {
            const string expected = "HelloWorld";
            var bytes = Utf8Helper.StrictUtf8Encoding.GetBytes(expected).Concat(new byte[] { 0 }).ToArray();

            var bsonStreamReader = new BsonStreamReader(new MemoryStream(bytes), Utf8Helper.StrictUtf8Encoding);

            bsonStreamReader.SkipCString();

            Assert.AreEqual(bytes.Length, bsonStreamReader.Position, "Position is incorrect");
        }

        [Test]
        public async Task TestSkipCStringAsync()
        {
            const string expected = "HelloWorld";
            var bytes = Utf8Helper.StrictUtf8Encoding.GetBytes(expected).Concat(new byte[] { 0 }).ToArray();

            var bsonStreamReader = new BsonStreamReader(new MemoryStream(bytes), Utf8Helper.StrictUtf8Encoding);

            await bsonStreamReader.SkipCStringAsync();

            Assert.AreEqual(bytes.Length, bsonStreamReader.Position, "Position is incorrect");
        }
    }
}

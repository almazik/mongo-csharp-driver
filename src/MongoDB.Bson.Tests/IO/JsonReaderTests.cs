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
using System.IO;
using System.Linq;
using System.Text;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using NUnit.Framework;

namespace MongoDB.Bson.Tests.IO
{
    [TestFixture]
    public class JsonReaderTests
    {
        private BsonReader _bsonReader;

        [Test]
        public void TestArrayEmpty()
        {
            var json = "[]";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.Array, _bsonReader.ReadBsonType());
                _bsonReader.ReadStartArray();
                Assert.AreEqual(BsonType.EndOfDocument, _bsonReader.ReadBsonType());
                _bsonReader.ReadEndArray();
                Assert.AreEqual(BsonReaderState.Done, _bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonArray>(json).ToJson());
        }

        [Test]
        public void TestArrayOneElement()
        {
            var json = "[1]";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.Array, _bsonReader.ReadBsonType());
                _bsonReader.ReadStartArray();
                Assert.AreEqual(BsonType.Int32, _bsonReader.ReadBsonType());
                Assert.AreEqual(1, _bsonReader.ReadInt32());
                Assert.AreEqual(BsonType.EndOfDocument, _bsonReader.ReadBsonType());
                _bsonReader.ReadEndArray();
                Assert.AreEqual(BsonReaderState.Done, _bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonArray>(json).ToJson());
        }

        [Test]
        public void TestArrayTwoElements()
        {
            var json = "[1, 2]";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.Array, _bsonReader.ReadBsonType());
                _bsonReader.ReadStartArray();
                Assert.AreEqual(BsonType.Int32, _bsonReader.ReadBsonType());
                Assert.AreEqual(1, _bsonReader.ReadInt32());
                Assert.AreEqual(BsonType.Int32, _bsonReader.ReadBsonType());
                Assert.AreEqual(2, _bsonReader.ReadInt32());
                Assert.AreEqual(BsonType.EndOfDocument, _bsonReader.ReadBsonType());
                _bsonReader.ReadEndArray();
                Assert.AreEqual(BsonReaderState.Done, _bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonArray>(json).ToJson());
        }

        [Test]
        public void TestBookmark()
        {
            var json = "{ \"x\" : 1, \"y\" : 2 }";
            using (_bsonReader = new JsonReader(json))
            {
                // do everything twice returning to bookmark in between
                var bookmark = _bsonReader.GetBookmark();
                Assert.AreEqual(BsonType.Document, _bsonReader.ReadBsonType());
                _bsonReader.ReturnToBookmark(bookmark);
                Assert.AreEqual(BsonType.Document, _bsonReader.ReadBsonType());

                bookmark = _bsonReader.GetBookmark();
                _bsonReader.ReadStartDocument();
                _bsonReader.ReturnToBookmark(bookmark);
                _bsonReader.ReadStartDocument();

                bookmark = _bsonReader.GetBookmark();
                Assert.AreEqual(BsonType.Int32, _bsonReader.ReadBsonType());
                _bsonReader.ReturnToBookmark(bookmark);
                Assert.AreEqual(BsonType.Int32, _bsonReader.ReadBsonType());

                bookmark = _bsonReader.GetBookmark();
                Assert.AreEqual("x", _bsonReader.ReadName());
                _bsonReader.ReturnToBookmark(bookmark);
                Assert.AreEqual("x", _bsonReader.ReadName());

                bookmark = _bsonReader.GetBookmark();
                Assert.AreEqual(1, _bsonReader.ReadInt32());
                _bsonReader.ReturnToBookmark(bookmark);
                Assert.AreEqual(1, _bsonReader.ReadInt32());

                bookmark = _bsonReader.GetBookmark();
                Assert.AreEqual(BsonType.Int32, _bsonReader.ReadBsonType());
                _bsonReader.ReturnToBookmark(bookmark);
                Assert.AreEqual(BsonType.Int32, _bsonReader.ReadBsonType());

                bookmark = _bsonReader.GetBookmark();
                Assert.AreEqual("y", _bsonReader.ReadName());
                _bsonReader.ReturnToBookmark(bookmark);
                Assert.AreEqual("y", _bsonReader.ReadName());

                bookmark = _bsonReader.GetBookmark();
                Assert.AreEqual(2, _bsonReader.ReadInt32());
                _bsonReader.ReturnToBookmark(bookmark);
                Assert.AreEqual(2, _bsonReader.ReadInt32());

                bookmark = _bsonReader.GetBookmark();
                Assert.AreEqual(BsonType.EndOfDocument, _bsonReader.ReadBsonType());
                _bsonReader.ReturnToBookmark(bookmark);
                Assert.AreEqual(BsonType.EndOfDocument, _bsonReader.ReadBsonType());

                bookmark = _bsonReader.GetBookmark();
                _bsonReader.ReadEndDocument();
                _bsonReader.ReturnToBookmark(bookmark);
                _bsonReader.ReadEndDocument();

                Assert.AreEqual(BsonReaderState.Done, _bsonReader.State);

            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonDocument>(json).ToJson());
        }

        [Test]
        public void TestBooleanFalse()
        {
            var json = "false";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.Boolean, _bsonReader.ReadBsonType());
                Assert.AreEqual(false, _bsonReader.ReadBoolean());
                Assert.AreEqual(BsonReaderState.Done, _bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<bool>(json).ToJson());
        }

        [Test]
        public void TestBooleanTrue()
        {
            var json = "true";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.Boolean, _bsonReader.ReadBsonType());
                Assert.AreEqual(true, _bsonReader.ReadBoolean());
                Assert.AreEqual(BsonReaderState.Done, _bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<bool>(json).ToJson());
        }

        [Test]
        public void TestDateTimeMinBson()
        {
            var json = "new Date(-9223372036854775808)";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.DateTime, _bsonReader.ReadBsonType());
                Assert.AreEqual(-9223372036854775808, _bsonReader.ReadDateTime());
                Assert.AreEqual(BsonReaderState.Done, _bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonDateTime>(json).ToJson());
        }

        [Test]
        public void TestDateTimeMaxBson()
        {
            var json = "new Date(9223372036854775807)";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.DateTime, _bsonReader.ReadBsonType());
                Assert.AreEqual(9223372036854775807, _bsonReader.ReadDateTime());
                Assert.AreEqual(BsonReaderState.Done, _bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonDateTime>(json).ToJson());
        }

        [Test]
        public void TestDateTimeShell()
        {
            var json = "ISODate(\"1970-01-01T00:00:00Z\")";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.DateTime, _bsonReader.ReadBsonType());
                Assert.AreEqual(0, _bsonReader.ReadDateTime());
                Assert.AreEqual(BsonReaderState.Done, _bsonReader.State);
            }
            var jsonSettings = new JsonWriterSettings { OutputMode = JsonOutputMode.Shell };
            Assert.AreEqual(json, BsonSerializer.Deserialize<DateTime>(json).ToJson(jsonSettings));
        }

        [Test]
        public void TestDateTimeStrict()
        {
            var json = "{ \"$date\" : 0 }";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.DateTime, _bsonReader.ReadBsonType());
                Assert.AreEqual(0, _bsonReader.ReadDateTime());
                Assert.AreEqual(BsonReaderState.Done, _bsonReader.State);
            }
            var jsonSettings = new JsonWriterSettings { OutputMode = JsonOutputMode.Strict };
            Assert.AreEqual(json, BsonSerializer.Deserialize<DateTime>(json).ToJson(jsonSettings));
        }

        [Test]
        public void TestDateTimeStrictIso8601()
        {
            var json = "{ \"$date\" : \"1970-01-01T00:00:00Z\" }";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.DateTime, _bsonReader.ReadBsonType());
                Assert.AreEqual(0, _bsonReader.ReadDateTime());
                Assert.AreEqual(BsonReaderState.Done, _bsonReader.State);
            }
            var expected = "{ \"$date\" : 0 }"; // it's still not ISO8601 on the way out
            var jsonSettings = new JsonWriterSettings { OutputMode = JsonOutputMode.Strict };
            Assert.AreEqual(expected, BsonSerializer.Deserialize<DateTime>(json).ToJson(jsonSettings));
        }

        [Test]
        public void TestDocumentEmpty()
        {
            var json = "{ }";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.Document, _bsonReader.ReadBsonType());
                _bsonReader.ReadStartDocument();
                Assert.AreEqual(BsonType.EndOfDocument, _bsonReader.ReadBsonType());
                _bsonReader.ReadEndDocument();
                Assert.AreEqual(BsonReaderState.Done, _bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonDocument>(json).ToJson());
        }

        [Test]
        public void TestDocumentNested()
        {
            var json = "{ \"a\" : { \"x\" : 1 }, \"y\" : 2 }";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.Document, _bsonReader.ReadBsonType());
                _bsonReader.ReadStartDocument();
                Assert.AreEqual(BsonType.Document, _bsonReader.ReadBsonType());
                Assert.AreEqual("a", _bsonReader.ReadName());
                _bsonReader.ReadStartDocument();
                Assert.AreEqual(BsonType.Int32, _bsonReader.ReadBsonType());
                Assert.AreEqual("x", _bsonReader.ReadName());
                Assert.AreEqual(1, _bsonReader.ReadInt32());
                Assert.AreEqual(BsonType.EndOfDocument, _bsonReader.ReadBsonType());
                _bsonReader.ReadEndDocument();
                Assert.AreEqual(BsonType.Int32, _bsonReader.ReadBsonType());
                Assert.AreEqual("y", _bsonReader.ReadName());
                Assert.AreEqual(2, _bsonReader.ReadInt32());
                Assert.AreEqual(BsonType.EndOfDocument, _bsonReader.ReadBsonType());
                _bsonReader.ReadEndDocument();
                Assert.AreEqual(BsonReaderState.Done, _bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonDocument>(json).ToJson());
        }

        [Test]
        public void TestDocumentOneElement()
        {
            var json = "{ \"x\" : 1 }";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.Document, _bsonReader.ReadBsonType());
                _bsonReader.ReadStartDocument();
                Assert.AreEqual(BsonType.Int32, _bsonReader.ReadBsonType());
                Assert.AreEqual("x", _bsonReader.ReadName());
                Assert.AreEqual(1, _bsonReader.ReadInt32());
                Assert.AreEqual(BsonType.EndOfDocument, _bsonReader.ReadBsonType());
                _bsonReader.ReadEndDocument();
                Assert.AreEqual(BsonReaderState.Done, _bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonDocument>(json).ToJson());
        }

        [Test]
        public void TestDocumentTwoElements()
        {
            var json = "{ \"x\" : 1, \"y\" : 2 }";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.Document, _bsonReader.ReadBsonType());
                _bsonReader.ReadStartDocument();
                Assert.AreEqual(BsonType.Int32, _bsonReader.ReadBsonType());
                Assert.AreEqual("x", _bsonReader.ReadName());
                Assert.AreEqual(1, _bsonReader.ReadInt32());
                Assert.AreEqual(BsonType.Int32, _bsonReader.ReadBsonType());
                Assert.AreEqual("y", _bsonReader.ReadName());
                Assert.AreEqual(2, _bsonReader.ReadInt32());
                Assert.AreEqual(BsonType.EndOfDocument, _bsonReader.ReadBsonType());
                _bsonReader.ReadEndDocument();
                Assert.AreEqual(BsonReaderState.Done, _bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonDocument>(json).ToJson());
        }

        [Test]
        public void TestDouble()
        {
            var json = "1.5";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.Double, _bsonReader.ReadBsonType());
                Assert.AreEqual(1.5, _bsonReader.ReadDouble());
                Assert.AreEqual(BsonReaderState.Done, _bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<double>(json).ToJson());
        }

        [Test]
        public void TestGuid()
        {
            var guid = new Guid("B5F21E0C2A0D42D6AD03D827008D8AB6");
            var json = "CSUUID(\"B5F21E0C2A0D42D6AD03D827008D8AB6\")";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.Binary, _bsonReader.ReadBsonType());
                var binaryData = _bsonReader.ReadBinaryData();
                Assert.IsTrue(binaryData.Bytes.SequenceEqual(guid.ToByteArray()));
                Assert.AreEqual(BsonBinarySubType.UuidLegacy, binaryData.SubType);
                Assert.AreEqual(GuidRepresentation.CSharpLegacy, binaryData.GuidRepresentation);
                Assert.AreEqual(BsonReaderState.Done, _bsonReader.State);
            }
            var expected = "CSUUID(\"b5f21e0c-2a0d-42d6-ad03-d827008d8ab6\")";
            Assert.AreEqual(expected, BsonSerializer.Deserialize<Guid>(json).ToJson());
        }

        [Test]
        public void TestHexData()
        {
            var expectedBytes = new byte[] { 0x01, 0x23 };
            var json = "HexData(0, \"123\")";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.Binary, _bsonReader.ReadBsonType());
                var bytes = _bsonReader.ReadBytes();
                Assert.IsTrue(expectedBytes.SequenceEqual(bytes));
                Assert.AreEqual(BsonReaderState.Done, _bsonReader.State);
            }
            var expectedJson = "new BinData(0, \"ASM=\")";
            Assert.AreEqual(expectedJson, BsonSerializer.Deserialize<byte[]>(json).ToJson());
        }

        [Test]
        public void TestInt32()
        {
            var json = "123";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.Int32, _bsonReader.ReadBsonType());
                Assert.AreEqual(123, _bsonReader.ReadInt32());
                Assert.AreEqual(BsonReaderState.Done, _bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<int>(json).ToJson());
        }

        [TestCase("Number(123)")]
        [TestCase("NumberInt(123)")]
        public void TestInt32Constructor(string json)
        {
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.Int32, _bsonReader.ReadBsonType());
                Assert.AreEqual(123, _bsonReader.ReadInt32());
                Assert.AreEqual(BsonReaderState.Done, _bsonReader.State);
            }
            var canonicalJson = "123";
            Assert.AreEqual(canonicalJson, BsonSerializer.Deserialize<int>(new StringReader(json)).ToJson());
        }

        [Test]
        public void TestInt64ConstructorQuoted()
        {
            var json = "NumberLong(\"123456789012\")";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.Int64, _bsonReader.ReadBsonType());
                Assert.AreEqual(123456789012, _bsonReader.ReadInt64());
                Assert.AreEqual(BsonReaderState.Done, _bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<long>(json).ToJson());
        }

        [Test]
        public void TestInt64ConstructorUnqutoed()
        {
            var json = "NumberLong(123)";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.Int64, _bsonReader.ReadBsonType());
                Assert.AreEqual(123, _bsonReader.ReadInt64());
                Assert.AreEqual(BsonReaderState.Done, _bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<long>(json).ToJson());
        }

        [Test]
        public void TestIsAtEndOfFileWithTwoArrays()
        {
            var json = "[1,2][1,2]";

            using (var jsonReader = new JsonReader(json))
            {
                var count = 0;
                while (!jsonReader.IsAtEndOfFile())
                {
                    var array = BsonSerializer.Deserialize<BsonArray>(jsonReader);
                    var expected = new BsonArray { 1, 2 };
                    Assert.AreEqual(expected, array);
                    count += 1;
                }
                Assert.AreEqual(2, count);
            }
        }

        [Test]
        public void TestIsAtEndOfFileWithTwoDocuments()
        {
            var json = "{x:1}{x:1}";

            using (var jsonReader = new JsonReader(json))
            {
                var count = 0;
                while (!jsonReader.IsAtEndOfFile())
                {
                    var document = BsonSerializer.Deserialize<BsonDocument>(jsonReader);
                    var expected = new BsonDocument("x", 1);
                    Assert.AreEqual(expected, document);
                    count += 1;
                }
                Assert.AreEqual(2, count);
            }
        }

        [Test]
        public void TestInt64ExtendedJson()
        {
            var json = "{ \"$numberLong\" : \"123\" }";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.Int64, _bsonReader.ReadBsonType());
                Assert.AreEqual(123, _bsonReader.ReadInt64());
                Assert.AreEqual(BsonReaderState.Done, _bsonReader.State);
            }
            var canonicalJson = "NumberLong(123)";
            Assert.AreEqual(canonicalJson, BsonSerializer.Deserialize<long>(new StringReader(json)).ToJson());
        }

        [Test]
        public void TestJavaScript()
        {
            string json = "{ \"$code\" : \"function f() { return 1; }\" }";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.JavaScript, _bsonReader.ReadBsonType());
                Assert.AreEqual("function f() { return 1; }", _bsonReader.ReadJavaScript());
                Assert.AreEqual(BsonReaderState.Done, _bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonJavaScript>(json).ToJson());
        }

        [Test]
        public void TestJavaScriptWithScope()
        {
            string json = "{ \"$code\" : \"function f() { return n; }\", \"$scope\" : { \"n\" : 1 } }";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.JavaScriptWithScope, _bsonReader.ReadBsonType());
                Assert.AreEqual("function f() { return n; }", _bsonReader.ReadJavaScriptWithScope());
                _bsonReader.ReadStartDocument();
                Assert.AreEqual(BsonType.Int32, _bsonReader.ReadBsonType());
                Assert.AreEqual("n", _bsonReader.ReadName());
                Assert.AreEqual(1, _bsonReader.ReadInt32());
                _bsonReader.ReadEndDocument();
                Assert.AreEqual(BsonReaderState.Done, _bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonJavaScriptWithScope>(json).ToJson());
        }

        [Test]
        public void TestMaxKeyExtendedJson()
        {
            var json = "{ \"$maxkey\" : 1 }";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.MaxKey, _bsonReader.ReadBsonType());
                _bsonReader.ReadMaxKey();
                Assert.AreEqual(BsonReaderState.Done, _bsonReader.State);
            }
            var canonicalJson = "MaxKey";
            Assert.AreEqual(canonicalJson, BsonSerializer.Deserialize<BsonMaxKey>(new StringReader(json)).ToJson());
        }

        [Test]
        public void TestMaxKeyExtendedJsonWithCapitalK()
        {
            var json = "{ \"$maxKey\" : 1 }";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.MaxKey, _bsonReader.ReadBsonType());
                _bsonReader.ReadMaxKey();
                Assert.AreEqual(BsonReaderState.Done, _bsonReader.State);
            }
            var canonicalJson = "MaxKey";
            Assert.AreEqual(canonicalJson, BsonSerializer.Deserialize<BsonMaxKey>(new StringReader(json)).ToJson());
        }

        [Test]
        public void TestMaxKeyKeyword()
        {
            var json = "MaxKey";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.MaxKey, _bsonReader.ReadBsonType());
                _bsonReader.ReadMaxKey();
                Assert.AreEqual(BsonReaderState.Done, _bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonMaxKey>(new StringReader(json)).ToJson());
        }

        [Test]
        public void TestMinKeyExtendedJson()
        {
            var json = "{ \"$minkey\" : 1 }";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.MinKey, _bsonReader.ReadBsonType());
                _bsonReader.ReadMinKey();
                Assert.AreEqual(BsonReaderState.Done, _bsonReader.State);
            }
            var canonicalJson = "MinKey";
            Assert.AreEqual(canonicalJson, BsonSerializer.Deserialize<BsonMinKey>(new StringReader(json)).ToJson());
        }

        [Test]
        public void TestMinKeyExtendedJsonWithCapitalK()
        {
            var json = "{ \"$minKey\" : 1 }";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.MinKey, _bsonReader.ReadBsonType());
                _bsonReader.ReadMinKey();
                Assert.AreEqual(BsonReaderState.Done, _bsonReader.State);
            }
            var canonicalJson = "MinKey";
            Assert.AreEqual(canonicalJson, BsonSerializer.Deserialize<BsonMinKey>(new StringReader(json)).ToJson());
        }

        [Test]
        public void TestMinKeyKeyword()
        {
            var json = "MinKey";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.MinKey, _bsonReader.ReadBsonType());
                _bsonReader.ReadMinKey();
                Assert.AreEqual(BsonReaderState.Done, _bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonMinKey>(new StringReader(json)).ToJson());
        }

        [Test]
        public void TestNestedArray()
        {
            var json = "{ \"a\" : [1, 2] }";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.Document, _bsonReader.ReadBsonType());
                _bsonReader.ReadStartDocument();
                Assert.AreEqual(BsonType.Array, _bsonReader.ReadBsonType());
                Assert.AreEqual("a", _bsonReader.ReadName());
                _bsonReader.ReadStartArray();
                Assert.AreEqual(1, _bsonReader.ReadInt32());
                Assert.AreEqual(2, _bsonReader.ReadInt32());
                _bsonReader.ReadEndArray();
                _bsonReader.ReadEndDocument();
                Assert.AreEqual(BsonReaderState.Done, _bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonDocument>(json).ToJson());
        }

        [Test]
        public void TestNestedDocument()
        {
            var json = "{ \"a\" : { \"b\" : 1, \"c\" : 2 } }";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.Document, _bsonReader.ReadBsonType());
                _bsonReader.ReadStartDocument();
                Assert.AreEqual(BsonType.Document, _bsonReader.ReadBsonType());
                Assert.AreEqual("a", _bsonReader.ReadName());
                _bsonReader.ReadStartDocument();
                Assert.AreEqual("b", _bsonReader.ReadName());
                Assert.AreEqual(1, _bsonReader.ReadInt32());
                Assert.AreEqual("c", _bsonReader.ReadName());
                Assert.AreEqual(2, _bsonReader.ReadInt32());
                _bsonReader.ReadEndDocument();
                _bsonReader.ReadEndDocument();
                Assert.AreEqual(BsonReaderState.Done, _bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonDocument>(json).ToJson());
        }

        [Test]
        public void TestNull()
        {
            var json = "null";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.Null, _bsonReader.ReadBsonType());
                _bsonReader.ReadNull();
                Assert.AreEqual(BsonReaderState.Done, _bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonNull>(json).ToJson());
        }

        [Test]
        public void TestObjectIdShell()
        {
            var json = "ObjectId(\"4d0ce088e447ad08b4721a37\")";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.ObjectId, _bsonReader.ReadBsonType());
                var objectId = _bsonReader.ReadObjectId();
                Assert.AreEqual("4d0ce088e447ad08b4721a37", objectId.ToString());
                Assert.AreEqual(BsonReaderState.Done, _bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<ObjectId>(json).ToJson());
        }

        [Test]
        public void TestObjectIdStrict()
        {
            var json = "{ \"$oid\" : \"4d0ce088e447ad08b4721a37\" }";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.ObjectId, _bsonReader.ReadBsonType());
                var objectId = _bsonReader.ReadObjectId();
                Assert.AreEqual("4d0ce088e447ad08b4721a37", objectId.ToString());
                Assert.AreEqual(BsonReaderState.Done, _bsonReader.State);
            }
            var jsonSettings = new JsonWriterSettings { OutputMode = JsonOutputMode.Strict };
            Assert.AreEqual(json, BsonSerializer.Deserialize<ObjectId>(json).ToJson(jsonSettings));
        }

        [Test]
        public void TestRegularExpressionShell()
        {
            var json = "/pattern/imxs";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.RegularExpression, _bsonReader.ReadBsonType());
                var regex = _bsonReader.ReadRegularExpression();
                Assert.AreEqual("pattern", regex.Pattern);
                Assert.AreEqual("imxs", regex.Options);
                Assert.AreEqual(BsonReaderState.Done, _bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonRegularExpression>(json).ToJson());
        }

        [Test]
        public void TestRegularExpressionStrict()
        {
            var json = "{ \"$regex\" : \"pattern\", \"$options\" : \"imxs\" }";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.RegularExpression, _bsonReader.ReadBsonType());
                var regex = _bsonReader.ReadRegularExpression();
                Assert.AreEqual("pattern", regex.Pattern);
                Assert.AreEqual("imxs", regex.Options);
                Assert.AreEqual(BsonReaderState.Done, _bsonReader.State);
            }
            var settings = new JsonWriterSettings { OutputMode = JsonOutputMode.Strict };
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonRegularExpression>(json).ToJson(settings));
        }

        [Test]
        public void TestString()
        {
            var json = "\"abc\"";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.String, _bsonReader.ReadBsonType());
                Assert.AreEqual("abc", _bsonReader.ReadString());
                Assert.AreEqual(BsonReaderState.Done, _bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<string>(json).ToJson());
        }

        [Test]
        public void TestStringEmpty()
        {
            var json = "\"\"";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.String, _bsonReader.ReadBsonType());
                Assert.AreEqual("", _bsonReader.ReadString());
                Assert.AreEqual(BsonReaderState.Done, _bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<string>(json).ToJson());
        }

        [Test]
        public void TestSymbol()
        {
            var json = "{ \"$symbol\" : \"symbol\" }";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.Symbol, _bsonReader.ReadBsonType());
                Assert.AreEqual("symbol", _bsonReader.ReadSymbol());
                Assert.AreEqual(BsonReaderState.Done, _bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonSymbol>(json).ToJson());
        }

        [Test]
        public void TestTimestampConstructor()
        {
            var json = "Timestamp(1, 2)";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.Timestamp, _bsonReader.ReadBsonType());
                Assert.AreEqual(new BsonTimestamp(1, 2).Value, _bsonReader.ReadTimestamp());
                Assert.AreEqual(BsonReaderState.Done, _bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonTimestamp>(new StringReader(json)).ToJson());
        }

        [Test]
        public void TestTimestampExtendedJsonNewRepresentation()
        {
            var json = "{ \"$timestamp\" : { \"t\" : 1, \"i\" : 2 } }";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.Timestamp, _bsonReader.ReadBsonType());
                Assert.AreEqual(new BsonTimestamp(1, 2).Value, _bsonReader.ReadTimestamp());
                Assert.AreEqual(BsonReaderState.Done, _bsonReader.State);
            }
            var canonicalJson = "Timestamp(1, 2)";
            Assert.AreEqual(canonicalJson, BsonSerializer.Deserialize<BsonTimestamp>(new StringReader(json)).ToJson());
        }

        [Test]
        public void TestTimestampExtendedJsonOldRepresentation()
        {
            var json = "{ \"$timestamp\" : NumberLong(1234) }";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.Timestamp, _bsonReader.ReadBsonType());
                Assert.AreEqual(1234L, _bsonReader.ReadTimestamp());
                Assert.AreEqual(BsonReaderState.Done, _bsonReader.State);
            }
            var canonicalJson = "Timestamp(0, 1234)";
            Assert.AreEqual(canonicalJson, BsonSerializer.Deserialize<BsonTimestamp>(new StringReader(json)).ToJson());
        }

        [Test]
        public void TestUndefinedExtendedJson()
        {
            var json = "{ \"$undefined\" : true }";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.Undefined, _bsonReader.ReadBsonType());
                _bsonReader.ReadUndefined();
                Assert.AreEqual(BsonReaderState.Done, _bsonReader.State);
            }
            var canonicalJson = "undefined";
            Assert.AreEqual(canonicalJson, BsonSerializer.Deserialize<BsonUndefined>(new StringReader(json)).ToJson());
        }

        [Test]
        public void TestUndefinedKeyword()
        {
            var json = "undefined";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.Undefined, _bsonReader.ReadBsonType());
                _bsonReader.ReadUndefined();
                Assert.AreEqual(BsonReaderState.Done, _bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonUndefined>(json).ToJson());
        }

        [Test]
        public void TestUtf16BigEndian()
        {
            var encoding = new UnicodeEncoding(true, false, true);

            var bytes = BsonUtils.ParseHexString("007b00200022007800220020003a002000310020007d");
            using (var memoryStream = new MemoryStream(bytes))
            using (var streamReader = new StreamReader(memoryStream, encoding))
            {
                var document = BsonSerializer.Deserialize<BsonDocument>(streamReader);
                Assert.AreEqual(1, document["x"].AsInt32);
            }
        }

        [Test]
        public void TestUtf16BigEndianAutoDetect()
        {
            var bytes = BsonUtils.ParseHexString("feff007b00200022007800220020003a002000310020007d");
            using (var memoryStream = new MemoryStream(bytes))
            using (var streamReader = new StreamReader(memoryStream, true))
            {
                var document = BsonSerializer.Deserialize<BsonDocument>(streamReader);
                Assert.AreEqual(1, document["x"].AsInt32);
            }
        }

        [Test]
        public void TestUtf16LittleEndian()
        {
            var encoding = new UnicodeEncoding(false, false, true);

            var bytes = BsonUtils.ParseHexString("7b00200022007800220020003a002000310020007d00");
            using (var memoryStream = new MemoryStream(bytes))
            using (var streamReader = new StreamReader(memoryStream, encoding))
            {
                var document = BsonSerializer.Deserialize<BsonDocument>(streamReader);
                Assert.AreEqual(1, document["x"].AsInt32);
            }
        }

        [Test]
        public void TestUtf16LittleEndianAutoDetect()
        {
            var bytes = BsonUtils.ParseHexString("fffe7b00200022007800220020003a002000310020007d00");
            using (var memoryStream = new MemoryStream(bytes))
            using (var streamReader = new StreamReader(memoryStream, true))
            {
                var document = BsonSerializer.Deserialize<BsonDocument>(streamReader);
                Assert.AreEqual(1, document["x"].AsInt32);
            }
        }

        [Test]
        public void TestUtf8()
        {
            var encoding = new UTF8Encoding(false, true);

            var bytes = BsonUtils.ParseHexString("7b20227822203a2031207d");
            using (var memoryStream = new MemoryStream(bytes))
            using (var streamReader = new StreamReader(memoryStream, encoding))
            {
                var document = BsonSerializer.Deserialize<BsonDocument>(streamReader);
                Assert.AreEqual(1, document["x"].AsInt32);
            }
        }

        [Test]
        public void TestUtf8AutoDetect()
        {
            var bytes = BsonUtils.ParseHexString("7b20227822203a2031207d");
            using (var memoryStream = new MemoryStream(bytes))
            using (var streamReader = new StreamReader(memoryStream, true))
            {
                var document = BsonSerializer.Deserialize<BsonDocument>(streamReader);
                Assert.AreEqual(1, document["x"].AsInt32);
            }
        }
    }
}

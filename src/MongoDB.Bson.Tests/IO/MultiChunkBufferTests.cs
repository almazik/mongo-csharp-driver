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
using System.Threading.Tasks;
using MongoDB.Bson.IO;
using NUnit.Framework;

namespace MongoDB.Bson.Tests.IO
{
    [TestFixture]
    public class MultiChunkBufferTests
    {
        [Test]
        public void TestGetSingleChunkSlice()
        {
            var chunkSize = BsonChunkPool.Default.ChunkSize;
            var length = chunkSize * 3;
            using (var buffer = ByteBufferFactory.Create(BsonChunkPool.Default, length))
            {
                buffer.MakeReadOnly();
                var slice = buffer.GetSlice(chunkSize, 1);
                Assert.IsInstanceOf<SingleChunkBuffer>(slice);
            }
        }

        [Test]
        public void TestGetMultipleChunkSlice()
        {
            var chunkSize = BsonChunkPool.Default.ChunkSize;
            var length = chunkSize * 3;
            using (var buffer = ByteBufferFactory.Create(BsonChunkPool.Default, length))
            {
                buffer.MakeReadOnly();
                var slice = buffer.GetSlice(chunkSize, chunkSize + 1);
                Assert.IsInstanceOf<MultiChunkBuffer>(slice);
            }
        }

        [Test]
        public void TestWriteTo()
        {
            var expected = new byte[] { 1, 2, 3, 4, 5 };

            var chunkSize = BsonChunkPool.Default.ChunkSize;
            var length = chunkSize * 3;
            using (var buffer = ByteBufferFactory.Create(BsonChunkPool.Default, length))
            using (var memoryStream = new MemoryStream())
            {
                buffer.WriteBytes(0, expected, 0, 5);
                buffer.Length = 5;

                buffer.WriteTo(memoryStream);
                Assert.AreEqual(5, memoryStream.Length);
                Assert.AreEqual(expected, memoryStream.ToArray());
            }
        }

        [Test]
        public void TestLoadFrom()
        {
            var expected = new byte[] { 1, 2, 3, 4, 5 };

            var chunkSize = BsonChunkPool.Default.ChunkSize;
            var length = chunkSize * 3;

            using (var buffer = ByteBufferFactory.Create(BsonChunkPool.Default, length))
            using (var memoryStream = new MemoryStream(expected))
            {
                buffer.LoadFrom(memoryStream, 10, 5);

                var actual = new byte[5];
                buffer.ReadBytes(10, actual, 0, 5);
                Assert.AreEqual(expected, actual);
            }
        }

        [Test]
        public async Task TestWriteToAsync()
        {
            var expected = new byte[] { 1, 2, 3, 4, 5 };

            var chunkSize = BsonChunkPool.Default.ChunkSize;
            var length = chunkSize * 3;
            using (var buffer = ByteBufferFactory.Create(BsonChunkPool.Default, length))
            using (var memoryStream = new MemoryStream())
            {
                buffer.WriteBytes(0, expected, 0, 5);
                buffer.Length = 5;

                await buffer.WriteToAsync(memoryStream);
                Assert.AreEqual(5, memoryStream.Length);
                Assert.AreEqual(expected, memoryStream.ToArray());
            }
        }

        [Test]
        public async Task TestLoadFromAsync()
        {
            var expected = new byte[] { 1, 2, 3, 4, 5 };

            var chunkSize = BsonChunkPool.Default.ChunkSize;
            var length = chunkSize * 3;

            using (var buffer = ByteBufferFactory.Create(BsonChunkPool.Default, length))
            using (var memoryStream = new MemoryStream(expected))
            {
                await buffer.LoadFromAsync(memoryStream, 10, 5);

                var actual = new byte[5];
                buffer.ReadBytes(10, actual, 0, 5);
                Assert.AreEqual(expected, actual);
            }
        }
    }
}

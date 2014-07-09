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

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson.IO;
using NUnit.Framework;

namespace MongoDB.Bson.Tests.IO
{
    [TestFixture]
    public class ByteBufferFactoryTests
    {
        [Test]
        public async Task TestLoadLengthPrefixedDataFromAsync()
        {
            //data is 12 bytes long, prefixed with 4 bytes of length = 16 bytes total
            var expectedData = new byte[] { 16, 0, 0, 0, 10, 20, 30, 40, 50, 60, 70, 80, 90, 100, 110, 120 };

            using (var memoryStream = new MemoryStream(expectedData))
            {
                var byteBuffer = await ByteBufferFactory.LoadLengthPrefixedDataFromAsync(memoryStream);
                Assert.AreEqual(expectedData, byteBuffer.AccessBackingBytes(0).ToArray());
            }
        }
    }
}

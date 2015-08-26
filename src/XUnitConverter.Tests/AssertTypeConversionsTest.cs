// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

using Xunit;

namespace XUnitConverter.Tests
{
    public class AssertTypeConverterTest : ConverterTestBase
    {
        protected override ConverterBase CreateConverter()
        {
            return new AssertTypeConverter();
        }

        [Fact]
        public async Task TestReplaceIsNotInstanceOfType()
        {
            string source = @"
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

public class Tests
{
    public void TestA()
    {
        Assert.IsInstanceOfType(string.Empty, typeof(String));
        Assert.IsNotInstanceOfType(string.Empty, typeof(String));
    }
}
";
            string expected = @"
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

public class Tests
{
    public void TestA()
    {
        Assert.IsType<String>(string.Empty);
        Assert.IsNotType<String>(string.Empty);
    }
}
";

            await Verify(source, expected);
        }
    }
}

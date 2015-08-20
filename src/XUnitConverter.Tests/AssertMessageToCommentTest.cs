// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;

using Xunit;

namespace XUnitConverter.Tests
{
    public class AssertMessageToCommentTest : ConverterTestBase
    {
        protected override ConverterBase CreateConverter()
        {
            return new AssertMessageToCommentConverter();
        }

        [Fact]
        public async Task TestSwapInvertedEqual()
        {
            string source = @"
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

public class Tests
{
    public void TestA()
    {
        Assert.AreEqual(1, 1, ""message"");
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
        Assert.AreEqual(1, 1); // ""message""
    }
}
";

            await Verify(source, expected);
        }
    }
}

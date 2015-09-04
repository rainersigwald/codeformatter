﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Xunit;
using System.Threading;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;

namespace XUnitConverter.Tests
{
    public class MSTestToXUnitConverterTests : ConverterTestBase
    {
        protected override XUnitConverter.ConverterBase CreateConverter()
        {
            return new XUnitConverter.MSTestToXUnitConverter();
        }

        [Fact]
        public async Task TestUpdatesUsingStatements()
        {
            var text = @"
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Composition.UnitTests
{
}
";

            var expected = @"
using System;
using Xunit;

namespace System.Composition.UnitTests
{
}
";
            await Verify(text, expected);
        }

        [Fact]
        public async Task TestUpdatesUsingStatementsWithIfDefs()
        {
            var text = @"
using System;
#if NETFX_CORE
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#elif PORTABLE_TESTS
using Microsoft.Bcl.Testing;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endif
namespace System.Composition.UnitTests
{
}
";

            var expected = @"
using System;
using Xunit;

namespace System.Composition.UnitTests
{
}
";
            await Verify(text, expected);
        }

        [Fact]
        public async Task TestRemovesTestClassAttributes()
        {
            var text = @"
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Composition.UnitTests
{
    [TestClass]
    public class MyTestClass
    {
    }
}
";

            var expected = @"
using System;
using Xunit;

namespace System.Composition.UnitTests
{
    public class MyTestClass
    {
    }
}
";
            await Verify(text, expected);
        }

        [Fact]
        public async Task TestRemovesMultipleTestClassAttributes()
        {
            var text = @"
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Composition.UnitTests
{
    [TestClass]
    public class MyTestClass
    {
    }

    [TestClass]
    public class MyTestClass2
    {
    }
}
";

            var expected = @"
using System;
using Xunit;

namespace System.Composition.UnitTests
{
    public class MyTestClass
    {
    }

    public class MyTestClass2
    {
    }
}
";
            await Verify(text, expected);
        }


        [Fact]
        public async Task TestPreserveClassDocComments()
        {
            string text = @"
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Composition.UnitTests
{
    /// <summary>
    /// Some sort of doc comment.
    /// </summary>
    [TestClass]
    public class MyTestClass
    {
    }
}
";
            var expected = @"
using System;
using Xunit;

namespace System.Composition.UnitTests
{
    /// <summary>
    /// Some sort of doc comment.
    /// </summary>
    public class MyTestClass
    {
    }
}
";
            await Verify(text, expected);
        }


        [Fact]
        public async Task TestUpdatesTestMethodAttributes()
        {
            var text = @"
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Composition.UnitTests
{
    public class MyTestClass
    {
        [TestMethod]
        public void MyTestMethod()
        {
        }
    }
}
";

            var expected = @"
using System;
using Xunit;

namespace System.Composition.UnitTests
{
    public class MyTestClass
    {
        [Fact]
        public void MyTestMethod()
        {
        }
    }
}
";
            await Verify(text, expected);
        }

        [Fact]
        public async Task TestSkipsIgnoredTests()
        {
            var text = @"
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Composition.UnitTests
{
    public class MyTestClass
    {
        [TestMethod]
        [Ignore]
        public void MyTestMethod()
        {
        }
    }
}
";

            var expected = @"
using System;
using Xunit;

namespace System.Composition.UnitTests
{
    public class MyTestClass
    {
        [Fact(Skip = ""Ignored in MSTest"")]
        public void MyTestMethod()
        {
        }
    }
}
";
            await Verify(text, expected);
        }

        [Fact]
        public async Task TestUpdatesAsserts()
        {
            var text = @"
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Composition.UnitTests
{
    public class MyTestClass
    {
        public void MyTestMethod()
        {
            object obj = new object();

            Assert.AreEqual(1, 1);
            Assert.AreNotEqual(1, 2);
            Assert.IsNull(null);
            Assert.IsNotNull(obj);
            Assert.AreSame(obj, obj);
            Assert.AreNotSame(obj, new object());
            Assert.IsTrue(true);
            Assert.IsFalse(false);
            Assert.IsInstanceOfType(string.Empty, typeof(String));
        }
    }
}
";

            var expected = @"
using System;
using Xunit;

namespace System.Composition.UnitTests
{
    public class MyTestClass
    {
        public void MyTestMethod()
        {
            object obj = new object();

            Assert.Equal(1, 1);
            Assert.NotEqual(1, 2);
            Assert.Null(null);
            Assert.NotNull(obj);
            Assert.Same(obj, obj);
            Assert.NotSame(obj, new object());
            Assert.True(true);
            Assert.False(false);
            Assert.IsAssignableFrom(typeof(String), string.Empty);
        }
    }
}
";
            await Verify(text, expected);
        }

        [Fact]
        public async Task TestWrapWholeExpectedExceptionBlock()
        {
            string source = @"
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

public class Tests
{
    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void TestA()
    {
        Assert.AreEqual(1, 1);
    }
}
";
            string expected = @"
using System;
using Xunit;

public class Tests
{
    [Fact]
    public void TestA()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            Assert.Equal(1, 1);
        }
       );
    }
}
";

            await Verify(source, expected);
        }

        [Fact]
        public async Task TestRemoveExpectedExceptionMoreThanOnce()
        {
            string source = @"
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

public class Tests
{
    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void TestA()
    {
        Assert.AreEqual(1, 1);
    }

    [ExpectedException(typeof(ArgumentNullException))]
    [TestMethod]
    public void TestB()
    {
        Assert.AreEqual(1, 1);
    }

}
";
            string expected = @"
using System;
using Xunit;

public class Tests
{
    [Fact]
    public void TestA()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            Assert.Equal(1, 1);
        }
       );
    }

    [Fact]
    public void TestB()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            Assert.Equal(1, 1);
        }
       );
    }
}
";

            await Verify(source, expected);
        }
    }
}

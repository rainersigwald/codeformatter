// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XUnitConverter
{
    public sealed class AssertMessageToCommentConverter : ConverterBase
    {
        private class Rewriter : CSharpSyntaxRewriter
        {
            private readonly SemanticModel _model;

            private static readonly Dictionary<string, int> s_targetMethodNumberOfArguments = new Dictionary<string, int>
            {
                {"Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual", 2},
                {"Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreNotEqual", 2},
                {"Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreNotSame", 2},
                {"Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreSame", 2},
                {"Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsFalse", 1},
                {"Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsInstanceOfType", 2},
                {"Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsNotInstanceOfType", 2},
                {"Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsNotNull", 1},
                {"Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue", 1},
                {"Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsNull", 1},
            };

            public Rewriter(SemanticModel model)
            {
                _model = model;
            }


            public override SyntaxNode VisitExpressionStatement(ExpressionStatementSyntax node)
            {
                var invocation = node.Expression as InvocationExpressionSyntax;
                if (invocation != null)
                {
                    var symbol = _model.GetSymbolInfo(invocation.Expression).Symbol as IMethodSymbol;
                    if (symbol != null)
                    {
                        int expectedArgumentCount;
                        if (s_targetMethodNumberOfArguments.TryGetValue(NameHelper.GetFullName(symbol), out expectedArgumentCount))
                        {
                            // if there's one extra argument . . .
                            if (invocation.ArgumentList.Arguments.Count == expectedArgumentCount + 1)
                            {
                                // . . . and it's a string literal . . .
                                if (invocation.ArgumentList.Arguments[expectedArgumentCount].Expression.Kind() == SyntaxKind.StringLiteralExpression)
                                {
                                    string messageText = (invocation.ArgumentList.Arguments[expectedArgumentCount].Expression).ToFullString();

                                    // . . . then take its text and make it a comment.
                                    var argumentsMinusMessage = invocation.ArgumentList.Arguments.RemoveAt(expectedArgumentCount);

                                    return SyntaxFactory.ExpressionStatement(
                                            invocation.WithArgumentList(
                                                SyntaxFactory.ArgumentList(argumentsMinusMessage)),
                                                SyntaxFactory.Token(
                                                    SyntaxFactory.TriviaList(),
                                                    SyntaxKind.SemicolonToken,
                                                    SyntaxFactory.TriviaList(
                                                        new SyntaxTrivia[] {
                                                            SyntaxFactory.Space,
                                                            SyntaxFactory.Comment(
                                                                @"// " + messageText) }
                                                        .Concat(node.GetTrailingTrivia()))));
                                }
                            }
                        }
                    }
                }

                return base.VisitExpressionStatement(node);
            }
        }

        protected override async Task<Solution> ProcessAsync(
        Document document,
        SyntaxNode syntaxRoot,
        CancellationToken cancellationToken)
        {
            var rewriter = new Rewriter(await document.GetSemanticModelAsync(cancellationToken));
            var newNode = rewriter.Visit(syntaxRoot);

            return document.Project.Solution.WithDocumentSyntaxRoot(document.Id, newNode);
        }
    }
}

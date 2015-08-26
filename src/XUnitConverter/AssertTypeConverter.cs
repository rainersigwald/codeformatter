// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XUnitConverter
{
    public sealed class AssertTypeConverter : ConverterBase
    {
        private class Rewriter : CSharpSyntaxRewriter
        {
            private readonly SemanticModel _model;

            const int IndexOfTypeParameter = 1;
            private const string DestinationNamespaceName = @"Assert";
            private static readonly Dictionary<string, string> s_typeCheckMethodReplacements = new Dictionary<string, string>
            {
                {"Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsInstanceOfType", "IsType"},
                {"Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsNotInstanceOfType", "IsNotType"},
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
                        string newMethodName;
                        if (s_typeCheckMethodReplacements.TryGetValue(NameHelper.GetFullName(symbol), out newMethodName))
                        {
                            // try to extract generic type.  For now only try to understand typeof()
                            var typeParameter = invocation.ArgumentList.Arguments[IndexOfTypeParameter].Expression as TypeOfExpressionSyntax;
                            if (typeParameter != null)
                            {
                                var expectedType = typeParameter.Type;

                                return SyntaxFactory.ExpressionStatement(
                                    // The new expression is basically the old one . . .
                                    invocation.WithExpression(
                                        // With the member part replaced by the xunit equivalent
                                        SyntaxFactory.MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            SyntaxFactory.IdentifierName(DestinationNamespaceName),
                                            SyntaxFactory.GenericName(
                                                SyntaxFactory.Identifier(newMethodName))
                                            // and the expected type moved into the generic type argument list . . .
                                            .WithTypeArgumentList(
                                                SyntaxFactory.TypeArgumentList(
                                                    SyntaxFactory.SingletonSeparatedList(expectedType)))))
                                        .WithArgumentList(
                                            SyntaxFactory.ArgumentList(
                                                // and removed from the argument list.
                                                invocation.ArgumentList.Arguments.RemoveAt(IndexOfTypeParameter))))
                                    .WithTriviaFrom(node);
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

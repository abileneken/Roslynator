﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Roslynator.Metrics.CSharp
{
    internal class CSharpCodeMetricsWalker : CSharpSyntaxWalker
    {
        public int CommentLineCount { get; set; }

        public int PreprocessorDirectiveLineCount { get; set; }

        public int BlockBoundaryLineCount { get; set; }

        public TextLineCollection Lines { get; }

        public CodeMetricsOptions Options { get; }

        public CancellationToken CancellationToken { get; }

        public CSharpCodeMetricsWalker(TextLineCollection lines, CodeMetricsOptions options, CancellationToken cancellationToken)
            : base(SyntaxWalkerDepth.Trivia)
        {
            Lines = lines;
            Options = options;
            CancellationToken = cancellationToken;
        }

        public override void VisitTrivia(SyntaxTrivia trivia)
        {
            if (trivia.IsDirective)
            {
                if (!Options.IncludePreprocessorDirectives)
                {
                    PreprocessorDirectiveLineCount++;
                }
            }
            else if (!Options.IncludeComments)
            {
                switch (trivia.Kind())
                {
                    case SyntaxKind.SingleLineCommentTrivia:
                        {
                            TextSpan span = trivia.Span;

                            TextLine line = Lines.GetLineFromPosition(span.Start);

                            if (line.IsEmptyOrWhiteSpace(TextSpan.FromBounds(line.Start, span.Start)))
                            {
                                CommentLineCount++;
                            }

                            break;
                        }
                    case SyntaxKind.SingleLineDocumentationCommentTrivia:
                        {
                            CommentLineCount += Lines.GetLineCount(trivia.Span) - 1;
                            break;
                        }
                    case SyntaxKind.MultiLineCommentTrivia:
                        {
                            TextSpan span = trivia.Span;

                            TextLine line = Lines.GetLineFromPosition(span.Start);

                            if (line.IsEmptyOrWhiteSpace(TextSpan.FromBounds(line.Start, span.Start)))
                            {
                                int lineCount = Lines.GetLineCount(trivia.Span);

                                if (lineCount == 1
                                    || line.IsEmptyOrWhiteSpace(TextSpan.FromBounds(Lines.GetLineFromPosition(span.End).End, span.End)))
                                {
                                    CommentLineCount += lineCount;
                                }
                            }

                            break;
                        }
                    case SyntaxKind.MultiLineDocumentationCommentTrivia:
                        {
                            CommentLineCount += Lines.GetLineCount(trivia.Span);
                            break;
                        }
                }
            }

            base.VisitTrivia(trivia);
        }

        public override void VisitAccessorList(AccessorListSyntax node)
        {
            VisitBraces(node.OpenBraceToken, node.CloseBraceToken);

            base.VisitAccessorList(node);
        }

        public override void VisitAnonymousObjectCreationExpression(AnonymousObjectCreationExpressionSyntax node)
        {
            VisitBraces(node.OpenBraceToken, node.CloseBraceToken);

            base.VisitAnonymousObjectCreationExpression(node);
        }

        public override void VisitBlock(BlockSyntax node)
        {
            VisitBraces(node.OpenBraceToken, node.CloseBraceToken);

            base.VisitBlock(node);
        }

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            VisitBraces(node.OpenBraceToken, node.CloseBraceToken);

            base.VisitClassDeclaration(node);
        }

        public override void VisitEnumDeclaration(EnumDeclarationSyntax node)
        {
            VisitBraces(node.OpenBraceToken, node.CloseBraceToken);

            base.VisitEnumDeclaration(node);
        }

        public override void VisitInitializerExpression(InitializerExpressionSyntax node)
        {
            VisitBraces(node.OpenBraceToken, node.CloseBraceToken);

            base.VisitInitializerExpression(node);
        }

        public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
        {
            VisitBraces(node.OpenBraceToken, node.CloseBraceToken);

            base.VisitInterfaceDeclaration(node);
        }

        public override void VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
        {
            VisitBraces(node.OpenBraceToken, node.CloseBraceToken);

            base.VisitNamespaceDeclaration(node);
        }

        public override void VisitStructDeclaration(StructDeclarationSyntax node)
        {
            VisitBraces(node.OpenBraceToken, node.CloseBraceToken);

            base.VisitStructDeclaration(node);
        }

        public override void VisitSwitchStatement(SwitchStatementSyntax node)
        {
            VisitBraces(node.OpenBraceToken, node.CloseBraceToken);

            base.VisitSwitchStatement(node);
        }

        private void VisitBraces(in SyntaxToken openBraceToken, in SyntaxToken closeBraceToken)
        {
            if (Options.IgnoreBlockBoundary)
            {
                if (Options.IncludeComments)
                {
                    VisitBrace(openBraceToken);
                    VisitBrace(closeBraceToken);
                }
                else
                {
                    VisitBraceTrivia(openBraceToken);
                    VisitBraceTrivia(closeBraceToken);
                }
            }
        }

        private void VisitBrace(in SyntaxToken braceToken)
        {
            TextSpan span = braceToken.Span;

            TextLine line = Lines.GetLineFromPosition(span.Start);

            if (line.IsEmptyOrWhiteSpace(TextSpan.FromBounds(line.Start, span.Start))
                && line.IsEmptyOrWhiteSpace(TextSpan.FromBounds(span.End, line.End)))
            {
                BlockBoundaryLineCount++;
            }
        }

        private void VisitBraceTrivia(in SyntaxToken braceToken)
        {
            SyntaxTree tree = braceToken.SyntaxTree;

            if (AnalyzeLeadingTrivia(braceToken.LeadingTrivia)
                && AnalyzeTrailingTrivia(braceToken.TrailingTrivia))
            {
                BlockBoundaryLineCount++;
            }

            bool AnalyzeLeadingTrivia(in SyntaxTriviaList leadingTrivia)
            {
                SyntaxTriviaList.Reversed.Enumerator en = leadingTrivia.Reverse().GetEnumerator();

                while (en.MoveNext())
                {
                    switch (en.Current.Kind())
                    {
                        case SyntaxKind.EndOfLineTrivia:
                            {
                                return true;
                            }
                        case SyntaxKind.WhitespaceTrivia:
                            {
                                break;
                            }
                        case SyntaxKind.MultiLineCommentTrivia:
                            {
                                if (tree.IsMultiLineSpan(en.Current.Span, CancellationToken))
                                    return true;

                                break;
                            }
                        default:
                            {
                                return false;
                            }
                    }
                }

                return false;
            }

            bool AnalyzeTrailingTrivia(in SyntaxTriviaList trailingTrivia)
            {
                SyntaxTriviaList.Enumerator en = trailingTrivia.GetEnumerator();

                while (en.MoveNext())
                {
                    switch (en.Current.Kind())
                    {
                        case SyntaxKind.EndOfLineTrivia:
                        case SyntaxKind.SingleLineCommentTrivia:
                            {
                                return true;
                            }
                        case SyntaxKind.WhitespaceTrivia:
                            {
                                break;
                            }
                        case SyntaxKind.MultiLineCommentTrivia:
                            {
                                if (tree.IsMultiLineSpan(en.Current.Span, CancellationToken))
                                    return true;

                                break;
                            }
                        default:
                            {
                                return false;
                            }
                    }
                }

                return false;
            }
        }
    }
}
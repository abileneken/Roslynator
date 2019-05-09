﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslynator.CSharp;
using Roslynator.Formatting.CSharp;

namespace Roslynator.Formatting.CodeFixes.CSharp
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(StatementCodeFixProvider))]
    [Shared]
    public class StatementCodeFixProvider : BaseCodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get
            {
                return ImmutableArray.Create(
                    DiagnosticIdentifiers.AddNewLineBeforeStatement,
                    DiagnosticIdentifiers.AddNewLineBeforeEmbeddedStatement,
                    DiagnosticIdentifiers.AddNewLineAfterSwitchLabel,
                    DiagnosticIdentifiers.AddEmptyLineAfterEmbeddedStatement);
            }
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            SyntaxNode root = await context.GetSyntaxRootAsync().ConfigureAwait(false);

            if (!TryFindFirstAncestorOrSelf(root, context.Span, out StatementSyntax statement))
                return;

            Document document = context.Document;
            Diagnostic diagnostic = context.Diagnostics[0];

            switch (diagnostic.Id)
            {
                case DiagnosticIdentifiers.AddNewLineBeforeStatement:
                case DiagnosticIdentifiers.AddNewLineBeforeEmbeddedStatement:
                case DiagnosticIdentifiers.AddNewLineAfterSwitchLabel:
                    {
                        CodeAction codeAction = CodeAction.Create(
                            "Add new line",
                            ct => AddNewLineBeforeStatementAsync(document, statement, ct),
                            GetEquivalenceKey(diagnostic));

                        context.RegisterCodeFix(codeAction, diagnostic);
                        break;
                    }
                case DiagnosticIdentifiers.AddEmptyLineAfterEmbeddedStatement:
                    {
                        CodeAction codeAction = CodeAction.Create(
                            "Add empty line",
                            ct => AddEmptyLineAfterEmbeddedStatementAsync(document, statement, ct),
                            GetEquivalenceKey(diagnostic));

                        context.RegisterCodeFix(codeAction, diagnostic);
                        break;
                    }
            }
        }

        private static async Task<Document> AddNewLineBeforeStatementAsync(
            Document document,
            StatementSyntax statement,
            CancellationToken cancellationToken)
        {
            StatementSyntax newStatement = statement
                .WithLeadingTrivia(statement.GetLeadingTrivia().Insert(0, CSharpFactory.NewLine()))
                .WithFormatterAnnotation();

            if (statement.IsParentKind(SyntaxKind.Block))
            {
                var block = (BlockSyntax)statement.Parent;

                if (block.IsSingleLine(includeExteriorTrivia: false))
                {
                    SyntaxTriviaList triviaList = block.CloseBraceToken.LeadingTrivia
                        .Add(CSharpFactory.NewLine());

                    BlockSyntax newBlock = block
                        .WithCloseBraceToken(block.CloseBraceToken.WithLeadingTrivia(triviaList))
                        .WithStatements(block.Statements.Replace(statement, newStatement))
                        .WithFormatterAnnotation();

                    return await document.ReplaceNodeAsync(block, newBlock, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    return await document.ReplaceNodeAsync(statement, newStatement, cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                return await document.ReplaceNodeAsync(statement, newStatement, cancellationToken).ConfigureAwait(false);
            }
        }

        private static Task<Document> AddEmptyLineAfterEmbeddedStatementAsync(
            Document document,
            StatementSyntax statement,
            CancellationToken cancellationToken)
        {
            StatementSyntax newNode = statement
                .AppendToTrailingTrivia(CSharpFactory.NewLine())
                .WithFormatterAnnotation();

            return document.ReplaceNodeAsync(statement, newNode, cancellationToken);
        }
    }
}
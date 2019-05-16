﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Roslynator.CSharp;

namespace Roslynator.Formatting.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class RemoveNewLineBetweenIfKeywordAndElseKeywordAnalyzer : BaseDiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return ImmutableArray.Create(DiagnosticDescriptors.RemoveNewLineBetweenIfKeywordAndElseKeyword); }
        }

        public override void Initialize(AnalysisContext context)
        {
            base.Initialize(context);

            context.RegisterSyntaxNodeAction(AnalyzeElseClause, SyntaxKind.ElseClause);
        }

        private static void AnalyzeElseClause(SyntaxNodeAnalysisContext context)
        {
            var elseClause = (ElseClauseSyntax)context.Node;

            StatementSyntax statement = elseClause.Statement;

            if (!statement.IsKind(SyntaxKind.IfStatement))
                return;

            if (!SyntaxTriviaAnalysis.IsOptionalWhitespaceThenEndOfLineTrivia(elseClause.ElseKeyword.TrailingTrivia))
                return;

            if (!statement.GetLeadingTrivia().IsEmptyOrWhitespace())
                return;

            context.ReportDiagnostic(
                DiagnosticDescriptors.RemoveNewLineBetweenIfKeywordAndElseKeyword,
                Location.Create(elseClause.SyntaxTree, new TextSpan(elseClause.ElseKeyword.Span.End, 0)));
        }
    }
}

﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Pihrtsoft.CodeAnalysis.CSharp.Refactoring
{
    internal static class MemberDeclarationRefactoring
    {
        public static async Task ComputeRefactoringsAsync(RefactoringContext context, MemberDeclarationSyntax member)
        {
            SplitAttributesRefactoring.ComputeRefactoring(context, member);
            MergeAttributesRefactoring.ComputeRefactoring(context, member);

            switch (member.Kind())
            {
                case SyntaxKind.MethodDeclaration:
                case SyntaxKind.IndexerDeclaration:
                case SyntaxKind.PropertyDeclaration:
                case SyntaxKind.OperatorDeclaration:
                case SyntaxKind.ConversionOperatorDeclaration:
                case SyntaxKind.ConstructorDeclaration:
                case SyntaxKind.EventDeclaration:
                case SyntaxKind.NamespaceDeclaration:
                case SyntaxKind.ClassDeclaration:
                case SyntaxKind.StructDeclaration:
                case SyntaxKind.InterfaceDeclaration:
                    {
                        if (CanBeRemovedOrDuplicated(context, member))
                        {
                            if (context.Settings.IsRefactoringEnabled(RefactoringIdentifiers.RemoveMember))
                            {
                                context.RegisterRefactoring(
                                    "Remove " + SyntaxHelper.GetSyntaxNodeTitle(member),
                                    cancellationToken => RemoveMemberDeclarationRefactoring.RefactorAsync(context.Document, member, cancellationToken));
                            }

                            if (context.Settings.IsRefactoringEnabled(RefactoringIdentifiers.DuplicateMember))
                            {
                                context.RegisterRefactoring(
                                    "Duplicate " + SyntaxHelper.GetSyntaxNodeTitle(member),
                                    cancellationToken => DuplicateMemberDeclarationRefactoring.RefactorAsync(context.Document, member, cancellationToken));
                            }

                            if (context.Settings.IsRefactoringEnabled(RefactoringIdentifiers.CommentOutMember))
                                CommentOutRefactoring.RegisterRefactoring(context, member);
                        }

                        break;
                    }
            }

#if DEBUG
            switch (member.Kind())
            {
                case SyntaxKind.MethodDeclaration:
                case SyntaxKind.OperatorDeclaration:
                case SyntaxKind.ConversionOperatorDeclaration:
                case SyntaxKind.ConstructorDeclaration:
                    {
                        if (RemoveAllStatementsRefactoring.CanRefactor(context, member))
                        {
                            context.RegisterRefactoring(
                                "Remove all statements",
                                cancellationToken => RemoveAllStatementsRefactoring.RefactorAsync(context.Document, member, cancellationToken));
                        }

                        break;
                    }
            }
#endif

            if (context.Settings.IsRefactoringEnabled(RefactoringIdentifiers.SwapMembers)
                && !member.Span.IntersectsWith(context.Span))
            {
                SwapMembersRefactoring.ComputeRefactoring(context, member);
            }

            switch (member.Kind())
            {
                case SyntaxKind.MethodDeclaration:
                    {
                        await MethodDeclarationRefactoring.ComputeRefactoringsAsync(context, (MethodDeclarationSyntax)member);
                        break;
                    }
                case SyntaxKind.ConstructorDeclaration:
                    {
                        ComputeRefactorings(context, (ConstructorDeclarationSyntax)member);
                        break;
                    }
                case SyntaxKind.IndexerDeclaration:
                    {
                        IndexerDeclarationRefactoring.ComputeRefactorings(context, (IndexerDeclarationSyntax)member);
                        break;
                    }
                case SyntaxKind.PropertyDeclaration:
                    {
                        await PropertyDeclarationRefactoring.ComputeRefactoringsAsync(context, (PropertyDeclarationSyntax)member);
                        break;
                    }
                case SyntaxKind.OperatorDeclaration:
                    {
                        ComputeRefactorings(context, (OperatorDeclarationSyntax)member);
                        break;
                    }
                case SyntaxKind.ConversionOperatorDeclaration:
                    {
                        ComputeRefactorings(context, (ConversionOperatorDeclarationSyntax)member);
                        break;
                    }
                case SyntaxKind.FieldDeclaration:
                    {
                        await FieldDeclarationRefactoring.ComputeRefactoringsAsync(context, (FieldDeclarationSyntax)member);
                        break;
                    }
                case SyntaxKind.EventDeclaration:
                    {
                        ComputeRefactorings(context, (EventDeclarationSyntax)member);
                        break;
                    }
                case SyntaxKind.EventFieldDeclaration:
                    {
                        EventFieldDeclarationRefactoring.ComputeRefactorings(context, (EventFieldDeclarationSyntax)member);
                        break;
                    }
            }
        }

        private static void ComputeRefactorings(RefactoringContext context, ConstructorDeclarationSyntax constructorDeclaration)
        {
            if (context.Settings.IsRefactoringEnabled(RefactoringIdentifiers.MarkMemberAsStatic)
                && constructorDeclaration.Span.Contains(context.Span)
                && MarkMemberAsStaticRefactoring.CanRefactor(constructorDeclaration))
            {
                context.RegisterRefactoring(
                    "Mark constructor as static",
                    cancellationToken => MarkMemberAsStaticRefactoring.RefactorAsync(context.Document, constructorDeclaration, cancellationToken));

                MarkAllMembersAsStaticRefactoring.RegisterRefactoring(context, (ClassDeclarationSyntax)constructorDeclaration.Parent);
            }
        }

        private static void ComputeRefactorings(RefactoringContext context, OperatorDeclarationSyntax operatorDeclaration)
        {
            if (context.Settings.IsRefactoringEnabled(RefactoringIdentifiers.UseExpressionBodiedMember)
                && operatorDeclaration.Body?.Span.Contains(context.Span) == true
                && context.SupportsCSharp6
                && UseExpressionBodiedMemberRefactoring.CanRefactor(operatorDeclaration))
            {
                context.RegisterRefactoring(
                    "Use expression-bodied member",
                    cancellationToken => UseExpressionBodiedMemberRefactoring.RefactorAsync(context.Document, operatorDeclaration, cancellationToken));
            }
        }

        private static void ComputeRefactorings(RefactoringContext context, ConversionOperatorDeclarationSyntax operatorDeclaration)
        {
            if (context.Settings.IsRefactoringEnabled(RefactoringIdentifiers.UseExpressionBodiedMember)
                && operatorDeclaration.Body?.Span.Contains(context.Span) == true
                && context.SupportsCSharp6
                && UseExpressionBodiedMemberRefactoring.CanRefactor(operatorDeclaration))
            {
                context.RegisterRefactoring(
                    "Use expression-bodied member",
                    cancellationToken => UseExpressionBodiedMemberRefactoring.RefactorAsync(context.Document, operatorDeclaration, cancellationToken));
            }
        }

        private static void ComputeRefactorings(RefactoringContext context, EventDeclarationSyntax eventDeclaration)
        {
            if (context.Settings.IsRefactoringEnabled(RefactoringIdentifiers.MarkMemberAsStatic)
                && eventDeclaration.Span.Contains(context.Span)
                && MarkMemberAsStaticRefactoring.CanRefactor(eventDeclaration))
            {
                context.RegisterRefactoring(
                    "Mark event as static",
                    cancellationToken => MarkMemberAsStaticRefactoring.RefactorAsync(context.Document, eventDeclaration, cancellationToken));

                MarkAllMembersAsStaticRefactoring.RegisterRefactoring(context, (ClassDeclarationSyntax)eventDeclaration.Parent);
            }
        }

        private static bool CanBeRemovedOrDuplicated(RefactoringContext context, MemberDeclarationSyntax member)
        {
            if (!context.Settings.IsAnyRefactoringEnabled(
                    RefactoringIdentifiers.RemoveMember,
                    RefactoringIdentifiers.DuplicateMember))
            {
                return false;
            }

            if (member.Parent?.IsAnyKind(
                    SyntaxKind.NamespaceDeclaration,
                    SyntaxKind.ClassDeclaration,
                    SyntaxKind.StructDeclaration,
                    SyntaxKind.InterfaceDeclaration) != true)
            {
                return false;
            }

            switch (member.Kind())
            {
                case SyntaxKind.MethodDeclaration:
                    {
                        return ((MethodDeclarationSyntax)member).Body?.BraceContains(context.Span) == true;
                    }
                case SyntaxKind.IndexerDeclaration:
                    {
                        return ((IndexerDeclarationSyntax)member).AccessorList?.BraceContains(context.Span) == true;
                    }
                case SyntaxKind.OperatorDeclaration:
                    {
                        return ((OperatorDeclarationSyntax)member).Body?.BraceContains(context.Span) == true;
                    }
                case SyntaxKind.ConversionOperatorDeclaration:
                    {
                        return ((ConversionOperatorDeclarationSyntax)member).Body?.BraceContains(context.Span) == true;
                    }
                case SyntaxKind.ConstructorDeclaration:
                    {
                        return ((ConstructorDeclarationSyntax)member).Body?.BraceContains(context.Span) == true;
                    }
                case SyntaxKind.PropertyDeclaration:
                    {
                        return ((PropertyDeclarationSyntax)member).AccessorList?.BraceContains(context.Span) == true;
                    }
                case SyntaxKind.EventDeclaration:
                    {
                        return ((EventDeclarationSyntax)member).AccessorList?.BraceContains(context.Span) == true;
                    }
                case SyntaxKind.NamespaceDeclaration:
                    {
                        var declaration = (NamespaceDeclarationSyntax)member;

                        return declaration.OpenBraceToken.Span.Contains(context.Span)
                            || declaration.CloseBraceToken.Span.Contains(context.Span);
                    }
                case SyntaxKind.ClassDeclaration:
                    {
                        var declaration = (ClassDeclarationSyntax)member;

                        return declaration.OpenBraceToken.Span.Contains(context.Span)
                            || declaration.CloseBraceToken.Span.Contains(context.Span);
                    }
                case SyntaxKind.StructDeclaration:
                    {
                        var declaration = (StructDeclarationSyntax)member;

                        return declaration.OpenBraceToken.Span.Contains(context.Span)
                            || declaration.CloseBraceToken.Span.Contains(context.Span);
                    }
                case SyntaxKind.InterfaceDeclaration:
                    {
                        var declaration = (InterfaceDeclarationSyntax)member;

                        return declaration.OpenBraceToken.Span.Contains(context.Span)
                            || declaration.CloseBraceToken.Span.Contains(context.Span);
                    }
            }

            return false;
        }

        private static bool BraceContains(this BlockSyntax body, TextSpan span)
        {
            if (body != null)
            {
                return body.OpenBraceToken.Span.Contains(span)
                    || body.CloseBraceToken.Span.Contains(span);
            }

            return false;
        }

        private static bool BraceContains(this AccessorListSyntax accessorList, TextSpan span)
        {
            if (accessorList != null)
            {
                return accessorList.OpenBraceToken.Span.Contains(span)
                    || accessorList.CloseBraceToken.Span.Contains(span);
            }

            return false;
        }
    }
}
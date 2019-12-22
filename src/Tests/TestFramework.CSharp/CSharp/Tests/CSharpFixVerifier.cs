﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Roslynator.Tests;

namespace Roslynator.CSharp.Tests
{
    public abstract class CSharpFixVerifier : FixVerifier
    {
        protected CSharpFixVerifier() : base(CSharpWorkspaceFactory.Instance)
        {
        }

        public override CodeVerificationOptions Options => CSharpCodeVerificationOptions.Default;
    }
}

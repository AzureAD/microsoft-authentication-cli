// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1124:Do not use regions", Justification = "We want to keep regions as it increases readabiliy.")]
[assembly: SuppressMessage("StyleCop.CSharp.NamingRules", "SA1310:Field names should not contain underscore", Justification = "We prefer screaming snake case for environment variable.")]
[assembly: SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "We might want to keep a few different classes in a single file if it's not length. Doing this specifically to by-pass Exceptions.cs file.")]
[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1649:File name should match first type name", Justification = "This is just an unnescessary mandate.")]
[assembly: SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "Internal fields are needed for tests.")]
[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1625:Element documentation should not be copied and pasted", Justification = "Some elements are legitimate duplicates.")]
[assembly: SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1118:Parameter should not span multiple lines", Justification = "There is no way to define attributes that need to span lines in a way that satisfys this rule.")]
[assembly: SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1000:Keywords should be spaced correctly", Justification ="Rule conflicts with how records are constructed with inferred type.")]

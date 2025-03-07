﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.EditAndContinue.UnitTests;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue.UnitTests
{
    [UseExportProvider]
    public class TopLevelEditingTests : EditingTestBase
    {
        #region Usings

        [Fact]
        public void Using_Global_Insert1()
        {
            var src1 = @"
using System.Collections.Generic;
";
            var src2 = @"
global using D = System.Diagnostics;
global using System.Collections;
using System.Collections.Generic;
";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [global using D = System.Diagnostics;]@2",
                "Insert [global using System.Collections;]@40");

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void Using_Global_Insert2()
        {
            var src1 = @"
using unsafe D3 = int*;
";
            var src2 = @"
global using D1 = int;
using D2 = (int, int);
using unsafe D3 = int*;
";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [global using D1 = int;]@2",
                "Insert [using D2 = (int, int);]@26");

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void Using_Delete1()
        {
            var src1 = @"
using System.Diagnostics;
";
            var src2 = @"";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyEdits("Delete [using System.Diagnostics;]@2");
            Assert.IsType<UsingDirectiveSyntax>(edits.Edits.First().OldNode);
            Assert.Null(edits.Edits.First().NewNode);
        }

        [Fact]
        public void Using_Delete2()
        {
            var src1 = @"
using D = System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
";
            var src2 = @"
using System.Collections.Generic;
";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Delete [using D = System.Diagnostics;]@2",
                "Delete [using System.Collections;]@33");

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void Using_Delete3()
        {
            var src1 = @"
global using D1 = int;
using D2 = (int, int);
using unsafe D3 = int*;
";
            var src2 = @"
using D2 = (int, int);
";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Delete [global using D1 = int;]@2",
                "Delete [using unsafe D3 = int*;]@50");

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void Using_Insert1()
        {
            var src1 = @"
using System.Collections.Generic;
";
            var src2 = @"
using D = System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [using D = System.Diagnostics;]@2",
                "Insert [using System.Collections;]@33");

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void Using_Insert2()
        {
            var src1 = @"
using System.Collections.Generic;
";
            var src2 = @"
global using D1 = int;
using D2 = (int, int);
using unsafe D3 = int*;
using System.Collections.Generic;
";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [global using D1 = int;]@2",
                "Insert [using D2 = (int, int);]@26",
                "Insert [using unsafe D3 = int*;]@50");

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void Using_Update1()
        {
            var src1 = @"
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
";
            var src2 = @"
using System.Diagnostics;
using X = System.Collections;
using System.Collections.Generic;
";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [using System.Collections;]@29 -> [using X = System.Collections;]@29");

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void Using_Update2()
        {
            var src1 = @"
using System.Diagnostics;
using X1 = System.Collections;
using System.Collections.Generic;
";
            var src2 = @"
using System.Diagnostics;
using X2 = System.Collections;
using System.Collections.Generic;
";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [using X1 = System.Collections;]@29 -> [using X2 = System.Collections;]@29");

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void Using_Update3()
        {
            var src1 = @"
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
";
            var src2 = @"
using System;
using System.Collections;
using System.Collections.Generic;
";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [using System.Diagnostics;]@2 -> [using System;]@2");

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void Using_Update4()
        {
            var src1 = @"
using X = int;
using Y = int;
using Z = int;
";
            var src2 = @"
using X = string;
using unsafe Y = int*;
global using Z = int;
";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [using X = int;]@2 -> [using X = string;]@2",
                "Update [using Y = int;]@18 -> [using unsafe Y = int*;]@21",
                "Update [using Z = int;]@34 -> [global using Z = int;]@45");

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void Using_Reorder1()
        {
            var src1 = @"
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
";
            var src2 = @"
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Reorder [using System.Diagnostics;]@2 -> @64");
        }

        [Fact]
        public void Using_Reorder2()
        {
            var src1 = @"
using X = int;
using Y = string;
";
            var src2 = @"
using Y = string;
using X = int;
";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Reorder [using Y = string;]@18 -> @2");
        }

        [Fact]
        public void Using_InsertDelete1()
        {
            var src1 = @"
namespace N
{
    using System.Collections;
}

namespace M
{
}
";
            var src2 = @"
namespace N
{
}

namespace M
{
    using System.Collections;
}
";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [using System.Collections;]@43",
                "Delete [using System.Collections;]@22");
        }

        [Fact]
        public void Using_InsertDelete2()
        {
            var src1 = @"
namespace N
{
    using System.Collections;
}
";
            var src2 = @"
using System.Collections;

namespace N
{
}
";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [using System.Collections;]@2",
                "Delete [using System.Collections;]@22");
        }

        [Fact]
        public void Using_Delete_ChangesCodeMeaning()
        {
            // This test specifically validates the scenario we _don't_ support, namely when inserting or deleting
            // a using directive, if existing code changes in meaning as a result, we don't issue edits for that code.
            // If this ever regresses then please buy a lottery ticket because the feature has magically fixed itself.
            var src1 = @"
using System.IO;
using DirectoryInfo = N.C;

namespace N
{
    public class C
    {
        public C(string a) { }
        public FileAttributes Attributes { get; set; }
    }

    public class D
    {
        public void M()
        {
            var d = new DirectoryInfo(""aa"");
            var x = directoryInfo.Attributes;
        }
    }
}";
            var src2 = @"
using System.IO;

namespace N
{
    public class C
    {
        public C(string a) { }
        public FileAttributes Attributes { get; set; }
    }

    public class D
    {
        public void M()
        {
            var d = new DirectoryInfo(""aa"");
            var x = directoryInfo.Attributes;
        }
    }
}";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Delete [using DirectoryInfo = N.C;]@20");

            edits.VerifySemantics();
        }

        [Fact]
        public void Using_Insert_ForNewCode()
        {
            // As distinct from the above, this test validates a real world scenario of inserting a using directive
            // and changing code that utilizes the new directive to some effect.
            var src1 = @"
namespace N
{
    class Program
    {
        static void Main(string[] args)
        {
        }
    }
}";
            var src2 = @"
using System;

namespace N
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(""Hello World!"");
        }
    }
}";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(SemanticEdit(SemanticEditKind.Update, c => c.GetMember("N.Program.Main")));
        }

        [Fact]
        public void Using_Delete_ForOldCode()
        {
            var src1 = @"
using System;

namespace N
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(""Hello World!"");
        }
    }
}";
            var src2 = @"
namespace N
{
    class Program
    {
        static void Main(string[] args)
        {
        }
    }
}";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(SemanticEdit(SemanticEditKind.Update, c => c.GetMember("N.Program.Main")));
        }

        [Fact]
        public void Using_Insert_CreatesAmbiguousCode()
        {
            // This test validates that we still issue edits for changed valid code, even when unchanged
            // code has ambiguities after adding a using.
            var src1 = @"
using System.Threading;

namespace N
{
    class C
    {
        void M()
        {
            // Timer exists in System.Threading and System.Timers
            var t = new Timer(s => System.Console.WriteLine(s));
        }
    }
}";
            var src2 = @"
using System.Threading;
using System.Timers;

namespace N
{
    class C
    {
        void M()
        {
            // Timer exists in System.Threading and System.Timers
            var t = new Timer(s => System.Console.WriteLine(s));
        }

        void M2()
        {
             // TimersDescriptionAttribute only exists in System.Timers
            System.Console.WriteLine(new TimersDescriptionAttribute(""""));
        }
    }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                new[] { SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("N.C.M2")) },
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
        }

        #endregion

        #region Extern Alias

        [Fact]
        public void ExternAliasUpdate()
        {
            var src1 = "extern alias X;";
            var src2 = "extern alias Y;";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [extern alias X;]@0 -> [extern alias Y;]@0");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Update, "extern alias Y;", CSharpFeaturesResources.extern_alias));
        }

        [Fact]
        public void ExternAliasInsert()
        {
            var src1 = "";
            var src2 = "extern alias Y;";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [extern alias Y;]@0");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Insert, "extern alias Y;", CSharpFeaturesResources.extern_alias));
        }

        [Fact]
        public void ExternAliasDelete()
        {
            var src1 = "extern alias Y;";
            var src2 = "";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Delete [extern alias Y;]@0");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Delete, null, CSharpFeaturesResources.extern_alias));
        }

        #endregion

        #region Assembly/Module Attributes

        [Fact]
        public void Insert_TopLevelAttribute()
        {
            var src1 = "";
            var src2 = "[assembly: System.Obsolete(\"2\")]";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [[assembly: System.Obsolete(\"2\")]]@0",
                "Insert [System.Obsolete(\"2\")]@11");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Insert, "[assembly: System.Obsolete(\"2\")]", FeaturesResources.attribute));
        }

        [Fact]
        public void Delete_TopLevelAttribute()
        {
            var src1 = "[assembly: System.Obsolete(\"2\")]";
            var src2 = "";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Delete [[assembly: System.Obsolete(\"2\")]]@0",
                "Delete [System.Obsolete(\"2\")]@11");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Delete, null, FeaturesResources.attribute));
        }

        [Fact]
        public void Update_TopLevelAttribute()
        {
            var src1 = "[assembly: System.Obsolete(\"1\")]";
            var src2 = "[assembly: System.Obsolete(\"2\")]";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [[assembly: System.Obsolete(\"1\")]]@0 -> [[assembly: System.Obsolete(\"2\")]]@0",
                "Update [System.Obsolete(\"1\")]@11 -> [System.Obsolete(\"2\")]@11");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Update, "System.Obsolete(\"2\")", FeaturesResources.attribute));
        }

        [Fact]
        public void Reorder_TopLevelAttribute()
        {
            var src1 = "[assembly: System.Obsolete(\"1\")][assembly: System.Obsolete(\"2\")]";
            var src2 = "[assembly: System.Obsolete(\"2\")][assembly: System.Obsolete(\"1\")]";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Reorder [[assembly: System.Obsolete(\"2\")]]@32 -> @0");

            edits.VerifySemanticDiagnostics();
        }

        #endregion

        #region Types

        [Theory]
        [InlineData("class", "struct")]
        [InlineData("class", "record")] // TODO: Allow this conversion: https://github.com/dotnet/roslyn/issues/51874
        [InlineData("class", "record struct")]
        [InlineData("class", "interface")]
        [InlineData("struct", "record struct")] // TODO: Allow this conversion: https://github.com/dotnet/roslyn/issues/51874
        public void Type_Kind_Update(string oldKeyword, string newKeyword)
        {
            var src1 = oldKeyword + " C { }";
            var src2 = newKeyword + " C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [" + oldKeyword + " C { }]@0 -> [" + newKeyword + " C { }]@0");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.TypeKindUpdate, newKeyword + " C"));
        }

        [Theory]
        [InlineData("class", "struct")]
        [InlineData("class", "record")]
        [InlineData("class", "record struct")]
        [InlineData("class", "interface")]
        [InlineData("struct", "record struct")]
        public void Type_Kind_Update_Reloadable(string oldKeyword, string newKeyword)
        {
            var src1 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]" + oldKeyword + " C { }";
            var src2 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]" + newKeyword + " C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [[CreateNewOnMetadataUpdate]" + oldKeyword + " C { }]@145 -> [[CreateNewOnMetadataUpdate]" + newKeyword + " C { }]@145");

            edits.VerifySemantics(
                new[] { SemanticEdit(SemanticEditKind.Replace, c => c.GetMember("C")) },
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void Type_Modifiers_Static_Remove()
        {
            var src1 = "public static class C { }";
            var src2 = "public class C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [public static class C { }]@0 -> [public class C { }]@0");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "public class C", FeaturesResources.class_));
        }

        [Theory]
        [InlineData("public")]
        [InlineData("protected")]
        [InlineData("private")]
        [InlineData("private protected")]
        [InlineData("internal protected")]
        public void Type_Modifiers_Accessibility_Change(string accessibility)
        {
            var src1 = accessibility + " class C { }";
            var src2 = "class C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [" + accessibility + " class C { }]@0 -> [class C { }]@0");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingAccessibility, "class C", FeaturesResources.class_));
        }

        [Theory]
        [InlineData("public", "public")]
        [InlineData("internal", "internal")]
        [InlineData("", "internal")]
        [InlineData("internal", "")]
        [InlineData("protected", "protected")]
        [InlineData("private", "private")]
        [InlineData("private protected", "private protected")]
        [InlineData("internal protected", "internal protected")]
        public void Type_Modifiers_Accessibility_Partial(string accessibilityA, string accessibilityB)
        {
            var srcA1 = accessibilityA + " partial class C { }";
            var srcB1 = "partial class C { }";
            var srcA2 = "partial class C { }";
            var srcB2 = accessibilityB + " partial class C { }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(),
                    DocumentResults(),
                });
        }

        [Fact]
        public void Type_Modifiers_Internal_Remove()
        {
            var src1 = "internal interface C { }";
            var src2 = "interface C { }";

            var edits = GetTopEdits(src1, src2);
            edits.VerifySemantics();
        }

        [Fact]
        public void Type_Modifiers_Internal_Add()
        {
            var src1 = "struct C { }";
            var src2 = "internal struct C { }";

            var edits = GetTopEdits(src1, src2);
            edits.VerifySemantics();
        }

        [Fact]
        public void Type_Modifiers_Accessibility_Reloadable()
        {
            var src1 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]public class C { }";
            var src2 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]internal class C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [[CreateNewOnMetadataUpdate]public class C { }]@145 -> [[CreateNewOnMetadataUpdate]internal class C { }]@145");

            edits.VerifySemantics(
                new[] { SemanticEdit(SemanticEditKind.Replace, c => c.GetMember("C")) },
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Theory]
        [InlineData("class")]
        [InlineData("struct")]
        [InlineData("interface")]
        [InlineData("record")]
        [InlineData("record struct")]
        public void Type_Modifiers_NestedPrivateInInterface_Remove(string keyword)
        {
            var src1 = "interface C { private " + keyword + " S { } }";
            var src2 = "interface C { " + keyword + " S { } }";

            var edits = GetTopEdits(src1, src2);
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingAccessibility, keyword + " S", GetResource(keyword)));
        }

        [Theory]
        [InlineData("class")]
        [InlineData("struct")]
        [InlineData("interface")]
        [InlineData("record")]
        [InlineData("record struct")]
        public void Type_Modifiers_NestedPrivateInClass_Add(string keyword)
        {
            var src1 = "class C { " + keyword + " S { } }";
            var src2 = "class C { private " + keyword + " S { } }";

            var edits = GetTopEdits(src1, src2);
            edits.VerifySemantics();
        }

        [Theory]
        [InlineData("class")]
        [InlineData("struct")]
        [InlineData("interface")]
        [InlineData("record")]
        [InlineData("record struct")]
        public void Type_Modifiers_NestedPublicInInterface_Add(string keyword)
        {
            var src1 = "interface C { " + keyword + " S { } }";
            var src2 = "interface C { public " + keyword + " S { } }";

            var edits = GetTopEdits(src1, src2);
            edits.VerifySemantics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48628")]
        public void Type_Modifiers_Unsafe_Add()
        {
            var src1 = "public class C { }";
            var src2 = "public unsafe class C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [public class C { }]@0 -> [public unsafe class C { }]@0");

            edits.VerifySemanticDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48628")]
        public void Type_Modifiers_Unsafe_Remove()
        {
            var src1 = @"
using System;
unsafe delegate void D();
class C
{
    unsafe class N { }
    public unsafe event Action<int> A { add { } remove { } }
    unsafe int F() => 0;
    unsafe int X;
    unsafe int Y { get; }
    unsafe C() {}
    unsafe ~C() {}
}
";
            var src2 = @"
using System;
delegate void D();
class C
{
    class N { }
    public event Action<int> A { add { } remove { } }
    int F() => 0;
    int X;
    int Y { get; }
    C() {}
    ~C() {}
}
";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [unsafe delegate void D();]@17 -> [delegate void D();]@17",
                "Update [unsafe class N { }]@60 -> [class N { }]@53",
                "Update [public unsafe event Action<int> A { add { } remove { } }]@84 -> [public event Action<int> A { add { } remove { } }]@70",
                "Update [unsafe int F() => 0;]@146 -> [int F() => 0;]@125",
                "Update [unsafe int X;]@172 -> [int X;]@144",
                "Update [unsafe int Y { get; }]@191 -> [int Y { get; }]@156",
                "Update [unsafe C() {}]@218 -> [C() {}]@176",
                "Update [unsafe ~C() {}]@237 -> [~C() {}]@188");

            edits.VerifySemanticDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48628")]
        public void Type_Modifiers_Unsafe_DeleteInsert()
        {
            var srcA1 = "partial class C { unsafe void F() { } }";
            var srcB1 = "partial class C { }";
            var srcA2 = "partial class C { }";
            var srcB2 = "partial class C { void F() { } }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(),
                    DocumentResults(semanticEdits: new[]
                    {
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember("F"))
                    }),
                });
        }

        [Fact]
        public void Type_Modifiers_Ref_Add()
        {
            var src1 = "public struct C { }";
            var src2 = "public ref struct C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [public struct C { }]@0 -> [public ref struct C { }]@0");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "public ref struct C", CSharpFeaturesResources.struct_));
        }

        [Fact]
        public void Type_Modifiers_Ref_Remove()
        {
            var src1 = "public ref struct C { }";
            var src2 = "public struct C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [public ref struct C { }]@0 -> [public struct C { }]@0");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "public struct C", CSharpFeaturesResources.struct_));
        }

        [Fact]
        public void Type_Modifiers_ReadOnly_Add()
        {
            var src1 = "public struct C { }";
            var src2 = "public readonly struct C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [public struct C { }]@0 -> [public readonly struct C { }]@0");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "public readonly struct C", CSharpFeaturesResources.struct_));
        }

        [Fact]
        public void Type_Modifiers_ReadOnly_Remove()
        {
            var src1 = "public readonly struct C { }";
            var src2 = "public struct C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [public readonly struct C { }]@0 -> [public struct C { }]@0");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "public struct C", CSharpFeaturesResources.struct_));
        }

        [Theory]
        [InlineData("[System.CLSCompliantAttribute]", "CLSCompliantAttribute")]
        [InlineData("[System.Diagnostics.CodeAnalysis.AllowNullAttribute]", "AllowNullAttribute")]
        [InlineData("[System.Diagnostics.CodeAnalysis.DisallowNullAttribute]", "DisallowNullAttribute")]
        [InlineData("[System.Diagnostics.CodeAnalysis.MaybeNullAttribute]", "MaybeNullAttribute")]
        [InlineData("[System.Diagnostics.CodeAnalysis.NotNullAttribute]", "NotNullAttribute")]
        [InlineData("[System.NonSerializedAttribute]", "NonSerializedAttribute")]
        [InlineData("[System.Reflection.AssemblyAlgorithmIdAttribute]", "AssemblyAlgorithmIdAttribute")]
        [InlineData("[System.Reflection.AssemblyCultureAttribute]", "AssemblyCultureAttribute")]
        [InlineData("[System.Reflection.AssemblyFlagsAttribute]", "AssemblyFlagsAttribute")]
        [InlineData("[System.Reflection.AssemblyVersionAttribute]", "AssemblyVersionAttribute")]
        [InlineData("[System.Runtime.CompilerServices.DllImportAttribute]", "DllImportAttribute")]
        [InlineData("[System.Runtime.CompilerServices.IndexerNameAttribute]", "IndexerNameAttribute")]
        [InlineData("[System.Runtime.CompilerServices.MethodImplAttribute]", "MethodImplAttribute")]
        [InlineData("[System.Runtime.CompilerServices.SpecialNameAttribute]", "SpecialNameAttribute")]
        [InlineData("[System.Runtime.CompilerServices.TypeForwardedToAttribute]", "TypeForwardedToAttribute")]
        [InlineData("[System.Runtime.InteropServices.ComImportAttribute]", "ComImportAttribute")]
        [InlineData("[System.Runtime.InteropServices.DefaultParameterValueAttribute]", "DefaultParameterValueAttribute")]
        [InlineData("[System.Runtime.InteropServices.FieldOffsetAttribute]", "FieldOffsetAttribute")]
        [InlineData("[System.Runtime.InteropServices.InAttribute]", "InAttribute")]
        [InlineData("[System.Runtime.InteropServices.MarshalAsAttribute]", "MarshalAsAttribute")]
        [InlineData("[System.Runtime.InteropServices.OptionalAttribute]", "OptionalAttribute")]
        [InlineData("[System.Runtime.InteropServices.OutAttribute]", "OutAttribute")]
        [InlineData("[System.Runtime.InteropServices.PreserveSigAttribute]", "PreserveSigAttribute")]
        [InlineData("[System.Runtime.InteropServices.StructLayoutAttribute]", "StructLayoutAttribute")]
        [InlineData("[System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeImportAttribute]", "WindowsRuntimeImportAttribute")]
        [InlineData("[System.Security.DynamicSecurityMethodAttribute]", "DynamicSecurityMethodAttribute")]
        [InlineData("[System.SerializableAttribute]", "SerializableAttribute")]
        [InlineData("[System.Runtime.CompilerServices.AsyncMethodBuilderAttribute]", "AsyncMethodBuilderAttribute")]
        public void Type_Attribute_Insert_SupportedByRuntime_NonCustomAttribute(string attributeType, string attributeName)
        {
            var src1 = @"class C { public void M(int a) {} }";
            var src2 = attributeType + @"class C { public void M(int a) {} } ";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [class C { public void M(int a) {} }]@0 -> [" + attributeType + "class C { public void M(int a) {} }]@0");

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingNonCustomAttribute, "class C", attributeName, FeaturesResources.class_) },
                capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
        }

        [Fact]
        public void Type_Attribute_Update_NotSupportedByRuntime1()
        {
            var attribute = "public class A1Attribute : System.Attribute { }\n\n" +
                            "public class A2Attribute : System.Attribute { }\n\n";

            var src1 = attribute + "[A1]class C { }";
            var src2 = attribute + "[A2]class C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [[A1]class C { }]@98 -> [[A2]class C { }]@98");

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "class C", FeaturesResources.class_) },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void Type_Attribute_Update_NotSupportedByRuntime2()
        {
            var src1 = "[System.Obsolete(\"1\")]class C { }";
            var src2 = "[System.Obsolete(\"2\")]class C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [[System.Obsolete(\"1\")]class C { }]@0 -> [[System.Obsolete(\"2\")]class C { }]@0");

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "class C", FeaturesResources.class_) },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void Type_Attribute_Delete_NotSupportedByRuntime1()
        {
            var attribute = "public class AAttribute : System.Attribute { }\n\n" +
                            "public class BAttribute : System.Attribute { }\n\n";

            var src1 = attribute + "[A, B]class C { }";
            var src2 = attribute + "[A]class C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [[A, B]class C { }]@96 -> [[A]class C { }]@96");

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "class C", FeaturesResources.class_) },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void Type_Attribute_Delete_NotSupportedByRuntime2()
        {
            var attribute = "public class AAttribute : System.Attribute { }\n\n" +
                            "public class BAttribute : System.Attribute { }\n\n";

            var src1 = attribute + "[B, A]class C { }";
            var src2 = attribute + "[A]class C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [[B, A]class C { }]@96 -> [[A]class C { }]@96");

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "class C", FeaturesResources.class_) },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void Type_Attribute_Change_Reloadable()
        {
            var attributeSrc = @"
public class A1 : System.Attribute { }
public class A2 : System.Attribute { }
public class A3 : System.Attribute { }
";

            var src1 = ReloadableAttributeSrc + attributeSrc + "[CreateNewOnMetadataUpdate, A1, A2]class C { }";
            var src2 = ReloadableAttributeSrc + attributeSrc + "[CreateNewOnMetadataUpdate, A2, A3]class C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [[CreateNewOnMetadataUpdate, A1, A2]class C { }]@267 -> [[CreateNewOnMetadataUpdate, A2, A3]class C { }]@267");

            edits.VerifySemantics(
                new[] { SemanticEdit(SemanticEditKind.Replace, c => c.GetMember("C")) },
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void Type_Attribute_ReloadableRemove()
        {
            var src1 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]class C { }";
            var src2 = ReloadableAttributeSrc + "class C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                new[] { SemanticEdit(SemanticEditKind.Replace, c => c.GetMember("C")) },
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void Type_Attribute_ReloadableAdd()
        {
            var src1 = ReloadableAttributeSrc + "class C { }";
            var src2 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]class C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C")) },
                capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
        }

        [Fact]
        public void Type_Attribute_ReloadableBase()
        {
            var src1 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]class B { } class C : B { }";
            var src2 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]class B { } class C : B { void F() {} }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                new[] { SemanticEdit(SemanticEditKind.Replace, c => c.GetMember("C")) },
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void Type_Attribute_Add()
        {
            var attribute = "public class AAttribute : System.Attribute { }\n\n" +
                            "public class BAttribute : System.Attribute { }\n\n";

            var src1 = attribute + "[A]class C { }";
            var src2 = attribute + "[A, B]class C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [[A]class C { }]@96 -> [[A, B]class C { }]@96");

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C")) },
                capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
        }

        [Fact]
        public void Type_Attribute_Add_NotSupportedByRuntime1()
        {
            var attribute = "public class AAttribute : System.Attribute { }\n\n" +
                            "public class BAttribute : System.Attribute { }\n\n";

            var src1 = attribute + "[A]class C { }";
            var src2 = attribute + "[A, B]class C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [[A]class C { }]@96 -> [[A, B]class C { }]@96");

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "class C", FeaturesResources.class_) },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void Type_Attribute_Add_NotSupportedByRuntime2()
        {
            var attribute = "public class AAttribute : System.Attribute { }\n\n";

            var src1 = attribute + "class C { }";
            var src2 = attribute + "[A]class C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [class C { }]@48 -> [[A]class C { }]@48");

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "class C", FeaturesResources.class_) },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void Type_Attribute_Reorder1()
        {
            var src1 = "[A(1), B(2), C(3)]class C { }";
            var src2 = "[C(3), A(1), B(2)]class C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [[A(1), B(2), C(3)]class C { }]@0 -> [[C(3), A(1), B(2)]class C { }]@0");

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void Type_Attribute_Reorder2()
        {
            var src1 = "[A, B, C]class C { }";
            var src2 = "[B, C, A]class C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [[A, B, C]class C { }]@0 -> [[B, C, A]class C { }]@0");

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void Type_Attribute_ReorderAndUpdate_NotSupportedByRuntime()
        {
            var attribute = "public class AAttribute : System.Attribute { }\n\n" +
                            "public class BAttribute : System.Attribute { }\n\n";

            var src1 = attribute + "[System.Obsolete(\"1\"), A, B]class C { }";
            var src2 = attribute + "[A, B, System.Obsolete(\"2\")]class C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [[System.Obsolete(\"1\"), A, B]class C { }]@96 -> [[A, B, System.Obsolete(\"2\")]class C { }]@96");

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "class C", FeaturesResources.class_) },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Theory]
        [InlineData("class")]
        [InlineData("struct")]
        [InlineData("interface")]
        [InlineData("record")]
        [InlineData("record struct")]
        public void Type_Rename(string keyword)
        {
            var src1 = keyword + " C { }";
            var src2 = keyword + " D { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [" + keyword + " C { }]@0 -> [" + keyword + " D { }]@0");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Renamed, keyword + " D", GetResource(keyword)));
        }

        [Fact]
        public void Type_Rename_AddAndDeleteMember()
        {
            var src1 = "class C { int x = 1; }";
            var src2 = "class D { void F() { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [class C { int x = 1; }]@0 -> [class D { void F() { } }]@0",
                "Insert [void F() { }]@10",
                "Insert [()]@16",
                "Delete [int x = 1;]@10",
                "Delete [int x = 1]@10",
                "Delete [x = 1]@14");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "class D", FeaturesResources.class_));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/54886")]
        public void Type_Rename_Reloadable()
        {
            var src1 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]class C { }";
            var src2 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]class D { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [[CreateNewOnMetadataUpdate]class C { }]@145 -> [[CreateNewOnMetadataUpdate]class D { }]@145");

            // TODO: expected: Replace edit of D
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "class D", FeaturesResources.class_));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/54886")]
        public void Type_Rename_Reloadable_AddAndDeleteMember()
        {
            var src1 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]class C { int x = 1; }";
            var src2 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]class D { void F() { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [[CreateNewOnMetadataUpdate]class C { int x = 1; }]@145 -> [[CreateNewOnMetadataUpdate]class D { void F() { } }]@145",
                "Insert [void F() { }]@182",
                "Insert [()]@188",
                "Delete [int x = 1;]@182",
                "Delete [int x = 1]@182",
                "Delete [x = 1]@186");

            // TODO: expected: Replace edit of D
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "class D", FeaturesResources.class_));
        }

        [Fact]
        public void Interface_NoModifiers_Insert()
        {
            var src1 = "";
            var src2 = "interface C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void Interface_NoModifiers_IntoNamespace_Insert()
        {
            var src1 = "namespace N { } ";
            var src2 = "namespace N { interface C { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void Interface_NoModifiers_IntoType_Insert()
        {
            var src1 = "interface N { }";
            var src2 = "interface N { interface C { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void Class_NoModifiers_Insert()
        {
            var src1 = "";
            var src2 = "class C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void Class_NoModifiers_IntoNamespace_Insert()
        {
            var src1 = "namespace N { }";
            var src2 = "namespace N { class C { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void Class_NoModifiers_IntoType_Insert()
        {
            var src1 = "struct N { }";
            var src2 = "struct N { class C { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void Struct_NoModifiers_Insert()
        {
            var src1 = "";
            var src2 = "struct C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void Struct_NoModifiers_IntoNamespace_Insert()
        {
            var src1 = "namespace N { }";
            var src2 = "namespace N { struct C { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void Struct_NoModifiers_IntoType_Insert()
        {
            var src1 = "struct N { }";
            var src2 = "struct N { struct C { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void Type_BaseType_Add_Unchanged()
        {
            var src1 = "class C { }";
            var src2 = "class C : object { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [class C { }]@0 -> [class C : object { }]@0");

            edits.VerifySemantics();
        }

        [Fact]
        public void Type_BaseType_Add_Changed()
        {
            var src1 = "class C { }";
            var src2 = "class C : D { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [class C { }]@0 -> [class C : D { }]@0");

            edits.VerifySemanticDiagnostics(
                 Diagnostic(RudeEditKind.BaseTypeOrInterfaceUpdate, "class C", FeaturesResources.class_));
        }

        [Theory]
        [InlineData("string", "string?")]
        [InlineData("string[]", "string[]?")]
        [InlineData("object", "dynamic")]
        [InlineData("dynamic?", "dynamic")]
        [InlineData("(int a, int b)", "(int a, int c)")]
        public void Type_BaseType_Update_RuntimeTypeUnchanged(string oldType, string newType)
        {
            var src1 = "class C : System.Collections.Generic.List<" + oldType + "> {}";
            var src2 = "class C : System.Collections.Generic.List<" + newType + "> {}";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C")));
        }

        [Theory]
        [InlineData("int", "string")]
        [InlineData("int", "int?")]
        [InlineData("(int a, int b)", "(int a, double b)")]
        public void Type_BaseType_Update_RuntimeTypeChanged(string oldType, string newType)
        {
            var src1 = "class C : System.Collections.Generic.List<" + oldType + "> {}";
            var src2 = "class C : System.Collections.Generic.List<" + newType + "> {}";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.BaseTypeOrInterfaceUpdate, "class C", FeaturesResources.class_));
        }

        [Fact]
        public void Type_BaseType_Update_CompileTimeTypeUnchanged()
        {
            var src1 = "using A = System.Int32; using B = System.Int32; class C : System.Collections.Generic.List<A> {}";
            var src2 = "using A = System.Int32; using B = System.Int32; class C : System.Collections.Generic.List<B> {}";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics();
        }

        [Fact]
        public void Type_BaseInterface_Add()
        {
            var src1 = "class C { }";
            var src2 = "class C : IDisposable { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [class C { }]@0 -> [class C : IDisposable { }]@0");

            edits.VerifySemanticDiagnostics(
                 Diagnostic(RudeEditKind.BaseTypeOrInterfaceUpdate, "class C", FeaturesResources.class_));
        }

        [Fact]
        public void Type_BaseInterface_Delete_Inherited()
        {
            var src1 = @"
interface B {}
interface A : B {}

class C : A, B {}
";
            var src2 = @"
interface B {}
interface A : B {}

class C : A {}
";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics();
        }

        [Fact]
        public void Type_BaseInterface_Reorder()
        {
            var src1 = "class C : IGoo, IBar { }";
            var src2 = "class C : IBar, IGoo { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [class C : IGoo, IBar { }]@0 -> [class C : IBar, IGoo { }]@0");

            edits.VerifySemanticDiagnostics(
                 Diagnostic(RudeEditKind.BaseTypeOrInterfaceUpdate, "class C", FeaturesResources.class_));
        }

        [Theory]
        [InlineData("string", "string?")]
        [InlineData("object", "dynamic")]
        [InlineData("(int a, int b)", "(int a, int c)")]
        public void Type_BaseInterface_Update_RuntimeTypeUnchanged(string oldType, string newType)
        {
            var src1 = "class C : System.Collections.Generic.IEnumerable<" + oldType + "> {}";
            var src2 = "class C : System.Collections.Generic.IEnumerable<" + newType + "> {}";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C")));
        }

        [Theory]
        [InlineData("int", "string")]
        [InlineData("int", "int?")]
        [InlineData("(int a, int b)", "(int a, double b)")]
        public void Type_BaseInterface_Update_RuntimeTypeChanged(string oldType, string newType)
        {
            var src1 = "class C : System.Collections.Generic.IEnumerable<" + oldType + "> {}";
            var src2 = "class C : System.Collections.Generic.IEnumerable<" + newType + "> {}";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.BaseTypeOrInterfaceUpdate, "class C", FeaturesResources.class_));
        }

        [Fact]
        public void Type_Base_Partial()
        {
            var srcA1 = "partial class C : B, I { }";
            var srcB1 = "partial class C : J { }";
            var srcA2 = "partial class C { }";
            var srcB2 = "partial class C : B, I, J { }";

            var srcC = @"
class B {}
interface I {}
interface J {}";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2), GetTopEdits(srcC, srcC) },
                new[]
                {
                    DocumentResults(),
                    DocumentResults(),
                    DocumentResults()
                });
        }

        [Fact]
        public void Type_Base_Partial_InsertDeleteAndUpdate()
        {
            var srcA1 = "partial class C { }";
            var srcB1 = "";
            var srcC1 = "partial class C { }";

            var srcA2 = "";
            var srcB2 = "partial class C : D { }";
            var srcC2 = "partial class C { }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2), GetTopEdits(srcC1, srcC2) },
                new[]
                {
                    DocumentResults(),

                    DocumentResults(
                        diagnostics: new[] { Diagnostic(RudeEditKind.BaseTypeOrInterfaceUpdate, "partial class C", FeaturesResources.class_) }),

                    DocumentResults(),
                });
        }

        [Fact]
        public void Type_Base_InsertDelete()
        {
            var srcA1 = "";
            var srcB1 = "class C : B, I { }";
            var srcA2 = "class C : B, I { }";
            var srcB2 = "";

            var srcC = @"
class B {}
interface I {}
interface J {}";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2), GetTopEdits(srcC, srcC) },
                new[]
                {
                    DocumentResults(),
                    DocumentResults(),
                    DocumentResults()
                });
        }

        [Fact]
        public void Type_Reloadable_NotSupportedByRuntime()
        {
            var src1 = ReloadableAttributeSrc + @"
[CreateNewOnMetadataUpdate]
public class C
{
    void F() { System.Console.WriteLine(1); }
}";
            var src2 = ReloadableAttributeSrc + @"
[CreateNewOnMetadataUpdate]
public class C
{
    void F() { System.Console.WriteLine(2); }
}";

            var edits = GetTopEdits(src1, src2);
            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingReloadableTypeNotSupportedByRuntime, "void F()", "CreateNewOnMetadataUpdateAttribute") },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void Type_Insert_AbstractVirtualOverride()
        {
            var src1 = "";
            var src2 = @"
public abstract class C<T>
{ 
    public abstract void F(); 
    public virtual void G() {}
    public override string ToString() => null;
}";

            var edits = GetTopEdits(src1, src2);
            edits.VerifySemanticDiagnostics(
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void Type_Insert_NotSupportedByRuntime()
        {
            var src1 = @"
public class C
{
    void F()
    {
    }
}";
            var src2 = @"
public class C
{
    void F()
    {
    }
}

public class D
{
    void M()
    {
    }
}";

            var edits = GetTopEdits(src1, src2);
            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.InsertNotSupportedByRuntime, "public class D", FeaturesResources.class_) },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void Type_Insert_Reloadable()
        {
            var src1 = ReloadableAttributeSrc + "";
            var src2 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]class C { void F() {} }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                new[] { SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C")) },
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void InterfaceInsert()
        {
            var src1 = "";
            var src2 = @"
public interface I 
{ 
    void F(); 
    static void G() {}
}";

            var edits = GetTopEdits(src1, src2);
            edits.VerifySemanticDiagnostics(
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void RefStructInsert()
        {
            var src1 = "";
            var src2 = "ref struct X { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [ref struct X { }]@0");

            edits.VerifySemanticDiagnostics(
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void Struct_ReadOnly_Insert()
        {
            var src1 = "";
            var src2 = "readonly struct X { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [readonly struct X { }]@0");

            edits.VerifySemanticDiagnostics(
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void Struct_RefModifier_Add()
        {
            var src1 = "struct X { }";
            var src2 = "ref struct X { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [struct X { }]@0 -> [ref struct X { }]@0");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "ref struct X", CSharpFeaturesResources.struct_));
        }

        [Fact]
        public void Struct_ReadonlyModifier_Add()
        {
            var src1 = "struct X { }";
            var src2 = "readonly struct X { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [struct X { }]@0 -> [readonly struct X { }]@0");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "readonly struct X", SyntaxFacts.GetText(SyntaxKind.StructKeyword)));
        }

        [Theory]
        [InlineData("ref")]
        [InlineData("readonly")]
        public void Struct_Modifiers_Partial_InsertDelete(string modifier)
        {
            var srcA1 = modifier + " partial struct S { }";
            var srcB1 = "partial struct S { }";
            var srcA2 = "partial struct S { }";
            var srcB2 = modifier + " partial struct S { }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(),
                    DocumentResults()
                });
        }

        [Fact]
        public void Class_ImplementingInterface_Add()
        {
            var src1 = @"
using System;

public interface ISample
{
    string Get();
}

public interface IConflict
{
    string Get();
}

public class BaseClass : ISample
{
    public virtual string Get() => string.Empty;
}
";
            var src2 = @"
using System;

public interface ISample
{
    string Get();
}

public interface IConflict
{
    string Get();
}

public class BaseClass : ISample
{
    public virtual string Get() => string.Empty;
}

public class SubClass : BaseClass, IConflict
{
    public override string Get() => string.Empty;

    string IConflict.Get() => String.Empty;
}
";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                @"Insert [public class SubClass : BaseClass, IConflict
{
    public override string Get() => string.Empty;

    string IConflict.Get() => String.Empty;
}]@219",
                "Insert [public override string Get() => string.Empty;]@272",
                "Insert [string IConflict.Get() => String.Empty;]@325",
                "Insert [()]@298",
                "Insert [()]@345");

            // Here we add a class implementing an interface and a method inside it with explicit interface specifier.
            // We want to be sure that adding the method will not tirgger a rude edit as it happens if adding a single method with explicit interface specifier.
            edits.VerifySemanticDiagnostics(
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37128")]
        public void Interface_InsertMembers()
        {
            var src1 = @"
using System;
interface I
{
}
";
            var src2 = @"
using System;
interface I
{
    static int StaticField = 10;

    static void StaticMethod() { }
    void VirtualMethod1() { }
    virtual void VirtualMethod2() { }
    abstract void AbstractMethod();
    sealed void NonVirtualMethod() { }

    public static int operator +(I a, I b) => 1;

    static int StaticProperty1 { get => 1; set { } }
    static int StaticProperty2 => 1;
    virtual int VirtualProperty1 { get => 1; set { } }
    virtual int VirtualProperty2 { get => 1; }
    int VirtualProperty3 { get => 1; set { } }
    int VirtualProperty4 { get => 1; }
    abstract int AbstractProperty1 { get; set; }
    abstract int AbstractProperty2 { get; }
    sealed int NonVirtualProperty => 1;

    int this[byte virtualIndexer] => 1;
    int this[sbyte virtualIndexer] { get => 1; }
    virtual int this[ushort virtualIndexer] { get => 1; set {} }
    virtual int this[short virtualIndexer] { get => 1; set {} }
    abstract int this[uint abstractIndexer] { get; set; }
    abstract int this[int abstractIndexer] { get; }
    sealed int this[ulong nonVirtualIndexer] { get => 1; set {} }
    sealed int this[long nonVirtualIndexer] { get => 1; set {} }
    
    static event Action StaticEvent;
    static event Action StaticEvent2 { add { } remove { } }

    event Action VirtualEvent { add { } remove { } }
    abstract event Action AbstractEvent;
    sealed event Action NonVirtualEvent { add { } remove { } }

    abstract class C { }
    interface J { }
    enum E { }
    delegate void D();
}
";
            var edits = GetTopEdits(src1, src2);

            // TODO: InsertIntoInterface errors are reported due to https://github.com/dotnet/roslyn/issues/37128.
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.InsertIntoInterface, "static void StaticMethod()", FeaturesResources.method),
                Diagnostic(RudeEditKind.InsertVirtual, "void VirtualMethod1()", FeaturesResources.method),
                Diagnostic(RudeEditKind.InsertVirtual, "virtual void VirtualMethod2()", FeaturesResources.method),
                Diagnostic(RudeEditKind.InsertVirtual, "abstract void AbstractMethod()", FeaturesResources.method),
                Diagnostic(RudeEditKind.InsertIntoInterface, "sealed void NonVirtualMethod()", FeaturesResources.method),
                Diagnostic(RudeEditKind.InsertOperator, "public static int operator +(I a, I b)", FeaturesResources.operator_),
                Diagnostic(RudeEditKind.InsertIntoInterface, "static int StaticProperty1", FeaturesResources.property_),
                Diagnostic(RudeEditKind.InsertIntoInterface, "static int StaticProperty2", FeaturesResources.property_),
                Diagnostic(RudeEditKind.InsertIntoInterface, "static int StaticProperty2", CSharpFeaturesResources.property_getter),
                Diagnostic(RudeEditKind.InsertVirtual, "virtual int VirtualProperty1", FeaturesResources.property_),
                Diagnostic(RudeEditKind.InsertVirtual, "virtual int VirtualProperty2", FeaturesResources.property_),
                Diagnostic(RudeEditKind.InsertVirtual, "int VirtualProperty3", FeaturesResources.property_),
                Diagnostic(RudeEditKind.InsertVirtual, "int VirtualProperty4", FeaturesResources.property_),
                Diagnostic(RudeEditKind.InsertVirtual, "abstract int AbstractProperty1", FeaturesResources.property_),
                Diagnostic(RudeEditKind.InsertVirtual, "abstract int AbstractProperty2", FeaturesResources.property_),
                Diagnostic(RudeEditKind.InsertIntoInterface, "sealed int NonVirtualProperty", FeaturesResources.property_),
                Diagnostic(RudeEditKind.InsertIntoInterface, "sealed int NonVirtualProperty", CSharpFeaturesResources.property_getter),
                Diagnostic(RudeEditKind.InsertVirtual, "int this[byte virtualIndexer]", FeaturesResources.indexer_),
                Diagnostic(RudeEditKind.InsertVirtual, "int this[byte virtualIndexer]", CSharpFeaturesResources.indexer_getter),
                Diagnostic(RudeEditKind.InsertVirtual, "int this[sbyte virtualIndexer]", FeaturesResources.indexer_),
                Diagnostic(RudeEditKind.InsertVirtual, "virtual int this[ushort virtualIndexer]", FeaturesResources.indexer_),
                Diagnostic(RudeEditKind.InsertVirtual, "virtual int this[short virtualIndexer]", FeaturesResources.indexer_),
                Diagnostic(RudeEditKind.InsertVirtual, "abstract int this[uint abstractIndexer]", FeaturesResources.indexer_),
                Diagnostic(RudeEditKind.InsertVirtual, "abstract int this[int abstractIndexer]", FeaturesResources.indexer_),
                Diagnostic(RudeEditKind.InsertIntoInterface, "sealed int this[ulong nonVirtualIndexer]", FeaturesResources.indexer_),
                Diagnostic(RudeEditKind.InsertIntoInterface, "sealed int this[long nonVirtualIndexer]", FeaturesResources.indexer_),
                Diagnostic(RudeEditKind.InsertIntoInterface, "static event Action StaticEvent2", FeaturesResources.event_),
                Diagnostic(RudeEditKind.InsertVirtual, "event Action VirtualEvent", FeaturesResources.event_),
                Diagnostic(RudeEditKind.InsertIntoInterface, "sealed event Action NonVirtualEvent", FeaturesResources.event_),
                Diagnostic(RudeEditKind.InsertIntoInterface, "StaticField = 10", FeaturesResources.field),
                Diagnostic(RudeEditKind.InsertIntoInterface, "StaticEvent", CSharpFeaturesResources.event_field),
                Diagnostic(RudeEditKind.InsertVirtual, "AbstractEvent", CSharpFeaturesResources.event_field));
        }

        [Fact]
        public void Interface_InsertDelete()
        {
            var srcA1 = @"
interface I
{
    static void M() { }
}
";
            var srcB1 = @"
";

            var srcA2 = @"
";
            var srcB2 = @"
interface I
{
    static void M() { }
}
";
            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(),

                    DocumentResults(
                        semanticEdits: new[]
                        {
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("I").GetMember("M"))
                        }),
                });
        }

        [Fact]
        public void Type_Generic_InsertMembers()
        {
            var src1 = @"
using System;
class C<T>
{
}
";
            var src2 = @"
using System;
class C<T>
{
    void M() {}
    int P1 { get; set; }
    int P2 { get => 1; set {} }
    int this[int i] { get => 1; set {} }
    event Action E { add {} remove {} }
    event Action EF;
    int F1, F2;

    enum E {}
    interface I {} 
    class D {}
}
";
            var edits = GetTopEdits(src1, src2);
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.InsertIntoGenericType, "void M()", FeaturesResources.method),
                Diagnostic(RudeEditKind.InsertIntoGenericType, "int P1", FeaturesResources.auto_property),
                Diagnostic(RudeEditKind.InsertIntoGenericType, "int P2", FeaturesResources.property_),
                Diagnostic(RudeEditKind.InsertIntoGenericType, "int this[int i]", FeaturesResources.indexer_),
                Diagnostic(RudeEditKind.InsertIntoGenericType, "event Action E", FeaturesResources.event_),
                Diagnostic(RudeEditKind.InsertIntoGenericType, "EF", CSharpFeaturesResources.event_field),
                Diagnostic(RudeEditKind.InsertIntoGenericType, "F1", FeaturesResources.field),
                Diagnostic(RudeEditKind.InsertIntoGenericType, "F2", FeaturesResources.field));
        }

        [Fact]
        public void Type_Generic_InsertMembers_Reloadable()
        {
            var src1 = ReloadableAttributeSrc + @"
[CreateNewOnMetadataUpdate]
class C<T>
{
}
";
            var src2 = ReloadableAttributeSrc + @"
[CreateNewOnMetadataUpdate]
class C<T>
{
    void M() {}
    int P1 { get; set; }
    int P2 { get => 1; set {} }
    int this[int i] { get => 1; set {} }
    event System.Action E { add {} remove {} }
    event System.Action EF;
    int F1, F2;

    enum E {}
    interface I {} 
    class D {}
}
";
            var edits = GetTopEdits(src1, src2);
            edits.VerifySemantics(
                new[] { SemanticEdit(SemanticEditKind.Replace, c => c.GetMember("C")) },
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void Type_Generic_DeleteInsert()
        {
            var srcA1 = @"
class C<T> { void F() {} }
struct S<T> { void F() {} }
interface I<T> { void F() {} }
";
            var srcB1 = "";

            var srcA2 = srcB1;
            var srcB2 = srcA1;

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(),

                    DocumentResults(
                        diagnostics: new[]
                        {
                            Diagnostic(RudeEditKind.GenericTypeUpdate, "class C<T>"),
                            Diagnostic(RudeEditKind.GenericTypeUpdate, "struct S<T>"),
                            Diagnostic(RudeEditKind.GenericTypeUpdate, "interface I<T>"),
                            Diagnostic(RudeEditKind.GenericTypeUpdate, "void F()"),
                            Diagnostic(RudeEditKind.GenericTypeUpdate, "void F()"),
                            Diagnostic(RudeEditKind.GenericTypeUpdate, "void F()"),
                            Diagnostic(RudeEditKind.GenericTypeUpdate, "T"),
                            Diagnostic(RudeEditKind.GenericTypeUpdate, "T"),
                            Diagnostic(RudeEditKind.GenericTypeUpdate, "T"),
                        })
                });
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/54881")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/54881")]
        public void Type_TypeParameter_Insert_Reloadable()
        {
            var src1 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]public class C<T> { void F() { } }";
            var src2 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]internal class C<T, S> { int x = 1; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                 SemanticEdit(SemanticEditKind.Replace, c => c.GetMember("C")));
        }

        [Fact]
        public void Type_Delete()
        {
            var src1 = @"
class C { void F() {} }
struct S { void F() {} }
interface I { void F() {} }
";
            var src2 = "";

            GetTopEdits(src1, src2).VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Delete, null, DeletedSymbolDisplay(FeaturesResources.class_, "C")),
                Diagnostic(RudeEditKind.Delete, null, DeletedSymbolDisplay(CSharpFeaturesResources.struct_, "S")),
                Diagnostic(RudeEditKind.Delete, null, DeletedSymbolDisplay(FeaturesResources.interface_, "I")));
        }

        [Fact]
        public void Type_Delete_Reloadable()
        {
            var src1 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]class C { void F() {} }";
            var src2 = ReloadableAttributeSrc;

            GetTopEdits(src1, src2).VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Delete, null, DeletedSymbolDisplay(FeaturesResources.class_, "C")));
        }

        [Fact]
        public void Type_Partial_DeleteDeclaration()
        {
            var srcA1 = "partial class C { void F() {} void M() { } }";
            var srcB1 = "partial class C { void G() {} }";
            var srcA2 = "";
            var srcB2 = "partial class C { void G() {} void M() { } }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(
                        semanticEdits: new[]
                        {
                            SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.F"), deletedSymbolContainerProvider: c => c.GetMember("C"))
                        }),

                    DocumentResults(
                        semanticEdits: new[]
                        {
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember("M")),
                        })
                });
        }

        [Fact]
        public void Type_Partial_InsertFirstDeclaration()
        {
            var src1 = "";
            var src2 = "partial class C { void F() {}  }";

            GetTopEdits(src1, src2).VerifySemantics(
                new[] { SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<INamedTypeSymbol>("C"), preserveLocalVariables: false) },
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void Type_Partial_InsertSecondDeclaration()
        {
            var srcA1 = "partial class C { void F() {} }";
            var srcB1 = "";
            var srcA2 = "partial class C { void F() {} }";
            var srcB2 = "partial class C { void G() {} }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(),

                    DocumentResults(
                        semanticEdits: new[]
                        {
                            SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<INamedTypeSymbol>("C").GetMember("G"), preserveLocalVariables: false)
                        }),
                },
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
        }

        [Fact]
        public void Type_Partial_Reloadable()
        {
            var srcA1 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]partial class C { void F() {} }";
            var srcB1 = "";
            var srcA2 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]partial class C { void F() {} }";
            var srcB2 = "partial class C { void G() {} }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(),
                    DocumentResults(
                        semanticEdits: new[]
                        {
                            SemanticEdit(SemanticEditKind.Replace, c => c.GetMember("C"), partialType: "C")
                        }),
                },
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void Type_DeleteInsert()
        {
            var srcA1 = @"
class C { void F() {} }
struct S { void F() {} }
interface I { void F() {} }
";
            var srcB1 = "";

            var srcA2 = srcB1;
            var srcB2 = srcA1;

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(),

                    DocumentResults(
                        semanticEdits: new[]
                        {
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember("F")),
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("S").GetMember("F")),
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("I").GetMember("F")),
                        })
                });
        }

        [Fact]
        public void Type_DeleteInsert_Reloadable()
        {
            var srcA1 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]class C { void F() {} }";
            var srcB1 = "";

            var srcA2 = ReloadableAttributeSrc;
            var srcB2 = "[CreateNewOnMetadataUpdate]class C { void F() {} }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(),
                    DocumentResults(
                        semanticEdits: new[]
                        {
                            SemanticEdit(SemanticEditKind.Replace, c => c.GetMember("C")),
                        })
                },
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void Type_NonInsertableMembers_DeleteInsert()
        {
            var srcA1 = @"
abstract class C
{
    public abstract void AbstractMethod();
    public virtual void VirtualMethod() {}
    public override string ToString() => null;
    public void I.G() {}
}

interface I
{
    void G();
    void F() {}
}
";
            var srcB1 = "";

            var srcA2 = srcB1;
            var srcB2 = srcA1;

            // TODO: The methods without bodies do not need to be updated.
            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(),

                    DocumentResults(
                        semanticEdits: new[]
                        {
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember("AbstractMethod")),
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember("VirtualMethod")),
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember("ToString")),
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember("I.G")),
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("I").GetMember("G")),
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("I").GetMember("F")),
                        })
                });
        }

        [Fact]
        public void Type_Attribute_NonInsertableMembers_DeleteInsert()
        {
            var srcA1 = @"
abstract class C
{
    public abstract void AbstractMethod();
    public virtual void VirtualMethod() {}
    public override string ToString() => null;
    public void I.G() {}
}

interface I
{
    void G();
    void F() {}
}
";
            var srcB1 = "";

            var srcA2 = "";
            var srcB2 = @"
abstract class C
{
    [System.Obsolete]public abstract void AbstractMethod();
    public virtual void VirtualMethod() {}
    public override string ToString() => null;
    public void I.G() {}
}

interface I
{
    [System.Obsolete]void G();
    void F() {}
}";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(),

                    DocumentResults(
                        semanticEdits: new[]
                        {
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember("AbstractMethod")),
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember("VirtualMethod")),
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember("ToString")),
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember("I.G")),
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("I").GetMember("G")),
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("I").GetMember("F")),
                        })
                },
                capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
        }

        [Fact]
        public void Type_DeleteInsert_DataMembers()
        {
            var srcA1 = @"
class C
{
    public int x = 1;
    public int y = 2;
    public int P { get; set; } = 3;
    public event System.Action E = new System.Action(null);
}
";
            var srcB1 = "";

            var srcA2 = "";
            var srcB2 = @"
class C
{
    public int x = 1;
    public int y = 2;
    public int P { get; set; } = 3;
    public event System.Action E = new System.Action(null);
}
";
            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(),

                    DocumentResults(
                        semanticEdits: new[]
                        {
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("C.P").GetMethod),
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("C.P").SetMethod),
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), preserveLocalVariables: true),
                        })
                });
        }

        [Fact]
        public void Type_DeleteInsert_DataMembers_PartialSplit()
        {
            var srcA1 = @"
class C
{
    public int x = 1;
    public int y = 2;
    public int P { get; set; } = 3;
}
";
            var srcB1 = "";

            var srcA2 = @"
partial class C
{
    public int x = 1;
    public int y = 2;
}
";
            var srcB2 = @"
partial class C
{
    public int P { get; set; } = 3;
}
";
            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(),

                    DocumentResults(
                        semanticEdits: new[]
                        {
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("C.P").GetMethod),
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("C.P").SetMethod),
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), partialType: "C", preserveLocalVariables: true),
                        })
                });
        }

        [Fact]
        public void Type_DeleteInsert_DataMembers_PartialMerge()
        {
            var srcA1 = @"
partial class C
{
    public int x = 1;
    public int y = 2;
}
";
            var srcB1 = @"
partial class C
{
    public int P { get; set; } = 3;
}";

            var srcA2 = @"
class C
{
    public int x = 1;
    public int y = 2;
    public int P { get; set; } = 3;
}
";

            var srcB2 = @"
";
            // note that accessors are not updated since they do not have bodies
            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(
                        semanticEdits: new[]
                        {
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("C.P").GetMethod),
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("C.P").SetMethod),
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), partialType: "C", preserveLocalVariables: true),
                        }),

                    DocumentResults()
                });
        }

        [Theory]
        [InlineData("class")]
        [InlineData("struct")]
        [InlineData("interface")]
        [InlineData("record")]
        [InlineData("record struct")]
        public void Type_Move_NamespaceChange(string keyword)
        {
            var declaration = keyword + " C {}";
            var src1 = $"namespace N {{{declaration,-20}}} namespace M {{             }}";
            var src2 = $"namespace N {{                 }} namespace M {{{declaration}}}";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Move [" + declaration + "]@13 -> @45");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingNamespace, keyword + " C", GetResource(keyword), "N", "M"));
        }

        [Fact]
        public void Type_Move_NamespaceChange_Delegate()
        {
            var src1 = @"namespace N { delegate void F(); } namespace M {                    }";
            var src2 = @"namespace N {                    } namespace M { delegate void F(); }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Move [delegate void F();]@14 -> @49");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingNamespace, "delegate void F()", GetResource("delegate"), "N", "M"));
        }

        [Fact]
        public void Type_Move_NamespaceChange_Subnamespace()
        {
            var src1 = @"namespace N { class C {} } namespace M { namespace O {            } }";
            var src2 = @"namespace N {            } namespace M { namespace O { class C {} } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Move [class C {}]@14 -> @55");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingNamespace, "class C", GetResource("class"), "N", "M.O"));
        }

        [Fact]
        public void Type_Move_SameEffectiveNamespace()
        {
            var src1 = @"namespace N.M { class C {} } namespace N { namespace M {            } }";
            var src2 = @"namespace N.M {            } namespace N { namespace M { class C {} } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Move [class C {}]@16 -> @57");

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void Type_Move_MultiFile()
        {
            var srcA1 = @"namespace N { class C {} } namespace M {            }";
            var srcB1 = @"namespace N {            } namespace M { class C {} }";
            var srcA2 = @"namespace N {            } namespace M { class C {} }";
            var srcB2 = @"namespace N { class C {} } namespace M {            }";

            var editsA = GetTopEdits(srcA1, srcA2);
            editsA.VerifyEdits(
                "Move [class C {}]@14 -> @41");

            var editsB = GetTopEdits(srcB1, srcB2);
            editsB.VerifyEdits(
                "Move [class C {}]@41 -> @14");

            EditAndContinueValidation.VerifySemantics(
                new[] { editsA, editsB },
                new[]
                {
                    DocumentResults(),
                    DocumentResults(),
                });
        }

        #endregion

        #region Records

        [Fact]
        public void Record_Partial_MovePrimaryConstructor()
        {
            var src1 = @"
partial record C { }
partial record C(int X);";
            var src2 = @"
partial record C(int X);
partial record C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics();

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void Record_Name_Update()
        {
            var src1 = "record C { }";
            var src2 = "record D { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [record C { }]@0 -> [record D { }]@0");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "record D", CSharpFeaturesResources.record_));
        }

        [Fact]
        public void RecordStruct_NoModifiers_Insert()
        {
            var src1 = "";
            var src2 = "record struct C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void RecordStruct_AddField()
        {
            var src1 = @"
record struct C(int X)
{
}";
            var src2 = @"
record struct C(int X)
{
    private int _y = 0;
}";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                 Diagnostic(RudeEditKind.InsertIntoStruct, "_y = 0", FeaturesResources.field, CSharpFeaturesResources.record_struct));
        }

        [Fact]
        public void RecordStruct_AddProperty()
        {
            var src1 = @"
record struct C(int X)
{
}";
            var src2 = @"
record struct C(int X)
{
    public int Y { get; set; } = 0;
}";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                 Diagnostic(RudeEditKind.InsertIntoStruct, "public int Y { get; set; } = 0;", FeaturesResources.auto_property, CSharpFeaturesResources.record_struct));
        }

        [Fact]
        public void Record_NoModifiers_Insert()
        {
            var src1 = "";
            var src2 = "record C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void Record_NoModifiers_IntoNamespace_Insert()
        {
            var src1 = "namespace N { }";
            var src2 = "namespace N { record C { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void Record_NoModifiers_IntoType_Insert()
        {
            var src1 = "struct N { }";
            var src2 = "struct N { record C { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void Record_BaseTypeUpdate1()
        {
            var src1 = "record C { }";
            var src2 = "record C : D { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [record C { }]@0 -> [record C : D { }]@0");

            edits.VerifySemanticDiagnostics(
                 Diagnostic(RudeEditKind.BaseTypeOrInterfaceUpdate, "record C", CSharpFeaturesResources.record_));
        }

        [Fact]
        public void Record_BaseTypeUpdate2()
        {
            var src1 = "record C : D1 { }";
            var src2 = "record C : D2 { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [record C : D1 { }]@0 -> [record C : D2 { }]@0");

            edits.VerifySemanticDiagnostics(
                 Diagnostic(RudeEditKind.BaseTypeOrInterfaceUpdate, "record C", CSharpFeaturesResources.record_));
        }

        [Fact]
        public void Record_BaseInterfaceUpdate1()
        {
            var src1 = "record C { }";
            var src2 = "record C : IDisposable { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [record C { }]@0 -> [record C : IDisposable { }]@0");

            edits.VerifySemanticDiagnostics(
                 Diagnostic(RudeEditKind.BaseTypeOrInterfaceUpdate, "record C", CSharpFeaturesResources.record_));
        }

        [Fact]
        public void Record_BaseInterfaceUpdate2()
        {
            var src1 = "record C : IGoo, IBar { }";
            var src2 = "record C : IGoo { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [record C : IGoo, IBar { }]@0 -> [record C : IGoo { }]@0");

            edits.VerifySemanticDiagnostics(
                 Diagnostic(RudeEditKind.BaseTypeOrInterfaceUpdate, "record C", CSharpFeaturesResources.record_));
        }

        [Fact]
        public void Record_BaseInterfaceUpdate3()
        {
            var src1 = "record C : IGoo, IBar { }";
            var src2 = "record C : IBar, IGoo { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [record C : IGoo, IBar { }]@0 -> [record C : IBar, IGoo { }]@0");

            edits.VerifySemanticDiagnostics(
                 Diagnostic(RudeEditKind.BaseTypeOrInterfaceUpdate, "record C", CSharpFeaturesResources.record_));
        }

        [Fact]
        public void RecordInsert_AbstractVirtualOverride()
        {
            var src1 = "";
            var src2 = @"
public abstract record C<T>
{ 
    public abstract void F(); 
    public virtual void G() {}
    public override void H() {}
}";

            var edits = GetTopEdits(src1, src2);
            edits.VerifySemanticDiagnostics(
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void Record_ImplementSynthesized_ParameterlessConstructor()
        {
            var src1 = "record C { }";
            var src2 = @"
record C
{
    public C()
    {
    }
}";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.First(c => c.ToString() == "C.C()"), preserveLocalVariables: true));

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void RecordStruct_ImplementSynthesized_ParameterlessConstructor()
        {
            var src1 = "record struct C { }";
            var src2 = @"
record struct C
{
    public C()
    {
    }
}";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.First(c => c.ToString() == "C.C()"), preserveLocalVariables: true));

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void Record_ImplementSynthesized_PrintMembers()
        {
            var src1 = "record C { }";
            var src2 = @"
record C
{
    protected virtual bool PrintMembers(System.Text.StringBuilder builder)
    {
        return true;
    }
}";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")));

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void RecordStruct_ImplementSynthesized_PrintMembers()
        {
            var src1 = "record struct C { }";
            var src2 = @"
record struct C
{
    private readonly bool PrintMembers(System.Text.StringBuilder builder)
    {
        return true;
    }
}";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")));

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void Record_ImplementSynthesized_WrongParameterName()
        {
            var src1 = "record C { }";
            var src2 = @"
record C
{
    protected virtual bool PrintMembers(System.Text.StringBuilder sb)
    {
        return false;
    }

    public virtual bool Equals(C rhs)
    {
        return false;
    }

    protected C(C other)
    {
    }
}";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                new[]
                {
                    Diagnostic(RudeEditKind.ExplicitRecordMethodParameterNamesMustMatch, "protected virtual bool PrintMembers(System.Text.StringBuilder sb)", "PrintMembers(System.Text.StringBuilder builder)"),
                    Diagnostic(RudeEditKind.RenamingNotSupportedByRuntime, "System.Text.StringBuilder sb", FeaturesResources.parameter),
                    Diagnostic(RudeEditKind.ExplicitRecordMethodParameterNamesMustMatch, "public virtual bool Equals(C rhs)", "Equals(C other)"),
                    Diagnostic(RudeEditKind.RenamingNotSupportedByRuntime, "C rhs", FeaturesResources.parameter),
                    Diagnostic(RudeEditKind.ExplicitRecordMethodParameterNamesMustMatch, "protected C(C other)", "C(C original)")
                },
                capabilities: EditAndContinueCapabilities.Baseline);

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMembers("Equals").OfType<IMethodSymbol>().First(m => SymbolEqualityComparer.Default.Equals(m.Parameters[0].Type, m.ContainingType))),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(c => c.Parameters.FirstOrDefault()?.Type.ToDisplayString() == "C"), preserveLocalVariables: true),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(c => c.Parameters.Length == 0), preserveLocalVariables: true),
                },
                capabilities: EditAndContinueCapabilities.UpdateParameters);
        }

        [Fact]
        public void Record_ImplementSynthesized_ToString()
        {
            var src1 = "record C { }";
            var src2 = @"
record C
{
    public override string ToString()
    {
        return ""R"";
    }
}";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.ToString")));

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void Record_UnImplementSynthesized_ToString()
        {
            var src1 = @"
record C
{
    public override string ToString()
    {
        return ""R"";
    }
}";
            var src2 = "record C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.ToString")));

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void Record_AddProperty_Primary()
        {
            var src1 = "record C(int X);";
            var src2 = "record C(int X, int Y);";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Insert, "int Y", FeaturesResources.parameter));
        }

        [Fact]
        public void Record_UnimplementSynthesized_ParameterlessConstructor()
        {
            var src1 = @"
record C
{
    public C()
    {
    }
}";
            var src2 = "record C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.First(c => c.ToString() == "C.C()"), preserveLocalVariables: true));

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void RecordStruct_UnimplementSynthesized_ParameterlessConstructor()
        {
            var src1 = @"
record struct C
{
    public C()
    {
    }
}";
            var src2 = "record struct C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.First(c => c.ToString() == "C.C()"), preserveLocalVariables: true));

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void Record_AddProperty_NotPrimary()
        {
            var src1 = "record C(int X);";
            var src2 = @"
record C(int X)
{
    public int Y { get; set; }
}";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMembers("Equals").OfType<IMethodSymbol>().First(m => SymbolEqualityComparer.Default.Equals(m.Parameters[0].Type, m.ContainingType))),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.Y")),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(c => c.Parameters[0].Type.ToDisplayString() == "int"), preserveLocalVariables: true),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(c => c.Parameters[0].Type.ToDisplayString() == "C"))
                },
                capabilities: EditAndContinueCapabilities.AddInstanceFieldToExistingType | EditAndContinueCapabilities.AddMethodToExistingType);
        }

        [Fact]
        public void Record_AddProperty_NotPrimary_WithConstructor()
        {
            var src1 = @"
record C(int X)
{
    public C(string fromAString)
    {
    }
}";
            var src2 = @"
record C(int X)
{
    public int Y { get; set; }

    public C(string fromAString)
    {
    }
}";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMembers("Equals").OfType<IMethodSymbol>().First(m => SymbolEqualityComparer.Default.Equals(m.Parameters[0].Type, m.ContainingType))),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.Y")),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(c => c.Parameters[0].Type.ToDisplayString() == "int"), preserveLocalVariables: true),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(c => c.Parameters[0].Type.ToDisplayString() == "C"))
                },
                capabilities: EditAndContinueCapabilities.AddInstanceFieldToExistingType | EditAndContinueCapabilities.AddMethodToExistingType);
        }

        [Fact]
        public void Record_AddProperty_NotPrimary_WithExplicitMembers()
        {
            var src1 = @"
record C(int X)
{
    protected virtual bool PrintMembers(System.Text.StringBuilder builder)
    {
        return false;
    }

    public override int GetHashCode()
    {
        return 0;
    }

    public virtual bool Equals(C other)
    {
        return false;
    }

    public C(C original)
    {
    }
}";
            var src2 = @"
record C(int X)
{
    public int Y { get; set; }

    protected virtual bool PrintMembers(System.Text.StringBuilder builder)
    {
        return false;
    }

    public override int GetHashCode()
    {
        return 0;
    }

    public virtual bool Equals(C other)
    {
        return false;
    }

    public C(C original)
    {
    }
}";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.Y")),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(c => c.Parameters[0].Type.ToDisplayString() == "int"), preserveLocalVariables: true)
                },
                capabilities: EditAndContinueCapabilities.AddInstanceFieldToExistingType | EditAndContinueCapabilities.AddMethodToExistingType);
        }

        [Fact]
        public void Record_AddProperty_NotPrimary_WithInitializer()
        {
            var src1 = "record C(int X);";
            var src2 = @"
record C(int X)
{
    public int Y { get; set; } = 1;
}";

            var edits = GetTopEdits(src1, src2);
            var syntaxMap = GetSyntaxMap(src1, src2);

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMembers("Equals").OfType<IMethodSymbol>().First(m => SymbolEqualityComparer.Default.Equals(m.Parameters[0].Type, m.ContainingType))),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.Y")),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(c => c.Parameters[0].Type.ToDisplayString() == "int"), preserveLocalVariables: true),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(c => c.Parameters[0].Type.ToDisplayString() == "C"))
                },
                capabilities: EditAndContinueCapabilities.AddInstanceFieldToExistingType | EditAndContinueCapabilities.AddMethodToExistingType);
        }

        [Fact]
        public void Record_AddField()
        {
            var src1 = "record C(int X) { }";
            var src2 = "record C(int X) { private int _y; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMembers("Equals").OfType<IMethodSymbol>().First(m => SymbolEqualityComparer.Default.Equals(m.Parameters[0].Type, m.ContainingType))),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C._y")),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(c => c.Parameters[0].Type.ToDisplayString() == "int"), preserveLocalVariables: true),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(c => c.Parameters[0].Type.ToDisplayString() == "C"))
                },
                capabilities: EditAndContinueCapabilities.AddInstanceFieldToExistingType);
        }

        [Fact]
        public void Record_AddField_WithExplicitMembers()
        {
            var src1 = @"
record C(int X)
{
    public C(C other)
    {
    }
}";
            var src2 = @"
record C(int X)
{
    private int _y;
    
    public C(C other)
    {
    }
}";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMembers("Equals").OfType<IMethodSymbol>().First(m => SymbolEqualityComparer.Default.Equals(m.Parameters[0].Type, m.ContainingType))),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C._y")),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(c => c.Parameters[0].Type.ToDisplayString() == "int"), preserveLocalVariables: true)
                },
                capabilities: EditAndContinueCapabilities.AddInstanceFieldToExistingType);
        }

        [Fact]
        public void Record_AddField_WithInitializer()
        {
            var src1 = "record C(int X) { }";
            var src2 = "record C(int X) { private int _y = 1; }";
            var syntaxMap = GetSyntaxMap(src1, src2);

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMembers("Equals").OfType<IMethodSymbol>().First(m => SymbolEqualityComparer.Default.Equals(m.Parameters[0].Type, m.ContainingType))),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C._y")),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(c => c.Parameters[0].Type.ToDisplayString() == "int"), preserveLocalVariables: true),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(c => c.Parameters[0].Type.ToDisplayString() == "C"))
                },
                capabilities: EditAndContinueCapabilities.AddInstanceFieldToExistingType);
        }

        [Fact]
        public void Record_AddField_WithExistingInitializer()
        {
            var src1 = "record C(int X) { private int _y = <N:0.0>1</N:0.0>; }";
            var src2 = "record C(int X) { private int _y = <N:0.0>1</N:0.0>; private int _z; }";

            var edits = GetTopEdits(src1, src2);
            var syntaxMap = GetSyntaxMap(src1, src2);

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMembers("Equals").OfType<IMethodSymbol>().First(m => SymbolEqualityComparer.Default.Equals(m.Parameters[0].Type, m.ContainingType))),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C._z")),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(c => c.Parameters[0].Type.ToDisplayString() == "int"), syntaxMap[0]),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(c => c.Parameters[0].Type.ToDisplayString() == "C"))
                },
                capabilities: EditAndContinueCapabilities.AddInstanceFieldToExistingType);
        }

        [Fact]
        public void Record_AddField_WithInitializerAndExistingInitializer()
        {
            var src1 = "record C(int X) { private int _y = <N:0.0>1</N:0.0>; }";
            var src2 = "record C(int X) { private int _y = <N:0.0>1</N:0.0>; private int _z = 1; }";

            var edits = GetTopEdits(src1, src2);
            var syntaxMap = GetSyntaxMap(src1, src2);

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMembers("Equals").OfType<IMethodSymbol>().First(m => SymbolEqualityComparer.Default.Equals(m.Parameters[0].Type, m.ContainingType))),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C._z")),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(c => c.Parameters[0].Type.ToDisplayString() == "int"), syntaxMap[0]),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(c => c.Parameters[0].Type.ToDisplayString() == "C"))
                },
                capabilities: EditAndContinueCapabilities.AddInstanceFieldToExistingType);
        }

        [Fact]
        public void Record_DeleteField()
        {
            var src1 = "record C(int X) { private int _y; }";
            var src2 = "record C(int X) { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(Diagnostic(RudeEditKind.Delete, "record C", DeletedSymbolDisplay(FeaturesResources.field, "_y")));
        }

        [Fact]
        public void Record_DeleteProperty_Primary()
        {
            var src1 = "record C(int X, int Y) { }";
            var src2 = "record C(int X) { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(Diagnostic(RudeEditKind.Delete, "record C", DeletedSymbolDisplay(FeaturesResources.parameter, "int Y")));
        }

        [Fact]
        public void Record_DeleteProperty_NotPrimary()
        {
            var src1 = "record C(int X) { public int P { get; set; } }";
            var src2 = "record C(int X) { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.get_P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.set_P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void Record_ImplementSynthesized_Property()
        {
            var src1 = "record C(int X);";
            var src2 = @"
record C(int X)
{
    public int X { get; init; }
}";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(c => c.Parameters[0].Type.ToDisplayString() == "int"), preserveLocalVariables: true));

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void Record_ImplementSynthesized_Property_WithBody()
        {
            var src1 = "record C(int X);";
            var src2 = @"
record C(int X)
{
    public int X
    {
        get
        {
            return 4;
        }
        init
        {
            throw null;
        }
    }
}";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMembers("Equals").OfType<IMethodSymbol>().First(m => SymbolEqualityComparer.Default.Equals(m.Parameters[0].Type, m.ContainingType))),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("C.X").GetMethod),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("C.X").SetMethod),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(c => c.Parameters[0].Type.ToDisplayString() == "int"), preserveLocalVariables: true),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(c => c.Parameters[0].Type.ToDisplayString() == "C")));

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void Record_ImplementSynthesized_Property_WithExpressionBody()
        {
            var src1 = "record C(int X);";
            var src2 = @"
record C(int X)
{
    public int X { get => 4; init => throw null; }
}";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMembers("Equals").OfType<IMethodSymbol>().First(m => SymbolEqualityComparer.Default.Equals(m.Parameters[0].Type, m.ContainingType))),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("C.X").GetMethod),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("C.X").SetMethod),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(c => c.Parameters[0].Type.ToDisplayString() == "int"), preserveLocalVariables: true),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(c => c.Parameters[0].Type.ToDisplayString() == "C")));

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void Record_ImplementSynthesized_Property_InitToSet()
        {
            var src1 = "record C(int X);";
            var src2 = @"
record C(int X)
{
    public int X { get; set; }
}";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ImplementRecordParameterWithSet, "public int X", "X"));
        }

        [Fact]
        public void Record_ImplementSynthesized_Property_MakeReadOnly()
        {
            var src1 = "record C(int X);";
            var src2 = @"
record C(int X)
{
    public int X { get; }
}";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ImplementRecordParameterAsReadOnly, "public int X", "X"));
        }

        [Fact]
        public void Record_UnImplementSynthesized_Property()
        {
            var src1 = @"
record C(int X)
{
    public int X { get; init; }
}";
            var src2 = "record C(int X);";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(c => c.Parameters[0].Type.ToDisplayString() == "int"), preserveLocalVariables: true));

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void Record_UnImplementSynthesized_Property_WithExpressionBody()
        {
            var src1 = @"
record C(int X)
{
    public int X { get => 4; init => throw null; }
}";
            var src2 = "record C(int X);";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMembers("Equals").OfType<IMethodSymbol>().First(m => SymbolEqualityComparer.Default.Equals(m.Parameters[0].Type, m.ContainingType))),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("C.X").GetMethod),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("C.X").SetMethod),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(c => c.Parameters[0].Type.ToDisplayString() == "int"), preserveLocalVariables: true),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(c => c.Parameters[0].Type.ToDisplayString() == "C")));

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void Record_UnImplementSynthesized_Property_WithBody()
        {
            var src1 = @"
record C(int X)
{
    public int X { get { return 4; } init { } }
}";
            var src2 = "record C(int X);";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMembers("Equals").OfType<IMethodSymbol>().First(m => SymbolEqualityComparer.Default.Equals(m.Parameters[0].Type, m.ContainingType))),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("C.X").GetMethod),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("C.X").SetMethod),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(c => c.Parameters[0].Type.ToDisplayString() == "int"), preserveLocalVariables: true),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(c => c.Parameters[0].Type.ToDisplayString() == "C")));

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void Record_ImplementSynthesized_Property_Partial()
        {
            var srcA1 = @"partial record C(int X);";
            var srcB1 = @"partial record C;";
            var srcA2 = @"partial record C(int X);";
            var srcB2 = @"
partial record C
{
    public int X { get; init; }
}";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(),
                    DocumentResults(
                        semanticEdits: new[]
                        {
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(c => c.Parameters[0].Type.ToDisplayString() == "int"), partialType: "C", preserveLocalVariables: true)
                        })
                });
        }

        [Fact]
        public void Record_UnImplementSynthesized_Property_Partial()
        {
            var srcA1 = @"partial record C(int X);";
            var srcB1 = @"
partial record C
{
    public int X { get; init; }
}";
            var srcA2 = @"partial record C(int X);";
            var srcB2 = @"partial record C;";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(),
                    DocumentResults(
                        semanticEdits: new[]
                        {
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(c => c.Parameters[0].Type.ToDisplayString() == "int"), partialType: "C", preserveLocalVariables: true)
                        })
                });
        }

        [Fact]
        public void Record_ImplementSynthesized_Property_Partial_WithBody()
        {
            var srcA1 = @"partial record C(int X);";
            var srcB1 = @"partial record C;";
            var srcA2 = @"partial record C(int X);";
            var srcB2 = @"
partial record C
{
    public int X
    {
        get
        {
            return 4;
        }
        init
        {
            throw null;
        }
    }
}";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(),
                    DocumentResults(
                        semanticEdits: new[]
                        {
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMembers("Equals").OfType<IMethodSymbol>().First(m => SymbolEqualityComparer.Default.Equals(m.Parameters[0].Type, m.ContainingType))),
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("C.X").GetMethod),
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("C.X").SetMethod),
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(c => c.Parameters[0].Type.ToDisplayString() == "int"), partialType : "C", preserveLocalVariables: true),
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(c => c.Parameters[0].Type.ToDisplayString() == "C"))
                        })
                });
        }

        [Fact]
        public void Record_UnImplementSynthesized_Property_Partial_WithBody()
        {
            var srcA1 = @"partial record C(int X);";
            var srcB1 = @"
partial record C
{
    public int X
    {
        get
        {
            return 4;
        }
        init
        {
            throw null;
        }
    }
}";
            var srcA2 = @"partial record C(int X);";
            var srcB2 = @"partial record C;";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(),
                    DocumentResults(
                        semanticEdits: new[]
                        {
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.PrintMembers")),
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMembers("Equals").OfType<IMethodSymbol>().First(m => SymbolEqualityComparer.Default.Equals(m.Parameters[0].Type, m.ContainingType))),
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.GetHashCode")),
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("C.X").GetMethod),
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("C.X").SetMethod),
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(c => c.Parameters[0].Type.ToDisplayString() == "int"), partialType : "C", preserveLocalVariables: true),
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(c => c.Parameters[0].Type.ToDisplayString() == "C"))
                        })
                });
        }

        [Fact]
        public void Record_MoveProperty_Partial()
        {
            var srcA1 = @"
partial record C(int X)
{
    public int Y { get; init; }
}";
            var srcB1 = @"
partial record C;
";

            var srcA2 = @"
partial record C(int X);
";

            var srcB2 = @"
partial record C
{
    public int Y { get; init; }
}";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(),
                    DocumentResults(
                        semanticEdits: new[]
                        {
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("C.Y").GetMethod),
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("C.Y").SetMethod)
                        }),
                });
        }

        [Fact]
        public void Record_UnImplementSynthesized_Property_WithInitializer()
        {
            var src1 = @"
record C(int X)
{
    public int X { get; init; } = 1;
}";
            var src2 = "record C(int X);";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(c => c.Parameters[0].Type.ToDisplayString() == "int"), preserveLocalVariables: true));

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void Record_UnImplementSynthesized_Property_WithInitializerMatchingCompilerGenerated()
        {
            var src1 = @"
record C(int X)
{
    public int X { get; init; } = X;
}";
            var src2 = "record C(int X);";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(c => c.Parameters[0].Type.ToDisplayString() == "int"), preserveLocalVariables: true));

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void Record_Property_Delete_NotPrimary()
        {
            var src1 = @"
record C(int X)
{
    public int Y { get; init; }
}";
            var src2 = "record C(int X);";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.get_Y"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.set_Y"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void Record_PropertyInitializer_Update_NotPrimary()
        {
            var src1 = "record C { int X { get; } = 0; }";
            var src2 = "record C { int X { get; } = 1; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(c => c.Parameters.Length == 0), preserveLocalVariables: true));
        }

        [Fact]
        public void Record_PropertyInitializer_Update_Primary()
        {
            var src1 = "record C(int X) { int X { get; } = 0; }";
            var src2 = "record C(int X) { int X { get; } = 1; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(c => c.Parameters[0].Type.ToDisplayString() == "int"), preserveLocalVariables: true));
        }

        #endregion

        #region Enums

        [Fact]
        public void Enum_NoModifiers_Insert()
        {
            var src1 = "";
            var src2 = "enum C { A }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void Enum_NoModifiers_IntoNamespace_Insert()
        {
            var src1 = "namespace N { }";
            var src2 = "namespace N { enum C { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void Enum_NoModifiers_IntoType_Insert()
        {
            var src1 = "struct N { }";
            var src2 = "struct N { enum C { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void Enum_Attribute_Insert()
        {
            var attribute = "public class AAttribute : System.Attribute { }\n\n";

            var src1 = attribute + "enum E { }";
            var src2 = attribute + "[A]enum E { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [enum E { }]@48 -> [[A]enum E { }]@48");

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "enum E", FeaturesResources.enum_) },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void Enum_Member_Attribute_Delete()
        {
            var attribute = "public class AAttribute : System.Attribute { }\n\n";

            var src1 = attribute + "enum E { [A]X }";
            var src2 = attribute + "enum E { X }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [[A]X]@57 -> [X]@57");

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "X", FeaturesResources.enum_value) },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void Enum_Member_Attribute_Insert()
        {
            var attribute = "public class AAttribute : System.Attribute { }\n\n";

            var src1 = attribute + "enum E { X }";
            var src2 = attribute + "enum E { [A]X }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [X]@57 -> [[A]X]@57");

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "[A]X", FeaturesResources.enum_value) },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void Enum_Member_Attribute_Update()
        {
            var attribute = "public class A1Attribute : System.Attribute { }\n\n" +
                            "public class A2Attribute : System.Attribute { }\n\n";

            var src1 = attribute + "enum E { [A1]X }";
            var src2 = attribute + "enum E { [A2]X }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [[A1]X]@107 -> [[A2]X]@107");

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "[A2]X", FeaturesResources.enum_value) },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void Enum_Member_Attribute_InsertDeleteAndUpdate()
        {
            var srcA1 = "";
            var srcB1 = "enum N { A = 1 }";
            var srcA2 = "enum N { [System.Obsolete]A = 1 }";
            var srcB2 = "";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(semanticEdits: new[]
                    {
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember("N.A"))
                    }),
                    DocumentResults()
                },
                capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
        }

        [Fact]
        public void Enum_Rename()
        {
            var src1 = "enum Color { Red = 1, Blue = 2, }";
            var src2 = "enum Colors { Red = 1, Blue = 2, }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [enum Color { Red = 1, Blue = 2, }]@0 -> [enum Colors { Red = 1, Blue = 2, }]@0");

            edits.VerifySemanticDiagnostics(
                 Diagnostic(RudeEditKind.Renamed, "enum Colors", FeaturesResources.enum_));
        }

        [Fact]
        public void Enum_BaseType_Add()
        {
            var src1 = "enum Color { Red = 1, Blue = 2, }";
            var src2 = "enum Color : ushort { Red = 1, Blue = 2, }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [enum Color { Red = 1, Blue = 2, }]@0 -> [enum Color : ushort { Red = 1, Blue = 2, }]@0");

            edits.VerifySemanticDiagnostics(
                 Diagnostic(RudeEditKind.EnumUnderlyingTypeUpdate, "enum Color", FeaturesResources.enum_));
        }

        [Fact]
        public void Enum_BaseType_Add_Unchanged()
        {
            var src1 = "enum Color { Red = 1, Blue = 2, }";
            var src2 = "enum Color : int { Red = 1, Blue = 2, }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [enum Color { Red = 1, Blue = 2, }]@0 -> [enum Color : int { Red = 1, Blue = 2, }]@0");

            edits.VerifySemantics();
        }

        [Fact]
        public void Enum_BaseType_Update()
        {
            var src1 = "enum Color : ushort { Red = 1, Blue = 2, }";
            var src2 = "enum Color : long { Red = 1, Blue = 2, }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [enum Color : ushort { Red = 1, Blue = 2, }]@0 -> [enum Color : long { Red = 1, Blue = 2, }]@0");

            edits.VerifySemanticDiagnostics(
                 Diagnostic(RudeEditKind.EnumUnderlyingTypeUpdate, "enum Color", FeaturesResources.enum_));
        }

        [Fact]
        public void Enum_BaseType_Delete_Unchanged()
        {
            var src1 = "enum Color : int { Red = 1, Blue = 2, }";
            var src2 = "enum Color { Red = 1, Blue = 2, }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [enum Color : int { Red = 1, Blue = 2, }]@0 -> [enum Color { Red = 1, Blue = 2, }]@0");

            edits.VerifySemantics();
        }

        [Fact]
        public void Enum_BaseType_Delete_Changed()
        {
            var src1 = "enum Color : ushort { Red = 1, Blue = 2, }";
            var src2 = "enum Color { Red = 1, Blue = 2, }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [enum Color : ushort { Red = 1, Blue = 2, }]@0 -> [enum Color { Red = 1, Blue = 2, }]@0");

            edits.VerifySemanticDiagnostics(
                 Diagnostic(RudeEditKind.EnumUnderlyingTypeUpdate, "enum Color", FeaturesResources.enum_));
        }

        [Fact]
        public void EnumAccessibilityChange()
        {
            var src1 = "public enum Color { Red = 1, Blue = 2, }";
            var src2 = "enum Color { Red = 1, Blue = 2, }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [public enum Color { Red = 1, Blue = 2, }]@0 -> [enum Color { Red = 1, Blue = 2, }]@0");

            edits.VerifySemanticDiagnostics(
                 Diagnostic(RudeEditKind.ChangingAccessibility, "enum Color", FeaturesResources.enum_));
        }

        [Fact]
        public void EnumAccessibilityNoChange()
        {
            var src1 = "internal enum Color { Red = 1, Blue = 2, }";
            var src2 = "enum Color { Red = 1, Blue = 2, }";

            var edits = GetTopEdits(src1, src2);
            edits.VerifySemantics();
        }

        [Fact]
        public void EnumInitializerUpdate()
        {
            var src1 = "enum Color { Red = 1, Blue = 2, }";
            var src2 = "enum Color { Red = 1, Blue = 3, }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [Blue = 2]@22 -> [Blue = 3]@22");

            edits.VerifySemanticDiagnostics(
                 Diagnostic(RudeEditKind.InitializerUpdate, "Blue = 3", FeaturesResources.enum_value));
        }

        [Fact]
        public void EnumInitializerUpdate2()
        {
            var src1 = "enum Color { Red = 1, Blue = 2, }";
            var src2 = "enum Color { Red = 1 << 0, Blue = 2 << 1, }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [Red = 1]@13 -> [Red = 1 << 0]@13",
                              "Update [Blue = 2]@22 -> [Blue = 2 << 1]@27");

            edits.VerifySemanticDiagnostics(
                 Diagnostic(RudeEditKind.InitializerUpdate, "Blue = 2 << 1", FeaturesResources.enum_value));
        }

        [Fact]
        public void EnumInitializerUpdate3()
        {
            var src1 = "enum Color { Red = int.MinValue }";
            var src2 = "enum Color { Red = int.MaxValue }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [Red = int.MinValue]@13 -> [Red = int.MaxValue]@13");

            edits.VerifySemanticDiagnostics(
                 Diagnostic(RudeEditKind.InitializerUpdate, "Red = int.MaxValue", FeaturesResources.enum_value));
        }

        [Fact]
        public void EnumInitializerUpdate_Reloadable()
        {
            var src1 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]enum Color { Red = 1 }";
            var src2 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]enum Color { Red = 2 }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [Red = 1]@185 -> [Red = 2]@185");

            edits.VerifySemantics(
                 new[] { SemanticEdit(SemanticEditKind.Replace, c => c.GetMember("Color")) },
                 capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void EnumInitializerAdd()
        {
            var src1 = "enum Color { Red, }";
            var src2 = "enum Color { Red = 1, }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [Red]@13 -> [Red = 1]@13");

            edits.VerifySemanticDiagnostics(
                 Diagnostic(RudeEditKind.InitializerUpdate, "Red = 1", FeaturesResources.enum_value));
        }

        [Fact]
        public void EnumInitializerDelete()
        {
            var src1 = "enum Color { Red = 1, }";
            var src2 = "enum Color { Red, }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [Red = 1]@13 -> [Red]@13");

            edits.VerifySemanticDiagnostics(
                 Diagnostic(RudeEditKind.InitializerUpdate, "Red", FeaturesResources.enum_value));
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/754916")]
        public void EnumMemberAdd()
        {
            var src1 = "enum Color { Red }";
            var src2 = "enum Color { Red, Blue}";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [enum Color { Red }]@0 -> [enum Color { Red, Blue}]@0",
                "Insert [Blue]@18");

            edits.VerifySemanticDiagnostics(
                 Diagnostic(RudeEditKind.Insert, "Blue", FeaturesResources.enum_value));
        }

        [Fact]
        public void EnumMemberAdd2()
        {
            var src1 = "enum Color { Red, }";
            var src2 = "enum Color { Red, Blue}";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Insert [Blue]@18");

            edits.VerifySemanticDiagnostics(
                 Diagnostic(RudeEditKind.Insert, "Blue", FeaturesResources.enum_value));
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/754916")]
        public void EnumMemberAdd3()
        {
            var src1 = "enum Color { Red, }";
            var src2 = "enum Color { Red, Blue,}";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [enum Color { Red, }]@0 -> [enum Color { Red, Blue,}]@0",
                              "Insert [Blue]@18");

            edits.VerifySemanticDiagnostics(
                 Diagnostic(RudeEditKind.Insert, "Blue", FeaturesResources.enum_value));
        }

        [Fact]
        public void EnumMemberUpdate()
        {
            var src1 = "enum Color { Red }";
            var src2 = "enum Color { Orange }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [Red]@13 -> [Orange]@13");

            edits.VerifySemanticDiagnostics(
                 Diagnostic(RudeEditKind.Renamed, "Orange", FeaturesResources.enum_value));
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/754916")]
        public void EnumMemberDelete()
        {
            var src1 = "enum Color { Red, Blue}";
            var src2 = "enum Color { Red }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [enum Color { Red, Blue}]@0 -> [enum Color { Red }]@0",
                "Delete [Blue]@18");

            edits.VerifySemanticDiagnostics(
                 Diagnostic(RudeEditKind.Delete, "enum Color", DeletedSymbolDisplay(FeaturesResources.enum_value, "Blue")));
        }

        [Fact]
        public void EnumMemberDelete2()
        {
            var src1 = "enum Color { Red, Blue}";
            var src2 = "enum Color { Red, }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Delete [Blue]@18");

            edits.VerifySemanticDiagnostics(
                 Diagnostic(RudeEditKind.Delete, "enum Color", DeletedSymbolDisplay(FeaturesResources.enum_value, "Blue")));
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/754916"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/793197")]
        public void EnumTrailingCommaAdd()
        {
            var src1 = "enum Color { Red }";
            var src2 = "enum Color { Red, }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [enum Color { Red }]@0 -> [enum Color { Red, }]@0");

            edits.VerifySemantics(ActiveStatementsDescription.Empty, NoSemanticEdits);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/754916"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/793197")]
        public void EnumTrailingCommaAdd_WithInitializer()
        {
            var src1 = "enum Color { Red = 1 }";
            var src2 = "enum Color { Red = 1, }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [enum Color { Red = 1 }]@0 -> [enum Color { Red = 1, }]@0");

            edits.VerifySemantics(ActiveStatementsDescription.Empty, NoSemanticEdits);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/754916"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/793197")]
        public void EnumTrailingCommaDelete()
        {
            var src1 = "enum Color { Red, }";
            var src2 = "enum Color { Red }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [enum Color { Red, }]@0 -> [enum Color { Red }]@0");

            edits.VerifySemantics(ActiveStatementsDescription.Empty, NoSemanticEdits);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/754916"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/793197")]
        public void EnumTrailingCommaDelete_WithInitializer()
        {
            var src1 = "enum Color { Red = 1, }";
            var src2 = "enum Color { Red = 1 }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [enum Color { Red = 1, }]@0 -> [enum Color { Red = 1 }]@0");

            edits.VerifySemantics(ActiveStatementsDescription.Empty, NoSemanticEdits);
        }

        #endregion

        #region Delegates

        [Fact]
        public void Delegate_NoModifiers_Insert()
        {
            var src1 = "";
            var src2 = "delegate void D();";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void Delegate_NoModifiers_IntoNamespace_Insert()
        {
            var src1 = "namespace N { }";
            var src2 = "namespace N { delegate void D(); }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void Delegate_NoModifiers_IntoType_Insert()
        {
            var src1 = "class C { }";
            var src2 = "class C { delegate void D(); }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void Delegate_Public_IntoType_Insert()
        {
            var src1 = "class C { }";
            var src2 = "class C { public delegate void D(); }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [public delegate void D();]@10",
                "Insert [()]@32");

            edits.VerifySemanticDiagnostics(
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void Delegate_Generic_Insert()
        {
            var src1 = "class C { }";
            var src2 = "class C { private delegate void D<T>(T a); }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [private delegate void D<T>(T a);]@10",
                "Insert [<T>]@33",
                "Insert [(T a)]@36",
                "Insert [T]@34",
                "Insert [T a]@37");

            edits.VerifySemanticDiagnostics(
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void Delegate_Delete()
        {
            var src1 = "class C { private delegate void D(); }";
            var src2 = "class C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Delete [private delegate void D();]@10",
                "Delete [()]@33");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Delete, "class C", DeletedSymbolDisplay(FeaturesResources.delegate_, "D")));
        }

        [Fact]
        public void Delegate_Rename()
        {
            var src1 = "public delegate void D();";
            var src2 = "public delegate void Z();";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [public delegate void D();]@0 -> [public delegate void Z();]@0");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "public delegate void Z()", FeaturesResources.delegate_));
        }

        [Fact]
        public void Delegate_Accessibility_Update()
        {
            var src1 = "public delegate void D();";
            var src2 = "private delegate void D();";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [public delegate void D();]@0 -> [private delegate void D();]@0");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingAccessibility, "private delegate void D()", FeaturesResources.delegate_));
        }

        [Fact]
        public void Delegate_ReturnType_Update()
        {
            var src1 = "public delegate int D();";
            var src2 = "public delegate void D();";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [public delegate int D();]@0 -> [public delegate void D();]@0");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.TypeUpdate, "public delegate void D()", FeaturesResources.delegate_));
        }

        [Fact]
        public void Delegate_ReturnType_AddAttribute()
        {
            var attribute = "public class A : System.Attribute { }\n\n";

            var src1 = attribute + "public delegate int D(int a);";
            var src2 = attribute + "[return: A]public delegate int D(int a);";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [public delegate int D(int a);]@39 -> [[return: A]public delegate int D(int a);]@39");

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember("D.Invoke")),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember("D.BeginInvoke"))
                },
                capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
        }

        [Fact]
        public void Delegate_Parameter_Insert()
        {
            var src1 = "public delegate int D();";
            var src2 = "public delegate int D(int a);";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [int a]@22");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Insert, "int a", FeaturesResources.parameter));
        }

        [Fact]
        public void Delegate_Parameter_Insert_Reloadable()
        {
            var src1 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]public delegate int D();";
            var src2 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]internal delegate bool D(int a);";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                 new[] { SemanticEdit(SemanticEditKind.Replace, c => c.GetMember("D")) },
                 capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void Delegate_Parameter_Delete()
        {
            var src1 = "public delegate int D(int a);";
            var src2 = "public delegate int D();";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Delete [int a]@22");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Delete, "public delegate int D()", DeletedSymbolDisplay(FeaturesResources.parameter, "int a")));
        }

        [Fact]
        public void Delegate_Parameter_Rename()
        {
            var src1 = "public delegate int D(int a);";
            var src2 = "public delegate int D(int b);";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [int a]@22 -> [int b]@22");

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.RenamingNotSupportedByRuntime, "int b", FeaturesResources.parameter) },
                capabilities: EditAndContinueCapabilities.Baseline);

            edits.VerifySemantics(
                new[]
                {
                     SemanticEdit(SemanticEditKind.Update, c => c.GetMember("D.Invoke")),
                     SemanticEdit(SemanticEditKind.Update, c => c.GetMember("D.BeginInvoke"))
                },
                capabilities: EditAndContinueCapabilities.UpdateParameters);
        }

        [Fact]
        public void Delegate_Parameter_Update()
        {
            var src1 = "public delegate int D(int a);";
            var src2 = "public delegate int D(byte a);";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [int a]@22 -> [byte a]@22");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.TypeUpdate, "byte a", FeaturesResources.parameter));
        }

        [Fact]
        public void Delegate_Parameter_AddAttribute_NotSupportedByRuntime()
        {
            var attribute = "public class AAttribute : System.Attribute { }\n\n";

            var src1 = attribute + "public delegate int D(int a);";
            var src2 = attribute + "public delegate int D([A]int a);";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [int a]@70 -> [[A]int a]@70");

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "int a", FeaturesResources.parameter) },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void Delegate_Parameter_AddAttribute()
        {
            var attribute = "public class A : System.Attribute { }\n\n";

            var src1 = attribute + "public delegate int D(int a);";
            var src2 = attribute + "public delegate int D([A]int a);";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [int a]@61 -> [[A]int a]@61");

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember("D.Invoke")),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember("D.BeginInvoke"))
                },
                capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
        }

        [Fact]
        public void Delegate_TypeParameter_Insert()
        {
            var src1 = "public delegate int D();";
            var src2 = "public delegate int D<T>();";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [<T>]@21",
                "Insert [T]@22");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Insert, "T", FeaturesResources.type_parameter));
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/54881")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/54881")]
        public void Delegate_TypeParameter_Insert_Reloadable()
        {
            var src1 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]public delegate int D<out T>();";
            var src2 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]internal delegate bool D<in T, out S>(int a);";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                 SemanticEdit(SemanticEditKind.Replace, c => c.GetMember("D")));
        }

        [Fact]
        public void Delegate_TypeParameter_Delete()
        {
            var src1 = "public delegate int D<T>();";
            var src2 = "public delegate int D();";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Delete [<T>]@21",
                "Delete [T]@22");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Delete, "public delegate int D()", DeletedSymbolDisplay(FeaturesResources.type_parameter, "T")));
        }

        [Fact]
        public void Delegate_TypeParameter_Rename()
        {
            var src1 = "public delegate int D<T>();";
            var src2 = "public delegate int D<S>();";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [T]@22 -> [S]@22");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "S", FeaturesResources.type_parameter),
                Diagnostic(RudeEditKind.GenericTypeUpdate, "S"));
        }

        [Fact]
        public void Delegate_TypeParameter_Variance1()
        {
            var src1 = "public delegate int D<T>();";
            var src2 = "public delegate int D<in T>();";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [T]@22 -> [in T]@22");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.VarianceUpdate, "T", FeaturesResources.type_parameter),
                Diagnostic(RudeEditKind.GenericTypeUpdate, "T"));
        }

        [Fact]
        public void Delegate_TypeParameter_Variance2()
        {
            var src1 = "public delegate int D<out T>();";
            var src2 = "public delegate int D<T>();";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [out T]@22 -> [T]@22");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.VarianceUpdate, "T", FeaturesResources.type_parameter),
                Diagnostic(RudeEditKind.GenericTypeUpdate, "T"));
        }

        [Fact]
        public void Delegate_TypeParameter_Variance3()
        {
            var src1 = "public delegate int D<out T>();";
            var src2 = "public delegate int D<in T>();";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [out T]@22 -> [in T]@22");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.VarianceUpdate, "T", FeaturesResources.type_parameter),
                Diagnostic(RudeEditKind.GenericTypeUpdate, "T"));
        }

        [Fact]
        public void Delegate_TypeParameter_AddAttribute()
        {
            var attribute = "public class AAttribute : System.Attribute { }\n\n";

            var src1 = attribute + "public delegate int D<T>();";
            var src2 = attribute + "public delegate int D<[A]T>();";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [T]@70 -> [[A]T]@70");

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.GenericTypeUpdate, "T") },
                capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
        }

        [Fact]
        public void Delegate_Attribute_Add_NotSupportedByRuntime()
        {
            var attribute = "public class AAttribute : System.Attribute { }\n\n";

            var src1 = attribute + "public delegate int D(int a);";
            var src2 = attribute + "[A]public delegate int D(int a);";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [public delegate int D(int a);]@48 -> [[A]public delegate int D(int a);]@48");

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "public delegate int D(int a)", FeaturesResources.delegate_) },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void Delegate_Attribute_Add()
        {
            var attribute = "public class AAttribute : System.Attribute { }\n\n";

            var src1 = attribute + "public delegate int D(int a);";
            var src2 = attribute + "[A]public delegate int D(int a);";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [public delegate int D(int a);]@48 -> [[A]public delegate int D(int a);]@48");

            edits.VerifySemantics(
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember("D")) },
                capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
        }

        [Fact]
        public void Delegate_Attribute_Add_WithReturnTypeAttribute()
        {
            var attribute = "public class AAttribute : System.Attribute { }\n\n";

            var src1 = attribute + "public delegate int D(int a);";
            var src2 = attribute + "[return: A][A]public delegate int D(int a);";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [public delegate int D(int a);]@48 -> [[return: A][A]public delegate int D(int a);]@48");

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember("D")),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember("D.Invoke")),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember("D.BeginInvoke"))
                },
                capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
        }

        [Fact]
        public void Delegate_ReadOnlyRef_Parameter_InsertWhole()
        {
            var src1 = "";
            var src2 = "public delegate int D(in int b);";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [public delegate int D(in int b);]@0",
                "Insert [(in int b)]@21",
                "Insert [in int b]@22");

            edits.VerifySemanticDiagnostics(
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void Delegate_ReadOnlyRef_Parameter_InsertParameter()
        {
            var src1 = "public delegate int D();";
            var src2 = "public delegate int D(in int b);";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [in int b]@22");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Insert, "in int b", FeaturesResources.parameter));
        }

        [Fact]
        public void Delegate_ReadOnlyRef_Parameter_Update()
        {
            var src1 = "public delegate int D(int b);";
            var src2 = "public delegate int D(in int b);";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [int b]@22 -> [in int b]@22");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "in int b", FeaturesResources.parameter));
        }

        [Fact]
        public void Delegate_ReadOnlyRef_ReturnType_Insert()
        {
            var src1 = "";
            var src2 = "public delegate ref readonly int D();";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [public delegate ref readonly int D();]@0",
                "Insert [()]@34");

            edits.VerifySemanticDiagnostics(
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void Delegate_ReadOnlyRef_ReturnType_Update()
        {
            var src1 = "public delegate int D();";
            var src2 = "public delegate ref readonly int D();";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [public delegate int D();]@0 -> [public delegate ref readonly int D();]@0");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.TypeUpdate, "public delegate ref readonly int D()", FeaturesResources.delegate_));
        }

        #endregion

        #region Nested Types

        [Fact]
        public void NestedType_Move_Sideways()
        {
            var src1 = @"class N { class C {} } class M {            }";
            var src2 = @"class N {            } class M { class C {} }";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyEdits(
                "Move [class C {}]@10 -> @33");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Move, "class C", GetResource("class")));
        }

        [Fact]
        public void NestedType_Move_Outside()
        {
            var src1 = @"class C { class D { } }";
            var src2 = @"class C { } class D { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Move [class D { }]@10 -> @12");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Move, "class D", FeaturesResources.class_));
        }

        [Fact]
        public void NestedType_Move_Insert()
        {
            var src1 = @"class C { class D { } }";
            var src2 = @"class C { class E { class D { } } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [class E { class D { } }]@10",
                "Move [class D { }]@10 -> @20");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Move, "class D", FeaturesResources.class_));
        }

        [Fact]
        public void NestedType_MoveAndNamespaceChange()
        {
            var src1 = @"namespace N { class C { class D { } } } namespace M { }";
            var src2 = @"namespace N { class C { } } namespace M { class D { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Move [class D { }]@24 -> @42");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Move, "class D", FeaturesResources.class_));
        }

        [Fact]
        public void NestedType_Move_MultiFile()
        {
            var srcA1 = @"partial class N { class C {} } partial class M {            }";
            var srcB1 = @"partial class N {            } partial class M { class C {} }";
            var srcA2 = @"partial class N {            } partial class M { class C {} }";
            var srcB2 = @"partial class N { class C {} } partial class M {            }";

            var editsA = GetTopEdits(srcA1, srcA2);
            editsA.VerifyEdits(
                "Move [class C {}]@18 -> @49");

            var editsB = GetTopEdits(srcB1, srcB2);
            editsB.VerifyEdits(
                "Move [class C {}]@49 -> @18");

            EditAndContinueValidation.VerifySemantics(
                new[] { editsA, editsB },
                new[]
                {
                    DocumentResults(),
                    DocumentResults(),
                });
        }

        [Fact]
        public void NestedType_Move_PartialTypesInSameFile()
        {
            var src1 = @"partial class N { class C {} class D {} } partial class N { }";
            var src2 = @"partial class N { class C {}            } partial class N { class D {} }";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyEdits(
                "Move [class D {}]@29 -> @60");

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void NestedType_Move_Reloadable()
        {
            var src1 = ReloadableAttributeSrc + "class N { [CreateNewOnMetadataUpdate]class C {} } class M { }";
            var src2 = ReloadableAttributeSrc + "class N { } class M { [CreateNewOnMetadataUpdate]class C {} }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Move, "class C", GetResource("class")));
        }

        [Fact]
        public void NestedType_Insert1()
        {
            var src1 = @"class C {  }";
            var src2 = @"class C { class D { class E { } } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [class D { class E { } }]@10",
                "Insert [class E { }]@20");

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void NestedType_Insert2()
        {
            var src1 = @"class C {  }";
            var src2 = @"class C { protected class D { public class E { } } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [protected class D { public class E { } }]@10",
                "Insert [public class E { }]@30");

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void NestedType_Insert3()
        {
            var src1 = @"class C {  }";
            var src2 = @"class C { private class D { public class E { } } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [private class D { public class E { } }]@10",
                "Insert [public class E { }]@28");

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void NestedType_Insert4()
        {
            var src1 = @"class C {  }";
            var src2 = @"class C { private class D { public D(int a, int b) { } public int P { get; set; } } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [private class D { public D(int a, int b) { } public int P { get; set; } }]@10",
                "Insert [public D(int a, int b) { }]@28",
                "Insert [public int P { get; set; }]@55",
                "Insert [(int a, int b)]@36",
                "Insert [{ get; set; }]@68",
                "Insert [int a]@37",
                "Insert [int b]@44",
                "Insert [get;]@70",
                "Insert [set;]@75");

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void NestedType_Insert_ReloadableIntoReloadable1()
        {
            var src1 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]class C { }";
            var src2 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]class C { [CreateNewOnMetadataUpdate]class D { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                 new[] { SemanticEdit(SemanticEditKind.Replace, c => c.GetMember("C")) },
                 capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void NestedType_Insert_ReloadableIntoReloadable2()
        {
            var src1 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]class C { }";
            var src2 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]class C { [CreateNewOnMetadataUpdate]class D { [CreateNewOnMetadataUpdate]class E { } } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                 new[] { SemanticEdit(SemanticEditKind.Replace, c => c.GetMember("C")) },
                 capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void NestedType_Insert_ReloadableIntoReloadable3()
        {
            var src1 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]class C { }";
            var src2 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]class C { class D { [CreateNewOnMetadataUpdate]class E { } } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                 new[] { SemanticEdit(SemanticEditKind.Replace, c => c.GetMember("C")) },
                 capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void NestedType_Insert_ReloadableIntoReloadable4()
        {
            var src1 = ReloadableAttributeSrc + "class C { }";
            var src2 = ReloadableAttributeSrc + "class C { [CreateNewOnMetadataUpdate]class D { [CreateNewOnMetadataUpdate]class E { } } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                 SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.D")));
        }

        [Fact]
        public void NestedType_Insert_Member_Reloadable()
        {
            var src1 = ReloadableAttributeSrc + "class C { [CreateNewOnMetadataUpdate]class D { } }";
            var src2 = ReloadableAttributeSrc + "class C { [CreateNewOnMetadataUpdate]class D { int x; } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                new[] { SemanticEdit(SemanticEditKind.Replace, c => c.GetMember("C.D")) },
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void NestedType_InsertMemberWithInitializer1()
        {
            var src1 = @"
class C
{
}";
            var src2 = @"
class C
{
    private class D
    {
        public int P = 1;
    }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(ActiveStatementsDescription.Empty, new[]
            {
                SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.D"), preserveLocalVariables: false)
            });
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/835827")]
        public void NestedType_Insert_PInvoke()
        {
            var src1 = @"
using System;
using System.Runtime.InteropServices;

class C
{
}";
            var src2 = @"
using System;
using System.Runtime.InteropServices;

class C
{
    abstract class D 
    {
        public extern D();

        public static extern int P { [DllImport(""msvcrt.dll"")]get; [DllImport(""msvcrt.dll"")]set; }

        [DllImport(""msvcrt.dll"")]
        public static extern int puts(string c);

        [DllImport(""msvcrt.dll"")]
        public static extern int operator +(D d, D g);

        [DllImport(""msvcrt.dll"")]
        public static extern explicit operator int (D d);
    }
}
";
            var edits = GetTopEdits(src1, src2);

            // Adding P/Invoke is not supported by the CLR.
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.InsertExtern, "public extern D()", FeaturesResources.constructor),
                Diagnostic(RudeEditKind.InsertExtern, "public static extern int P", FeaturesResources.property_),
                Diagnostic(RudeEditKind.InsertExtern, "public static extern int puts(string c)", FeaturesResources.method),
                Diagnostic(RudeEditKind.InsertExtern, "public static extern int operator +(D d, D g)", FeaturesResources.operator_),
                Diagnostic(RudeEditKind.InsertExtern, "public static extern explicit operator int (D d)", CSharpFeaturesResources.conversion_operator));
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/835827")]
        public void NestedType_Insert_VirtualAbstract()
        {
            var src1 = @"
using System;
using System.Runtime.InteropServices;

class C
{
}";
            var src2 = @"
using System;
using System.Runtime.InteropServices;

class C
{
    abstract class D 
    {
        public abstract int P { get; }
        public abstract int this[int i] { get; }
        public abstract int puts(string c);

        public virtual event Action E { add { } remove { } }
        public virtual int Q { get { return 1; } }
        public virtual int this[string i] { get { return 1; } }
        public virtual int M(string c) { return 1; }
    }
}
";
            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void NestedType_TypeReorder1()
        {
            var src1 = @"class C { struct E { } class F { } delegate void D(); interface I {} }";
            var src2 = @"class C { class F { } interface I {} delegate void D(); struct E { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Reorder [struct E { }]@10 -> @56",
                "Reorder [interface I {}]@54 -> @22");

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void NestedType_MethodDeleteInsert()
        {
            var src1 = @"public class C { public void goo() {} }";
            var src2 = @"public class C { private class D { public void goo() {} } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [private class D { public void goo() {} }]@17",
                "Insert [public void goo() {}]@35",
                "Insert [()]@50",
                "Delete [public void goo() {}]@17",
                "Delete [()]@32");

            edits.VerifySemantics(
                new[] {
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.D")),
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.goo"), deletedSymbolContainerProvider: c => c.GetMember("C"))
                },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void NestedType_ClassDeleteInsert()
        {
            var src1 = @"public class C { public class X {} }";
            var src2 = @"public class C { public class D { public class X {} } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [public class D { public class X {} }]@17",
                "Move [public class X {}]@17 -> @34");

            edits.VerifySemanticDiagnostics(
                 Diagnostic(RudeEditKind.Move, "public class X", FeaturesResources.class_));
        }

        /// <summary>
        /// A new generic type can be added whether it's nested and inherits generic parameters from the containing type, or top-level.
        /// </summary>
        [Fact]
        public void NestedClassGeneric_Insert()
        {
            var src1 = @"
using System;
class C<T>
{
}
";
            var src2 = @"
using System;
class C<T>
{
    class D {}
    struct S {}
    enum N {}
    interface I {}
    delegate void D();
}

class D<T>
{
    
}
";
            var edits = GetTopEdits(src1, src2);
            edits.VerifySemanticDiagnostics(
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void NestedEnum_InsertMember()
        {
            var src1 = "struct S { enum N { A = 1 } }";
            var src2 = "struct S { enum N { A = 1, B = 2 } }";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyEdits(
                "Update [enum N { A = 1 }]@11 -> [enum N { A = 1, B = 2 }]@11",
                "Insert [B = 2]@27");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Insert, "B = 2", FeaturesResources.enum_value));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/50876")]
        public void NestedEnumInPartialType_InsertDelete()
        {
            var srcA1 = "partial struct S { }";
            var srcB1 = "partial struct S { enum N { A = 1 } }";
            var srcA2 = "partial struct S { enum N { A = 1 } }";
            var srcB2 = "partial struct S { }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(),
                    DocumentResults()
                });
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/50876")]
        public void NestedEnumInPartialType_InsertDeleteAndUpdateMember()
        {
            var srcA1 = "partial struct S { }";
            var srcB1 = "partial struct S { enum N { A = 1 } }";
            var srcA2 = "partial struct S { enum N { A = 2 } }";
            var srcB2 = "partial struct S { }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(
                        diagnostics: new[]
                        {
                            Diagnostic(RudeEditKind.InitializerUpdate, "A = 2", FeaturesResources.enum_value),
                        }),

                    DocumentResults()
                });
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/50876")]
        public void NestedEnumInPartialType_InsertDeleteAndUpdateBase()
        {
            var srcA1 = "partial struct S { }";
            var srcB1 = "partial struct S { enum N : uint { A = 1 } }";
            var srcA2 = "partial struct S { enum N : int { A = 1 } }";
            var srcB2 = "partial struct S { }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(
                        diagnostics: new[]
                        {
                            Diagnostic(RudeEditKind.EnumUnderlyingTypeUpdate, "enum N", FeaturesResources.enum_),
                        }),

                    DocumentResults()
                });
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/50876")]
        public void NestedEnumInPartialType_InsertDeleteAndInsertMember()
        {
            var srcA1 = "partial struct S { }";
            var srcB1 = "partial struct S { enum N { A = 1 } }";
            var srcA2 = "partial struct S { enum N { A = 1, B = 2 } }";
            var srcB2 = "partial struct S { }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(
                        diagnostics: new[] { Diagnostic(RudeEditKind.Insert, "B = 2", FeaturesResources.enum_value) }),

                    DocumentResults()
                });
        }

        [Fact]
        public void NestedDelegateInPartialType_InsertDelete()
        {
            var srcA1 = "partial struct S { }";
            var srcB1 = "partial struct S { delegate void D(); }";
            var srcA2 = "partial struct S { delegate void D(); }";
            var srcB2 = "partial struct S { }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(
                        // delegate does not have any user-defined method body and this does not need a PDB update
                        semanticEdits: NoSemanticEdits),

                    DocumentResults()
                });
        }

        [Fact]
        public void NestedDelegateInPartialType_InsertDeleteAndChangeParameters()
        {
            var srcA1 = "partial struct S { }";
            var srcB1 = "partial struct S { delegate void D(); }";
            var srcA2 = "partial struct S { delegate void D(int x); }";
            var srcB2 = "partial struct S { }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(
                        diagnostics: new[]
                        {
                            Diagnostic(RudeEditKind.ChangingParameterTypes, "delegate void D(int x)", FeaturesResources.delegate_)
                        }),

                    DocumentResults()
                });
        }

        [Fact]
        public void NestedDelegateInPartialType_InsertDeleteAndChangeReturnType()
        {
            var srcA1 = "partial struct S { }";
            var srcB1 = "partial struct S { delegate ref int D(); }";
            var srcA2 = "partial struct S { delegate ref readonly int D(); }";
            var srcB2 = "partial struct S { }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(
                        diagnostics: new[]
                        {
                            Diagnostic(RudeEditKind.TypeUpdate, "delegate ref readonly int D()", FeaturesResources.delegate_)
                        }),

                    DocumentResults()
                });
        }

        [Fact]
        public void NestedDelegateInPartialType_InsertDeleteAndChangeOptionalParameterValue()
        {
            var srcA1 = "partial struct S { }";
            var srcB1 = "partial struct S { delegate void D(int x = 1); }";
            var srcA2 = "partial struct S { delegate void D(int x = 2); }";
            var srcB2 = "partial struct S { }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(
                        diagnostics: new[]
                        {
                            Diagnostic(RudeEditKind.InitializerUpdate, "int x = 2", FeaturesResources.parameter)
                        }),

                    DocumentResults()
                });
        }

        [Fact]
        public void NestedPartialTypeInPartialType_InsertDeleteAndChange()
        {
            var srcA1 = "partial struct S { partial class C { void F1() {} } }";
            var srcB1 = "partial struct S { partial class C { void F2(byte x) {} } }";
            var srcC1 = "partial struct S { }";

            var srcA2 = "partial struct S { partial class C { void F1() {} } }";
            var srcB2 = "partial struct S { }";
            var srcC2 = "partial struct S { partial class C { void F2(int x) {} } }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2), GetTopEdits(srcC1, srcC2) },
                new[]
                {
                    DocumentResults(),

                    DocumentResults(
                        semanticEdits: new[]
                        {
                            SemanticEdit(SemanticEditKind.Delete, c =>  c.GetMembers("S.C.F2").FirstOrDefault(m => m.GetParameterTypes().Any(t => t.SpecialType == SpecialType.System_Byte))?.ISymbol, deletedSymbolContainerProvider: c => c.GetMember("S.C"))
                        }),

                    DocumentResults(
                        semanticEdits: new[] { SemanticEdit(SemanticEditKind.Insert, c => c.GetMembers("S.C.F2").FirstOrDefault(m => m.GetParameterTypes().Any(t => t.SpecialType == SpecialType.System_Int32))?.ISymbol) })
                },
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
        }

        [Fact]
        public void Type_Partial_AddMultiple()
        {
            var srcA1 = "";
            var srcB1 = "";

            var srcA2 = "partial class C { }";
            var srcB2 = "partial class C { }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(semanticEdits: new[]
                    {
                        SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C"), partialType: "C")
                    }),
                    DocumentResults(semanticEdits: new[]
                    {
                        SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C"), partialType: "C")
                    }),
                },
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void Type_Partial_InsertDeleteAndChange_Attribute()
        {
            var srcA1 = "partial class C { }";
            var srcB1 = "";
            var srcC1 = "partial class C { }";

            var srcA2 = "";
            var srcB2 = "[A]partial class C { }";
            var srcC2 = "partial class C { }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2), GetTopEdits(srcC1, srcC2) },
                new[]
                {
                    DocumentResults(),
                    DocumentResults(semanticEdits: new[]
                    {
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C"), partialType: "C")
                    }),
                    DocumentResults(),
                },
                capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
        }

        [Fact]
        public void Type_Partial_InsertDeleteAndChange_TypeParameterAttribute_NotSupportedByRuntime()
        {
            var srcA1 = "partial class C<T> { }";
            var srcB1 = "";
            var srcC1 = "partial class C<T> { }";

            var srcA2 = "";
            var srcB2 = "partial class C<[A]T> { }";
            var srcC2 = "partial class C<T> { }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2), GetTopEdits(srcC1, srcC2) },
                new[]
                {
                    DocumentResults(),

                    DocumentResults(
                        diagnostics: new[]
                        {
                            Diagnostic(RudeEditKind.GenericTypeUpdate, "partial class C<[A]T>"),
                            Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "T", FeaturesResources.type_parameter),
                            Diagnostic(RudeEditKind.GenericTypeUpdate, "T")
                        }),

                    DocumentResults(),
                },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void Type_Partial_InsertDeleteAndChange_Constraint()
        {
            var srcA1 = "partial class C<T> { }";
            var srcB1 = "";
            var srcC1 = "partial class C<T> { }";

            var srcA2 = "";
            var srcB2 = "partial class C<T> where T : new() { }";
            var srcC2 = "partial class C<T> { }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2), GetTopEdits(srcC1, srcC2) },
                new[]
                {
                    DocumentResults(),

                    DocumentResults(
                        diagnostics: new[]
                        {
                            Diagnostic(RudeEditKind.GenericTypeUpdate, "partial class C<T>"),
                            Diagnostic(RudeEditKind.ChangingConstraints, "where T : new()", FeaturesResources.type_parameter),
                            Diagnostic(RudeEditKind.GenericTypeUpdate, "where T : new()")
                        }),

                    DocumentResults(),
                });
        }

        [Fact]
        public void Type_Partial_InsertDeleteRefactor()
        {
            var srcA1 = "partial class C : I { void F() { } }";
            var srcB1 = "[A][B]partial class C : J { void G() { } }";
            var srcC1 = "";
            var srcD1 = "";

            var srcA2 = "";
            var srcB2 = "";
            var srcC2 = "[A]partial class C : I, J { void F() { } }";
            var srcD2 = "[B]partial class C { void G() { } }";

            var srcE = "interface I {} interface J {}";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2), GetTopEdits(srcC1, srcC2), GetTopEdits(srcD1, srcD2), GetTopEdits(srcE, srcE) },
                new[]
                {
                    DocumentResults(),
                    DocumentResults(),

                    DocumentResults(
                        semanticEdits: new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember("F")) }),

                    DocumentResults(
                        semanticEdits: new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember("G")) }),

                    DocumentResults(),
                });
        }

        [Fact]
        public void Type_Partial_Attribute_AddMultiple()
        {
            var attributes = @"
class A : System.Attribute {}
class B : System.Attribute {}
";

            var srcA1 = "partial class C { }" + attributes;
            var srcB1 = "partial class C { }";

            var srcA2 = "[A]partial class C { }" + attributes;
            var srcB2 = "[B]partial class C { }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(semanticEdits: new[]
                    {
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C"), partialType: "C")
                    }),
                    DocumentResults(semanticEdits: new[]
                    {
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C"), partialType: "C")
                    }),
                },
                capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
        }

        [Fact]
        public void Type_Partial_InsertDeleteRefactor_AttributeListSplitting()
        {
            var srcA1 = "partial class C { void F() { } }";
            var srcB1 = "[A,B]partial class C { void G() { } }";
            var srcC1 = "";
            var srcD1 = "";

            var srcA2 = "";
            var srcB2 = "";
            var srcC2 = "[A]partial class C { void F() { } }";
            var srcD2 = "[B]partial class C { void G() { } }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2), GetTopEdits(srcC1, srcC2), GetTopEdits(srcD1, srcD2) },
                new[]
                {
                    DocumentResults(),
                    DocumentResults(),
                    DocumentResults(semanticEdits: new[]
                    {
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.F"))
                    }),
                    DocumentResults(semanticEdits: new[]
                    {
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.G"))
                    }),
                });
        }

        [Fact]
        public void Type_Partial_InsertDeleteChangeMember()
        {
            var srcA1 = "partial class C { void F(int y = 1) { } }";
            var srcB1 = "partial class C { void G(int x = 1) { } }";
            var srcC1 = "";

            var srcA2 = "";
            var srcB2 = "partial class C { void G(int x = 2) { } }";
            var srcC2 = "partial class C { void F(int y = 2) { } }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2), GetTopEdits(srcC1, srcC2) },
                new[]
                {
                    DocumentResults(),
                    DocumentResults(diagnostics: new[] { Diagnostic(RudeEditKind.InitializerUpdate, "int x = 2", FeaturesResources.parameter) }),
                    DocumentResults(diagnostics: new[] { Diagnostic(RudeEditKind.InitializerUpdate, "int y = 2", FeaturesResources.parameter) }),
                });
        }

        [Fact]
        public void NestedPartialTypeInPartialType_InsertDeleteAndInsertVirtual()
        {
            var srcA1 = "partial interface I { partial class C { virtual void F1() {} } }";
            var srcB1 = "partial interface I { partial class C { virtual void F2() {} } }";
            var srcC1 = "partial interface I { partial class C { } }";
            var srcD1 = "partial interface I { partial class C { } }";
            var srcE1 = "partial interface I { }";
            var srcF1 = "partial interface I { }";

            var srcA2 = "partial interface I { partial class C { } }";
            var srcB2 = "";
            var srcC2 = "partial interface I { partial class C { virtual void F1() {} } }"; // move existing virtual into existing partial decl
            var srcD2 = "partial interface I { partial class C { virtual void N1() {} } }"; // insert new virtual into existing partial decl
            var srcE2 = "partial interface I { partial class C { virtual void F2() {} } }"; // move existing virtual into a new partial decl
            var srcF2 = "partial interface I { partial class C { virtual void N2() {} } }"; // insert new virtual into new partial decl

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2), GetTopEdits(srcC1, srcC2), GetTopEdits(srcD1, srcD2), GetTopEdits(srcE1, srcE2), GetTopEdits(srcF1, srcF2) },
                new[]
                {
                    // A
                    DocumentResults(),

                    // B
                    DocumentResults(),

                    // C
                    DocumentResults(
                        semanticEdits: new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("I").GetMember<INamedTypeSymbol>("C").GetMember("F1")) }),

                    // D
                    DocumentResults(
                        diagnostics: new[] { Diagnostic(RudeEditKind.InsertVirtual, "virtual void N1()", FeaturesResources.method) }),

                    // E
                    DocumentResults(
                        semanticEdits: new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("I").GetMember<INamedTypeSymbol>("C").GetMember("F2")) }),

                    // F
                    DocumentResults(
                        diagnostics: new[] { Diagnostic(RudeEditKind.InsertVirtual, "virtual void N2()", FeaturesResources.method) }),
                });
        }

        #endregion

        #region Namespaces

        [Fact]
        public void Namespace_Empty_Insert()
        {
            var src1 = @"";
            var src2 = @"namespace C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [namespace C { }]@0");

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void Namespace_Empty_InsertNested()
        {
            var src1 = @"namespace C { }";
            var src2 = @"namespace C { namespace D { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [namespace D { }]@14");

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void Namespace_Empty_DeleteNested()
        {
            var src1 = @"namespace C { namespace D { } }";
            var src2 = @"namespace C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Delete [namespace D { }]@14");

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void Namespace_Empty_Move()
        {
            var src1 = @"namespace C { namespace D { } }";
            var src2 = @"namespace C { } namespace D { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Move [namespace D { }]@14 -> @16");

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void Namespace_Empty_Reorder1()
        {
            var src1 = @"namespace C { namespace D { } class T { } namespace E { } }";
            var src2 = @"namespace C { namespace E { } class T { } namespace D { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Reorder [class T { }]@30 -> @30",
                "Reorder [namespace E { }]@42 -> @14");

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void Namespace_Empty_Reorder2()
        {
            var src1 = @"namespace C { namespace D1 { } namespace D2 { } namespace D3 { } class T { } namespace E { } }";
            var src2 = @"namespace C { namespace E { }                                    class T { } namespace D1 { } namespace D2 { } namespace D3 { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Reorder [class T { }]@65 -> @65",
                "Reorder [namespace E { }]@77 -> @14");

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void Namespace_Empty_FileScoped_Insert()
        {
            var src1 = @"";
            var src2 = @"namespace N;";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [namespace N;]@0");

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void Namespace_Empty_FileScoped_Delete()
        {
            var src1 = @"namespace N;";
            var src2 = @"";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Delete [namespace N;]@0");

            edits.VerifySemanticDiagnostics();
        }

        [Theory]
        [InlineData("namespace N; class C {}")]
        [InlineData("namespace N { class C {} }")]
        public void Namespace_Insert_NewType(string src2)
        {
            var src1 = @"";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.InsertNotSupportedByRuntime, "class C", GetResource("class")) },
                capabilities: EditAndContinueCapabilities.Baseline);

            edits.VerifySemantics(
                new[] { SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("N.C")) },
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Theory]
        [InlineData("namespace N.M { class C {} }")]
        [InlineData("namespace N.M; class C {}")]
        public void Namespace_Insert_NewType_Qualified(string src2)
        {
            var src1 = "";
            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.InsertNotSupportedByRuntime, "class C", GetResource("class")) },
                capabilities: EditAndContinueCapabilities.Baseline);

            edits.VerifySemantics(
                new[] { SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("N.M.C")) },
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Theory]
        [InlineData("class")]
        [InlineData("interface")]
        [InlineData("enum")]
        [InlineData("struct")]
        [InlineData("record")]
        [InlineData("record struct")]
        public void Namespace_Insert(string keyword)
        {
            var declaration = keyword + " X {}";
            var src1 = declaration;
            var src2 = "namespace N { " + declaration + " }";

            var edits = GetTopEdits(src1, src2);
            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingNamespace, keyword + " X", GetResource(keyword), "<global namespace>", "N") },
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void Namespace_Insert_Delegate()
        {
            var declaration = "delegate void X();";
            var src1 = declaration;
            var src2 = "namespace N { " + declaration + " }";

            var edits = GetTopEdits(src1, src2);
            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingNamespace, "delegate void X()", GetResource("delegate"), "<global namespace>", "N") },
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void Namespace_Insert_MultipleDeclarations()
        {
            var src1 = @"class C {} class D {}";
            var src2 = "namespace N { class C {} class D { } }";

            var edits = GetTopEdits(src1, src2);
            edits.VerifySemanticDiagnostics(
                new[]
                {
                    Diagnostic(RudeEditKind.ChangingNamespace, "class C", GetResource("class"), "<global namespace>", "N"),
                    Diagnostic(RudeEditKind.ChangingNamespace, "class D", GetResource("class"), "<global namespace>", "N")
                },
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void Namespace_Insert_FileScoped()
        {
            var src1 = @"class C {}";
            var src2 = @"namespace N; class C {}";

            var edits = GetTopEdits(src1, src2);
            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingNamespace, "class C", GetResource("class"), "<global namespace>", "N") },
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void Namespace_Insert_Nested()
        {
            var src1 = @"namespace N { class C {} }";
            var src2 = @"namespace N { namespace M { class C {} } }";

            var edits = GetTopEdits(src1, src2);
            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingNamespace, "class C", GetResource("class"), "N", "N.M") },
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void Namespace_Insert_Qualified()
        {
            var src1 = @"class C {}";
            var src2 = @"namespace N.M { class C {} }";

            var edits = GetTopEdits(src1, src2);
            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingNamespace, "class C", GetResource("class"), "<global namespace>", "N.M") },
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void Namespace_Insert_Qualified_FileScoped()
        {
            var src1 = @"class C {}";
            var src2 = @"namespace N.M; class C {}";

            var edits = GetTopEdits(src1, src2);
            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingNamespace, "class C", GetResource("class"), "<global namespace>", "N.M") },
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Theory]
        [InlineData("class")]
        [InlineData("interface")]
        [InlineData("enum")]
        [InlineData("struct")]
        [InlineData("record")]
        [InlineData("record struct")]
        public void Namespace_Delete(string keyword)
        {
            var declaration = keyword + " X {}";
            var src1 = "namespace N { " + declaration + " }";
            var src2 = declaration;

            var edits = GetTopEdits(src1, src2);
            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingNamespace, keyword + " X", GetResource(keyword), "N", "<global namespace>") },
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void Namespace_Delete_Delegate()
        {
            var declaration = "delegate void X();";
            var src1 = "namespace N { " + declaration + " }";
            var src2 = declaration;

            var edits = GetTopEdits(src1, src2);
            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingNamespace, "delegate void X()", GetResource("delegate"), "N", "<global namespace>") },
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void Namespace_Delete_MultipleDeclarations()
        {
            var src1 = @"namespace N { class C {} class D { } }";
            var src2 = @"class C {} class D {}";

            var edits = GetTopEdits(src1, src2);
            edits.VerifySemanticDiagnostics(
                new[]
                {
                    Diagnostic(RudeEditKind.ChangingNamespace, "class C", GetResource("class"), "N", "<global namespace>"),
                    Diagnostic(RudeEditKind.ChangingNamespace, "class D", GetResource("class"), "N", "<global namespace>")
                },
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Theory]
        [InlineData("namespace N.M { class C {} }")]
        [InlineData("namespace N.M; class C {}")]
        public void Namespace_Delete_Qualified(string src1)
        {
            var src2 = @"class C {}";

            var edits = GetTopEdits(src1, src2);
            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingNamespace, "class C", GetResource("class"), "N.M", "<global namespace>") },
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void Namespace_Qualified_ToFileScoped()
        {
            var src1 = @"namespace N.M { class C {} }";
            var src2 = @"namespace N.M; class C {}";

            var edits = GetTopEdits(src1, src2);
            edits.VerifySemanticDiagnostics();
        }

        [Theory]
        [InlineData("class")]
        [InlineData("interface")]
        [InlineData("enum")]
        [InlineData("struct")]
        [InlineData("record")]
        [InlineData("record struct")]
        public void Namespace_Update(string keyword)
        {
            var declaration = keyword + " X {}";
            var src1 = "namespace N { " + declaration + " }";
            var src2 = "namespace M { " + declaration + " }";

            var edits = GetTopEdits(src1, src2);
            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingNamespace, keyword + " X", GetResource(keyword), "N", "M") },
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void Namespace_Update_Delegate()
        {
            var declaration = "delegate void X();";
            var src1 = "namespace N { " + declaration + " }";
            var src2 = "namespace M { " + declaration + " }";

            var edits = GetTopEdits(src1, src2);
            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingNamespace, "delegate void X()", GetResource("delegate"), "N", "M") },
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void Namespace_Update_Multiple()
        {
            var src1 = @"namespace N { class C {} class D {} }";
            var src2 = @"namespace M { class C {} class D {} }";

            var edits = GetTopEdits(src1, src2);
            edits.VerifySemanticDiagnostics(
                new[]
                {
                    Diagnostic(RudeEditKind.ChangingNamespace, "class C", GetResource("class"), "N", "M"),
                    Diagnostic(RudeEditKind.ChangingNamespace, "class D", GetResource("class"), "N", "M"),
                },
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void Namespace_Update_Qualified1()
        {
            var src1 = @"namespace N.M { class C {} }";
            var src2 = @"namespace N.M.O { class C {} }";

            var edits = GetTopEdits(src1, src2);
            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingNamespace, "class C", GetResource("class"), "N.M", "N.M.O") },
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void Namespace_Update_Qualified2()
        {
            var src1 = @"namespace N.M { class C {} }";
            var src2 = @"namespace N { class C {} }";

            var edits = GetTopEdits(src1, src2);
            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingNamespace, "class C", GetResource("class"), "N.M", "N") },
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void Namespace_Update_Qualified3()
        {
            var src1 = @"namespace N.M1.O { class C {} }";
            var src2 = @"namespace N.M2.O { class C {} }";

            var edits = GetTopEdits(src1, src2);
            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingNamespace, "class C", GetResource("class"), "N.M1.O", "N.M2.O") },
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void Namespace_Update_FileScoped()
        {
            var src1 = @"namespace N; class C {}";
            var src2 = @"namespace M; class C {}";

            var edits = GetTopEdits(src1, src2);
            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingNamespace, "class C", GetResource("class"), "N", "M") },
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void Namespace_Update_MultiplePartials1()
        {
            var srcA1 = @"namespace N { partial class/*1*/C {} } namespace N { partial class/*2*/C {} }";
            var srcB1 = @"namespace N { partial class/*3*/C {} } namespace N { partial class/*4*/C {} }";
            var srcA2 = @"namespace N { partial class/*1*/C {} } namespace M { partial class/*2*/C {} }";
            var srcB2 = @"namespace M { partial class/*3*/C {} } namespace N { partial class/*4*/C {} }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(
                        semanticEdits: new[]
                        {
                            SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<INamedTypeSymbol>("M.C"), partialType: "M.C"),
                        }),
                    DocumentResults(
                        semanticEdits: new[]
                        {
                            SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<INamedTypeSymbol>("M.C"), partialType: "M.C"),
                        }),
                },
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void Namespace_Update_MultiplePartials2()
        {
            var srcA1 = @"namespace N { partial class/*1*/C {} } namespace N { partial class/*2*/C {} }";
            var srcB1 = @"namespace N { partial class/*3*/C {} } namespace N { partial class/*4*/C {} }";
            var srcA2 = @"namespace M { partial class/*1*/C {} } namespace M { partial class/*2*/C {} }";
            var srcB2 = @"namespace M { partial class/*3*/C {} } namespace M { partial class/*4*/C {} }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(diagnostics: new[]
                    {
                        Diagnostic(RudeEditKind.ChangingNamespace, "partial class/*1*/C", GetResource("class"), "N", "M")
                    }),
                    DocumentResults(diagnostics: new[]
                    {
                        Diagnostic(RudeEditKind.ChangingNamespace, "partial class/*3*/C", GetResource("class"), "N", "M")
                    }),
                },
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void Namespace_Update_MultiplePartials_MergeInNewNamspace()
        {
            var src1 = @"namespace N { partial class C {} } namespace M { partial class C {} }";
            var src2 = @"namespace X { partial class C {} } namespace X { partial class C {} }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingNamespace, "partial class C", GetResource("class"), "M", "X"),
                Diagnostic(RudeEditKind.Delete, "partial class C", DeletedSymbolDisplay(GetResource("class"), "C")));
        }

        [Fact]
        public void Namespace_Update_MultipleTypesWithSameNameAndArity()
        {
            var src1 = @"namespace N1 { class C {} } namespace N2 { class C {} } namespace O { class C {} }";
            var src2 = @"namespace M1 { class C {} } namespace M2 { class C {} } namespace O { class C {} }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingNamespace, "class C", GetResource("class"), "N2", "M2"),
                Diagnostic(RudeEditKind.ChangingNamespace, "class C", GetResource("class"), "N1", "M1"));
        }

        [Fact]
        public void Namespace_UpdateAndInsert()
        {
            var src1 = @"namespace N.M { class C {} }";
            var src2 = @"namespace N { namespace M { class C {} } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void Namespace_UpdateAndDelete()
        {
            var src1 = @"namespace N { namespace M { class C {} } }";
            var src2 = @"namespace N.M { class C {} }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void Namespace_Move1()
        {
            var src1 = @"namespace N { namespace M { class C {} class C<T> {} } class D {} }";
            var src2 = @"namespace N { class D {} } namespace M { class C<T> {} class C {} }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Move [namespace M { class C {} class C<T> {} }]@14 -> @27",
                "Reorder [class C<T> {}]@39 -> @41");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingNamespace, "class C", GetResource("class"), "N.M", "M"),
                Diagnostic(RudeEditKind.ChangingNamespace, "class C<T>", GetResource("class"), "N.M", "M"));
        }

        [Fact]
        public void Namespace_Move2()
        {
            var src1 = @"namespace N1 { namespace M { class C {} } namespace N2 { } }";
            var src2 = @"namespace N1 { } namespace N2 { namespace M { class C {} } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Move [namespace N2 { }]@42 -> @17",
                "Move [namespace M { class C {} }]@15 -> @32");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingNamespace, "class C", "class", "N1.M", "N2.M"));
        }

        #endregion

        #region Members

        [Fact]
        public void PartialMember_DeleteInsert_SingleDocument()
        {
            var src1 = @"
using System;

partial class C
{
    void M() {}
    int P1 { get; set; }
    int P2 { get => 1; set {} }
    int this[int i] { get => 1; set {} }
    int this[byte i] { get => 1; set {} }
    event Action E { add {} remove {} }
    event Action EF;
    int F1;
    int F2;
}

partial class C
{
}
";
            var src2 = @"
using System;

partial class C
{
}

partial class C
{
    void M() {}
    int P1 { get; set; }
    int P2 { get => 1; set {} }
    int this[int i] { get => 1; set {} }
    int this[byte i] { get => 1; set {} }
    event Action E { add {} remove {} }
    event Action EF;
    int F1, F2;
}
";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [void M() {}]@68",
                "Insert [int P1 { get; set; }]@85",
                "Insert [int P2 { get => 1; set {} }]@111",
                "Insert [int this[int i] { get => 1; set {} }]@144",
                "Insert [int this[byte i] { get => 1; set {} }]@186",
                "Insert [event Action E { add {} remove {} }]@229",
                "Insert [event Action EF;]@270",
                "Insert [int F1, F2;]@292",
                "Insert [()]@74",
                "Insert [{ get; set; }]@92",
                "Insert [{ get => 1; set {} }]@118",
                "Insert [[int i]]@152",
                "Insert [{ get => 1; set {} }]@160",
                "Insert [[byte i]]@194",
                "Insert [{ get => 1; set {} }]@203",
                "Insert [{ add {} remove {} }]@244",
                "Insert [Action EF]@276",
                "Insert [int F1, F2]@292",
                "Insert [get;]@94",
                "Insert [set;]@99",
                "Insert [get => 1;]@120",
                "Insert [set {}]@130",
                "Insert [int i]@153",
                "Insert [get => 1;]@162",
                "Insert [set {}]@172",
                "Insert [byte i]@195",
                "Insert [get => 1;]@205",
                "Insert [set {}]@215",
                "Insert [add {}]@246",
                "Insert [remove {}]@253",
                "Insert [EF]@283",
                "Insert [F1]@296",
                "Insert [F2]@300",
                "Delete [void M() {}]@43",
                "Delete [()]@49",
                "Delete [int P1 { get; set; }]@60",
                "Delete [{ get; set; }]@67",
                "Delete [get;]@69",
                "Delete [set;]@74",
                "Delete [int P2 { get => 1; set {} }]@86",
                "Delete [{ get => 1; set {} }]@93",
                "Delete [get => 1;]@95",
                "Delete [set {}]@105",
                "Delete [int this[int i] { get => 1; set {} }]@119",
                "Delete [[int i]]@127",
                "Delete [int i]@128",
                "Delete [{ get => 1; set {} }]@135",
                "Delete [get => 1;]@137",
                "Delete [set {}]@147",
                "Delete [int this[byte i] { get => 1; set {} }]@161",
                "Delete [[byte i]]@169",
                "Delete [byte i]@170",
                "Delete [{ get => 1; set {} }]@178",
                "Delete [get => 1;]@180",
                "Delete [set {}]@190",
                "Delete [event Action E { add {} remove {} }]@204",
                "Delete [{ add {} remove {} }]@219",
                "Delete [add {}]@221",
                "Delete [remove {}]@228",
                "Delete [event Action EF;]@245",
                "Delete [Action EF]@251",
                "Delete [EF]@258",
                "Delete [int F1;]@267",
                "Delete [int F1]@267",
                "Delete [F1]@271",
                "Delete [int F2;]@280",
                "Delete [int F2]@280",
                "Delete [F2]@284");

            EditAndContinueValidation.VerifySemantics(
                new[] { edits },
                new[]
                {
                    DocumentResults(
                        semanticEdits: new[]
                        {
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IMethodSymbol>("M"), preserveLocalVariables: false),
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IPropertySymbol>("P1").GetMethod, preserveLocalVariables: false),
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IPropertySymbol>("P1").SetMethod, preserveLocalVariables: false),
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IPropertySymbol>("P2").GetMethod, preserveLocalVariables: false),
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IPropertySymbol>("P2").SetMethod, preserveLocalVariables: false),
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMembers("this[]").Cast<IPropertySymbol>().Single(m => m.GetParameters().Single().Type.Name == "Int32").GetMethod, preserveLocalVariables: false),
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMembers("this[]").Cast<IPropertySymbol>().Single(m => m.GetParameters().Single().Type.Name == "Int32").SetMethod, preserveLocalVariables: false),
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMembers("this[]").Cast<IPropertySymbol>().Single(m => m.GetParameters().Single().Type.Name == "Byte").GetMethod, preserveLocalVariables: false),
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMembers("this[]").Cast<IPropertySymbol>().Single(m => m.GetParameters().Single().Type.Name == "Byte").SetMethod, preserveLocalVariables: false),
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IEventSymbol>("E").AddMethod, preserveLocalVariables: false),
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IEventSymbol>("E").RemoveMethod, preserveLocalVariables: false),
                        })
                });
        }

        [Fact]
        public void PartialMember_InsertDelete_MultipleDocuments()
        {
            var srcA1 = "partial class C { }";
            var srcB1 = "partial class C { void F() {} }";
            var srcA2 = "partial class C { void F() {} }";
            var srcB2 = "partial class C { }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(
                        semanticEdits: new[]
                        {
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember("F"), preserveLocalVariables: false)
                        }),

                    DocumentResults()
                });
        }

        [Fact]
        public void PartialMember_DeleteInsert_MultipleDocuments()
        {
            var srcA1 = "partial class C { void F() {} }";
            var srcB1 = "partial class C { }";
            var srcA2 = "partial class C { }";
            var srcB2 = "partial class C { void F() {} }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(),

                    DocumentResults(
                        semanticEdits: new[]
                        {
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IMethodSymbol>("F"), preserveLocalVariables: false)
                        })
                });
        }

        [Fact]
        public void PartialMember_DeleteInsert_GenericMethod()
        {
            var srcA1 = "partial class C { void F<T>() {} }";
            var srcB1 = "partial class C { }";
            var srcA2 = "partial class C { }";
            var srcB2 = "partial class C { void F<T>() {} }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(),
                    DocumentResults(diagnostics: new[]
                    {
                        Diagnostic(RudeEditKind.GenericMethodUpdate, "void F<T>()"),
                        Diagnostic(RudeEditKind.GenericMethodUpdate, "T")
                    })
                });
        }

        [Fact]
        public void PartialMember_DeleteInsert_GenericType()
        {
            var srcA1 = "partial class C<T> { void F() {} }";
            var srcB1 = "partial class C<T> { }";
            var srcA2 = "partial class C<T> { }";
            var srcB2 = "partial class C<T> { void F() {} }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(),
                    DocumentResults(diagnostics: new[]
                    {
                        Diagnostic(RudeEditKind.GenericTypeUpdate, "void F()")
                    })
                });
        }

        [Fact]
        public void PartialMember_DeleteInsert_Destructor()
        {
            var srcA1 = "partial class C { ~C() {} }";
            var srcB1 = "partial class C { }";
            var srcA2 = "partial class C { }";
            var srcB2 = "partial class C { ~C() {} }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(),

                    DocumentResults(
                        semanticEdits: new[]
                        {
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember("Finalize"), preserveLocalVariables: false),
                        })
                });
        }

        [Fact]
        public void PartialNestedType_InsertDeleteAndChange()
        {
            var srcA1 = "partial class C { }";
            var srcB1 = "partial class C { class D { void M() {} } interface I { } }";

            var srcA2 = "partial class C { class D : I { void M() {} } interface I { } }";
            var srcB2 = "partial class C { }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(
                        diagnostics: new[]
                        {
                            Diagnostic(RudeEditKind.BaseTypeOrInterfaceUpdate, "class D", FeaturesResources.class_),
                        }),

                    DocumentResults()
                });
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/51011")]
        public void PartialMember_RenameInsertDelete()
        {
            var srcA1 = "partial class C { void F1() {} }";
            var srcB1 = "partial class C { void F2() {} }";
            var srcA2 = "partial class C { void F2() {} }";
            var srcB2 = "partial class C { void F1() {} }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(semanticEdits: new[]
                        {
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember("F2")),
                        }),

                    DocumentResults(
                        semanticEdits: new[]
                        {
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember("F1")),
                        })
                },
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
        }

        [Fact]
        public void PartialMember_DeleteInsert_UpdateMethodBodyError()
        {
            var srcA1 = @"
using System.Collections.Generic;

partial class C
{
    IEnumerable<int> F() { yield return 1; }
}
";
            var srcB1 = @"
using System.Collections.Generic;

partial class C
{
}
";

            var srcA2 = @"
using System.Collections.Generic;

partial class C
{
}
";
            var srcB2 = @"
using System.Collections.Generic;

partial class C
{
    IEnumerable<int> F() { yield return 1; yield return 2; }
}
";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(),
                    DocumentResults(semanticEdits: new[]
                    {
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.F"), preserveLocalVariables: true)
                    })
                });
        }

        [Fact]
        public void PartialMember_DeleteInsert_UpdatePropertyAccessors()
        {
            var srcA1 = "partial class C { int P { get => 1; set { Console.WriteLine(1); } } }";
            var srcB1 = "partial class C { }";

            var srcA2 = "partial class C { }";
            var srcB2 = "partial class C { int P { get => 2; set { Console.WriteLine(2); } } }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(),
                    DocumentResults(semanticEdits: new[]
                    {
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IPropertySymbol>("P").GetMethod),
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IPropertySymbol>("P").SetMethod)
                    })
                });
        }

        [Fact]
        public void PartialMember_DeleteInsert_UpdateAutoProperty()
        {
            var srcA1 = "partial class C { int P => 1; }";
            var srcB1 = "partial class C { }";

            var srcA2 = "partial class C { }";
            var srcB2 = "partial class C { int P => 2; }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(),
                    DocumentResults(semanticEdits: new[]
                    {
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IPropertySymbol>("P").GetMethod)
                    })
                });
        }

        [Fact]
        public void PartialMember_DeleteInsert_AddFieldInitializer()
        {
            var srcA1 = "partial class C { int f; }";
            var srcB1 = "partial class C { }";

            var srcA2 = "partial class C { }";
            var srcB2 = "partial class C { int f = 1; }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(),
                    DocumentResults(semanticEdits: new[]
                    {
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), partialType: "C", preserveLocalVariables: true)
                    })
                });
        }

        [Fact]
        public void PartialMember_DeleteInsert_RemoveFieldInitializer()
        {
            var srcA1 = "partial class C { int f = 1; }";
            var srcB1 = "partial class C { }";

            var srcA2 = "partial class C { }";
            var srcB2 = "partial class C { int f; }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(),
                    DocumentResults(semanticEdits: new[]
                    {
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), partialType: "C", preserveLocalVariables: true)
                    })
                });
        }

        [Fact]
        public void PartialMember_DeleteInsert_ConstructorWithInitializers()
        {
            var srcA1 = "partial class C { int f = 1; C(int x) { f = x; } }";
            var srcB1 = "partial class C { }";

            var srcA2 = "partial class C { int f = 1; }";
            var srcB2 = "partial class C { C(int x) { f = x + 1; } }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(),
                    DocumentResults(semanticEdits: new[]
                    {
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), partialType: "C", preserveLocalVariables: true)
                    })
                });
        }

        [Fact]
        public void PartialMember_DeleteInsert_MethodAddParameter()
        {
            var srcA1 = "partial struct S { }";
            var srcB1 = "partial struct S { void F() {} }";
            var srcA2 = "partial struct S { void F(int x) {} }";
            var srcB2 = "partial struct S { }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(
                        semanticEdits: new[]
                        {
                            SemanticEdit(SemanticEditKind.Insert, c => c.GetMembers("S.F").FirstOrDefault(m => m.GetParameterCount() == 1)?.ISymbol)
                        }),

                    DocumentResults(
                        semanticEdits: new[]
                        {
                            SemanticEdit(SemanticEditKind.Delete, c => c.GetMembers("S.F").FirstOrDefault(m => m.GetParameterCount() == 0)?.ISymbol, deletedSymbolContainerProvider: c => c.GetMember("S"))
                        }),
                },
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
        }

        [Fact]
        public void PartialMember_DeleteInsert_UpdateMethodParameterType()
        {
            var srcA1 = "partial struct S { }";
            var srcB1 = "partial struct S { void F(int x); }";
            var srcA2 = "partial struct S { void F(byte x); }";
            var srcB2 = "partial struct S { }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(
                        semanticEdits: new[]
                        {
                            SemanticEdit(SemanticEditKind.Insert, c => c.GetMembers("S.F").FirstOrDefault(m => m.GetParameterTypes().Any(t => t.SpecialType == SpecialType.System_Byte))?.ISymbol)
                        }),

                    DocumentResults(
                        semanticEdits: new[]
                        {
                            SemanticEdit(SemanticEditKind.Delete, c => c.GetMembers("S.F").FirstOrDefault(m => m.GetParameterTypes().Any(t => t.SpecialType == SpecialType.System_Int32))?.ISymbol, deletedSymbolContainerProvider: c => c.GetMember("S"))
                        }),
                },
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
        }

        [Fact]
        public void PartialMember_DeleteInsert_MethodAddTypeParameter()
        {
            var srcA1 = "partial struct S { }";
            var srcB1 = "partial struct S { void F(); }";
            var srcA2 = "partial struct S { void F<T>(); }";
            var srcB2 = "partial struct S { }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(
                        diagnostics: new[]
                        {
                            Diagnostic(RudeEditKind.InsertGenericMethod, "void F<T>()", FeaturesResources.method)
                        }),

                    DocumentResults(
                        semanticEdits: new[]
                        {
                            SemanticEdit(SemanticEditKind.Delete, c => c.GetMembers("S.F").FirstOrDefault(m => m.GetMemberTypeParameters().Length == 0)?.ISymbol, deletedSymbolContainerProvider: c => c.GetMember("S"))
                        }),
                });
        }

        #endregion

        #region Methods

        [Theory]
        [InlineData("static")]
        [InlineData("virtual")]
        [InlineData("abstract")]
        [InlineData("override")]
        [InlineData("sealed override", "override")]
        public void Method_Modifiers_Update(string oldModifiers, string newModifiers = "")
        {
            if (oldModifiers != "")
            {
                oldModifiers += " ";
            }

            if (newModifiers != "")
            {
                newModifiers += " ";
            }

            var src1 = "class C { " + oldModifiers + "int F() => 0; }";
            var src2 = "class C { " + newModifiers + "int F() => 0; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [" + oldModifiers + "int F() => 0;]@10 -> [" + newModifiers + "int F() => 0;]@10");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, newModifiers + "int F()", FeaturesResources.method));
        }

        [Fact]
        public void Method_NewModifier_Add()
        {
            var src1 = "class C { int F() => 0; }";
            var src2 = "class C { new int F() => 0; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [int F() => 0;]@10 -> [new int F() => 0;]@10");

            // Currently, an edit is produced eventhough there is no metadata/IL change. Consider improving.
            edits.VerifySemantics(
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IMethodSymbol>("F")));
        }

        [Fact]
        public void Method_NewModifier_Remove()
        {
            var src1 = "class C { new int F() => 0; }";
            var src2 = "class C { int F() => 0; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [new int F() => 0;]@10 -> [int F() => 0;]@10");

            // Currently, an edit is produced eventhough there is no metadata/IL change. Consider improving.
            edits.VerifySemantics(
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IMethodSymbol>("F")));
        }

        [Fact]
        public void Method_ReadOnlyModifier_Add_InMutableStruct()
        {
            var src1 = @"
struct S
{
    public int M() => 1;
}";
            var src2 = @"
struct S
{
    public readonly int M() => 1;
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "public readonly int M()", FeaturesResources.method));
        }

        [Fact]
        public void Method_ReadOnlyModifier_Add_InReadOnlyStruct1()
        {
            var src1 = @"
readonly struct S
{
    public int M()
        => 1;
}";
            var src2 = @"
readonly struct S
{
    public readonly int M()
        => 1;
}";

            var edits = GetTopEdits(src1, src2);

            // Currently, an edit is produced eventhough the body nor IsReadOnly attribute have changed. Consider improving.
            edits.VerifySemantics(
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("S").GetMember<IMethodSymbol>("M")));
        }

        [Fact]
        public void Method_ReadOnlyModifier_Add_InReadOnlyStruct2()
        {
            var src1 = @"
readonly struct S
{
    public int M() => 1;
}";
            var src2 = @"
struct S
{
    public readonly int M() => 1;
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "struct S", "struct"));
        }

        [Fact]
        public void Method_AsyncModifier_Remove()
        {
            var src1 = @"
class Test
{
    public async Task<int> WaitAsync()
    {
        return 1;
    }
}";
            var src2 = @"
class Test
{
    public Task<int> WaitAsync()
    {
        return Task.FromResult(1);
    }
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingFromAsynchronousToSynchronous, "public Task<int> WaitAsync()", FeaturesResources.method));
        }

        [Fact]
        public void Method_AsyncModifier_Add()
        {
            var src1 = @"
class Test
{
    public Task<int> WaitAsync()
    {
        return 1;
    }
}";
            var src2 = @"
class Test
{
    public async Task<int> WaitAsync()
    {
        await Task.Delay(1000);
        return 1;
    }
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifySemanticDiagnostics(
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);

            VerifyPreserveLocalVariables(edits, preserveLocalVariables: false);
        }

        [Fact]
        public void Method_AsyncModifier_Add_NotSupported()
        {
            var src1 = @"
class Test
{
    public Task<int> WaitAsync()
    {
        return 1;
    }
}";
            var src2 = @"
class Test
{
    public async Task<int> WaitAsync()
    {
        await Task.Delay(1000);
        return 1;
    }
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.MakeMethodAsyncNotSupportedByRuntime, "public async Task<int> WaitAsync()") },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Theory]
        [InlineData("string", "string?")]
        [InlineData("object", "dynamic")]
        [InlineData("(int a, int b)", "(int a, int c)")]
        public void Method_ReturnType_Update_RuntimeTypeUnchanged(string oldType, string newType)
        {
            var src1 = "class C { " + oldType + " M() => default; }";
            var src2 = "class C { " + newType + " M() => default; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.M")));
        }

        [Theory]
        [InlineData("int", "string")]
        [InlineData("int", "int?")]
        [InlineData("(int a, int b)", "(int a, double b)")]
        public void Method_ReturnType_Update_RuntimeTypeChanged(string oldType, string newType)
        {
            var src1 = "class C { " + oldType + " M() => default; }";
            var src2 = "class C { " + newType + " M() => default; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.M"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.M"))
                },
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingTypeNotSupportedByRuntime, newType + " M()", FeaturesResources.method) },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void Method_ReturnType_Update_AndBodyChange()
        {
            var src1 = "class C { int M() => 1; }";
            var src2 = "class C { char M() => 'a'; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.M"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.M"))
                },
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingTypeNotSupportedByRuntime, "char M()", FeaturesResources.method) },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void Method_Update()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        int a = 1;
        int b = 2;
        System.Console.WriteLine(a + b);
    }
}
";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        int b = 2;
        int a = 1;
        System.Console.WriteLine(a + b);
    }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                @"Update [static void Main(string[] args)
    {
        int a = 1;
        int b = 2;
        System.Console.WriteLine(a + b);
    }]@18 -> [static void Main(string[] args)
    {
        int b = 2;
        int a = 1;
        System.Console.WriteLine(a + b);
    }]@18");

            edits.VerifySemanticDiagnostics();

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.Main"), preserveLocalVariables: false) });
        }

        [Fact]
        public void MethodWithExpressionBody_Update()
        {
            var src1 = @"
class C
{
    static int Main(string[] args) => F(1);
    static int F(int a) => 1;
}
";
            var src2 = @"
class C
{
    static int Main(string[] args) => F(2);
    static int F(int a) => 1;
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                @"Update [static int Main(string[] args) => F(1);]@18 -> [static int Main(string[] args) => F(2);]@18");

            edits.VerifySemanticDiagnostics();

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.Main"), preserveLocalVariables: false) });
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/51297")]
        public void MethodWithExpressionBody_Update_LiftedParameter()
        {
            var src1 = @"
using System;

class C
{
    int M(int a) => new Func<int>(() => a + 1)();
}
";
            var src2 = @"
using System;

class C
{
    int M(int a) => new Func<int>(() => 2)();   // not capturing a anymore
}";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [int M(int a) => new Func<int>(() => a + 1)();]@35 -> [int M(int a) => new Func<int>(() => 2)();]@35");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.NotCapturingVariable, "a", "a"));
        }

        [Fact]
        public void MethodWithExpressionBody_ToBlockBody()
        {
            var src1 = "class C { static int F(int a) => 1; }";
            var src2 = "class C { static int F(int a) { return 2; } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [static int F(int a) => 1;]@10 -> [static int F(int a) { return 2; }]@10");

            edits.VerifySemantics(ActiveStatementsDescription.Empty, new[]
            {
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.F"), preserveLocalVariables: false)
            });
        }

        [Fact]
        public void MethodWithBlockBody_ToExpressionBody()
        {
            var src1 = "class C { static int F(int a) { return 2; } }";
            var src2 = "class C { static int F(int a) => 1; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [static int F(int a) { return 2; }]@10 -> [static int F(int a) => 1;]@10");

            edits.VerifySemantics(ActiveStatementsDescription.Empty, new[]
            {
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.F"), preserveLocalVariables: false)
            });
        }

        [Fact]
        public void MethodWithLambda_Update()
        {
            var src1 = @"
using System;

class C
{
    static void F()
    {
        Func<int> a = () => { <N:0.0>return 1;</N:0.0> };
        Func<Func<int>> b = () => () => { <N:0.1>return 1;</N:0.1> };
    }
}
";
            var src2 = @"
using System;

class C
{
    static void F()
    {
        Func<int> a = () => { <N:0.0>return 1;</N:0.0> };
        Func<Func<int>> b = () => () => { <N:0.1>return 1;</N:0.1> };

        Console.WriteLine(1);
    }
}";
            var edits = GetTopEdits(src1, src2);
            var syntaxMap = GetSyntaxMap(src1, src2);

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.F"), syntaxMap[0]) });
        }

        [Fact]
        public void MethodUpdate_LocalVariableDeclaration()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        int x = 1;
        Console.WriteLine(x);
    }
}
";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        int x = 2;
        Console.WriteLine(x);
    }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
@"Update [static void Main(string[] args)
    {
        int x = 1;
        Console.WriteLine(x);
    }]@18 -> [static void Main(string[] args)
    {
        int x = 2;
        Console.WriteLine(x);
    }]@18");
        }

        [Fact]
        public void Method_Delete()
        {
            var src1 = @"
class C
{
    void goo() { }
}
";
            var src2 = @"
class C
{
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Delete [void goo() { }]@18",
                "Delete [()]@26");

            edits.VerifySemantics(
                new[] { SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.goo"), deletedSymbolContainerProvider: c => c.GetMember("C")) },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Theory]
        [InlineData("virtual")]
        [InlineData("abstract")]
        [InlineData("override")]
        public void Method_Delete_Modifiers(string modifier)
        {
            /* TODO: https://github.com/dotnet/roslyn/issues/59264

               This should be a supported edit. Consider the following inheritance chain:

                public class C { public virtual void M() => Console.WriteLine("C"); } 
                public class D : C { public override void M() { base.M(); Console.WriteLine("D"); } } 
                public class E : D { public override void M() { base.M(); Console.WriteLine("E"); } } 

                If D.M is deleted we expect E.M to print "C E" and not throw.

            */
            var src1 = $$"""
                class C
                {
                    {{modifier}} void goo() { }
                }
                """;
            var src2 = """
                class C
                {
                }
                """;
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                $"Delete [{modifier} void goo() {{ }}]@16",
                $"Delete [()]@{25 + modifier.Length}");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Delete, "class C", DeletedSymbolDisplay(FeaturesResources.method, "goo()")));
        }

        [Fact]
        public void MethodWithExpressionBody_Delete()
        {
            var src1 = @"
class C
{
    int goo() => 1;
}
";
            var src2 = @"
class C
{
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Delete [int goo() => 1;]@18",
                "Delete [()]@25");

            edits.VerifySemantics(
                new[] { SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.goo"), deletedSymbolContainerProvider: c => c.GetMember("C")) },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/754853")]
        public void MethodDelete_WithParameterAndAttribute()
        {
            var src1 = @"
class C
{
    [Obsolete]
    void goo(int a) { }
}
";
            var src2 = @"
class C
{
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                @"Delete [[Obsolete]
    void goo(int a) { }]@18",
                "Delete [(int a)]@42",
                "Delete [int a]@43");

            edits.VerifySemantics(
                new[] { SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.goo"), deletedSymbolContainerProvider: c => c.GetMember("C")) },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/754853")]
        public void MethodDelete_PInvoke()
        {
            var src1 = @"
using System;
using System.Runtime.InteropServices;

class C
{
    [DllImport(""msvcrt.dll"")]
    public static extern int puts(string c);
}
";
            var src2 = @"
using System;
using System.Runtime.InteropServices;

class C
{
}";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                @"Delete [[DllImport(""msvcrt.dll"")]
    public static extern int puts(string c);]@74",
                 "Delete [(string c)]@134",
                 "Delete [string c]@135");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Delete, "class C", DeletedSymbolDisplay(FeaturesResources.method, "puts(string c)")));
        }

        [Fact]
        public void MethodInsert_NotSupportedByRuntime()
        {
            var src1 = "class C {  }";
            var src2 = "class C { void goo() { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.InsertNotSupportedByRuntime, "void goo()", FeaturesResources.method) },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void PrivateMethodInsert()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        Console.ReadLine();
    }
}";
            var src2 = @"
class C
{
    void goo() { }

    static void Main(string[] args)
    {
        Console.ReadLine();
    }
}
";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [void goo() { }]@18",
                "Insert [()]@26");

            edits.VerifySemanticDiagnostics(
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/755784")]
        public void PrivateMethodInsert_WithParameters()
        {
            var src1 = @"
using System;

class C
{
    static void Main(string[] args)
    {
        Console.ReadLine();
    }
}";
            var src2 = @"
using System;

class C
{
    void goo(int a) { }

    static void Main(string[] args)
    {
        Console.ReadLine();
    }
}
";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [void goo(int a) { }]@35",
                "Insert [(int a)]@43",
                "Insert [int a]@44");

            edits.VerifySemantics(
                new[] { SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.goo")) },
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/755784")]
        public void PrivateMethodInsert_WithAttribute()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        Console.ReadLine();
    }
}";
            var src2 = @"
class C
{
    [System.Obsolete]
    void goo(int a) { }

    static void Main(string[] args)
    {
        Console.ReadLine();
    }
}
";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                @"Insert [[System.Obsolete]
    void goo(int a) { }]@18",
                "Insert [(int a)]@49",
                "Insert [int a]@50");

            edits.VerifySemanticDiagnostics(
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
        }

        [Fact]
        public void MethodInsert_Virtual()
        {
            var src1 = @"
class C
{
}";
            var src2 = @"
class C
{
    public virtual void F() {}
}
";
            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.InsertVirtual, "public virtual void F()", FeaturesResources.method));
        }

        [Fact]
        public void MethodInsert_Abstract()
        {
            var src1 = @"
abstract class C
{
}";
            var src2 = @"
abstract class C
{
    public abstract void F();
}
";
            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.InsertVirtual, "public abstract void F()", FeaturesResources.method));
        }

        [Fact]
        public void MethodInsert_Override()
        {
            var src1 = @"
class C
{
}";
            var src2 = @"
class C
{
    public override void F() { }
}
";
            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.InsertVirtual, "public override void F()", FeaturesResources.method));
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/755784"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/835827")]
        public void ExternMethodInsert()
        {
            var src1 = @"
using System;
using System.Runtime.InteropServices;

class C
{
}";
            var src2 = @"
using System;
using System.Runtime.InteropServices;

class C
{
    [DllImport(""msvcrt.dll"")]
    private static extern int puts(string c);
}
";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                @"Insert [[DllImport(""msvcrt.dll"")]
    private static extern int puts(string c);]@74",
                "Insert [(string c)]@135",
                "Insert [string c]@136");

            // CLR doesn't support methods without a body
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.InsertExtern, "private static extern int puts(string c)", FeaturesResources.method));
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/755784"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/835827")]
        public void ExternMethodDeleteInsert()
        {
            var srcA1 = @"
using System;
using System.Runtime.InteropServices;

class C
{
    [DllImport(""msvcrt.dll"")]
    private static extern int puts(string c);
}";
            var srcA2 = @"
using System;
using System.Runtime.InteropServices;
";

            var srcB1 = @"
using System;
using System.Runtime.InteropServices;
";
            var srcB2 = @"
using System;
using System.Runtime.InteropServices;

class C
{
    [DllImport(""msvcrt.dll"")]
    private static extern int puts(string c);
}
";
            // TODO: The method does not need to be updated since there are no sequence points generated for it.
            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(),
                    DocumentResults(semanticEdits: new[]
                    {
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.puts")),
                    })
                });
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/755784"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/835827")]
        public void ExternMethod_Attribute_DeleteInsert()
        {
            var srcA1 = @"
using System;
using System.Runtime.InteropServices;

class C
{
    [DllImport(""msvcrt.dll"")]
    private static extern int puts(string c);
}";
            var srcA2 = @"
using System;
using System.Runtime.InteropServices;
";

            var srcB1 = @"
using System;
using System.Runtime.InteropServices;
";
            var srcB2 = @"
using System;
using System.Runtime.InteropServices;

class C
{
    [DllImport(""msvcrt.dll"")]
    [Obsolete]
    private static extern int puts(string c);
}
";
            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(),
                    DocumentResults(semanticEdits: new[]
                    {
                        SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.puts")),
                    })
                },
                capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
        }

        [Fact]
        public void MethodReorder1()
        {
            var src1 = "class C { void f(int a, int b) { a = b; } void g() { } }";
            var src2 = "class C { void g() { } void f(int a, int b) { a = b; } }";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyEdits("Reorder [void g() { }]@42 -> @10");
        }

        [Fact]
        public void MethodInsertDelete1()
        {
            var src1 = "class C { class D { } void f(int a, int b) { a = b; } }";
            var src2 = "class C { class D { void f(int a, int b) { a = b; } } }";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyEdits(
                "Insert [void f(int a, int b) { a = b; }]@20",
                "Insert [(int a, int b)]@26",
                "Insert [int a]@27",
                "Insert [int b]@34",
                "Delete [void f(int a, int b) { a = b; }]@22",
                "Delete [(int a, int b)]@28",
                "Delete [int a]@29",
                "Delete [int b]@36");
        }

        [Fact]
        public void MethodUpdate_AddParameter()
        {
            var src1 = @"
class C
{
    static void Main()
    {
        
    }
}";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        
    }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [string[] args]@35");

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMembers("C.Main").FirstOrDefault(m => m.GetParameterCount() == 0)?.ISymbol, deletedSymbolContainerProvider: c => c.GetMember("C")),
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMembers("C.Main").FirstOrDefault(m => m.GetParameterCount() == 1)?.ISymbol)
                },
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.InsertNotSupportedByRuntime, "string[] args", FeaturesResources.parameter) },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void MethodUpdate_AddParameters()
        {
            var src1 = @"
class C
{
    void M(int a)
    {
    }
}";
            var src2 = @"
class C
{
    void M(int a, int b, int c)
    {
    }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMembers("C.M").FirstOrDefault(m => m.GetParameterCount() == 1)?.ISymbol, deletedSymbolContainerProvider: c => c.GetMember("C")),
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMembers("C.M").FirstOrDefault(m => m.GetParameterCount() == 3)?.ISymbol)
                },
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);

            edits.VerifySemanticDiagnostics(
                new[]
                {
                    Diagnostic(RudeEditKind.InsertNotSupportedByRuntime, "int b", FeaturesResources.parameter),
                    Diagnostic(RudeEditKind.InsertNotSupportedByRuntime, "int c", FeaturesResources.parameter)
                },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void MethodUpdate_AddParameter_Partial()
        {
            var src1 = @"
class C
{
    partial void M(int a);

    partial void M(int a)
    {
    }
}";
            var src2 = @"
class C
{
    partial void M(int a, int b, int c);

    partial void M(int a, int b, int c)
    {
    }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMembers("C.M").FirstOrDefault(m => m.GetParameterCount() == 1)?.ISymbol, deletedSymbolContainerProvider: c => c.GetMember("C")),
                    SemanticEdit(SemanticEditKind.Insert, c => ((IMethodSymbol)c.GetMembers("C.M").FirstOrDefault(m => m.GetParameterCount() == 3)?.ISymbol)?.PartialImplementationPart)
                },
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);

            edits.VerifySemanticDiagnostics(
                new[]
                {
                    Diagnostic(RudeEditKind.InsertNotSupportedByRuntime, "int b", FeaturesResources.parameter),
                    Diagnostic(RudeEditKind.InsertNotSupportedByRuntime, "int c", FeaturesResources.parameter)
                },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void MethodUpdate_ChangeParameterType()
        {
            var src1 = @"
class C
{
    static void Main(bool x)
    {
        
    }
}";
            var src2 = @"
class C
{
    static void Main(int x)
    {
        
    }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [bool x]@35 -> [int x]@35");

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMembers("C.Main").FirstOrDefault(m => m.GetParameterTypes().Any(t => t.SpecialType == SpecialType.System_Boolean))?.ISymbol, deletedSymbolContainerProvider: c => c.GetMember("C")),
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMembers("C.Main").FirstOrDefault(m => m.GetParameterTypes().Any(t => t.SpecialType == SpecialType.System_Int32))?.ISymbol)
                },
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingTypeNotSupportedByRuntime, "int x", FeaturesResources.parameter) },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void MethodUpdate_ChangeParameterTypeAndRename()
        {
            var src1 = @"
class C
{
    static void Main(bool someBool)
    {
        
    }
}";
            var src2 = @"
class C
{
    static void Main(int someInt)
    {
        
    }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [bool someBool]@35 -> [int someInt]@35");

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMembers("C.Main").FirstOrDefault(m => m.GetParameterTypes().Any(t => t.SpecialType == SpecialType.System_Boolean))?.ISymbol, deletedSymbolContainerProvider: c => c.GetMember("C")),
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMembers("C.Main").FirstOrDefault(m => m.GetParameterTypes().Any(t => t.SpecialType == SpecialType.System_Int32))?.ISymbol)
                },
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
        }

        [Fact]
        public void MethodUpdate_DeleteParameter()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        
    }
}";
            var src2 = @"
class C
{
    static void Main()
    {
        
    }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Delete [string[] args]@35");

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMembers("C.Main").FirstOrDefault(m => m.GetParameterCount() == 1)?.ISymbol, deletedSymbolContainerProvider: c => c.GetMember("C")),
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMembers("C.Main").FirstOrDefault(m => m.GetParameterCount() == 0)?.ISymbol)
                },
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.DeleteNotSupportedByRuntime, "static void Main()", DeletedSymbolDisplay(FeaturesResources.parameter, "string[] args")) },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void MethodUpdate_DeleteParameters()
        {
            var src1 = @"
class C
{
    void M(int a, int b, int c)
    {
    }
}";
            var src2 = @"
class C
{
    void M(int a)
    {
    }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMembers("C.M").FirstOrDefault(m => m.GetParameterCount() == 3)?.ISymbol, deletedSymbolContainerProvider: c => c.GetMember("C")),
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMembers("C.M").FirstOrDefault(m => m.GetParameterCount() == 1)?.ISymbol)
                },
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);

            edits.VerifySemanticDiagnostics(
                new[]
                {
                    Diagnostic(RudeEditKind.DeleteNotSupportedByRuntime, "void M(int a)", DeletedSymbolDisplay(FeaturesResources.parameter, "int b")),
                    Diagnostic(RudeEditKind.DeleteNotSupportedByRuntime, "void M(int a)", DeletedSymbolDisplay(FeaturesResources.parameter, "int c"))
                },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void MethodUpdate_UpdateParameter()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        
    }
}";
            var src2 = @"
class C
{
    static void Main(string[] b)
    {
        
    }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [string[] args]@35 -> [string[] b]@35");

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.RenamingNotSupportedByRuntime, "string[] b", FeaturesResources.parameter) },
                capabilities: EditAndContinueCapabilities.Baseline);

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.Main"))
                },
                capabilities: EditAndContinueCapabilities.UpdateParameters);
        }

        [Fact]
        public void MethodUpdate_UpdateParameterAndBody()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        
    }
}";
            var src2 = @"
class C
{
    static void Main(string[] b)
    {
        System.Console.Write(1);
    }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.RenamingNotSupportedByRuntime, "string[] b", FeaturesResources.parameter) },
                capabilities: EditAndContinueCapabilities.Baseline);

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.Main"))
                },
                capabilities: EditAndContinueCapabilities.UpdateParameters);
        }

        [Fact]
        public void Method_Rename()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        
    }
}";
            var src2 = @"
class C
{
    static void EntryPoint(string[] args)
    {
        
    }
}";
            var edits = GetTopEdits(src1, src2);

            var expectedEdit = @"Update [static void Main(string[] args)
    {
        
    }]@18 -> [static void EntryPoint(string[] args)
    {
        
    }]@18";

            edits.VerifyEdits(expectedEdit);

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.Main"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.EntryPoint"))
                },
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.RenamingNotSupportedByRuntime, "static void EntryPoint(string[] args)", FeaturesResources.method) },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Theory]
        [InlineData("virtual")]
        [InlineData("abstract")]
        [InlineData("override")]
        public void Method_Rename_Modifiers(string modifier)
        {
            /* TODO: https://github.com/dotnet/roslyn/issues/59264

               This should be a supported edit. Consider the following inheritance chain:

                public class C { public virtual void M() => Console.WriteLine("C"); } 
                public class D : C { public override void M() { base.M(); Console.WriteLine("D"); } } 
                public class E : D { public override void M() { base.M(); Console.WriteLine("E"); } } 

                If D.M is deleted we expect E.M to print "C E" and not throw.

            */
            var src1 = $$"""
                class C
                {
                    {{modifier}} void goo() { }
                }
                """;
            var src2 = $$"""
                class C
                {
                    {{modifier}} void boo() { }
                }
                """;
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                $"Update [{modifier} void goo() {{ }}]@16 -> [{modifier} void boo() {{ }}]@16");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Renamed, $"{modifier} void boo()", FeaturesResources.method));
        }

        [Fact]
        public void MethodUpdate_AsyncMethod0()
        {
            var src1 = @"
class Test
{
    public async Task<int> WaitAsync()
    {
        await Task.Delay(1000);
        return 1;
    }
}";
            var src2 = @"
class Test
{
    public async Task<int> WaitAsync()
    {
        await Task.Delay(500);
        return 1;
    }
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifySemanticDiagnostics();

            VerifyPreserveLocalVariables(edits, preserveLocalVariables: true);
        }

        [Fact]
        public void MethodUpdate_AsyncMethod1()
        {
            var src1 = @"
class Test
{
    static void Main(string[] args)
    {
        Test f = new Test();
        string result = f.WaitAsync().Result;
    }

    public async Task<string> WaitAsync()
    {
        await Task.Delay(1000);
        return ""Done"";
    }
}";
            var src2 = @"
class Test
{
    static void Main(string[] args)
    {
        Test f = new Test();
        string result = f.WaitAsync().Result;
    }

    public async Task<string> WaitAsync()
    {
        await Task.Delay(1000);
        return ""Not Done"";
    }
}";
            var edits = GetTopEdits(src1, src2);
            var expectedEdit = @"Update [public async Task<string> WaitAsync()
    {
        await Task.Delay(1000);
        return ""Done"";
    }]@151 -> [public async Task<string> WaitAsync()
    {
        await Task.Delay(1000);
        return ""Not Done"";
    }]@151";

            edits.VerifyEdits(expectedEdit);

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void MethodUpdate_AddReturnTypeAttribute()
        {
            var src1 = @"
using System;

class Test
{
    static void Main(string[] args)
    {
        System.Console.Write(5);
    }
}";
            var src2 = @"
using System;

class Test
{
    [return: Obsolete]
    static void Main(string[] args)
    {
        System.Console.Write(5);
    }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(@"Update [static void Main(string[] args)
    {
        System.Console.Write(5);
    }]@38 -> [[return: Obsolete]
    static void Main(string[] args)
    {
        System.Console.Write(5);
    }]@38");

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "static void Main(string[] args)", FeaturesResources.method) },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void MethodUpdate_AddAttribute()
        {
            var src1 = @"
using System;

class Test
{
    static void Main(string[] args)
    {
        System.Console.Write(5);
    }
}";
            var src2 = @"
using System;

class Test
{
    [Obsolete]
    static void Main(string[] args)
    {
        System.Console.Write(5);
    }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(@"Update [static void Main(string[] args)
    {
        System.Console.Write(5);
    }]@38 -> [[Obsolete]
    static void Main(string[] args)
    {
        System.Console.Write(5);
    }]@38");

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "static void Main(string[] args)", FeaturesResources.method) },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void MethodUpdate_AddAttribute_SupportedByRuntime()
        {
            var src1 = @"
using System;

class Test
{
    static void Main(string[] args)
    {
        System.Console.Write(5);
    }
}";
            var src2 = @"
using System;

class Test
{
    [Obsolete]
    static void Main(string[] args)
    {
        System.Console.Write(5);
    }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(@"Update [static void Main(string[] args)
    {
        System.Console.Write(5);
    }]@38 -> [[Obsolete]
    static void Main(string[] args)
    {
        System.Console.Write(5);
    }]@38");

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember("Test.Main")) },
                capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
        }

        [Fact]
        public void MethodUpdate_Attribute_ArrayParameter()
        {
            var src1 = @"
class AAttribute : System.Attribute
{
    public AAttribute(int[] nums) { }
}

class C
{
    [A(new int[] { 1, 2, 3})]
    void M()
    {
    }
}";
            var src2 = @"
class AAttribute : System.Attribute
{
    public AAttribute(int[] nums) { }
}

class C
{
    [A(new int[] { 4, 5, 6})]
    void M()
    {
    }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.M")) },
                capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
        }

        [Fact]
        public void MethodUpdate_Attribute_ArrayParameter_NoChange()
        {
            var src1 = @"
class AAttribute : System.Attribute
{
    public AAttribute(int[] nums) { }
}

class C
{
    [A(new int[] { 1, 2, 3})]
    void M()
    {
        var x = 1;
    }
}";
            var src2 = @"
class AAttribute : System.Attribute
{
    public AAttribute(int[] nums) { }
}

class C
{
    [A(new int[] { 1, 2, 3})]
    void M()
    {
        var x = 2;
    }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.M")) });

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void MethodUpdate_AddAttribute2()
        {
            var src1 = @"
using System;

class Test
{
    [Obsolete]
    static void Main(string[] args)
    {
        System.Console.Write(5);
    }
}";
            var src2 = @"
using System;

class Test
{
    [Obsolete, STAThread]
    static void Main(string[] args)
    {
        System.Console.Write(5);
    }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "static void Main(string[] args)", FeaturesResources.method) },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void MethodUpdate_AddAttribute3()
        {
            var src1 = @"
using System;

class Test
{
    [Obsolete]
    static void Main(string[] args)
    {
        System.Console.Write(5);
    }
}";
            var src2 = @"
using System;

class Test
{
    [Obsolete]
    [STAThread]
    static void Main(string[] args)
    {
        System.Console.Write(5);
    }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "static void Main(string[] args)", FeaturesResources.method) },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void MethodUpdate_AddAttribute4()
        {
            var src1 = @"
using System;

class Test
{
    static void Main(string[] args)
    {
        System.Console.Write(5);
    }
}";
            var src2 = @"
using System;

class Test
{
    [Obsolete, STAThread]
    static void Main(string[] args)
    {
        System.Console.Write(5);
    }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "static void Main(string[] args)", FeaturesResources.method) },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void MethodUpdate_UpdateAttribute()
        {
            var src1 = @"
using System;

class Test
{
    [Obsolete]
    static void Main(string[] args)
    {
        System.Console.Write(5);
    }
}";
            var src2 = @"
using System;

class Test
{
    [Obsolete("""")]
    static void Main(string[] args)
    {
        System.Console.Write(5);
    }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "static void Main(string[] args)", FeaturesResources.method) },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/754853")]
        public void MethodUpdate_DeleteAttribute()
        {
            var src1 = @"
using System;

class Test
{
    [Obsolete]
    static void Main(string[] args)
    {
        System.Console.Write(5);
    }
}";
            var src2 = @"
using System;

class Test
{
    static void Main(string[] args)
    {
        System.Console.Write(5);
    }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "static void Main(string[] args)", FeaturesResources.method) },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void MethodUpdate_DeleteAttribute2()
        {
            var src1 = @"
using System;

class Test
{
    [Obsolete, STAThread]
    static void Main(string[] args)
    {
        System.Console.Write(5);
    }
}";
            var src2 = @"
using System;

class Test
{
    [Obsolete]
    static void Main(string[] args)
    {
        System.Console.Write(5);
    }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "static void Main(string[] args)", FeaturesResources.method) },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void MethodUpdate_DeleteAttribute3()
        {
            var src1 = @"
using System;

class Test
{
    [Obsolete]
    [STAThread]
    static void Main(string[] args)
    {
        System.Console.Write(5);
    }
}";
            var src2 = @"
using System;

class Test
{
    [Obsolete]
    static void Main(string[] args)
    {
        System.Console.Write(5);
    }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "static void Main(string[] args)", FeaturesResources.method) },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void MethodUpdate_ExplicitlyImplemented1()
        {
            var src1 = @"
class C : I, J
{
    void I.Goo() { Console.WriteLine(2); }
    void J.Goo() { Console.WriteLine(1); }
}";
            var src2 = @"
class C : I, J
{
    void I.Goo() { Console.WriteLine(1); }
    void J.Goo() { Console.WriteLine(2); }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [void I.Goo() { Console.WriteLine(2); }]@25 -> [void I.Goo() { Console.WriteLine(1); }]@25",
                "Update [void J.Goo() { Console.WriteLine(1); }]@69 -> [void J.Goo() { Console.WriteLine(2); }]@69");

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void MethodUpdate_ExplicitlyImplemented2()
        {
            var src1 = @"
class C : I, J
{
    void I.Goo() { Console.WriteLine(1); }
    void J.Goo() { Console.WriteLine(2); }
}";
            var src2 = @"
class C : I, J
{
    void Goo() { Console.WriteLine(1); }
    void J.Goo() { Console.WriteLine(2); }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [void I.Goo() { Console.WriteLine(1); }]@25 -> [void Goo() { Console.WriteLine(1); }]@25");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "void Goo()", FeaturesResources.method));
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/754255")]
        public void MethodUpdate_UpdateStackAlloc()
        {
            var src1 = @"
class C
{
    static void Main(string[] args) 
    { 
            int i = 10;
            unsafe
            {
                int* px2 = &i;
            }
    }
}";
            var src2 = @"
class C
{
    static void Main(string[] args) 
    { 
            int i = 10;
            unsafe
            {
                char* buffer = stackalloc char[16];
                int* px2 = &i;
            }
    }
}";
            var expectedEdit = @"Update [static void Main(string[] args) 
    { 
            int i = 10;
            unsafe
            {
                int* px2 = &i;
            }
    }]@18 -> [static void Main(string[] args) 
    { 
            int i = 10;
            unsafe
            {
                char* buffer = stackalloc char[16];
                int* px2 = &i;
            }
    }]@18";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(expectedEdit);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.StackAllocUpdate, "stackalloc", FeaturesResources.method));
        }

        [Theory]
        [InlineData("stackalloc int[3]")]
        [InlineData("stackalloc int[3] { 1, 2, 3 }")]
        [InlineData("stackalloc int[] { 1, 2, 3 }")]
        [InlineData("stackalloc[] { 1, 2, 3 }")]
        public void MethodUpdate_UpdateStackAlloc2(string stackallocDecl)
        {
            var src1 = @"unsafe class C { static int F() { var x = " + stackallocDecl + "; return 1; } }";
            var src2 = @"unsafe class C { static int F() { var x = " + stackallocDecl + "; return 2; } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.StackAllocUpdate, "stackalloc", FeaturesResources.method));
        }

        [Fact]
        public void MethodUpdate_UpdateStackAllocInLambda1()
        {
            var src1 = "unsafe class C { void M() { F(1, () => { int* a = stackalloc int[10]; }); } }";
            var src2 = "unsafe class C { void M() { F(2, () => { int* a = stackalloc int[10]; }); } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void MethodUpdate_UpdateStackAllocInLambda2()
        {
            var src1 = "unsafe class C { void M() { F(1, x => { int* a = stackalloc int[10]; }); } }";
            var src2 = "unsafe class C { void M() { F(2, x => { int* a = stackalloc int[10]; }); } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void MethodUpdate_UpdateStackAllocInAnonymousMethod()
        {
            var src1 = "unsafe class C { void M() { F(1, delegate(int x) { int* a = stackalloc int[10]; }); } }";
            var src2 = "unsafe class C { void M() { F(2, delegate(int x) { int* a = stackalloc int[10]; }); } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void MethodUpdate_UpdateStackAllocInLocalFunction()
        {
            var src1 = "class C { void M() { unsafe void f(int x) { int* a = stackalloc int[10]; } f(1); } }";
            var src2 = "class C { void M() { unsafe void f(int x) { int* a = stackalloc int[10]; } f(2); } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void MethodUpdate_SwitchExpressionInLambda1()
        {
            var src1 = "class C { void M() { F(1, a => a switch { 0 => 0, _ => 2 }); } }";
            var src2 = "class C { void M() { F(2, a => a switch { 0 => 0, _ => 2 }); } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void MethodUpdate_SwitchExpressionInLambda2()
        {
            var src1 = "class C { void M() { F(1, a => a switch { 0 => 0, _ => 2 }); } }";
            var src2 = "class C { void M() { F(2, a => a switch { 0 => 0, _ => 2 }); } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void MethodUpdate_SwitchExpressionInAnonymousMethod()
        {
            var src1 = "class C { void M() { F(1, delegate(int a) { return a switch { 0 => 0, _ => 2 }; }); } }";
            var src2 = "class C { void M() { F(2, delegate(int a) { return a switch { 0 => 0, _ => 2 }; }); } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void MethodUpdate_SwitchExpressionInLocalFunction()
        {
            var src1 = "class C { void M() { int f(int a) => a switch { 0 => 0, _ => 2 }; f(1); } }";
            var src2 = "class C { void M() { int f(int a) => a switch { 0 => 0, _ => 2 }; f(2); } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void MethodUpdate_SwitchExpressionInQuery()
        {
            var src1 = "class C { void M() { var x = from z in new[] { 1, 2, 3 } where z switch { 0 => true, _ => false } select z + 1; } }";
            var src2 = "class C { void M() { var x = from z in new[] { 1, 2, 3 } where z switch { 0 => true, _ => false } select z + 2; } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void MethodUpdate_UpdateAnonymousMethod()
        {
            var src1 = "class C { void M() { F(1, delegate(int a) { return a; }); } }";
            var src2 = "class C { void M() { F(2, delegate(int a) { return a; }); } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void MethodWithExpressionBody_Update_UpdateAnonymousMethod()
        {
            var src1 = "class C { void M() => F(1, delegate(int a) { return a; }); }";
            var src2 = "class C { void M() => F(2, delegate(int a) { return a; }); }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void MethodUpdate_Query()
        {
            var src1 = "class C { void M() { F(1, from goo in bar select baz); } }";
            var src2 = "class C { void M() { F(2, from goo in bar select baz); } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void MethodWithExpressionBody_Update_Query()
        {
            var src1 = "class C { void M() => F(1, from goo in bar select baz); }";
            var src2 = "class C { void M() => F(2, from goo in bar select baz); }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void MethodUpdate_AnonymousType()
        {
            var src1 = "class C { void M() { F(1, new { A = 1, B = 2 }); } }";
            var src2 = "class C { void M() { F(2, new { A = 1, B = 2 }); } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void MethodWithExpressionBody_Update_AnonymousType()
        {
            var src1 = "class C { void M() => F(new { A = 1, B = 2 }); }";
            var src2 = "class C { void M() => F(new { A = 10, B = 20 }); }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void MethodUpdate_Iterator_YieldReturn()
        {
            var src1 = "class C { IEnumerable<int> M() { yield return 1; } }";
            var src2 = "class C { IEnumerable<int> M() { yield return 2; } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics();

            VerifyPreserveLocalVariables(edits, preserveLocalVariables: true);
        }

        [Fact]
        public void MethodUpdate_AddYieldReturn()
        {
            var src1 = "class C { IEnumerable<int> M() { return new[] { 1, 2, 3}; } }";
            var src2 = "class C { IEnumerable<int> M() { yield return 2; } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);

            VerifyPreserveLocalVariables(edits, preserveLocalVariables: false);
        }

        [Fact]
        public void MethodUpdate_AddYieldReturn_NotSupported()
        {
            var src1 = "class C { IEnumerable<int> M() { return new[] { 1, 2, 3}; } }";
            var src2 = "class C { IEnumerable<int> M() { yield return 2; } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.MakeMethodIteratorNotSupportedByRuntime, "IEnumerable<int> M()") },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void MethodUpdate_Iterator_YieldBreak()
        {
            var src1 = "class C { IEnumerable<int> M() { F(); yield break; } }";
            var src2 = "class C { IEnumerable<int> M() { G(); yield break; } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics();

            VerifyPreserveLocalVariables(edits, preserveLocalVariables: false);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1087305")]
        public void MethodUpdate_LabeledStatement()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        goto Label1;
 
    Label1:
        {
            Console.WriteLine(1);
        }
    }
}";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        goto Label1;
 
    Label1:
        {
            Console.WriteLine(2);
        }
    }
}";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void MethodUpdate_LocalFunctionsParameterRefnessInBody()
        {
            var src1 = @"class C { public void M(int a) { void f(ref int b) => b = 1; } }";
            var src2 = @"class C { public void M(int a) { void f(out int b) => b = 1; } } ";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyEdits(
                "Update [public void M(int a) { void f(ref int b) => b = 1; }]@10 -> [public void M(int a) { void f(out int b) => b = 1; }]@10");
        }

        [Fact]
        public void MethodUpdate_LambdaParameterRefnessInBody()
        {
            var src1 = @"class C { public void M(int a) { f((ref int b) => b = 1); } }";
            var src2 = @"class C { public void M(int a) { f((out int b) => b = 1); } } ";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyEdits(
                "Update [public void M(int a) { f((ref int b) => b = 1); }]@10 -> [public void M(int a) { f((out int b) => b = 1); }]@10");
        }

        [Fact]
        public void Method_ReadOnlyRef_Parameter_InsertWhole()
        {
            var src1 = "class Test { }";
            var src2 = "class Test { int M(in int b) => throw null; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [int M(in int b) => throw null;]@13",
                "Insert [(in int b)]@18",
                "Insert [in int b]@19");

            edits.VerifySemanticDiagnostics(
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
        }

        [Fact]
        public void Method_ReadOnlyRef_Parameter_InsertParameter()
        {
            var src1 = "class Test { int M() => throw null; }";
            var src2 = "class Test { int M(in int b) => throw null; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [in int b]@19");

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMembers("Test.M").FirstOrDefault(m => m.GetParameterCount() == 0)?.ISymbol, deletedSymbolContainerProvider: c => c.GetMember("Test")),
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMembers("Test.M").FirstOrDefault(m => m.GetParameterCount() == 1)?.ISymbol)
                },
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.InsertNotSupportedByRuntime, "in int b", FeaturesResources.parameter) },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void Method_ReadOnlyRef_Parameter_Update()
        {
            var src1 = "class Test { int M(int b) => throw null; }";
            var src2 = "class Test { int M(in int b) => throw null; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [int b]@19 -> [in int b]@19");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "in int b", FeaturesResources.parameter));
        }

        [Fact]
        public void Method_ReadOnlyRef_ReturnType_Insert()
        {
            var src1 = "class Test { }";
            var src2 = "class Test { ref readonly int M() => throw null; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [ref readonly int M() => throw null;]@13",
                "Insert [()]@31");

            edits.VerifySemanticDiagnostics(
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
        }

        [Fact]
        public void Method_ReadOnlyRef_ReturnType_Update()
        {
            var src1 = "class Test { int M() => throw null; }";
            var src2 = "class Test { ref readonly int M() => throw null; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [int M() => throw null;]@13 -> [ref readonly int M() => throw null;]@13");

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("Test.M"), deletedSymbolContainerProvider: c => c.GetMember("Test")),
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("Test.M"))
                },
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingTypeNotSupportedByRuntime, "ref readonly int M()", FeaturesResources.method) },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void Method_ImplementingInterface_Add()
        {
            var src1 = @"
using System;

public interface ISample
{
    string Get();
}

public interface IConflict
{
    string Get();
}

public class BaseClass : ISample
{
    public virtual string Get() => string.Empty;
}

public class SubClass : BaseClass, IConflict
{
    public override string Get() => string.Empty;
}
";
            var src2 = @"
using System;

public interface ISample
{
    string Get();
}

public interface IConflict
{
    string Get();
}

public class BaseClass : ISample
{
    public virtual string Get() => string.Empty;
}

public class SubClass : BaseClass, IConflict
{
    public override string Get() => string.Empty;

    string IConflict.Get() => String.Empty;
}
";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [string IConflict.Get() => String.Empty;]@325",
                "Insert [()]@345");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.InsertMethodWithExplicitInterfaceSpecifier, "string IConflict.Get()", FeaturesResources.method));
        }

        [Fact]
        public void Method_Partial_DeleteInsert_DefinitionPart()
        {
            var srcA1 = "partial class C { partial void F(); }";
            var srcB1 = "partial class C { partial void F() { } }";
            var srcC1 = "partial class C { }";

            var srcA2 = "partial class C { }";
            var srcB2 = "partial class C { partial void F() { } }";
            var srcC2 = "partial class C { partial void F(); }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2), GetTopEdits(srcC1, srcC2) },
                new[]
                {
                    DocumentResults(),
                    DocumentResults(),
                    DocumentResults(),
                });
        }

        [Fact]
        public void Method_Partial_DeleteInsert_ImplementationPart()
        {
            var srcA1 = "partial class C { partial void F(); }";
            var srcB1 = "partial class C { partial void F() { } }";
            var srcC1 = "partial class C { }";

            var srcA2 = "partial class C { partial void F(); }";
            var srcB2 = "partial class C { }";
            var srcC2 = "partial class C { partial void F() { } }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2), GetTopEdits(srcC1, srcC2) },
                new[]
                {
                    DocumentResults(),
                    DocumentResults(),
                    DocumentResults(
                        semanticEdits: new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IMethodSymbol>("F").PartialImplementationPart) }),
                });
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/51011")]
        public void Method_Partial_Swap_ImplementationAndDefinitionParts()
        {
            var srcA1 = "partial class C { partial void F(); }";
            var srcB1 = "partial class C { partial void F() { } }";

            var srcA2 = "partial class C { partial void F() { } }";
            var srcB2 = "partial class C { partial void F(); }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(
                        semanticEdits: new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IMethodSymbol>("F").PartialImplementationPart) }),
                    DocumentResults(),
                });
        }

        [Fact]
        public void Method_Partial_DeleteImplementation()
        {
            var srcA1 = "partial class C { partial void F(); }";
            var srcB1 = "partial class C { partial void F() { } }";

            var srcA2 = "partial class C { partial void F(); }";
            var srcB2 = "partial class C { }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(),

                    DocumentResults(
                        semanticEdits: new[]
                        {
                            SemanticEdit(SemanticEditKind.Delete, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IMethodSymbol>("F"), deletedSymbolContainerProvider: c => c.GetMember("C"))
                        }),
                });
        }

        [Fact]
        public void Method_Partial_DeleteBoth()
        {
            var srcA1 = "partial class C { partial void F(); }";
            var srcB1 = "partial class C { partial void F() { } }";

            var srcA2 = "partial class C { }";
            var srcB2 = "partial class C { }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(),

                    DocumentResults(
                        semanticEdits: new[]
                        {
                            SemanticEdit(SemanticEditKind.Delete, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IMethodSymbol>("F")?.PartialImplementationPart, deletedSymbolContainerProvider: c => c.GetMember("C"))
                        }),
                });
        }

        [Fact]
        public void Method_Partial_DeleteInsertBoth()
        {
            var srcA1 = "partial class C { partial void F(); }";
            var srcB1 = "partial class C { partial void F() { } }";
            var srcC1 = "partial class C { }";
            var srcD1 = "partial class C { }";

            var srcA2 = "partial class C { }";
            var srcB2 = "partial class C { }";
            var srcC2 = "partial class C { partial void F(); }";
            var srcD2 = "partial class C { partial void F() { } }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2), GetTopEdits(srcC1, srcC2), GetTopEdits(srcD1, srcD2) },
                new[]
                {
                    DocumentResults(),
                    DocumentResults(),
                    DocumentResults(),
                    DocumentResults(
                        semanticEdits: new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IMethodSymbol>("F").PartialImplementationPart) })
                });
        }

        [Fact]
        public void Method_Partial_Insert()
        {
            var srcA1 = "partial class C { }";
            var srcB1 = "partial class C { }";

            var srcA2 = "partial class C { partial void F(); }";
            var srcB2 = "partial class C { partial void F() { } }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(),

                    DocumentResults(
                        semanticEdits: new[] { SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IMethodSymbol>("F").PartialImplementationPart) }),
                },
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
        }

        [Fact]
        public void Method_Partial_Insert_Reloadable()
        {
            var srcA1 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]partial class C { }";
            var srcB1 = "partial class C { }";

            var srcA2 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]partial class C { partial void F(); }";
            var srcB2 = "partial class C { partial void F() { } }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(),
                    DocumentResults(
                        semanticEdits: new[] { SemanticEdit(SemanticEditKind.Replace, c => c.GetMember("C"), partialType: "C") }),
                },
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        #endregion

        #region Operators

        [Theory]
        [InlineData("implicit", "explicit")]
        [InlineData("explicit", "implicit")]
        public void Operator_Modifiers_Update(string oldModifiers, string newModifiers)
        {
            var src1 = "class C { public static " + oldModifiers + " operator int (C c) => 0; }";
            var src2 = "class C { public static " + newModifiers + " operator int (C c) => 0; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [public static " + oldModifiers + " operator int (C c) => 0;]@10 -> [public static " + newModifiers + " operator int (C c) => 0;]@10");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "public static " + newModifiers + " operator int (C c)", CSharpFeaturesResources.conversion_operator));
        }

        [Fact]
        public void Operator_Modifiers_Update_Reloadable()
        {
            var src1 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]class C { public static implicit operator int (C c) => 0; }";
            var src2 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]class C { public static explicit operator int (C c) => 0; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                new[] { SemanticEdit(SemanticEditKind.Replace, c => c.GetMember("C")) },
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void Operator_Conversion_ExternModifiers_Add()
        {
            var src1 = "class C { public static implicit operator bool (C c) => default; }";
            var src2 = "class C { extern public static implicit operator bool (C c); }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "extern public static implicit operator bool (C c)", CSharpFeaturesResources.conversion_operator));
        }

        [Fact]
        public void Operator_Conversion_ExternModifiers_Remove()
        {
            var src1 = "class C { extern public static implicit operator bool (C c); }";
            var src2 = "class C { public static implicit operator bool (C c) => default; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "public static implicit operator bool (C c)", CSharpFeaturesResources.conversion_operator));
        }

        [Fact]
        public void OperatorInsert()
        {
            var src1 = @"
class C
{
}
";
            var src2 = @"
class C
{
    public static implicit operator bool (C c) 
    {
        return false;
    }

    public static C operator +(C c, C d) 
    {
        return c;
    }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.InsertOperator, "public static implicit operator bool (C c)", CSharpFeaturesResources.conversion_operator),
                Diagnostic(RudeEditKind.InsertOperator, "public static C operator +(C c, C d)", FeaturesResources.operator_));
        }

        [Fact]
        public void OperatorDelete()
        {
            var src1 = @"
class C
{
    public static implicit operator bool (C c) 
    {
        return false;
    }

    public static C operator +(C c, C d) 
    {
        return c;
    }
}
";
            var src2 = @"
class C
{
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.op_Implicit"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.op_Addition"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void OperatorInsertDelete()
        {
            var srcA1 = @"
partial class C
{
    public static implicit operator bool (C c)  => false;
}
";
            var srcB1 = @"
partial class C
{
    public static C operator +(C c, C d) => c;
}
";

            var srcA2 = srcB1;
            var srcB2 = srcA1;

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(
                        semanticEdits: new[]
                        {
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember("op_Addition"))
                        }),

                    DocumentResults(
                        semanticEdits: new[]
                        {
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember("op_Implicit"))
                        }),
                });
        }

        [Fact]
        public void OperatorUpdate()
        {
            var src1 = @"
class C
{
    public static implicit operator bool (C c) 
    {
        return false;
    }

    public static C operator +(C c, C d) 
    {
        return c;
    }
}
";
            var src2 = @"
class C
{
    public static implicit operator bool (C c) 
    {
        return true;
    }

    public static C operator +(C c, C d) 
    {
        return d;
    }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(ActiveStatementsDescription.Empty, new[]
            {
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.op_Implicit")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.op_Addition")),
            });
        }

        [Fact]
        public void OperatorWithExpressionBody_Update()
        {
            var src1 = @"
class C
{
    public static implicit operator bool (C c) => false;
    public static C operator +(C c, C d) => c;
}
";
            var src2 = @"
class C
{
    public static implicit operator bool (C c) => true;
    public static C operator +(C c, C d) => d;
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(ActiveStatementsDescription.Empty, new[]
            {
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.op_Implicit")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.op_Addition")),
            });
        }

        [Fact]
        public void OperatorWithExpressionBody_ToBlockBody()
        {
            var src1 = "class C { public static C operator +(C c, C d) => d; }";
            var src2 = "class C { public static C operator +(C c, C d) { return c; } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [public static C operator +(C c, C d) => d;]@10 -> [public static C operator +(C c, C d) { return c; }]@10");

            edits.VerifySemantics(ActiveStatementsDescription.Empty, new[]
            {
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.op_Addition"))
            });
        }

        [Fact]
        public void OperatorWithBlockBody_ToExpressionBody()
        {
            var src1 = "class C { public static C operator +(C c, C d) { return c; } }";
            var src2 = "class C { public static C operator +(C c, C d) => d;  }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [public static C operator +(C c, C d) { return c; }]@10 -> [public static C operator +(C c, C d) => d;]@10");

            edits.VerifySemantics(ActiveStatementsDescription.Empty, new[]
            {
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.op_Addition"))
            });
        }

        [Fact]
        public void Operator_Rename()
        {
            var src1 = @"
class C
{
    public static C operator +(C c, C d) { return c; }
}
";
            var src2 = @"
class C
{
    public static C operator -(C c, C d) { return d; }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(ActiveStatementsDescription.Empty, new[]
                {
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.op_Addition"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.op_Subtraction"))
                },
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
        }

        [Fact]
        public void OperatorReorder1()
        {
            var src1 = @"
class C
{
    public static implicit operator bool (C c) { return false; }
    public static implicit operator int (C c) { return 1; }
}
";
            var src2 = @"
class C
{
    public static implicit operator int (C c) { return 1; }
    public static implicit operator bool (C c) { return false; }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Reorder [public static implicit operator int (C c) { return 1; }]@84 -> @18");

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void OperatorReorder2()
        {
            var src1 = @"
class C
{
    public static C operator +(C c, C d) { return c; }
    public static C operator -(C c, C d) { return d; }
}
";
            var src2 = @"
class C
{
    public static C operator -(C c, C d) { return d; }
    public static C operator +(C c, C d) { return c; }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Reorder [public static C operator -(C c, C d) { return d; }]@74 -> @18");

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void Operator_ReadOnlyRef_Parameter_InsertWhole()
        {
            var src1 = "class Test { }";
            var src2 = "class Test { public static bool operator !(in Test b) => throw null; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [public static bool operator !(in Test b) => throw null;]@13",
                "Insert [(in Test b)]@42",
                "Insert [in Test b]@43");

            edits.VerifySemanticDiagnostics(
                 Diagnostic(RudeEditKind.InsertOperator, "public static bool operator !(in Test b)", FeaturesResources.operator_));
        }

        [Fact]
        public void Operator_ReadOnlyRef_Parameter_Update()
        {
            var src1 = "class Test { public static bool operator !(Test b) => throw null; }";
            var src2 = "class Test { public static bool operator !(in Test b) => throw null; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [Test b]@43 -> [in Test b]@43");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "in Test b", FeaturesResources.parameter));
        }

        [Fact]
        public void Operator_Delete()
        {
            var src1 = "class C { public static bool operator !(C b) => true; }";
            var src2 = "class C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                new[] { SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.op_LogicalNot"), deletedSymbolContainerProvider: c => c.GetMember("C")) },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        #endregion

        #region Constructor, Destructor

        [Fact]
        public void Constructor_Parameter_AddAttribute()
        {
            var src1 = @"
class C
{
    private int x = 1;

    public C(int a)
    {
    }
}";
            var src2 = @"
class C
{
    private int x = 2;

    public C([System.Obsolete]int a)
    {
    }
}";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [x = 1]@30 -> [x = 2]@30",
                "Update [int a]@53 -> [[System.Obsolete]int a]@53");

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C..ctor"))
                },
                capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
        }

        [Fact]
        public void Constructor_ChangeParameterType()
        {
            var src1 = @"
class C
{
    public C(bool x)
    {
    }
}";
            var src2 = @"
class C
{
    public C(int x)
    {
    }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMembers("C..ctor").FirstOrDefault(m => m.GetParameterTypes().Any(t => t.SpecialType == SpecialType.System_Boolean))?.ISymbol, deletedSymbolContainerProvider: c => c.GetMember("C")),
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMembers("C..ctor").FirstOrDefault(m => m.GetParameterTypes().Any(t => t.SpecialType == SpecialType.System_Int32))?.ISymbol)
                },
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingTypeNotSupportedByRuntime, "int x", FeaturesResources.parameter) },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2068")]
        public void Constructor_ExternModifier_Add()
        {
            var src1 = "class C { }";
            var src2 = "class C { public extern C(); }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [public extern C();]@10",
                "Insert [()]@25");

            // This can be allowed as the compiler generates an empty constructor, but it's not worth the complexity.
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "public extern C()", FeaturesResources.constructor));
        }

        [Fact]
        public void ConstructorInitializer_Update1()
        {
            var src1 = @"
class C
{
    public C(int a) : base(a) { }
}";
            var src2 = @"
class C
{
    public C(int a) : base(a + 1) { }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [public C(int a) : base(a) { }]@18 -> [public C(int a) : base(a + 1) { }]@18");

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void ConstructorInitializer_Update2()
        {
            var src1 = @"
class C<T>
{
    public C(int a) : base(a) { }
}";
            var src2 = @"
class C<T>
{
    public C(int a) { }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [public C(int a) : base(a) { }]@21 -> [public C(int a) { }]@21");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.GenericTypeUpdate, "public C(int a)"));
        }

        [Fact]
        public void ConstructorInitializer_Update3()
        {
            var src1 = @"
class C
{
    public C(int a) { }
}";
            var src2 = @"
class C
{
    public C(int a) : base(a) { }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [public C(int a) { }]@18 -> [public C(int a) : base(a) { }]@18");

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void ConstructorInitializer_Update4()
        {
            var src1 = @"
class C<T>
{
    public C(int a) : base(a) { }
}";
            var src2 = @"
class C<T>
{
    public C(int a) : base(a + 1) { }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [public C(int a) : base(a) { }]@21 -> [public C(int a) : base(a + 1) { }]@21");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.GenericTypeUpdate, "public C(int a)"));
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/743552")]
        public void ConstructorUpdate_AddParameter()
        {
            var src1 = @"
class C
{
    public C(int a) { }
}";
            var src2 = @"
class C
{
    public C(int a, int b) { }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [(int a)]@26 -> [(int a, int b)]@26",
                "Insert [int b]@34");

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMembers("C..ctor").FirstOrDefault(m => m.GetParameterCount() == 1)?.ISymbol, deletedSymbolContainerProvider: c => c.GetMember("C")),
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMembers("C..ctor").FirstOrDefault(m => m.GetParameterCount() == 2)?.ISymbol)
                },
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.InsertNotSupportedByRuntime, "int b", FeaturesResources.parameter) },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void DestructorDelete()
        {
            var src1 = @"class B { ~B() { } }";
            var src2 = @"class B { }";

            var expectedEdit1 = @"Delete [~B() { }]@10";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(expectedEdit1);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Delete, "class B", DeletedSymbolDisplay(CSharpFeaturesResources.destructor, "~B()")));
        }

        [Fact]
        public void DestructorDelete_InsertConstructor()
        {
            var src1 = @"class B { ~B() { } }";
            var src2 = @"class B { B() { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [B() { }]@10",
                "Insert [()]@11",
                "Delete [~B() { }]@10");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingAccessibility, "B()", FeaturesResources.constructor),
                Diagnostic(RudeEditKind.Delete, "class B", DeletedSymbolDisplay(CSharpFeaturesResources.destructor, "~B()")));
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/789577")]
        public void ConstructorUpdate_AnonymousTypeInFieldInitializer()
        {
            var src1 = "class C { int a = F(new { A = 1, B = 2 }); C() { x = 1; } }";
            var src2 = "class C { int a = F(new { A = 1, B = 2 }); C() { x = 2; } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void Constructor_Delete()
        {
            var src1 = "class C { public C(int x) { } }";
            var src2 = "class C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                new[] { SemanticEdit(SemanticEditKind.Delete, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.FirstOrDefault(c => c.Parameters.Length == 1), deletedSymbolContainerProvider: c => c.GetMember("C")) },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void Constructor_Static_Delete()
        {
            var src1 = "class C { static C() { } }";
            var src2 = "class C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Delete, "class C", DeletedSymbolDisplay(FeaturesResources.static_constructor, "C()")));
        }

        [Fact]
        public void Constructor_Static_Delete_Reloadable()
        {
            var src1 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]class C { static C() { } }";
            var src2 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]class C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                new[] { SemanticEdit(SemanticEditKind.Replace, c => c.GetMember("C")) },
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void Constructor_Static_Insert()
        {
            var src1 = "class C { }";
            var src2 = "class C { static C() { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                new[] { SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<INamedTypeSymbol>("C").StaticConstructors.Single()) },
                EditAndContinueCapabilities.AddMethodToExistingType);
        }

        [Fact]
        public void InstanceCtorDelete_Public()
        {
            var src1 = "class C { public C() { } }";
            var src2 = "class C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), preserveLocalVariables: true));
        }

        [Theory]
        [InlineData("")]
        [InlineData("private")]
        [InlineData("protected")]
        [InlineData("internal")]
        [InlineData("private protected")]
        [InlineData("protected internal")]
        public void InstanceCtorDelete_NonPublic(string accessibility)
        {
            var src1 = "class C { [System.Obsolete] " + accessibility + " C() { } }";
            var src2 = "class C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                new[]
                {
                    Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "class C", DeletedSymbolDisplay(FeaturesResources.constructor, "C()")),
                    Diagnostic(RudeEditKind.ChangingAccessibility, "class C", DeletedSymbolDisplay(FeaturesResources.constructor, "C()"))
                },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void InstanceCtorDelete_Public_PartialWithInitializerUpdate()
        {
            var srcA1 = "partial class C { public C() { } }";
            var srcB1 = "partial class C { int x = 1; }";

            var srcA2 = "partial class C { }";
            var srcB2 = "partial class C { int x = 2; }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(
                        semanticEdits: new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), partialType: "C", preserveLocalVariables: true) }),

                    DocumentResults(
                        semanticEdits: new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), partialType: "C", preserveLocalVariables: true) })
                });
        }

        [Fact]
        public void InstanceCtorInsert_Public_Implicit()
        {
            var src1 = "class C { }";
            var src2 = "class C { public C() { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), preserveLocalVariables: true) });
        }

        [Fact]
        public void InstanceCtorInsert_Partial_Public_Implicit()
        {
            var srcA1 = "partial class C { }";
            var srcB1 = "partial class C { }";

            var srcA2 = "partial class C { }";
            var srcB2 = "partial class C { public C() { } }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    // no change in document A
                    DocumentResults(),

                    DocumentResults(
                        semanticEdits: new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), partialType: "C", preserveLocalVariables: true) }),
                });
        }

        [Fact]
        public void InstanceCtorInsert_Public_NoImplicit()
        {
            var src1 = "class C { public C(int a) { } }";
            var src2 = "class C { public C(int a) { } public C() { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                semanticEdits: new[]
                {
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(c => c.Parameters.IsEmpty))
                },
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
        }

        [Fact]
        public void InstanceCtorInsert_Partial_Public_NoImplicit()
        {
            var srcA1 = "partial class C { }";
            var srcB1 = "partial class C { public C(int a) { } }";

            var srcA2 = "partial class C { public C() { } }";
            var srcB2 = "partial class C { public C(int a) { } }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(
                        semanticEdits: new[]
                        {
                            SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(c => c.Parameters.IsEmpty))
                        }),

                    // no change in document B
                    DocumentResults(),
                },
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
        }

        [Fact]
        public void InstanceCtorInsert_Private_Implicit1()
        {
            var src1 = "class C { }";
            var src2 = "class C { private C() { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingAccessibility, "private C()", FeaturesResources.constructor));
        }

        [Fact]
        public void InstanceCtorInsert_Private_Implicit2()
        {
            var src1 = "class C { }";
            var src2 = "class C { C() { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingAccessibility, "C()", FeaturesResources.constructor));
        }

        [Fact]
        public void InstanceCtorInsert_Protected_PublicImplicit()
        {
            var src1 = "class C { }";
            var src2 = "class C { protected C() { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingAccessibility, "protected C()", FeaturesResources.constructor));
        }

        [Fact]
        public void InstanceCtorInsert_Internal_PublicImplicit()
        {
            var src1 = "class C { }";
            var src2 = "class C { internal C() { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingAccessibility, "internal C()", FeaturesResources.constructor));
        }

        [Fact]
        public void InstanceCtorInsert_Internal_ProtectedImplicit()
        {
            var src1 = "abstract class C { }";
            var src2 = "abstract class C { internal C() { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingAccessibility, "internal C()", FeaturesResources.constructor));
        }

        [Fact]
        public void InstanceCtorUpdate_ProtectedImplicit()
        {
            var src1 = "abstract class C { }";
            var src2 = "abstract class C { protected C() { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), preserveLocalVariables: true));
        }

        [Fact]
        public void InstanceCtorInsert_Private_NoImplicit()
        {
            var src1 = "class C { public C(int a) { } }";
            var src2 = "class C { public C(int a) { } private C() { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<INamedTypeSymbol>("C")
                        .InstanceConstructors.Single(ctor => ctor.DeclaredAccessibility == Accessibility.Private))
                },
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
        }

        [Fact]
        public void InstanceCtorInsert_Internal_NoImplicit()
        {
            var src1 = "class C { public C(int a) { } }";
            var src2 = "class C { public C(int a) { } internal C() { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
        }

        [Fact]
        public void InstanceCtorInsert_Protected_NoImplicit()
        {
            var src1 = "class C { public C(int a) { } }";
            var src2 = "class C { public C(int a) { } protected C() { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
        }

        [Fact]
        public void InstanceCtorInsert_InternalProtected_NoImplicit()
        {
            var src1 = "class C { public C(int a) { } }";
            var src2 = "class C { public C(int a) { } internal protected C() { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
        }

        [Fact]
        public void StaticCtor_Partial_DeleteInsert()
        {
            var srcA1 = "partial class C { static C() { } }";
            var srcB1 = "partial class C {  }";

            var srcA2 = "partial class C { }";
            var srcB2 = "partial class C { static C() { } }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    // delete of the constructor in partial part will be represented as a semantic update in the other document where it was inserted back
                    DocumentResults(),

                    DocumentResults(
                        semanticEdits: new[]
                        {
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").StaticConstructors.Single(), partialType: "C", preserveLocalVariables: true)
                        }),
                });
        }

        [Fact]
        public void InstanceCtor_Partial_DeletePrivateInsertPrivate()
        {
            var srcA1 = "partial class C { C() { } }";
            var srcB1 = "partial class C {  }";

            var srcA2 = "partial class C { }";
            var srcB2 = "partial class C { C() { } }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    // delete of the constructor in partial part will be represented as a semantic update in the other document where it was inserted back
                    DocumentResults(),

                    DocumentResults(
                        semanticEdits: new[]
                        {
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), partialType: "C", preserveLocalVariables: true)
                        }),
                });
        }

        [Fact]
        public void InstanceCtor_Partial_DeletePublicInsertPublic()
        {
            var srcA1 = "partial class C { public C() { } }";
            var srcB1 = "partial class C { }";

            var srcA2 = "partial class C { }";
            var srcB2 = "partial class C { public C() { } }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    // delete of the constructor in partial part will be represented as a semantic update in the other document where it was inserted back
                    DocumentResults(),

                    DocumentResults(
                        semanticEdits: new[]
                        {
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), partialType: "C", preserveLocalVariables: true)
                        }),
                });
        }

        [Fact]
        public void InstanceCtor_Partial_DeletePrivateInsertPublic()
        {
            var srcA1 = "partial class C { C() { } }";
            var srcB1 = "partial class C { }";

            var srcA2 = "partial class C { }";
            var srcB2 = "partial class C { public C() { } }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    // delete of the constructor in partial part will be reported as rude edit in the other document where it was inserted back with changed accessibility
                    DocumentResults(
                        semanticEdits: NoSemanticEdits),

                    DocumentResults(
                        diagnostics: new[] { Diagnostic(RudeEditKind.ChangingAccessibility, "public C()", FeaturesResources.constructor) }),
                });
        }

        [Fact]
        public void StaticCtor_Partial_InsertDelete()
        {
            var srcA1 = "partial class C { }";
            var srcB1 = "partial class C { static C() { } }";

            var srcA2 = "partial class C { static C() { } }";
            var srcB2 = "partial class C { }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(
                        semanticEdits: new[]
                        {
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").StaticConstructors.Single(), partialType: "C", preserveLocalVariables: true)
                        }),

                    // delete of the constructor in partial part will be represented as a semantic update in the other document where it was inserted back
                    DocumentResults(),
                });
        }

        [Fact]
        public void InstanceCtor_Partial_InsertPublicDeletePublic()
        {
            var srcA1 = "partial class C { }";
            var srcB1 = "partial class C { public C() { } }";

            var srcA2 = "partial class C { public C() { } }";
            var srcB2 = "partial class C { }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(
                        semanticEdits: new[]
                        {
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), partialType: "C", preserveLocalVariables: true)
                        }),

                    // delete of the constructor in partial part will be represented as a semantic update in the other document where it was inserted back
                    DocumentResults(),
                });
        }

        [Fact]
        public void InstanceCtor_Partial_InsertPrivateDeletePrivate()
        {
            var srcA1 = "partial class C { }";
            var srcB1 = "partial class C { private C() { } }";

            var srcA2 = "partial class C { private C() { } }";
            var srcB2 = "partial class C { }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(
                        semanticEdits: new[]
                        {
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), partialType: "C", preserveLocalVariables: true)
                        }),

                    // delete of the constructor in partial part will be represented as a semantic update in the other document where it was inserted back
                    DocumentResults(),
                });
        }

        [Fact]
        public void InstanceCtor_Partial_DeleteInternalInsertInternal()
        {
            var srcA1 = "partial class C { }";
            var srcB1 = "partial class C { internal C() { } }";

            var srcA2 = "partial class C { internal C() { } }";
            var srcB2 = "partial class C { }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(
                        semanticEdits: new[]
                        {
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), partialType: "C", preserveLocalVariables: true)
                        }),

                    // delete of the constructor in partial part will be represented as a semantic update in the other document where it was inserted back
                    DocumentResults(),
                });
        }

        [Fact]
        public void InstanceCtor_Partial_InsertInternalDeleteInternal_WithBody()
        {
            var srcA1 = "partial class C { }";
            var srcB1 = "partial class C { internal C() { } }";

            var srcA2 = "partial class C { internal C() { Console.WriteLine(1); } }";
            var srcB2 = "partial class C { }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(
                        semanticEdits: new[]
                        {
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), partialType: "C", preserveLocalVariables: true)
                        }),

                    // delete of the constructor in partial part will be represented as a semantic update in the other document where it was inserted back
                    DocumentResults(),
                });
        }

        [Fact]
        public void InstanceCtor_Partial_InsertPublicDeletePrivate()
        {
            var srcA1 = "partial class C { }";
            var srcB1 = "partial class C { private C() { } }";

            var srcA2 = "partial class C { public C() { } }";
            var srcB2 = "partial class C { }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(
                        diagnostics: new[] { Diagnostic(RudeEditKind.ChangingAccessibility, "public C()", FeaturesResources.constructor) }),

                    // delete of the constructor in partial part will be reported as rude in the the other document where it was inserted with changed accessibility
                    DocumentResults(),
                });
        }

        [Fact]
        public void InstanceCtor_Partial_InsertInternalDeletePrivate()
        {
            var srcA1 = "partial class C { }";
            var srcB1 = "partial class C { private C() { } }";

            var srcA2 = "partial class C { internal C() { } }";
            var srcB2 = "partial class C { }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(
                        diagnostics: new[] { Diagnostic(RudeEditKind.ChangingAccessibility, "internal C()", FeaturesResources.constructor) }),

                    DocumentResults(),
                });
        }

        [Fact]
        public void InstanceCtor_Partial_Update_LambdaInInitializer1()
        {
            var src1 = @"
using System;

partial class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
}

partial class C
{
    int B { get; } = F(<N:0.1>b => b + 1</N:0.1>);

    public C()
    {
        F(<N:0.2>c => c + 1</N:0.2>);
    }
}
";
            var src2 = @"
using System;

partial class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
}

partial class C
{
    int B { get; } = F(<N:0.1>b => b + 1</N:0.1>);

    public C()
    {
        F(<N:0.2>c => c + 2</N:0.2>);
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var syntaxMap = GetSyntaxMap(src1, src2);

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(), syntaxMap[0]) });
        }

        [Fact]
        public void InstanceCtor_Partial_Update_LambdaInInitializer_Trivia1()
        {
            var src1 = @"
using System;

partial class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
}

partial class C
{
    int B { get; } = F(<N:0.1>b => b + 1</N:0.1>);

    public C() { F(<N:0.2>c => c + 1</N:0.2>); }
}
";
            var src2 = @"
using System;

partial class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
}

partial class C
{
    int B { get; } = F(<N:0.1>b => b + 1</N:0.1>);

    /*new trivia*/public C() { F(<N:0.2>c => c + 1</N:0.2>); }
}
";
            var edits = GetTopEdits(src1, src2);
            var syntaxMap = GetSyntaxMap(src1, src2);

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(), syntaxMap[0]) });
        }

        [Fact]
        public void InstanceCtor_Partial_Update_LambdaInInitializer_ExplicitInterfaceImpl1()
        {
            var src1 = @"
using System;

public interface I { int B { get; } }
public interface J { int B { get; } }

partial class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
}

partial class C : I, J
{
    int I.B { get; } = F(<N:0.1>ib => ib + 1</N:0.1>);
    int J.B { get; } = F(<N:0.2>jb => jb + 1</N:0.2>);

    public C()
    {
        F(<N:0.3>c => c + 1</N:0.3>);
    }
}
";
            var src2 = @"
using System;

public interface I { int B { get; } }
public interface J { int B { get; } }

partial class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
}

partial class C : I, J
{
    int I.B { get; } = F(<N:0.1>ib => ib + 1</N:0.1>);
    int J.B { get; } = F(<N:0.2>jb => jb + 1</N:0.2>);

    public C()
    {
        F(<N:0.3>c => c + 2</N:0.3>);
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var syntaxMap = GetSyntaxMap(src1, src2);

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(), syntaxMap[0]) });
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2504")]
        public void InstanceCtor_Partial_Insert_Parameterless_LambdaInInitializer1()
        {
            var src1 = @"
using System;

partial class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
}

partial class C
{
    int B { get; } = F(<N:0.1>b => b + 1</N:0.1>);
}
";
            var src2 = @"
using System;

partial class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
}

partial class C
{
    int B { get; } = F(<N:0.1>b => b + 1</N:0.1>);

    public C()   // new ctor
    {
        F(c => c + 1);
    }
}
";
            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.InsertConstructorToTypeWithInitializersWithLambdas, "public C()"));

            // TODO: 
            //var syntaxMap = GetSyntaxMap(src1, src2);

            //edits.VerifySemantics(
            //    ActiveStatementsDescription.Empty,
            //    new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(), syntaxMap[0]) });
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2504")]
        public void InstanceCtor_Partial_Insert_WithParameters_LambdaInInitializer1()
        {
            var src1 = @"
using System;

partial class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
}

partial class C
{
    int B { get; } = F(<N:0.1>b => b + 1</N:0.1>);
}
";
            var src2 = @"
using System;

partial class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
}

partial class C
{
    int B { get; } = F(<N:0.1>b => b + 1</N:0.1>);

    public C(int x)                                 // new ctor
    {
        F(c => c + 1);
    }
}
";
            var edits = GetTopEdits(src1, src2);
            _ = GetSyntaxMap(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.InsertConstructorToTypeWithInitializersWithLambdas, "public C(int x)"));

            // TODO: bug https://github.com/dotnet/roslyn/issues/2504
            //edits.VerifySemantics(
            //    ActiveStatementsDescription.Empty,
            //    new[] { SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<NamedTypeSymbol>("C").Constructors.Single(), syntaxMap[0]) });
        }

        [Fact]
        public void InstanceCtor_Partial_Explicit_Update()
        {
            var srcA1 = @"
using System;

partial class C
{
    C(int arg) => Console.WriteLine(0);
    C(bool arg) => Console.WriteLine(1);
}
";
            var srcB1 = @"
using System;

partial class C
{
    int a <N:0.0>= 1</N:0.0>;

    C(uint arg) => Console.WriteLine(2);
}
";

            var srcA2 = @"
using System;

partial class C
{
    C(int arg) => Console.WriteLine(0);
    C(bool arg) => Console.WriteLine(1);
}
";
            var srcB2 = @"
using System;

partial class C
{
    int a <N:0.0>= 2</N:0.0>;             // updated field initializer

    C(uint arg) => Console.WriteLine(2);
    C(byte arg) => Console.WriteLine(3);  // new ctor
}
";
            var syntaxMapB = GetSyntaxMap(srcB1, srcB2)[0];

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    // No changes in document A
                    DocumentResults(),

                    DocumentResults(
                        semanticEdits: new[]
                        {
                           SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(c => c.Parameters.Single().Type.Name == "Int32"), partialType: "C", syntaxMap: syntaxMapB),
                           SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(c => c.Parameters.Single().Type.Name == "Boolean"), partialType: "C", syntaxMap: syntaxMapB),
                           SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(c => c.Parameters.Single().Type.Name == "UInt32"), partialType: "C", syntaxMap: syntaxMapB),
                           SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(c => c.Parameters.Single().Type.Name == "Byte"), syntaxMap: null),
                        })
                },
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
        }

        [Fact]
        public void InstanceCtor_Partial_Explicit_Update_SemanticError()
        {
            var srcA1 = @"
using System;

partial class C
{
    C(int arg) => Console.WriteLine(0);
    C(int arg) => Console.WriteLine(1);
}
";
            var srcB1 = @"
using System;

partial class C
{
    int a = 1;
}
";

            var srcA2 = @"
using System;

partial class C
{
    C(int arg) => Console.WriteLine(0);
    C(int arg) => Console.WriteLine(1);
}
";
            var srcB2 = @"
using System;

partial class C
{
    int a = 2;

    C(int arg) => Console.WriteLine(2);
}
";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    // No changes in document A
                    DocumentResults(),

                    // The actual edits do not matter since there are semantic errors in the compilation.
                    // We just should not crash.
                    DocumentResults(diagnostics: Array.Empty<RudeEditDiagnosticDescription>())
                });
        }

        [Fact]
        public void InstanceCtor_Partial_Implicit_Update()
        {
            var srcA1 = "partial class C { int F = 1; }";
            var srcB1 = "partial class C { int G = 1; }";

            var srcA2 = "partial class C { int F = 2; }";
            var srcB2 = "partial class C { int G = 2; }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(
                        semanticEdits: new[]
                        {
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), partialType: "C", preserveLocalVariables: true)
                        }),
                    DocumentResults(
                        semanticEdits: new[]
                        {
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), partialType: "C", preserveLocalVariables: true)
                        }),
                });
        }

        [Fact]
        public void ParameterlessConstructor_SemanticError_Delete1()
        {
            var src1 = @"
class C
{
    D() {}
}
";
            var src2 = @"
class C
{
}
";
            var edits = GetTopEdits(src1, src2);

            // The compiler interprets D() as a constructor declaration.
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingAccessibility, "class C", DeletedSymbolDisplay(FeaturesResources.constructor, "C()")));
        }

        [Fact]
        public void Constructor_SemanticError_Partial()
        {
            var src1 = @"
partial class C
{
    partial void C(int x);
}

partial class C
{
    partial void C(int x)
    {
        System.Console.WriteLine(1);
    }
}
";
            var src2 = @"
partial class C
{
    partial void C(int x);
}

partial class C
{
    partial void C(int x)
    {
        System.Console.WriteLine(2);
    }
}
";
            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IMethodSymbol>("C").PartialImplementationPart));
        }

        [Fact]
        public void PartialDeclaration_Delete()
        {
            var srcA1 = "partial class C { public C() { } void F() { } }";
            var srcB1 = "partial class C { int x = 1; }";

            var srcA2 = "";
            var srcB2 = "partial class C { int x = 2; void F() { } }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(
                        semanticEdits: new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), partialType: "C", preserveLocalVariables: true) }),

                    DocumentResults(
                        semanticEdits: new[]
                        {
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IMethodSymbol>("F")),
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), partialType: "C", preserveLocalVariables: true)
                        }),
                });
        }

        [Fact]
        public void PartialDeclaration_Insert()
        {
            var srcA1 = "";
            var srcB1 = "partial class C { int x = 1; void F() { } }";

            var srcA2 = "partial class C { public C() { } void F() { } }";
            var srcB2 = "partial class C { int x = 2; }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(
                        semanticEdits: new[]
                        {
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IMethodSymbol>("F")),
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), partialType: "C", preserveLocalVariables: true)
                        }),

                    DocumentResults(
                        semanticEdits: new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), partialType: "C", preserveLocalVariables: true) }),
                });
        }

        [Fact]
        public void PartialDeclaration_Insert_Reloadable()
        {
            var srcA1 = "";
            var srcB1 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]partial class C { int x = 1; void F() { } }";

            var srcA2 = "partial class C { public C() { } void F() { } }";
            var srcB2 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]partial class C { int x = 2; }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(
                        semanticEdits: new[]
                        {
                            SemanticEdit(SemanticEditKind.Replace, c => c.GetMember("C"), partialType: "C")
                        }),

                    DocumentResults(semanticEdits: new[]
                        {
                            SemanticEdit(SemanticEditKind.Replace, c => c.GetMember("C"), partialType: "C")
                        }),
                },
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Theory]
        [InlineData("class ")]
        [InlineData("struct")]
        public void Constructor_DeleteParameterless(string typeKind)
        {
            var src1 = @"
" + typeKind + @" C
{
    private int a = 10;
    private int b;

    public C() { b = 3; }
}
";
            var src2 = @"
" + typeKind + @" C
{
    private int a = 10;
    private int b;
}
";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Delete [public C() { b = 3; }]@66", "Delete [()]@74");

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), preserveLocalVariables: true)
                });
        }

        [Theory]
        [InlineData("class ")]
        [InlineData("struct")]
        public void Constructor_InsertParameterless(string typeKind)
        {
            var src1 = @"
" + typeKind + @" C
{
    private int a = 10;
    private int b;
}
";
            var src2 = @"
" + typeKind + @" C
{
    private int a = 10;
    private int b;

    public C() { b = 3; }
}
";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Insert [public C() { b = 3; }]@66", "Insert [()]@74");

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), preserveLocalVariables: true)
                });
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17681")]
        public void Constructor_BlockBodyToExpressionBody()
        {
            var src1 = @"
public class C
{
    private int _value;

    public C(int value) { _value = value; }
}
";
            var src2 = @"
public class C
{
    private int _value;

    public C(int value) => _value = value;
}
";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [public C(int value) { _value = value; }]@52 -> [public C(int value) => _value = value;]@52");

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), preserveLocalVariables: true)
                });
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17681")]
        public void ConstructorWithInitializer_BlockBodyToExpressionBody()
        {
            var src1 = @"
public class B { B(int value) {} }
public class C : B
{
    private int _value;
    public C(int value) : base(value) { _value = value; }
}
";
            var src2 = @"
public class B { B(int value) {} }
public class C : B
{
    private int _value;
    public C(int value) : base(value) => _value = value;
}
";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [public C(int value) : base(value) { _value = value; }]@90 -> [public C(int value) : base(value) => _value = value;]@90");

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), preserveLocalVariables: true)
                });
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17681")]
        public void Constructor_ExpressionBodyToBlockBody()
        {
            var src1 = @"
public class C
{
    private int _value;

    public C(int value) => _value = value;
}
";
            var src2 = @"
public class C
{
    private int _value;

    public C(int value) { _value = value; }
}
";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(@"Update [public C(int value) => _value = value;]@52 -> [public C(int value) { _value = value; }]@52");

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), preserveLocalVariables: true)
                });
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17681")]
        public void ConstructorWithInitializer_ExpressionBodyToBlockBody()
        {
            var src1 = @"
public class B { B(int value) {} }
public class C : B
{
    private int _value;
    public C(int value) : base(value) => _value = value;
}
";
            var src2 = @"
public class B { B(int value) {} }
public class C : B
{
    private int _value;
    public C(int value) : base(value) { _value = value; }
}
";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(@"Update [public C(int value) : base(value) => _value = value;]@90 -> [public C(int value) : base(value) { _value = value; }]@90");

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), preserveLocalVariables: true)
                });
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17681")]
        public void Destructor_BlockBodyToExpressionBody()
        {
            var src1 = @"
public class C
{
    ~C() { Console.WriteLine(0); }
}
";
            var src2 = @"
public class C
{
    ~C() => Console.WriteLine(0);
}
";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [~C() { Console.WriteLine(0); }]@25 -> [~C() => Console.WriteLine(0);]@25");

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.Finalize"), preserveLocalVariables: false)
                });
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17681")]
        public void Destructor_ExpressionBodyToBlockBody()
        {
            var src1 = @"
public class C
{
    ~C() => Console.WriteLine(0);
}
";
            var src2 = @"
public class C
{
    ~C() { Console.WriteLine(0); }
}
";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [~C() => Console.WriteLine(0);]@25 -> [~C() { Console.WriteLine(0); }]@25");

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.Finalize"), preserveLocalVariables: false)
                });
        }

        [Fact]
        public void Constructor_ReadOnlyRef_Parameter_InsertWhole()
        {
            var src1 = "class Test { }";
            var src2 = "class Test { Test(in int b) => throw null; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [Test(in int b) => throw null;]@13",
                "Insert [(in int b)]@17",
                "Insert [in int b]@18");

            edits.VerifySemanticDiagnostics(
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
        }

        [Fact]
        public void Constructor_ReadOnlyRef_Parameter_InsertParameter()
        {
            var src1 = "class Test { Test() => throw null; }";
            var src2 = "class Test { Test(in int b) => throw null; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [in int b]@18");

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMembers("Test..ctor").FirstOrDefault(m => m.GetParameterCount() == 0)?.ISymbol, deletedSymbolContainerProvider: c => c.GetMember("Test")),
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMembers("Test..ctor").FirstOrDefault(m => m.GetParameterCount() == 1)?.ISymbol)
                },
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.InsertNotSupportedByRuntime, "in int b", FeaturesResources.parameter) },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void Constructor_ReadOnlyRef_Parameter_Update()
        {
            var src1 = "class Test { Test(int b) => throw null; }";
            var src2 = "class Test { Test(in int b) => throw null; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [int b]@18 -> [in int b]@18");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "in int b", FeaturesResources.parameter));
        }

        #endregion

        #region Fields and Properties with Initializers

        [Theory]
        [InlineData("class ")]
        [InlineData("struct")]
        public void FieldInitializer_Update1(string typeKind)
        {
            var src1 = typeKind + " C { int a = 0; }";
            var src2 = typeKind + " C { int a = 1; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [a = 0]@15 -> [a = 1]@15");

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), preserveLocalVariables: true) });
        }

        [Theory]
        [InlineData("class ")]
        [InlineData("struct")]
        public void PropertyInitializer_Update1(string typeKind)
        {
            var src1 = typeKind + " C { int a { get; } = 0; }";
            var src2 = typeKind + " C { int a { get; } = 1; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [int a { get; } = 0;]@11 -> [int a { get; } = 1;]@11");

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), preserveLocalVariables: true) });
        }

        [Fact]
        public void FieldInitializer_Update2()
        {
            var src1 = "class C { int a = 0; }";
            var src2 = "class C { int a; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [a = 0]@14 -> [a]@14");

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void PropertyInitializer_Update2()
        {
            var src1 = "class C { int a { get; } = 0; }";
            var src2 = "class C { int a { get { return 1; } } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [int a { get; } = 0;]@10 -> [int a { get { return 1; } }]@10",
                "Update [get;]@18 -> [get { return 1; }]@18");

            edits.VerifySemantics(
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("C.a").GetMethod),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(), preserveLocalVariables: true));
        }

        [Fact]
        public void PropertyInitializer_InsertDelete()
        {
            var srcA1 = "partial class C { }";
            var srcB1 = "partial class C { int a { get; } = 0; }";
            var srcA2 = "partial class C { int a { get { return 1; } } }";
            var srcB2 = "partial class C { }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(
                        semanticEdits: new[]
                        {
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("C.a").GetMethod),
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(), partialType: "C", preserveLocalVariables: true)
                        }),
                    DocumentResults()
                });
        }

        [Fact]
        public void FieldInitializer_Update3()
        {
            var src1 = "class C { int a; }";
            var src2 = "class C { int a = 0; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [a]@14 -> [a = 0]@14");

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), preserveLocalVariables: true) });
        }

        [Fact]
        public void PropertyInitializer_Update3()
        {
            var src1 = "class C { int a { get { return 1; } } }";
            var src2 = "class C { int a { get; } = 0; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [int a { get { return 1; } }]@10 -> [int a { get; } = 0;]@10",
                "Update [get { return 1; }]@18 -> [get;]@18");

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IPropertySymbol>("C.a").GetMethod),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), preserveLocalVariables: true)
                });
        }

        [Fact]
        public void FieldInitializerUpdate_StaticCtorUpdate1()
        {
            var src1 = "class C { static int a; static C() { } }";
            var src2 = "class C { static int a = 0; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [a]@21 -> [a = 0]@21",
                "Delete [static C() { }]@24",
                "Delete [()]@32");

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").StaticConstructors.Single(), preserveLocalVariables: true) });
        }

        [Fact]
        public void PropertyInitializerUpdate_StaticCtorUpdate1()
        {
            var src1 = "class C { static int a { get; } = 1; static C() { } }";
            var src2 = "class C { static int a { get; } = 2;}";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").StaticConstructors.Single(), preserveLocalVariables: true) });
        }

        [Theory]
        [InlineData("class ")]
        [InlineData("struct")]
        public void FieldInitializerUpdate_InstanceCtorUpdate_Private(string typeKind)
        {
            var src1 = typeKind + " C { int a; [System.Obsolete]C() { } }";
            var src2 = typeKind + " C { int a = 0; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                new[]
                {
                    Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, $"{typeKind} C", DeletedSymbolDisplay(FeaturesResources.constructor, "C()")),
                    Diagnostic(RudeEditKind.ChangingAccessibility, $"{typeKind} C", DeletedSymbolDisplay(FeaturesResources.constructor, "C()"))
                },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Theory]
        [InlineData("class ")]
        [InlineData("struct")]
        public void PropertyInitializerUpdate_InstanceCtorUpdate_Private(string typeKind)
        {
            var src1 = typeKind + " C { int a { get; } = 1; C() { } }";
            var src2 = typeKind + " C { int a { get; } = 2; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingAccessibility, $"{typeKind} C", DeletedSymbolDisplay(FeaturesResources.constructor, "C()")));
        }

        [Theory]
        [InlineData("class ")]
        [InlineData("struct")]
        public void FieldInitializerUpdate_InstanceCtorUpdate_Public(string typeKind)
        {
            var src1 = typeKind + " C { int a; public C() { } }";
            var src2 = typeKind + " C { int a = 0; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), preserveLocalVariables: true) });
        }

        [Theory]
        [InlineData("class ")]
        [InlineData("struct")]
        public void PropertyInitializerUpdate_InstanceCtorUpdate_Public(string typeKind)
        {
            var src1 = typeKind + " C { int a { get; } = 1; public C() { } }";
            var src2 = typeKind + " C { int a { get; } = 2; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), preserveLocalVariables: true) });
        }

        [Fact]
        public void FieldInitializerUpdate_StaticCtorUpdate2()
        {
            var src1 = "class C { static int a; static C() { } }";
            var src2 = "class C { static int a = 0; static C() { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [a]@21 -> [a = 0]@21");

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").StaticConstructors.Single(), preserveLocalVariables: true) });
        }

        [Fact]
        public void PropertyInitializerUpdate_StaticCtorUpdate2()
        {
            var src1 = "class C { static int a { get; } = 1; static C() { } }";
            var src2 = "class C { static int a { get; } = 2; static C() { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").StaticConstructors.Single(), preserveLocalVariables: true) });
        }

        [Theory]
        [InlineData("class ")]
        [InlineData("struct")]
        public void FieldInitializerUpdate_InstanceCtorUpdate2(string typeKind)
        {
            var src1 = typeKind + " C { int a; public C() { } }";
            var src2 = typeKind + " C { int a = 0; public C() { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [a]@15 -> [a = 0]@15");

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), preserveLocalVariables: true) });
        }

        [Theory]
        [InlineData("class ")]
        [InlineData("struct")]
        public void PropertyInitializerUpdate_InstanceCtorUpdate2(string typeKind)
        {
            var src1 = typeKind + " C { int a { get; } = 1; public C() { } }";
            var src2 = typeKind + " C { int a { get; } = 2; public C() { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), preserveLocalVariables: true) });
        }

        [Fact]
        public void FieldInitializerUpdate_InstanceCtorUpdate3()
        {
            var src1 = "class C { int a; }";
            var src2 = "class C { int a = 0; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [a]@14 -> [a = 0]@14");

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), preserveLocalVariables: true) });
        }

        [Fact]
        public void PropertyInitializerUpdate_InstanceCtorUpdate3()
        {
            var src1 = "class C { int a { get; } = 1; }";
            var src2 = "class C { int a { get; } = 2; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), preserveLocalVariables: true) });
        }

        [Fact]
        public void FieldInitializerUpdate_InstanceCtorUpdate4()
        {
            var src1 = "class C { int a = 0; }";
            var src2 = "class C { int a; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [a = 0]@14 -> [a]@14");

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), preserveLocalVariables: true) });
        }

        [Fact]
        public void FieldInitializerUpdate_InstanceCtorUpdate5()
        {
            var src1 = "class C { int a;     private C(int a) { }    private C(bool a) { } }";
            var src2 = "class C { int a = 0; private C(int a) { } private C(bool a) { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [a]@14 -> [a = 0]@14");

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(m => m.ToString() == "C.C(int)"), preserveLocalVariables: true),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(m => m.ToString() == "C.C(bool)"), preserveLocalVariables: true),
                });
        }

        [Fact]
        public void FieldInitializerUpdate_Struct_InstanceCtorUpdate5()
        {
            var src1 = "struct C { int a;     private C(int a) { } private C(bool a) { } }";
            var src2 = "struct C { int a = 0; private C(int a) { } private C(bool a) { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [a]@15 -> [a = 0]@15");

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(m => m.ToString() == "C.C(int)"), preserveLocalVariables: true),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(m => m.ToString() == "C.C(bool)"), preserveLocalVariables: true),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(m => m.ToString() == "C.C()"), preserveLocalVariables: true),
                });
        }

        [Fact]
        public void PropertyInitializerUpdate_InstanceCtorUpdate5()
        {
            var src1 = "class C { int a { get; } = 1;     private C(int a) { }    private C(bool a) { } }";
            var src2 = "class C { int a { get; } = 10000; private C(int a) { } private C(bool a) { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(m => m.ToString() == "C.C(int)"), preserveLocalVariables: true),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(m => m.ToString() == "C.C(bool)"), preserveLocalVariables: true),
                });
        }

        [Fact]
        public void PropertyInitializerUpdate_Struct_InstanceCtorUpdate5()
        {
            var src1 = "struct C { int a { get; } = 1;     private C(int a) { } private C(bool a) { } }";
            var src2 = "struct C { int a { get; } = 10000; private C(int a) { } private C(bool a) { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(m => m.ToString() == "C.C(int)"), preserveLocalVariables: true),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(m => m.ToString() == "C.C(bool)"), preserveLocalVariables: true),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(m => m.ToString() == "C.C()"), preserveLocalVariables: true),
                });
        }

        [Fact]
        public void FieldInitializerUpdate_InstanceCtorUpdate6()
        {
            var src1 = "class C { int a;     private C(int a) : this(true) { } private C(bool a) { } }";
            var src2 = "class C { int a = 0; private C(int a) : this(true) { } private C(bool a) { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [a]@14 -> [a = 0]@14");

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(m => m.ToString() == "C.C(bool)"), preserveLocalVariables: true)
                });
        }

        [Fact]
        public void FieldInitializerUpdate_StaticCtorInsertImplicit()
        {
            var src1 = "class C { static int a; }";
            var src2 = "class C { static int a = 0; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [a]@21 -> [a = 0]@21");

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[] { SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<INamedTypeSymbol>("C").StaticConstructors.Single()) });
        }

        [Fact]
        public void FieldInitializerUpdate_StaticCtorInsertExplicit()
        {
            var src1 = "class C { static int a; }";
            var src2 = "class C { static int a = 0; static C() { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [static C() { }]@28",
                "Insert [()]@36",
                "Update [a]@21 -> [a = 0]@21");

            edits.VerifySemantics(
                new[] { SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<INamedTypeSymbol>("C").StaticConstructors.Single()) },
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
        }

        [Theory]
        [InlineData("class ")]
        [InlineData("struct")]
        public void FieldInitializerUpdate_InstanceCtorInsertExplicit(string typeKind)
        {
            var src1 = typeKind + " C { int a; }";
            var src2 = typeKind + " C { int a = 0; public C() { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), preserveLocalVariables: true) });
        }

        [Theory]
        [InlineData("class ")]
        [InlineData("struct")]
        public void PropertyInitializerUpdate_InstanceCtorInsertExplicit(string typeKind)
        {
            var src1 = typeKind + " C { int a { get; } = 1; }";
            var src2 = typeKind + " C { int a { get; } = 2; public C() { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), preserveLocalVariables: true) });
        }

        [Fact]
        public void FieldInitializerUpdate_GenericType()
        {
            var src1 = "class C<T> { int a = 1; }";
            var src2 = "class C<T> { int a = 2; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [a = 1]@17 -> [a = 2]@17");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.GenericTypeUpdate, "a = 2"),
                Diagnostic(RudeEditKind.GenericTypeUpdate, "class C<T>"));
        }

        [Fact]
        public void PropertyInitializerUpdate_GenericType()
        {
            var src1 = "class C<T> { int a { get; } = 1; }";
            var src2 = "class C<T> { int a { get; } = 2; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.GenericTypeUpdate, "int a"),
                Diagnostic(RudeEditKind.GenericTypeUpdate, "class C<T>"));
        }

        [Fact]
        public void FieldInitializerUpdate_StackAllocInConstructor()
        {
            var src1 = "unsafe class C { int a = 1; public C() { int* a = stackalloc int[10]; } }";
            var src2 = "unsafe class C { int a = 2; public C() { int* a = stackalloc int[10]; } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [a = 1]@21 -> [a = 2]@21");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.StackAllocUpdate, "stackalloc", FeaturesResources.constructor));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37172")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/43099")]
        public void FieldInitializerUpdate_SwitchExpressionInConstructor()
        {
            var src1 = "class C { int a = 1; public C() { var b = a switch { 0 => 0, _ => 1 }; } }";
            var src2 = "class C { int a = 2; public C() { var b = a switch { 0 => 0, _ => 1 }; } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void PropertyInitializerUpdate_StackAllocInConstructor1()
        {
            var src1 = "unsafe class C { int a { get; } = 1; public C() { int* a = stackalloc int[10]; } }";
            var src2 = "unsafe class C { int a { get; } = 2; public C() { int* a = stackalloc int[10]; } }";

            var edits = GetTopEdits(src1, src2);

            // TODO (tomat): diagnostic should point to the property initializer
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.StackAllocUpdate, "stackalloc", FeaturesResources.constructor));
        }

        [Fact]
        public void PropertyInitializerUpdate_StackAllocInConstructor2()
        {
            var src1 = "unsafe class C { int a { get; } = 1; public C() : this(1) { int* a = stackalloc int[10]; } public C(int a) { } }";
            var src2 = "unsafe class C { int a { get; } = 2; public C() : this(1) { int* a = stackalloc int[10]; } public C(int a) { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void PropertyInitializerUpdate_StackAllocInConstructor3()
        {
            var src1 = "unsafe class C { int a { get; } = 1; public C() { } public C(int b) { int* a = stackalloc int[10]; } }";
            var src2 = "unsafe class C { int a { get; } = 2; public C() { } public C(int b) { int* a = stackalloc int[10]; } }";

            var edits = GetTopEdits(src1, src2);

            // TODO (tomat): diagnostic should point to the property initializer
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.StackAllocUpdate, "stackalloc", FeaturesResources.constructor));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37172")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/43099")]
        public void PropertyInitializerUpdate_SwitchExpressionInConstructor1()
        {
            var src1 = "class C { int a { get; } = 1; public C() { var b = a switch { 0 => 0, _ => 1 }; } }";
            var src2 = "class C { int a { get; } = 2; public C() { var b = a switch { 0 => 0, _ => 1 }; } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37172")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/43099")]
        public void PropertyInitializerUpdate_SwitchExpressionInConstructor2()
        {
            var src1 = "class C { int a { get; } = 1; public C() : this(1) { var b = a switch { 0 => 0, _ => 1 }; } public C(int a) { } }";
            var src2 = "class C { int a { get; } = 2; public C() : this(1) { var b = a switch { 0 => 0, _ => 1 }; } public C(int a) { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37172")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/43099")]
        public void PropertyInitializerUpdate_SwitchExpressionInConstructor3()
        {
            var src1 = "class C { int a { get; } = 1; public C() { } public C(int b) { var b = a switch { 0 => 0, _ => 1 }; } }";
            var src2 = "class C { int a { get; } = 2; public C() { } public C(int b) { var b = a switch { 0 => 0, _ => 1 }; } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void FieldInitializerUpdate_LambdaInConstructor()
        {
            var src1 = "class C { int a = 1; public C() { F(() => {}); } static void F(System.Action a) {} }";
            var src2 = "class C { int a = 2; public C() { F(() => {}); } static void F(System.Action a) {} }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [a = 1]@14 -> [a = 2]@14");

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void PropertyInitializerUpdate_LambdaInConstructor()
        {
            var src1 = "class C { int a { get; } = 1; public C() { F(() => {}); } static void F(System.Action a) {} }";
            var src2 = "class C { int a { get; } = 2; public C() { F(() => {}); } static void F(System.Action a) {} }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void FieldInitializerUpdate_QueryInConstructor()
        {
            var src1 = "using System.Linq; class C { int a = 1; public C() { F(from a in new[] {1,2,3} select a + 1); } static void F(System.Collections.Generic.IEnumerable<int> x) {} }";
            var src2 = "using System.Linq; class C { int a = 2; public C() { F(from a in new[] {1,2,3} select a + 1); } static void F(System.Collections.Generic.IEnumerable<int> x) {} }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [a = 1]@33 -> [a = 2]@33");

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void PropertyInitializerUpdate_QueryInConstructor()
        {
            var src1 = "using System.Linq; class C { int a { get; } = 1; public C() { F(from a in new[] {1,2,3} select a + 1); } static void F(System.Collections.Generic.IEnumerable<int> x) {} }";
            var src2 = "using System.Linq; class C { int a { get; } = 2; public C() { F(from a in new[] {1,2,3} select a + 1); } static void F(System.Collections.Generic.IEnumerable<int> x) {} }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void FieldInitializerUpdate_AnonymousTypeInConstructor()
        {
            var src1 = "class C { int a = 1; C() { F(new { A = 1, B = 2 }); } static void F(object x) {} }";
            var src2 = "class C { int a = 2; C() { F(new { A = 1, B = 2 }); } static void F(object x) {} }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void PropertyInitializerUpdate_AnonymousTypeInConstructor()
        {
            var src1 = "class C { int a { get; } = 1; C() { F(new { A = 1, B = 2 }); } static void F(object x) {} }";
            var src2 = "class C { int a { get; } = 2; C() { F(new { A = 1, B = 2 }); } static void F(object x) {} }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void FieldInitializerUpdate_PartialTypeWithSingleDeclaration()
        {
            var src1 = "partial class C { int a = 1; }";
            var src2 = "partial class C { int a = 2; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [a = 1]@22 -> [a = 2]@22");

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(), preserveLocalVariables: true)
                });
        }

        [Fact]
        public void PropertyInitializerUpdate_PartialTypeWithSingleDeclaration()
        {
            var src1 = "partial class C { int a { get; } = 1; }";
            var src2 = "partial class C { int a { get; } = 2; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(), preserveLocalVariables: true)
                });
        }

        [Fact]
        public void FieldInitializerUpdate_PartialTypeWithMultipleDeclarations()
        {
            var src1 = "partial class C { int a = 1; } partial class C { }";
            var src2 = "partial class C { int a = 2; } partial class C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [a = 1]@22 -> [a = 2]@22");

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(), preserveLocalVariables: true)
                });
        }

        [Fact]
        public void PropertyInitializerUpdate_PartialTypeWithMultipleDeclarations()
        {
            var src1 = "partial class C { int a { get; } = 1; } partial class C { }";
            var src2 = "partial class C { int a { get; } = 2; } partial class C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(), preserveLocalVariables: true)
                });
        }

        [Fact]
        public void FieldInitializerUpdate_ParenthesizedLambda()
        {
            var src1 = "class C { int a = F(1, (x, y) => x + y); }";
            var src2 = "class C { int a = F(2, (x, y) => x + y); }";

            var edits = GetTopEdits(src1, src2);
            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void PropertyInitializerUpdate_ParenthesizedLambda()
        {
            var src1 = "class C { int a { get; } = F(1, (x, y) => x + y); }";
            var src2 = "class C { int a { get; } = F(2, (x, y) => x + y); }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void FieldInitializerUpdate_SimpleLambda()
        {
            var src1 = "class C { int a = F(1, x => x); }";
            var src2 = "class C { int a = F(2, x => x); }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void PropertyInitializerUpdate_SimpleLambda()
        {
            var src1 = "class C { int a { get; } = F(1, x => x); }";
            var src2 = "class C { int a { get; } = F(2, x => x); }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void FieldInitializerUpdate_Query()
        {
            var src1 = "class C { int a = F(1, from goo in bar select baz); }";
            var src2 = "class C { int a = F(2, from goo in bar select baz); }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void PropertyInitializerUpdate_Query()
        {
            var src1 = "class C { int a { get; } = F(1, from goo in bar select baz); }";
            var src2 = "class C { int a { get; } = F(2, from goo in bar select baz); }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void FieldInitializerUpdate_AnonymousType()
        {
            var src1 = "class C { int a = F(1, new { A = 1, B = 2 }); }";
            var src2 = "class C { int a = F(2, new { A = 1, B = 2 }); }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void PropertyInitializerUpdate_AnonymousType()
        {
            var src1 = "class C { int a { get; } = F(1, new { A = 1, B = 2 }); }";
            var src2 = "class C { int a { get; } = F(2, new { A = 1, B = 2 }); }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void FieldInitializerUpdate_Lambdas_ImplicitCtor_EditInitializerWithLambda1()
        {
            var src1 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
    int B = F(<N:0.1>b => b + 1</N:0.1>);
}
";
            var src2 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
    int B = F(<N:0.1>b => b + 2</N:0.1>);
}
";
            var edits = GetTopEdits(src1, src2);
            var syntaxMap = GetSyntaxMap(src1, src2);

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(), syntaxMap[0]) });
        }

        [Fact]
        public void FieldInitializerUpdate_Lambdas_ImplicitCtor_EditInitializerWithoutLambda1()
        {
            var src1 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = 1;
    int B = F(<N:0.0>b => b + 1</N:0.0>);
}
";
            var src2 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = 2;
    int B = F(<N:0.0>b => b + 1</N:0.0>);
}
";
            var edits = GetTopEdits(src1, src2);
            var syntaxMap = GetSyntaxMap(src1, src2);

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(), syntaxMap[0]) });
        }

        [Fact]
        public void FieldInitializerUpdate_Lambdas_CtorIncludingInitializers_EditInitializerWithLambda1()
        {
            var src1 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
    int B = F(<N:0.1>b => b + 1</N:0.1>);

    public C() {}
}
";
            var src2 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
    int B = F(<N:0.1>b => b + 2</N:0.1>);

    public C() {}
}
";
            var edits = GetTopEdits(src1, src2);
            var syntaxMap = GetSyntaxMap(src1, src2);

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(), syntaxMap[0]) });
        }

        [Fact]
        public void FieldInitializerUpdate_Lambdas_CtorIncludingInitializers_EditInitializerWithoutLambda1()
        {
            var src1 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = 1;
    int B = F(<N:0.0>b => b + 1</N:0.0>);

    public C() {}
}
";
            var src2 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = 2;
    int B = F(<N:0.0>b => b + 1</N:0.0>);

    public C() {}
}
";
            var edits = GetTopEdits(src1, src2);
            var syntaxMap = GetSyntaxMap(src1, src2);

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(), syntaxMap[0]) });
        }

        [Fact]
        public void FieldInitializerUpdate_Lambdas_MultipleCtorsIncludingInitializers_EditInitializerWithLambda1()
        {
            var src1 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
    int B = F(<N:0.1>b => b + 1</N:0.1>);

    public C(int a) {}
    public C(bool b) {}
}
";
            var src2 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
    int B = F(<N:0.1>b => b + 2</N:0.1>);

    public C(int a) {}
    public C(bool b) {}
}
";
            var edits = GetTopEdits(src1, src2);
            var syntaxMap = GetSyntaxMap(src1, src2);

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors[0], syntaxMap[0]),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors[1], syntaxMap[0])
                });
        }

        [Fact]
        public void FieldInitializerUpdate_Lambdas_MultipleCtorsIncludingInitializersContainingLambdas_EditInitializerWithLambda1()
        {
            var src1 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
    int B = F(<N:0.1>b => b + 1</N:0.1>);

    public C(int a) { F(<N:0.2>c => c + 1</N:0.2>); }
    public C(bool b) { F(<N:0.3>d => d + 1</N:0.3>); }
}
";
            var src2 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
    int B = F(<N:0.1>b => b + 2</N:0.1>);

    public C(int a) { F(<N:0.2>c => c + 1</N:0.2>); }
    public C(bool b) { F(<N:0.3>d => d + 1</N:0.3>); }
}
";
            var edits = GetTopEdits(src1, src2);
            var syntaxMap = GetSyntaxMap(src1, src2);

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors[0], syntaxMap[0]),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors[1], syntaxMap[0])
                });
        }

        [Fact]
        public void FieldInitializerUpdate_Lambdas_MultipleCtorsIncludingInitializersContainingLambdas_EditInitializerWithLambda_Trivia1()
        {
            var src1 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
    int B = F(<N:0.1>b => b + 1</N:0.1>);

    public C(int a) { F(<N:0.2>c => c + 1</N:0.2>); }
    public C(bool b) { F(<N:0.3>d => d + 1</N:0.3>); }
}
";
            var src2 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
    int B =   F(<N:0.1>b => b + 1</N:0.1>);

    public C(int a) { F(<N:0.2>c => c + 1</N:0.2>); }
    public C(bool b) { F(<N:0.3>d => d + 1</N:0.3>); }
}
";
            var edits = GetTopEdits(src1, src2);
            var syntaxMap = GetSyntaxMap(src1, src2);

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors[0], syntaxMap[0]),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors[1], syntaxMap[0])
                });
        }

        [Fact]
        public void FieldInitializerUpdate_Lambdas_MultipleCtorsIncludingInitializersContainingLambdas_EditConstructorWithLambda1()
        {
            var src1 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
    int B = F(<N:0.1>b => b + 1</N:0.1>);

    public C(int a) { F(<N:0.2>c => c + 1</N:0.2>); }
    public C(bool b) { F(d => d + 1); }
}
";
            var src2 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
    int B = F(<N:0.1>b => b + 1</N:0.1>);

    public C(int a) { F(<N:0.2>c => c + 2</N:0.2>); }
    public C(bool b) { F(d => d + 1); }
}
";
            var edits = GetTopEdits(src1, src2);
            var syntaxMap = GetSyntaxMap(src1, src2);

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(ctor => ctor.ToTestDisplayString() == "C..ctor(System.Int32 a)"), syntaxMap[0])
                });
        }

        [Fact]
        public void FieldInitializerUpdate_Lambdas_MultipleCtorsIncludingInitializersContainingLambdas_EditConstructorWithLambda_Trivia1()
        {
            var src1 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
    int B = F(<N:0.1>b => b + 1</N:0.1>);

    public C(int a) { F(<N:0.2>c => c + 1</N:0.2>); }
    public C(bool b) { F(d => d + 1); }
}
";
            var src2 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
    int B = F(<N:0.1>b => b + 1</N:0.1>);

        public C(int a) { F(<N:0.2>c => c + 1</N:0.2>); }
    public C(bool b) { F(d => d + 1); }
}
";
            var edits = GetTopEdits(src1, src2);
            var syntaxMap = GetSyntaxMap(src1, src2);

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(ctor => ctor.ToTestDisplayString() == "C..ctor(System.Int32 a)"), syntaxMap[0])
                });
        }

        [Fact]
        public void FieldInitializerUpdate_Lambdas_MultipleCtorsIncludingInitializersContainingLambdas_EditConstructorWithoutLambda1()
        {
            var src1 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
    int B = F(<N:0.1>b => b + 1</N:0.1>);

    public C(int a) { F(c => c + 1); }
    public C(bool b) { Console.WriteLine(1); }
}
";
            var src2 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
    int B = F(<N:0.1>b => b + 1</N:0.1>);

    public C(int a) { F(c => c + 1); }
    public C(bool b) { Console.WriteLine(2); }
}
";
            var edits = GetTopEdits(src1, src2);
            var syntaxMap = GetSyntaxMap(src1, src2);

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(ctor => ctor.ToTestDisplayString() == "C..ctor(System.Boolean b)"), syntaxMap[0])
                });
        }

        [Fact]
        public void FieldInitializerUpdate_Lambdas_EditConstructorNotIncludingInitializers()
        {
            var src1 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(a => a + 1);
    int B = F(b => b + 1);

    public C(int a) { F(c => c + 1); }
    public C(bool b) : this(1) { Console.WriteLine(1); }
}
";
            var src2 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(a => a + 1);
    int B = F(b => b + 1);

    public C(int a) { F(c => c + 1); }
    public C(bool b) : this(1) { Console.WriteLine(2); }
}
";
            var edits = GetTopEdits(src1, src2);
            var syntaxMap = GetSyntaxMap(src1, src2);

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(ctor => ctor.ToTestDisplayString() == "C..ctor(System.Boolean b)"))
                });
        }

        [Fact]
        public void FieldInitializerUpdate_Lambdas_RemoveCtorInitializer1()
        {
            var src1 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
    int B = F(<N:0.1>b => b + 1</N:0.1>);

    unsafe public C(int a) { char* buffer = stackalloc char[16]; F(c => c + 1); }
    public C(bool b) : this(1) { Console.WriteLine(1); }
}
";
            var src2 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
    int B = F(<N:0.1>b => b + 1</N:0.1>);

    unsafe public C(int a) { char* buffer = stackalloc char[16]; F(c => c + 1); }
    public C(bool b) { Console.WriteLine(1); }
}
";
            var edits = GetTopEdits(src1, src2);
            var syntaxMap = GetSyntaxMap(src1, src2);

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(ctor => ctor.ToTestDisplayString() == "C..ctor(System.Boolean b)"), syntaxMap[0])
                });
        }

        [Fact]
        public void FieldInitializerUpdate_Lambdas_AddCtorInitializer1()
        {
            var src1 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(a => a + 1);
    int B = F(b => b + 1);

    public C(int a) { F(c => c + 1); }
    public C(bool b) { Console.WriteLine(1); }
}
";
            var src2 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(a => a + 1);
    int B = F(b => b + 1);

    public C(int a) { F(c => c + 1); }
    public C(bool b) : this(1) { Console.WriteLine(1); }
}
";
            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(ctor => ctor.ToTestDisplayString() == "C..ctor(System.Boolean b)"))
                });
        }

        [Fact]
        public void FieldInitializerUpdate_Lambdas_UpdateBaseCtorInitializerWithLambdas1()
        {
            var src1 = @"
using System;

class B
{
    public B(int a) { }
}

class C : B
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
    int B = F(<N:0.1>b => b + 1</N:0.1>);

    public C(bool b)
      : base(F(<N:0.2>c => c + 1</N:0.2>))
    { 
        F(<N:0.3>d => d + 1</N:0.3>);
    }
}
";
            var src2 = @"
using System;

class B
{
    public B(int a) { }
}

class C : B
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
    int B = F(<N:0.1>b => b + 1</N:0.1>);

    public C(bool b)
      : base(F(<N:0.2>c => c + 2</N:0.2>))
    {
        F(<N:0.3>d => d + 1</N:0.3>);
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var syntaxMap = GetSyntaxMap(src1, src2);

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(ctor => ctor.ToTestDisplayString() == "C..ctor(System.Boolean b)"), syntaxMap[0])
                });
        }

        [Fact]
        public void FieldInitializerUpdate_Lambdas_PartialDeclarationDelete_SingleDocument()
        {
            var src1 = @"
partial class C
{
    int x = F(<N:0.0>a => a + 1</N:0.0>);
}

partial class C
{
    int y = F(<N:0.1>a => a + 10</N:0.1>);
}

partial class C
{
    public C() { }
    static int F(Func<int, int> x) => 1;
}
";

            var src2 = @"
partial class C
{
    int x = F(<N:0.0>a => a + 1</N:0.0>);
}

partial class C
{
    int y = F(<N:0.1>a => a + 10</N:0.1>);

    static int F(Func<int, int> x) => 1;
}
";
            var edits = GetTopEdits(src1, src2);

            var syntaxMap = GetSyntaxMap(src1, src2);

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember("F")),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), syntaxMap[0]),
                });
        }

        [Fact]
        public void FieldInitializerUpdate_ActiveStatements1()
        {
            var src1 = @"
using System;

class C
{
    <AS:0>int A = <N:0.0>1</N:0.0>;</AS:0>
    int B = 1;

    public C(int a) { Console.WriteLine(1); }
    public C(bool b) { Console.WriteLine(1); }
}
";
            var src2 = @"
using System;

class C
{
    <AS:0>int A = <N:0.0>1</N:0.0>;</AS:0>
    int B = 2;

    public C(int a) { Console.WriteLine(1); }
    public C(bool b) { Console.WriteLine(1); }
}
";
            var edits = GetTopEdits(src1, src2);
            var syntaxMap = GetSyntaxMap(src1, src2);
            var activeStatements = GetActiveStatements(src1, src2);

            edits.VerifySemantics(
                activeStatements,
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors[0], syntaxMap[0]),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors[1], syntaxMap[0]),
                });
        }

        [Fact]
        public void PropertyWithInitializer_SemanticError_Partial()
        {
            var src1 = @"
partial class C
{
    partial int P => 1;
}

partial class C
{
    partial int P => 1;
}
";
            var src2 = @"
partial class C
{
    partial int P => 1;
}

partial class C
{
    partial int P => 2;

    public C() { }
}
";
            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                SemanticEdit(SemanticEditKind.Update, c => ((IPropertySymbol)c.GetMember<INamedTypeSymbol>("C").GetMembers("P").First()).GetMethod),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), preserveLocalVariables: true));
        }

        [Fact]
        public void Field_Partial_DeleteInsert_InitializerRemoval()
        {
            var srcA1 = "partial class C { int F = 1; }";
            var srcB1 = "partial class C { }";

            var srcA2 = "partial class C {  }";
            var srcB2 = "partial class C { int F; }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(),
                    DocumentResults(
                        semanticEdits: new[]
                        {
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), partialType: "C", preserveLocalVariables: true)
                        }),
                });
        }

        [Fact]
        public void Field_Partial_DeleteInsert_InitializerUpdate()
        {
            var srcA1 = "partial class C { int F = 1; }";
            var srcB1 = "partial class C { }";

            var srcA2 = "partial class C {  }";
            var srcB2 = "partial class C { int F = 2; }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(),
                    DocumentResults(
                        semanticEdits: new[]
                        {
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), partialType: "C", preserveLocalVariables: true)
                        }),
                });
        }

        #endregion

        #region Fields

        [Fact]
        public void Field_Rename()
        {
            var src1 = "class C { int a = 0; }";
            var src2 = "class C { int b = 0; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [a = 0]@14 -> [b = 0]@14");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "b = 0", FeaturesResources.field));
        }

        [Fact]
        public void Field_Kind_Update()
        {
            var src1 = "class C { Action a; }";
            var src2 = "class C { event Action a; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [Action a;]@10 -> [event Action a;]@10");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.FieldKindUpdate, "event Action a", FeaturesResources.event_));
        }

        [Theory]
        [InlineData("static")]
        [InlineData("volatile")]
        [InlineData("const")]
        public void Field_Modifiers_Update(string oldModifiers, string newModifiers = "")
        {
            if (oldModifiers != "")
            {
                oldModifiers += " ";
            }

            if (newModifiers != "")
            {
                newModifiers += " ";
            }

            var src1 = "class C { " + oldModifiers + "int F = 0; }";
            var src2 = "class C { " + newModifiers + "int F = 0; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [" + oldModifiers + "int F = 0;]@10 -> [" + newModifiers + "int F = 0;]@10");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, newModifiers + "int F = 0", FeaturesResources.field));
        }

        [Fact]
        public void Field_Modifier_Add_InsertDelete()
        {
            var srcA1 = "partial class C { }";
            var srcB1 = "partial class C { int F; }";

            var srcA2 = "partial class C { static int F; }";
            var srcB2 = "partial class C { }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(
                        diagnostics: new[]
                        {
                            Diagnostic(RudeEditKind.ModifiersUpdate, "F", FeaturesResources.field)
                        }),

                    DocumentResults(),
                });
        }

        [Fact]
        public void Field_Attribute_Add_InsertDelete()
        {
            var srcA1 = "partial class C { }";
            var srcB1 = "partial class C { int F; }";

            var srcA2 = "partial class C { [System.Obsolete]int F; }";
            var srcB2 = "partial class C { }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(
                        semanticEdits: new[]
                        {
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.F"))
                        }),

                    DocumentResults(),
                },
                capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
        }

        [Fact]
        public void Field_FixedSize_Update()
        {
            var src1 = "struct S { public unsafe fixed byte a[1], b[2]; }";
            var src2 = "struct S { public unsafe fixed byte a[2], b[3]; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [a[1]]@36 -> [a[2]]@36",
                "Update [b[2]]@42 -> [b[3]]@42");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.FixedSizeFieldUpdate, "a[2]", FeaturesResources.field),
                Diagnostic(RudeEditKind.FixedSizeFieldUpdate, "b[3]", FeaturesResources.field));
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1120407")]
        public void Field_Const_Update()
        {
            var src1 = "class C { const int x = 0; }";
            var src2 = "class C { const int x = 1; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [x = 0]@20 -> [x = 1]@20");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.InitializerUpdate, "x = 1", FeaturesResources.const_field));
        }

        [Fact]
        public void Field_Event_VariableDeclarator_Update()
        {
            var src1 = "class C { event Action a; }";
            var src2 = "class C { event Action a = () => { }; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [a]@23 -> [a = () => { }]@23");

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void Field_Reorder()
        {
            var src1 = "class C { int a = 0; int b = 1; int c = 2; }";
            var src2 = "class C { int c = 2; int a = 0; int b = 1; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Reorder [int c = 2;]@32 -> @10");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Move, "int c = 2", FeaturesResources.field));
        }

        [Fact]
        public void Field_Insert()
        {
            var src1 = "class C {  }";
            var src2 = "class C { int a = 1; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [int a = 1;]@10",
                "Insert [int a = 1]@10",
                "Insert [a = 1]@14");

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.a")),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), preserveLocalVariables: true)
                },
                capabilities: EditAndContinueCapabilities.AddInstanceFieldToExistingType);
        }

        [Fact]
        public void Field_Insert_IntoStruct()
        {
            var src1 = @"
struct S 
{ 
    public int a; 

    public S(int z) { this = default(S); a = z; }
}
";
            var src2 = @"
struct S 
{ 
    public int a; 

    private int b; 
    private static int c; 
    private static int f = 1;
    private event System.Action d; 

    public S(int z) { this = default(S); a = z; }
}
";
            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.InsertIntoStruct, "b", FeaturesResources.field, CSharpFeaturesResources.struct_),
                Diagnostic(RudeEditKind.InsertIntoStruct, "c", FeaturesResources.field, CSharpFeaturesResources.struct_),
                Diagnostic(RudeEditKind.InsertIntoStruct, "f = 1", FeaturesResources.field, CSharpFeaturesResources.struct_),
                Diagnostic(RudeEditKind.InsertIntoStruct, "d", CSharpFeaturesResources.event_field, CSharpFeaturesResources.struct_));
        }

        [Fact]
        public void Field_Insert_IntoLayoutClass_Auto()
        {
            var src1 = @"
using System.Runtime.InteropServices;

[StructLayoutAttribute(LayoutKind.Auto)]
class C 
{ 
    private int a; 
}
";
            var src2 = @"
using System.Runtime.InteropServices;

[StructLayoutAttribute(LayoutKind.Auto)]
class C 
{ 
    private int a; 
    private int b; 
    private int c; 
    private static int d; 
}
";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.b")),
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.c")),
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.d")),
                },
                capabilities: EditAndContinueCapabilities.AddInstanceFieldToExistingType | EditAndContinueCapabilities.AddStaticFieldToExistingType);
        }

        [Fact]
        public void Field_Insert_IntoLayoutClass_Explicit()
        {
            var src1 = @"
using System.Runtime.InteropServices;

[StructLayoutAttribute(LayoutKind.Explicit)]
class C 
{ 
    [FieldOffset(0)]
    private int a; 
}
";
            var src2 = @"
using System.Runtime.InteropServices;

[StructLayoutAttribute(LayoutKind.Explicit)]
class C 
{ 
    [FieldOffset(0)]
    private int a; 

    [FieldOffset(0)]
    private int b; 

    [FieldOffset(4)]
    private int c; 

    private static int d; 
}
";
            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.InsertIntoClassWithLayout, "b", FeaturesResources.field, FeaturesResources.class_),
                Diagnostic(RudeEditKind.InsertIntoClassWithLayout, "c", FeaturesResources.field, FeaturesResources.class_),
                Diagnostic(RudeEditKind.InsertIntoClassWithLayout, "d", FeaturesResources.field, FeaturesResources.class_));
        }

        [Fact]
        public void Field_Insert_IntoLayoutClass_Sequential()
        {
            var src1 = @"
using System.Runtime.InteropServices;

[StructLayoutAttribute(LayoutKind.Sequential)]
class C 
{ 
    private int a; 
}
";
            var src2 = @"
using System.Runtime.InteropServices;

[StructLayoutAttribute(LayoutKind.Sequential)]
class C 
{ 
    private int a; 
    private int b; 
    private int c; 
    private static int d; 
}
";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.InsertIntoClassWithLayout, "b", FeaturesResources.field, FeaturesResources.class_),
                Diagnostic(RudeEditKind.InsertIntoClassWithLayout, "c", FeaturesResources.field, FeaturesResources.class_),
                Diagnostic(RudeEditKind.InsertIntoClassWithLayout, "d", FeaturesResources.field, FeaturesResources.class_));
        }

        [Fact]
        public void Field_Insert_WithInitializersAndLambdas1()
        {
            var src1 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);

    public C()
    {
        F(<N:0.1>c => c + 1</N:0.1>);
    }
}
";
            var src2 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
    int B = F(b => b + 1);                    // new field

    public C()
    {
        F(<N:0.1>c => c + 1</N:0.1>);
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var syntaxMap = GetSyntaxMap(src1, src2);

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.B")),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(), syntaxMap[0])
                },
                capabilities: EditAndContinueCapabilities.AddInstanceFieldToExistingType);
        }

        [Fact]
        public void Field_Insert_ConstructorReplacingImplicitConstructor_WithInitializersAndLambdas()
        {
            var src1 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
}
";
            var src2 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
    int B = F(b => b + 1);                    // new field

    public C()                                // new ctor replacing existing implicit constructor
    {
        F(c => c + 1);
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var syntaxMap = GetSyntaxMap(src1, src2);

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.B")),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single(), syntaxMap[0])
                },
                capabilities: EditAndContinueCapabilities.AddInstanceFieldToExistingType | EditAndContinueCapabilities.AddMethodToExistingType);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2504")]
        public void Field_Insert_ParameterlessConstructorInsert_WithInitializersAndLambdas()
        {
            var src1 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);

    public C(int x) {}
}
";
            var src2 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);

    public C(int x) {}

    public C()                                // new ctor
    {
        F(c => c + 1);
    }
}
";
            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.InsertConstructorToTypeWithInitializersWithLambdas, "public C()"));

            // TODO (bug https://github.com/dotnet/roslyn/issues/2504):
            //edits.VerifySemantics(
            //    ActiveStatementsDescription.Empty,
            //    new[]
            //    {
            //        SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<NamedTypeSymbol>("C").Constructors.Single(), syntaxMap[0])
            //    });
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2504")]
        public void Field_Insert_ConstructorInsert_WithInitializersAndLambdas1()
        {
            var src1 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
}
";
            var src2 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(<N:0.0>a => a + 1</N:0.0>);
    int B = F(b => b + 1);                    // new field

    public C(int x)                           // new ctor
    {
        F(c => c + 1);
    }
}
";
            var edits = GetTopEdits(src1, src2);
            _ = GetSyntaxMap(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.InsertConstructorToTypeWithInitializersWithLambdas, "public C(int x)"));

            // TODO (bug https://github.com/dotnet/roslyn/issues/2504):
            //edits.VerifySemantics(
            //    ActiveStatementsDescription.Empty,
            //    new[]
            //    {
            //        SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.B")),
            //        SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<NamedTypeSymbol>("C").Constructors.Single(), syntaxMap[0])
            //    });
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2504")]
        public void Field_Insert_ConstructorInsert_WithInitializersButNoExistingLambdas1()
        {
            var src1 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(null);
}
";
            var src2 = @"
using System;

class C
{
    static int F(Func<int, int> x) => 1;

    int A = F(null);
    int B = F(b => b + 1);                    // new field

    public C(int x)                           // new ctor
    {
        F(c => c + 1);
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var syntaxMap = GetSyntaxMap(src1, src2);

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.B")),
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<INamedTypeSymbol>("C").Constructors.Single())
                },
                capabilities: EditAndContinueCapabilities.AddInstanceFieldToExistingType | EditAndContinueCapabilities.AddMethodToExistingType);
        }

        [Fact]
        public void Field_Insert_NotSupportedByRuntime()
        {
            var src1 = "class C {  }";
            var src2 = "class C { public int a = 1; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.InsertNotSupportedByRuntime, "a = 1", FeaturesResources.field) },
                capabilities: EditAndContinueCapabilities.AddStaticFieldToExistingType);
        }

        [Fact]
        public void Field_Insert_Static_NotSupportedByRuntime()
        {
            var src1 = "class C {  }";
            var src2 = "class C { public static int a = 1; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.InsertNotSupportedByRuntime, "a = 1", FeaturesResources.field) },
                capabilities: EditAndContinueCapabilities.AddInstanceFieldToExistingType);
        }

        [Fact]
        public void Field_Attribute_Add_NotSupportedByRuntime()
        {
            var src1 = @"
class C
{
    public int a = 1, x = 1;
}";
            var src2 = @"
class C
{
    [System.Obsolete]public int a = 1, x = 1;
}";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [public int a = 1, x = 1;]@18 -> [[System.Obsolete]public int a = 1, x = 1;]@18");

            edits.VerifySemanticDiagnostics(
                new[]
                {
                    Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "public int a = 1, x = 1", FeaturesResources.field),
                    Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "public int a = 1, x = 1", FeaturesResources.field),
                },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void Field_Attribute_Add()
        {
            var src1 = @"
class C
{
    public int a, b;
}";
            var src2 = @"
class C
{
    [System.Obsolete]public int a, b;
}";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.a")),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.b"))
                },
                capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
        }

        [Fact]
        public void Field_Attribute_Add_WithInitializer()
        {
            var src1 = @"
class C
{
    int a;
}";
            var src2 = @"
class C
{
    [System.Obsolete]int a = 0;
}";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.a")),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), preserveLocalVariables: true),
                },
                capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
        }

        [Fact]
        public void Field_Attribute_DeleteInsertUpdate_WithInitializer()
        {
            var srcA1 = "partial class C { int a = 1; }";
            var srcB1 = "partial class C { }";

            var srcA2 = "partial class C { }";
            var srcB2 = "partial class C { [System.Obsolete]int a = 2; }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(),
                    DocumentResults(
                        semanticEdits: new[]
                        {
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.a"), preserveLocalVariables: true),
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), partialType: "C", preserveLocalVariables: true)
                        }),
                },
                capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
        }

        [Fact]
        public void Field_Delete1()
        {
            var src1 = "class C { int a = 1; }";
            var src2 = "class C {  }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Delete [int a = 1;]@10",
                "Delete [int a = 1]@10",
                "Delete [a = 1]@14");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Delete, "class C", DeletedSymbolDisplay(FeaturesResources.field, "a")));
        }

        [Fact]
        public void Field_UnsafeModifier_Update()
        {
            var src1 = "struct Node { unsafe Node* left; }";
            var src2 = "struct Node { Node* left; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [unsafe Node* left;]@14 -> [Node* left;]@14");

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void Field_ModifierAndType_Update()
        {
            var src1 = "struct Node { unsafe Node* left; }";
            var src2 = "struct Node { Node left; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [unsafe Node* left;]@14 -> [Node left;]@14",
                "Update [Node* left]@21 -> [Node left]@14");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.TypeUpdate, "Node left", FeaturesResources.field));
        }

        [Theory]
        [InlineData("string", "string?")]
        [InlineData("object", "dynamic")]
        [InlineData("(int a, int b)", "(int a, int c)")]
        public void Field_Type_Update_RuntimeTypeUnchanged(string oldType, string newType)
        {
            var src1 = "class C { " + oldType + " F, G; }";
            var src2 = "class C { " + newType + " F, G; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.F")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.G")));
        }

        [Theory]
        [InlineData("int", "string")]
        [InlineData("int", "int?")]
        [InlineData("(int a, int b)", "(int a, double b)")]
        public void Field_Type_Update_RuntimeTypeChanged(string oldType, string newType)
        {
            var src1 = "class C { " + oldType + " F, G; }";
            var src2 = "class C { " + newType + " F, G; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.TypeUpdate, newType + " F, G", FeaturesResources.field),
                Diagnostic(RudeEditKind.TypeUpdate, newType + " F, G", FeaturesResources.field));
        }

        [Theory]
        [InlineData("string", "string?")]
        [InlineData("object", "dynamic")]
        [InlineData("(int a, int b)", "(int a, int c)")]
        public void Field_Event_Type_Update_RuntimeTypeUnchanged(string oldType, string newType)
        {
            var src1 = "class C { event System.Action<" + oldType + "> F, G; }";
            var src2 = "class C { event System.Action<" + newType + "> F, G; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.F")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.G")));
        }

        [Theory]
        [InlineData("int", "string")]
        [InlineData("int", "int?")]
        public void Field_Event_Type_Update_RuntimeTypeChanged(string oldType, string newType)
        {
            var src1 = "class C { event System.Action<" + oldType + "> E; }";
            var src2 = "class C { event System.Action<" + newType + "> E; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMembers("C.add_E").FirstOrDefault(m => m.GetParameterTypes()[0].Type.GetMemberTypeArgumentsNoUseSiteDiagnostics()[0].SpecialType == SpecialType.System_Int32)?.ISymbol, deletedSymbolContainerProvider: c => c.GetMember("C")),
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMembers("C.remove_E").FirstOrDefault(m => m.GetParameterTypes()[0].Type.GetMemberTypeArgumentsNoUseSiteDiagnostics()[0].SpecialType == SpecialType.System_Int32)?.ISymbol, deletedSymbolContainerProvider: c => c.GetMember("C")),
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.E")),
                },
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingTypeNotSupportedByRuntime, "event System.Action<" + newType + "> E", FeaturesResources.event_) },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void Field_Event_Type_Update_TupleType()
        {
            var src1 = "class C { event System.Action<(int a, int b)> E; }";
            var src2 = "class C { event System.Action<(int a, double b)> E; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMembers("C.add_E").FirstOrDefault(m => m.GetParameterTypes()[0].Type.GetMemberTypeArgumentsNoUseSiteDiagnostics()[0].GetMemberTypeArgumentsNoUseSiteDiagnostics()[1].SpecialType == SpecialType.System_Int32)?.ISymbol, deletedSymbolContainerProvider: c => c.GetMember("C")),
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMembers("C.remove_E").FirstOrDefault(m => m.GetParameterTypes()[0].Type.GetMemberTypeArgumentsNoUseSiteDiagnostics()[0].GetMemberTypeArgumentsNoUseSiteDiagnostics()[1].SpecialType == SpecialType.System_Int32)?.ISymbol, deletedSymbolContainerProvider: c => c.GetMember("C")),
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.E")),
                },
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingTypeNotSupportedByRuntime, "event System.Action<(int a, double b)> E", FeaturesResources.event_) },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void Field_Type_Update_ReorderRemoveAdd()
        {
            var src1 = "class C { int F, G, H; bool U; }";
            var src2 = "class C { string G, F; double V, U; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [int F, G, H]@10 -> [string G, F]@10",
                "Reorder [G]@17 -> @17",
                "Update [bool U]@23 -> [double V, U]@23",
                "Insert [V]@30",
                "Delete [H]@20");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Move, "G", FeaturesResources.field),
                Diagnostic(RudeEditKind.TypeUpdate, "string G, F", FeaturesResources.field),
                Diagnostic(RudeEditKind.TypeUpdate, "string G, F", FeaturesResources.field),
                Diagnostic(RudeEditKind.TypeUpdate, "double V, U", FeaturesResources.field),
                Diagnostic(RudeEditKind.Delete, "string G, F", DeletedSymbolDisplay(FeaturesResources.field, "H")));
        }

        [Fact]
        public void Event_Rename1()
        {
            var src1 = "class C { event int E { remove { } add { } } }";
            var src2 = "class C { event int F { remove { } add { } } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.add_E"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.remove_E"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.F")),
                },
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.RenamingNotSupportedByRuntime, "event int F", FeaturesResources.event_) },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void Event_Rename2()
        {
            var src1 = "class C { event int E; }";
            var src2 = "class C { event int F; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.add_E"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.remove_E"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.F")),
                },
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
        }

        [Fact]
        public void Event_UpdateType()
        {
            var src1 = "class C { event int E { remove { } add { } } }";
            var src2 = "class C { event string E { remove { } add { } } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMembers("C.add_E").FirstOrDefault(m => m.GetParameterTypes()[0].Type.SpecialType == SpecialType.System_Int32)?.ISymbol, deletedSymbolContainerProvider: c => c.GetMember("C")),
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMembers("C.remove_E").FirstOrDefault(m => m.GetParameterTypes()[0].Type.SpecialType == SpecialType.System_Int32)?.ISymbol, deletedSymbolContainerProvider: c => c.GetMember("C")),
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.E")),
                },
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingTypeNotSupportedByRuntime, "event string E", FeaturesResources.event_) },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void Field_Event_Reorder()
        {
            var src1 = "class C { int a = 0; int b = 1; event int c = 2; }";
            var src2 = "class C { event int c = 2; int a = 0; int b = 1; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Reorder [event int c = 2;]@32 -> @10");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Move, "event int c = 2", CSharpFeaturesResources.event_field));
        }

        [Fact]
        public void Field_Event_Partial_InsertDelete()
        {
            var srcA1 = "partial class C { }";
            var srcB1 = "partial class C { event int E = 2; }";

            var srcA2 = "partial class C { event int E = 2; }";
            var srcB2 = "partial class C { }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(
                        semanticEdits: new[]
                        {
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), partialType: "C", preserveLocalVariables: true)
                        }),

                    DocumentResults(),
                });
        }

        #endregion

        #region Properties

        [Theory]
        [InlineData("static")]
        [InlineData("virtual")]
        [InlineData("abstract")]
        [InlineData("override")]
        [InlineData("sealed override", "override")]
        public void Property_Modifiers_Update(string oldModifiers, string newModifiers = "")
        {
            if (oldModifiers != "")
            {
                oldModifiers += " ";
            }

            if (newModifiers != "")
            {
                newModifiers += " ";
            }

            var src1 = "class C { " + oldModifiers + "int F => 0; }";
            var src2 = "class C { " + newModifiers + "int F => 0; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [" + oldModifiers + "int F => 0;]@10 -> [" + newModifiers + "int F => 0;]@10");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, newModifiers + "int F", FeaturesResources.property_));
        }

        [Fact]
        public void Property_ExpressionBody_Rename()
        {
            var src1 = "class C { int P => 1; }";
            var src2 = "class C { int Q => 1; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.get_P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.Q")),
                },
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
        }

        [Fact]
        public void Property_ExpressionBody_Update()
        {
            var src1 = "class C { int P => 1; }";
            var src2 = "class C { int P => 2; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [int P => 1;]@10 -> [int P => 2;]@10",
                "Update [=> 1]@16 -> [=> 2]@16");

            edits.VerifySemantics(ActiveStatementsDescription.Empty, new[]
            {
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_P"), preserveLocalVariables: false)
            });
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48628")]
        public void Property_ExpressionBody_ModifierUpdate()
        {
            var src1 = "class C { int P => 1; }";
            var src2 = "class C { unsafe int P => 1; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [int P => 1;]@10 -> [unsafe int P => 1;]@10");

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void Property_ExpressionBodyToBlockBody1()
        {
            var src1 = "class C { int P => 1; }";
            var src2 = "class C { int P { get { return 2; } } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [int P => 1;]@10 -> [int P { get { return 2; } }]@10",
                "Insert [{ get { return 2; } }]@16",
                "Insert [get { return 2; }]@18",
                "Delete [=> 1]@16");

            edits.VerifySemantics(ActiveStatementsDescription.Empty, new[]
            {
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_P"), preserveLocalVariables: false)
            });
        }

        [Fact]
        public void Property_ExpressionBodyToBlockBody2()
        {
            var src1 = "class C { int P => 1; }";
            var src2 = "class C { int P { get { return 2; } set { } } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [int P => 1;]@10 -> [int P { get { return 2; } set { } }]@10",
                "Insert [{ get { return 2; } set { } }]@16",
                "Insert [get { return 2; }]@18",
                "Insert [set { }]@36",
                "Delete [=> 1]@16");

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_P"), preserveLocalVariables: false),
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.set_P"), preserveLocalVariables: false)
                },
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
        }

        [Fact]
        public void Property_BlockBodyToExpressionBody1()
        {
            var src1 = "class C { int P { get { return 2; } } }";
            var src2 = "class C { int P => 1; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [int P { get { return 2; } }]@10 -> [int P => 1;]@10",
                "Insert [=> 1]@16",
                "Delete [{ get { return 2; } }]@16",
                "Delete [get { return 2; }]@18");

            edits.VerifySemantics(ActiveStatementsDescription.Empty, new[]
            {
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_P"), preserveLocalVariables: false)
            });
        }

        [Fact]
        public void Property_BlockBodyToExpressionBody2()
        {
            var src1 = "class C { int P { get { return 2; } set { } } }";
            var src2 = "class C { int P => 1; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [int P { get { return 2; } set { } }]@10 -> [int P => 1;]@10",
                "Insert [=> 1]@16",
                "Delete [{ get { return 2; } set { } }]@16",
                "Delete [get { return 2; }]@18",
                "Delete [set { }]@36");

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_P"), preserveLocalVariables: false),
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.set_P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17681")]
        public void Property_ExpressionBodyToGetterExpressionBody()
        {
            var src1 = "class C { int P => 1; }";
            var src2 = "class C { int P { get => 2; } }";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [int P => 1;]@10 -> [int P { get => 2; }]@10",
                "Insert [{ get => 2; }]@16",
                "Insert [get => 2;]@18",
                "Delete [=> 1]@16");

            edits.VerifySemantics(ActiveStatementsDescription.Empty, new[]
            {
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_P"), preserveLocalVariables: false),
            });
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17681")]
        public void Property_GetterExpressionBodyToExpressionBody()
        {
            var src1 = "class C { int P { get => 2; } }";
            var src2 = "class C { int P => 1; }";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyEdits(
                "Update [int P { get => 2; }]@10 -> [int P => 1;]@10",
                "Insert [=> 1]@16",
                "Delete [{ get => 2; }]@16",
                "Delete [get => 2;]@18");

            edits.VerifySemantics(ActiveStatementsDescription.Empty, new[]
            {
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_P"), preserveLocalVariables: false),
            });
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17681")]
        public void Property_GetterBlockBodyToGetterExpressionBody()
        {
            var src1 = "class C { int P { get { return 2; } } }";
            var src2 = "class C { int P { get => 2; } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [get { return 2; }]@18 -> [get => 2;]@18");

            edits.VerifySemantics(ActiveStatementsDescription.Empty, new[]
            {
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_P"), preserveLocalVariables: false),
            });
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17681")]
        public void Property_SetterBlockBodyToSetterExpressionBody()
        {
            var src1 = "class C { int P { set { } } }";
            var src2 = "class C { int P { set => F(); } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [set { }]@18 -> [set => F();]@18");

            edits.VerifySemantics(ActiveStatementsDescription.Empty, new[]
            {
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IPropertySymbol>("P").SetMethod),
            });
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17681")]
        public void Property_InitBlockBodyToInitExpressionBody()
        {
            var src1 = "class C { int P { init { } } }";
            var src2 = "class C { int P { init => F(); } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [init { }]@18 -> [init => F();]@18");

            edits.VerifySemantics(ActiveStatementsDescription.Empty, new[]
            {
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IPropertySymbol>("P").SetMethod, preserveLocalVariables: false),
            });
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17681")]
        public void Property_GetterExpressionBodyToGetterBlockBody()
        {
            var src1 = "class C { int P { get => 2; } }";
            var src2 = "class C { int P { get { return 2; } } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [get => 2;]@18 -> [get { return 2; }]@18");

            edits.VerifySemantics(ActiveStatementsDescription.Empty, new[]
            {
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_P"), preserveLocalVariables: false)
            });
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17681")]
        public void Property_GetterBlockBodyWithSetterToGetterExpressionBodyWithSetter()
        {
            var src1 = "class C { int P { get => 2;         set { Console.WriteLine(0); } } }";
            var src2 = "class C { int P { get { return 2; } set { Console.WriteLine(0); } } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [get => 2;]@18 -> [get { return 2; }]@18");

            edits.VerifySemantics(ActiveStatementsDescription.Empty, new[]
            {
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_P"), preserveLocalVariables: false),
            });
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17681")]
        public void Property_GetterExpressionBodyWithSetterToGetterBlockBodyWithSetter()
        {
            var src1 = "class C { int P { get { return 2; } set { Console.WriteLine(0); } } }";
            var src2 = "class C { int P { get => 2; set { Console.WriteLine(0); } } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [get { return 2; }]@18 -> [get => 2;]@18");

            edits.VerifySemantics(ActiveStatementsDescription.Empty, new[]
            {
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_P"), preserveLocalVariables: false),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.set_P"), preserveLocalVariables: false)
            });
        }

        [Fact]
        public void Property_Rename1()
        {
            var src1 = "class C { int P { get { return 1; } } }";
            var src2 = "class C { int Q { get { return 1; } } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.get_P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.Q"))
                },
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.RenamingNotSupportedByRuntime, "int Q", FeaturesResources.property_) },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void Property_Rename2()
        {
            var interfaces = """
                interface I
                {
                    int P { get; }
                }
                
                interface J
                {
                    int P { get; }
                }
                """;
            var src1 = "class C { int I.P { get { return 1; } } } " + interfaces;
            var src2 = "class C { int J.P { get { return 1; } } } " + interfaces;

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "int J.P", FeaturesResources.property_));
        }

        [Fact]
        public void Property_Rename3()
        {
            var src1 = "class C { int P { get { return 1; } set { } } }";
            var src2 = "class C { int Q { get { return 1; } set { } } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.get_P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.set_P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.Q")),
                },
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
        }

        [Fact]
        public void Property_Rename4()
        {
            var src1 = "class C { int P { get; set; } }";
            var src2 = "class C { int Q { get; set; } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.get_P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.set_P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.Q")),
                },
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.AddInstanceFieldToExistingType);
        }

        [Fact]
        public void Property_RenameAndUpdate()
        {
            var src1 = "class C { int P { get { return 1; } } }";
            var src2 = "class C { int Q { get { return 2; } } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.get_P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.Q"))
                },
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
        }

        [Fact]
        public void PropertyDelete()
        {
            var src1 = "class C { int P { get { return 1; } set { } } }";
            var src2 = "class C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.get_P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.set_P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void PropertyDelete_GetOnly()
        {
            var src1 = "class C { int P { get { return 1; } } }";
            var src2 = "class C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.get_P"), deletedSymbolContainerProvider: c => c.GetMember("C"))
                },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void PropertyAccessorDelete1()
        {
            var src1 = "class C { int P { get { return 1; } set { } } }";
            var src2 = "class C { int P { get { return 1; } } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.set_P"), deletedSymbolContainerProvider: c => c.GetMember("C"))
                },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void PropertyAccessorDelete2()
        {
            var src1 = "class C { int P { set { } get { return 1; } } }";
            var src2 = "class C { int P { set { } } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.get_P"), deletedSymbolContainerProvider: c => c.GetMember("C"))
                },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void PropertyReorder1()
        {
            var src1 = "class C { int P { get { return 1; } } int Q { get { return 1; } }  }";
            var src2 = "class C { int Q { get { return 1; } } int P { get { return 1; } }  }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Reorder [int Q { get { return 1; } }]@38 -> @10");

            // TODO: we can allow the move since the property doesn't have a backing field
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Move, "int Q", FeaturesResources.property_));
        }

        [Fact]
        public void PropertyReorder2()
        {
            var src1 = "class C { int P { get; set; } int Q { get; set; }  }";
            var src2 = "class C { int Q { get; set; } int P { get; set; }  }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Reorder [int Q { get; set; }]@30 -> @10");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Move, "int Q", FeaturesResources.auto_property));
        }

        [Fact]
        public void PropertyAccessorReorder_GetSet()
        {
            var src1 = "class C { int P { get { return 1; } set { } } }";
            var src2 = "class C { int P { set { } get { return 1; } } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Reorder [set { }]@36 -> @18");

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void PropertyAccessorReorder_GetInit()
        {
            var src1 = "class C { int P { get { return 1; } init { } } }";
            var src2 = "class C { int P { init { } get { return 1; } } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Reorder [init { }]@36 -> @18");

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void PropertyTypeUpdate()
        {
            var src1 = "class C { int P { get; set; } }";
            var src2 = "class C { char P { get; set; } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [int P { get; set; }]@10 -> [char P { get; set; }]@10");

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.get_P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMembers("C.set_P").FirstOrDefault(p => p.GetParameters()[0].Type.SpecialType == SpecialType.System_Int32)?.ISymbol, deletedSymbolContainerProvider: c => c.GetMember("C")),
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.P")),
                },
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.AddInstanceFieldToExistingType);

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingTypeNotSupportedByRuntime, "char P", FeaturesResources.property_) },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void PropertyTypeUpdate_WithBodies()
        {
            var src1 = "class C { int P { get { return 1; } set { } } }";
            var src2 = "class C { char P { get { return 'a'; } set { } } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.get_P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMembers("C.set_P").FirstOrDefault(p => p.GetParameters()[0].Type.SpecialType == SpecialType.System_Int32)?.ISymbol, deletedSymbolContainerProvider: c => c.GetMember("C")),
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.P")),
                },
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.AddInstanceFieldToExistingType);

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingTypeNotSupportedByRuntime, "char P", FeaturesResources.property_) },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void PropertyUpdate_AddAttribute()
        {
            var src1 = "class C { int P { get; set; } }";
            var src2 = "class C { [System.Obsolete]int P { get; set; } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "int P", FeaturesResources.property_) },
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
        }

        [Fact]
        public void PropertyUpdate_AddAttribute_SupportedByRuntime()
        {
            var src1 = "class C { int P { get; set; } }";
            var src2 = "class C { [System.Obsolete]int P { get; set; } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                new[] {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.P"))
                },
                capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
        }

        [Fact]
        public void PropertyAccessorUpdate_AddAttribute()
        {
            var src1 = "class C { int P { get; set; } }";
            var src2 = "class C { int P { [System.Obsolete]get; set; } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "get", CSharpFeaturesResources.property_getter) },
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
        }

        [Fact]
        public void PropertyAccessorUpdate_AddAttribute2()
        {
            var src1 = "class C { int P { get; set; } }";
            var src2 = "class C { int P { get; [System.Obsolete]set; } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "set", CSharpFeaturesResources.property_setter) },
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
        }

        [Fact]
        public void PropertyAccessorUpdate_AddAttribute_SupportedByRuntime()
        {
            var src1 = "class C { int P { get; set; } }";
            var src2 = "class C { int P { [System.Obsolete]get; set; } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IPropertySymbol>("P").GetMethod) },
                capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
        }

        [Fact]
        public void PropertyAccessorUpdate_AddAttribute_SupportedByRuntime2()
        {
            var src1 = "class C { int P { get; set; } }";
            var src2 = "class C { int P { get; [System.Obsolete]set; } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IPropertySymbol>("P").SetMethod) },
                capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
        }

        [Fact]
        public void Property_Insert()
        {
            var src1 = "class C { }";
            var src2 = "class C { int P { get => 1; set { } } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                new[] { SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<INamedTypeSymbol>("C").GetMember("P")) },
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
        }

        [Fact]
        public void Property_Insert_Static()
        {
            var src1 = "class C { }";
            var src2 = "class C { static int P { get => 1; set { } } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                new[] { SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<INamedTypeSymbol>("C").GetMember("P")) },
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
        }

        [Fact]
        public void PropertyInsert_NotSupportedByRuntime()
        {
            var src1 = "class C { }";
            var src2 = "class C { int P { get => 1; set { } } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.InsertNotSupportedByRuntime, "int P", FeaturesResources.property_) },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/992578")]
        public void Property_Insert_Incomplete()
        {
            var src1 = "class C { }";
            var src2 = "class C { public int P { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Insert [public int P { }]@10", "Insert [{ }]@23");

            edits.VerifySemanticDiagnostics(
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/835827")]
        public void Property_Insert_PInvoke()
        {
            var src1 = @"
using System;
using System.Runtime.InteropServices;

class C
{
}";
            var src2 = @"
using System;
using System.Runtime.InteropServices;

class C
{
    private static extern int P1 { [DllImport(""x.dll"")]get; }
    private static extern int P2 { [DllImport(""x.dll"")]set; }
    private static extern int P3 { [DllImport(""x.dll"")]get; [DllImport(""x.dll"")]set; }
}
";
            var edits = GetTopEdits(src1, src2);

            // CLR doesn't support methods without a body
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.InsertExtern, "private static extern int P1", FeaturesResources.property_),
                Diagnostic(RudeEditKind.InsertExtern, "private static extern int P2", FeaturesResources.property_),
                Diagnostic(RudeEditKind.InsertExtern, "private static extern int P3", FeaturesResources.property_));
        }

        [Fact]
        public void Property_Insert_IntoStruct()
        {
            var src1 = @"
struct S 
{ 
    public int a; 
    
    public S(int z) { a = z; } 
}
";
            var src2 = @"
struct S 
{ 
    public int a; 
    private static int c { get; set; } 
    private static int e { get { return 0; } set { } } 
    private static int g { get; } = 1;
    private static int i { get; set; } = 1;
    private static int k => 1;
    private static int l { get => 1; set {} }
    private static int m { get => 1; set => k; }
    public S(int z) { a = z; }
}
";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.InsertIntoStruct, "private static int c { get; set; }", FeaturesResources.auto_property, CSharpFeaturesResources.struct_),
                Diagnostic(RudeEditKind.InsertIntoStruct, "private static int g { get; } = 1;", FeaturesResources.auto_property, CSharpFeaturesResources.struct_),
                Diagnostic(RudeEditKind.InsertIntoStruct, "private static int i { get; set; } = 1;", FeaturesResources.auto_property, CSharpFeaturesResources.struct_));
        }

        [Fact]
        public void Property_Insert_IntoLayoutClass_Sequential()
        {
            var src1 = @"
using System.Runtime.InteropServices;

[StructLayoutAttribute(LayoutKind.Sequential)]
class C 
{ 
    private int a; 
}
";
            var src2 = @"
using System.Runtime.InteropServices;

[StructLayoutAttribute(LayoutKind.Sequential)]
class C 
{ 
    private int a; 
    private int b { get; set; }
    private static int c { get; set; } 
    private int d { get { return 0; } set { } }
    private static int e { get { return 0; } set { } } 
    private int f { get; } = 1;
    private static int g { get; } = 1;
    private int h { get; set; } = 1;
    private static int i { get; set; } = 1;
    private int j => 1;
    private static int k => 1;
    private int l { get => 1; set { } }
    private static int m { get => 1; set { } }
    private int n { get { return 1; } set => a; }
    private static int o { get { return 1; } set => a; }
}    
";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.InsertIntoClassWithLayout, "private int b { get; set; }", FeaturesResources.auto_property, FeaturesResources.class_),
                Diagnostic(RudeEditKind.InsertIntoClassWithLayout, "private static int c { get; set; }", FeaturesResources.auto_property, FeaturesResources.class_),
                Diagnostic(RudeEditKind.InsertIntoClassWithLayout, "private int f { get; } = 1;", FeaturesResources.auto_property, FeaturesResources.class_),
                Diagnostic(RudeEditKind.InsertIntoClassWithLayout, "private static int g { get; } = 1;", FeaturesResources.auto_property, FeaturesResources.class_),
                Diagnostic(RudeEditKind.InsertIntoClassWithLayout, "private int h { get; set; } = 1;", FeaturesResources.auto_property, FeaturesResources.class_),
                Diagnostic(RudeEditKind.InsertIntoClassWithLayout, "private static int i { get; set; } = 1;", FeaturesResources.auto_property, FeaturesResources.class_));
        }

        [Fact]
        public void Property_Insert_Auto()
        {
            var src1 = "class C { }";
            var src2 = "class C { int P { get; set; } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                new[] { SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<INamedTypeSymbol>("C").GetMember("P")) },
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.AddInstanceFieldToExistingType);
        }

        [Fact]
        public void Property_Insert_Auto_Static()
        {
            var src1 = "class C { }";
            var src2 = "class C { static int P { get; set; } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                new[] { SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<INamedTypeSymbol>("C").GetMember("P")) },
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.AddStaticFieldToExistingType);
        }

        // Design: Adding private accessors should also be allowed since we now allow adding private methods
        // and adding public properties and/or public accessors are not allowed.
        [Fact]
        public void Property_Private_AccessorAdd()
        {
            var src1 = "class C { int _p; int P { get { return 1; } } }";
            var src2 = "class C { int _p; int P { get { return 1; } set { _p = value; } } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Insert [set { _p = value; }]@44");

            edits.VerifySemanticDiagnostics(
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/755975")]
        public void Property_Private_AccessorDelete()
        {
            var src1 = "class C { int _p; int P { get { return 1; } set { _p = value; } } }";
            var src2 = "class C { int _p; int P { get { return 1; } } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Delete [set { _p = value; }]@44");

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.set_P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void Property_Auto_Private_AccessorAdd1()
        {
            var src1 = "class C { int P { get; } }";
            var src2 = "class C { int P { get; set; } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Insert [set;]@23");

            edits.VerifySemanticDiagnostics(
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
        }

        [Fact]
        public void Property_Auto_Private_AccessorAdd2()
        {
            var src1 = "class C { public int P { get; } }";
            var src2 = "class C { public int P { get; private set; } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Insert [private set;]@30");

            edits.VerifySemanticDiagnostics(
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
        }

        [Fact]
        public void Property_Auto_Private_AccessorAdd4()
        {
            var src1 = "class C { public int P { get; } }";
            var src2 = "class C { public int P { get; set; } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Insert [set;]@30");

            edits.VerifySemanticDiagnostics(
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
        }

        [Fact]
        public void Property_Auto_Private_AccessorAdd5()
        {
            var src1 = "class C { public int P { get; } }";
            var src2 = "class C { public int P { get; internal set; } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Insert [internal set;]@30");

            edits.VerifySemanticDiagnostics(
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
        }

        [Fact]
        public void Property_Auto_Private_AccessorAdd6()
        {
            var src1 = "class C { int P { get; } = 1; }";
            var src2 = "class C { int P { get; set; } = 1; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Insert [set;]@23");

            edits.VerifySemanticDiagnostics(
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
        }

        [Fact]
        public void Property_Auto_Private_AccessorAdd_Init()
        {
            var src1 = "class C { int P { get; } = 1; }";
            var src2 = "class C { int P { get; init; } = 1; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Insert [init;]@23");

            edits.VerifySemanticDiagnostics(
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/755975")]
        public void Property_Auto_Private_AccessorDelete_Get()
        {
            var src1 = "class C { int P { get; set; } }";
            var src2 = "class C { int P { set; } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Delete [get;]@18");

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.get_P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void Property_Auto_Accessor_SetToInit()
        {
            var src1 = "class C { int P { get; set; } }";
            var src2 = "class C { int P { get; init; } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [set;]@23 -> [init;]@23");

            // not allowed since it changes the backing field readonly-ness and the signature of the setter (modreq)
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.AccessorKindUpdate, "init", CSharpFeaturesResources.property_setter));
        }

        [Fact]
        public void Property_Auto_Accessor_InitToSet()
        {
            var src1 = "class C { int P { get; init; } }";
            var src2 = "class C { int P { get; set; } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [init;]@23 -> [set;]@23");

            // not allowed since it changes the backing field readonly-ness and the signature of the setter (modreq)
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.AccessorKindUpdate, "set", CSharpFeaturesResources.property_setter));
        }

        [Fact]
        public void Propert_Auto_Private_AccessorDelete_Set()
        {
            var src1 = "class C { int P { get; set; } = 1; }";
            var src2 = "class C { int P { get; } = 1; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Delete [set;]@23");

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.set_P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C..ctor"), preserveLocalVariables: true),
                },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void Propert_Auto_Private_AccessorDelete_Init()
        {
            var src1 = "class C { int P { get; init; } = 1; }";
            var src2 = "class C { int P { get; } = 1; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Delete [init;]@23");

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.set_P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C..ctor"), preserveLocalVariables: true),
                },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void Property_Auto_AccessorUpdate()
        {
            var src1 = "class C { int P { get; } }";
            var src2 = "class C { int P { set; } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [get;]@18 -> [set;]@18");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.AccessorKindUpdate, "set", CSharpFeaturesResources.property_setter));
        }

        [Fact]
        public void Property_ReadOnlyRef_Insert()
        {
            var src1 = "class Test { }";
            var src2 = "class Test { ref readonly int P { get; } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [ref readonly int P { get; }]@13",
                "Insert [{ get; }]@32",
                "Insert [get;]@34");

            edits.VerifySemanticDiagnostics(
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.AddInstanceFieldToExistingType);
        }

        [Fact]
        public void Property_ReadOnlyRef_Update()
        {
            var src1 = "class Test { int P { get; } }";
            var src2 = "class Test { ref readonly int P { get; } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [int P { get; }]@13 -> [ref readonly int P { get; }]@13");

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("Test.get_P"), deletedSymbolContainerProvider: c => c.GetMember("Test")),
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("Test.P")),
                },
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.AddInstanceFieldToExistingType);

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingTypeNotSupportedByRuntime, "ref readonly int P", FeaturesResources.property_) },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void Property_Partial_InsertDelete()
        {
            var srcA1 = "partial class C { }";
            var srcB1 = "partial class C { int P { get => 1; set { } } }";

            var srcA2 = "partial class C { int P { get => 1; set { } } }";
            var srcB2 = "partial class C { }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(
                        semanticEdits: new[]
                        {
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IPropertySymbol>("P").GetMethod),
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IPropertySymbol>("P").SetMethod)
                        }),

                    DocumentResults(),
                });
        }

        [Fact]
        public void PropertyInit_Partial_InsertDelete()
        {
            var srcA1 = "partial class C { }";
            var srcB1 = "partial class C { int Q { get => 1; init { } }}";

            var srcA2 = "partial class C { int Q { get => 1; init { } }}";
            var srcB2 = "partial class C { }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(
                        semanticEdits: new[]
                        {
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IPropertySymbol>("Q").GetMethod),
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IPropertySymbol>("Q").SetMethod)
                        }),

                    DocumentResults(),
                });
        }

        [Fact]
        public void Property_Auto_Partial_InsertDelete()
        {
            var srcA1 = "partial class C { }";
            var srcB1 = "partial class C { int P { get; set; } int Q { get; init; } }";

            var srcA2 = "partial class C { int P { get; set; } int Q { get; init; } }";
            var srcB2 = "partial class C { }";

            // Accessors need to be updated even though they do not have an explicit body. 
            // There is still a sequence point generated for them whose location needs to be updated.
            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(
                        semanticEdits: new[]
                        {
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IPropertySymbol>("P").GetMethod),
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IPropertySymbol>("P").SetMethod),
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IPropertySymbol>("Q").GetMethod),
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IPropertySymbol>("Q").SetMethod),
                        }),
                    DocumentResults(),
                });
        }

        [Fact]
        public void Property_AutoWithInitializer_Partial_InsertDelete()
        {
            var srcA1 = "partial class C { }";
            var srcB1 = "partial class C { int P { get; set; } = 1; }";

            var srcA2 = "partial class C { int P { get; set; } = 1; }";
            var srcB2 = "partial class C { }";

            // Accessors need to be updated even though they do not have an explicit body. 
            // There is still a sequence point generated for them whose location needs to be updated.
            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(
                        semanticEdits: new[]
                        {
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IPropertySymbol>("P").GetMethod),
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IPropertySymbol>("P").SetMethod),
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").InstanceConstructors.Single(), partialType: "C", preserveLocalVariables: true)
                        }),

                    DocumentResults(),
                });
        }

        [Fact]
        public void Property_WithExpressionBody_Partial_InsertDeleteUpdate()
        {
            var srcA1 = "partial class C { }";
            var srcB1 = "partial class C { int P => 1; }";

            var srcA2 = "partial class C { int P => 2; }";
            var srcB2 = "partial class C { }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(
                        semanticEdits: new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IPropertySymbol>("P").GetMethod) }),

                    DocumentResults(),
                });
        }

        [Fact]
        public void Property_Auto_ReadOnly_Add()
        {
            var src1 = @"
struct S
{
    int P { get; }
}";
            var src2 = @"
struct S
{
    readonly int P { get; }
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void Property_InMutableStruct_ReadOnly_Add()
        {
            var src1 = @"
struct S
{
     int P1 { get => 1; }
     int P2 { get => 1; set {}}
     int P3 { get => 1; set {}}
     int P4 { get => 1; set {}}
}";
            var src2 = @"
struct S
{
     readonly int P1 { get => 1; }
     int P2 { readonly get => 1; set {}}
     int P3 { get => 1; readonly set {}}
     readonly int P4 { get => 1; set {}}
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "readonly int P1", CSharpFeaturesResources.property_getter),
                Diagnostic(RudeEditKind.ModifiersUpdate, "readonly int P4", CSharpFeaturesResources.property_getter),
                Diagnostic(RudeEditKind.ModifiersUpdate, "readonly int P4", CSharpFeaturesResources.property_setter),
                Diagnostic(RudeEditKind.ModifiersUpdate, "readonly get", CSharpFeaturesResources.property_getter),
                Diagnostic(RudeEditKind.ModifiersUpdate, "readonly set", CSharpFeaturesResources.property_setter));
        }

        [Fact]
        public void Property_InReadOnlyStruct_ReadOnly_Add()
        {
            // indent to align accessor bodies and avoid updates caused by sequence point location changes

            var src1 = @"
readonly struct S
{
              int P1 { get => 1; }
     int P2 {          get => 1; set {}}
     int P3 { get => 1;          set {}}
              int P4 { get => 1; set {}}
}";
            var src2 = @"
readonly struct S
{
     readonly int P1 { get => 1; }
     int P2 { readonly get => 1; set {}}
     int P3 { get => 1; readonly set {}}
     readonly int P4 { get => 1; set {}}
}";
            var edits = GetTopEdits(src1, src2);

            // updates only for accessors whose modifiers were explicitly updated
            edits.VerifySemantics(new[]
            {
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("S").GetMember<IPropertySymbol>("P2").GetMethod, preserveLocalVariables: false),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("S").GetMember<IPropertySymbol>("P3").SetMethod, preserveLocalVariables: false)
            });
        }

        #endregion

        #region Indexers

        [Theory]
        [InlineData("virtual")]
        [InlineData("abstract")]
        [InlineData("override")]
        [InlineData("sealed override", "override")]
        public void Indexer_Modifiers_Update(string oldModifiers, string newModifiers = "")
        {
            if (oldModifiers != "")
            {
                oldModifiers += " ";
            }

            if (newModifiers != "")
            {
                newModifiers += " ";
            }

            var src1 = "class C { " + oldModifiers + "int this[int a] => 0; }";
            var src2 = "class C { " + newModifiers + "int this[int a] => 0; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [" + oldModifiers + "int this[int a] => 0;]@10 -> [" + newModifiers + "int this[int a] => 0;]@10");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, newModifiers + "int this[int a]", FeaturesResources.indexer_));
        }

        [Fact]
        public void Indexer_GetterUpdate()
        {
            var src1 = "class C { int this[int a] { get { return 1; } } }";
            var src2 = "class C { int this[int a] { get { return 2; } } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [get { return 1; }]@28 -> [get { return 2; }]@28");

            edits.VerifySemantics(ActiveStatementsDescription.Empty, new[]
            {
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_Item"), preserveLocalVariables: false)
            });
        }

        [Fact]
        public void Indexer_SetterUpdate()
        {
            var src1 = "class C { int this[int a] { get { return 1; } set { System.Console.WriteLine(value); } } }";
            var src2 = "class C { int this[int a] { get { return 1; } set { System.Console.WriteLine(value + 1); } } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [set { System.Console.WriteLine(value); }]@46 -> [set { System.Console.WriteLine(value + 1); }]@46");

            edits.VerifySemantics(ActiveStatementsDescription.Empty, new[]
            {
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.set_Item"), preserveLocalVariables: false)
            });
        }

        [Fact]
        public void Indexer_InitUpdate()
        {
            var src1 = "class C { int this[int a] { get { return 1; } init { System.Console.WriteLine(value); } } }";
            var src2 = "class C { int this[int a] { get { return 1; } init { System.Console.WriteLine(value + 1); } } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [init { System.Console.WriteLine(value); }]@46 -> [init { System.Console.WriteLine(value + 1); }]@46");

            edits.VerifySemantics(ActiveStatementsDescription.Empty, new[]
            {
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.set_Item"), preserveLocalVariables: false)
            });
        }

        [Fact]
        public void IndexerWithExpressionBody_Update()
        {
            var src1 = "class C { int this[int a] => 1; }";
            var src2 = "class C { int this[int a] => 2; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [int this[int a] => 1;]@10 -> [int this[int a] => 2;]@10",
                "Update [=> 1]@26 -> [=> 2]@26");

            edits.VerifySemantics(ActiveStatementsDescription.Empty, new[]
            {
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_Item"), preserveLocalVariables: false)
            });
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/51297")]
        public void IndexerWithExpressionBody_Update_LiftedParameter()
        {
            var src1 = @"
using System;

class C
{
    int this[int a] => new Func<int>(() => a + 1)() + 10;
}
";
            var src2 = @"
using System;

class C
{
    int this[int a] => new Func<int>(() => 2)() + 11;   // not capturing a anymore
}";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [int this[int a] => new Func<int>(() => a + 1)() + 10;]@35 -> [int this[int a] => new Func<int>(() => 2)() + 11;]@35",
                "Update [=> new Func<int>(() => a + 1)() + 10]@51 -> [=> new Func<int>(() => 2)() + 11]@51");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.NotCapturingVariable, "a", "a"));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/51297")]
        public void IndexerWithExpressionBody_Update_LiftedParameter_2()
        {
            var src1 = @"
using System;

class C
{
    int this[int a] => new Func<int>(() => a + 1)();
}
";
            var src2 = @"
using System;

class C
{
    int this[int a] => new Func<int>(() => 2)();   // not capturing a anymore
}";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [int this[int a] => new Func<int>(() => a + 1)();]@35 -> [int this[int a] => new Func<int>(() => 2)();]@35",
                "Update [=> new Func<int>(() => a + 1)()]@51 -> [=> new Func<int>(() => 2)()]@51");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.NotCapturingVariable, "a", "a"));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/51297")]
        public void IndexerWithExpressionBody_Update_LiftedParameter_3()
        {
            var src1 = @"
using System;

class C
{
    int this[int a] => new Func<int>(() => { return a + 1; })();
}
";
            var src2 = @"
using System;

class C
{
    int this[int a] => new Func<int>(() => { return 2; })();   // not capturing a anymore
}";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [int this[int a] => new Func<int>(() => { return a + 1; })();]@35 -> [int this[int a] => new Func<int>(() => { return 2; })();]@35",
                "Update [=> new Func<int>(() => { return a + 1; })()]@51 -> [=> new Func<int>(() => { return 2; })()]@51");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.NotCapturingVariable, "a", "a"));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/51297")]
        public void IndexerWithExpressionBody_Update_LiftedParameter_4()
        {
            var src1 = @"
using System;

class C
{
    int this[int a] => new Func<int>(delegate { return a + 1; })();
}
";
            var src2 = @"
using System;

class C
{
    int this[int a] => new Func<int>(delegate { return 2; })();   // not capturing a anymore
}";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [int this[int a] => new Func<int>(delegate { return a + 1; })();]@35 -> [int this[int a] => new Func<int>(delegate { return 2; })();]@35",
                "Update [=> new Func<int>(delegate { return a + 1; })()]@51 -> [=> new Func<int>(delegate { return 2; })()]@51");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.NotCapturingVariable, "a", "a"));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17681")]
        public void Indexer_ExpressionBodyToBlockBody()
        {
            var src1 = "class C { int this[int a] => 1; }";
            var src2 = "class C { int this[int a] { get { return 1; } } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [int this[int a] => 1;]@10 -> [int this[int a] { get { return 1; } }]@10",
                "Insert [{ get { return 1; } }]@26",
                "Insert [get { return 1; }]@28",
                "Delete [=> 1]@26");

            edits.VerifySemantics(ActiveStatementsDescription.Empty, new[]
            {
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_Item"), preserveLocalVariables: false)
            });
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17681")]
        public void Indexer_BlockBodyToExpressionBody()
        {
            var src1 = "class C { int this[int a] { get { return 1; } } }";
            var src2 = "class C { int this[int a] => 1; } ";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [int this[int a] { get { return 1; } }]@10 -> [int this[int a] => 1;]@10",
                "Insert [=> 1]@26",
                "Delete [{ get { return 1; } }]@26",
                "Delete [get { return 1; }]@28");

            edits.VerifySemantics(ActiveStatementsDescription.Empty, new[]
            {
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_Item"), preserveLocalVariables: false)
            });
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17681")]
        public void Indexer_GetterExpressionBodyToBlockBody()
        {
            var src1 = "class C { int this[int a] { get => 1; } }";
            var src2 = "class C { int this[int a] { get { return 1; } } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [get => 1;]@28 -> [get { return 1; }]@28");

            edits.VerifySemantics(ActiveStatementsDescription.Empty, new[]
            {
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_Item"), preserveLocalVariables: false)
            });
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17681")]
        public void Indexer_BlockBodyToGetterExpressionBody()
        {
            var src1 = "class C { int this[int a] { get { return 1; } } }";
            var src2 = "class C { int this[int a] { get => 1; } }";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [get { return 1; }]@28 -> [get => 1;]@28");
            edits.VerifySemanticDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17681")]
        public void Indexer_GetterExpressionBodyToExpressionBody()
        {
            var src1 = "class C { int this[int a] { get => 1; } }";
            var src2 = "class C { int this[int a] => 1; } ";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [int this[int a] { get => 1; }]@10 -> [int this[int a] => 1;]@10",
                "Insert [=> 1]@26",
                "Delete [{ get => 1; }]@26",
                "Delete [get => 1;]@28");

            edits.VerifySemantics(ActiveStatementsDescription.Empty, new[]
            {
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_Item"), preserveLocalVariables: false)
            });
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17681")]
        public void Indexer_ExpressionBodyToGetterExpressionBody()
        {
            var src1 = "class C { int this[int a] => 1; }";
            var src2 = "class C { int this[int a] { get => 1; } }";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [int this[int a] => 1;]@10 -> [int this[int a] { get => 1; }]@10",
                "Insert [{ get => 1; }]@26",
                "Insert [get => 1;]@28",
                "Delete [=> 1]@26");

            edits.VerifySemantics(ActiveStatementsDescription.Empty, new[]
            {
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_Item"), preserveLocalVariables: false)
            });
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17681")]
        public void Indexer_GetterBlockBodyToGetterExpressionBody()
        {
            var src1 = "class C { int this[int a] { get { return 1; } set { Console.WriteLine(0); } } }";
            var src2 = "class C { int this[int a] { get => 1;         set { Console.WriteLine(0); } } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [get { return 1; }]@28 -> [get => 1;]@28");

            edits.VerifySemantics(ActiveStatementsDescription.Empty, new[]
            {
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_Item"), preserveLocalVariables: false),
            });
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17681")]
        public void Indexer_SetterBlockBodyToSetterExpressionBody()
        {
            var src1 = "class C { int this[int a] { set { } } void F() { } }";
            var src2 = "class C { int this[int a] { set => F(); } void F() { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [set { }]@28 -> [set => F();]@28");

            edits.VerifySemantics(ActiveStatementsDescription.Empty, new[]
            {
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.set_Item")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.F")),
            });
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17681")]
        public void Indexer_InitBlockBodyToInitExpressionBody()
        {
            var src1 = "class C { int this[int a] { init { } } void F() { } }";
            var src2 = "class C { int this[int a] { init => F(); } void F() { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [init { }]@28 -> [init => F();]@28");

            edits.VerifySemantics(ActiveStatementsDescription.Empty, new[]
            {
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.set_Item")),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.F")),
            });
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17681")]
        public void Indexer_GetterExpressionBodyToGetterBlockBody()
        {
            var src1 = "class C { int this[int a] { get => 1; set { Console.WriteLine(0); } } }";
            var src2 = "class C { int this[int a] { get { return 1; } set { Console.WriteLine(0); } } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [get => 1;]@28 -> [get { return 1; }]@28");

            edits.VerifySemantics(ActiveStatementsDescription.Empty, new[]
            {
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_Item"), preserveLocalVariables: false),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.set_Item"), preserveLocalVariables: false)
            });
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17681")]
        public void Indexer_GetterAndSetterBlockBodiesToExpressionBody()
        {
            var src1 = "class C { int this[int a] { get { return 1; } set { Console.WriteLine(0); } } }";
            var src2 = "class C { int this[int a] => 1; }";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [int this[int a] { get { return 1; } set { Console.WriteLine(0); } }]@10 -> [int this[int a] => 1;]@10",
                "Insert [=> 1]@26",
                "Delete [{ get { return 1; } set { Console.WriteLine(0); } }]@26",
                "Delete [get { return 1; }]@28",
                "Delete [set { Console.WriteLine(0); }]@46");

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_Item")),
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.set_Item"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17681")]
        public void Indexer_ExpressionBodyToGetterAndSetterBlockBodies()
        {
            var src1 = "class C { int this[int a] => 1; }";
            var src2 = "class C { int this[int a] { get { return 1; } set { Console.WriteLine(0); } } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [int this[int a] => 1;]@10 -> [int this[int a] { get { return 1; } set { Console.WriteLine(0); } }]@10",
                "Insert [{ get { return 1; } set { Console.WriteLine(0); } }]@26",
                "Insert [get { return 1; }]@28",
                "Insert [set { Console.WriteLine(0); }]@46",
                "Delete [=> 1]@26");

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_Item"), preserveLocalVariables: false),
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.set_Item"), preserveLocalVariables: false)
                },
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
        }

        [Fact]
        public void Indexer_Rename()
        {
            var interfaces = """
                interface I
                {
                    int this[int a] { get; }
                }
                
                interface J
                {
                    int this[int a] { get; }
                }
                """;
            var src1 = "class C { int I.this[int a] { get { return 1; } } } " + interfaces;
            var src2 = "class C { int J.this[int a] { get { return 1; } } } " + interfaces;

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "int J.this[int a]", CSharpFeaturesResources.indexer));
        }

        [Fact]
        public void Indexer_Reorder1()
        {
            var src1 = "class C { int this[int a] { get { return 1; } } int this[string a] { get { return 1; } }  }";
            var src2 = "class C { int this[string a] { get { return 1; } } int this[int a] { get { return 1; } }  }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Reorder [int this[string a] { get { return 1; } }]@48 -> @10");

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void Indexer_AccessorReorder()
        {
            var src1 = "class C { int this[int a] { get { return 1; } set { } } }";
            var src2 = "class C { int this[int a] { set { } get { return 1; } } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Reorder [set { }]@46 -> @28");

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void Indexer_TypeUpdate()
        {
            var src1 = "class C { int this[int a] { get; set; } }";
            var src2 = "class C { string this[int a] { get; set; } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [int this[int a] { get; set; }]@10 -> [string this[int a] { get; set; }]@10");

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.get_Item"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMembers("C.set_Item").FirstOrDefault(p => p.GetParameters()[1].Type.SpecialType == SpecialType.System_Int32)?.ISymbol, deletedSymbolContainerProvider: c => c.GetMember("C")),
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.this[]")),
                },
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.AddInstanceFieldToExistingType);

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingTypeNotSupportedByRuntime, "string this[int a]", CSharpFeaturesResources.indexer) },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void Tuple_TypeUpdate()
        {
            var src1 = "class C { (int, int) M() { throw new System.Exception(); } }";
            var src2 = "class C { (string, int) M() { throw new System.Exception(); } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [(int, int) M() { throw new System.Exception(); }]@10 -> [(string, int) M() { throw new System.Exception(); }]@10");

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.M"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.M"))
                },
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingTypeNotSupportedByRuntime, "(string, int) M()", FeaturesResources.method) },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void TupleElementDelete()
        {
            var src1 = "class C { (int, int, int a) M() { return (1, 2, 3); } }";
            var src2 = "class C { (int, int) M() { return (1, 2); } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [(int, int, int a) M() { return (1, 2, 3); }]@10 -> [(int, int) M() { return (1, 2); }]@10");

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.M"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.M"))
                },
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingTypeNotSupportedByRuntime, "(int, int) M()", FeaturesResources.method) },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void TupleElementAdd()
        {
            var src1 = "class C { (int, int) M() { return (1, 2); } }";
            var src2 = "class C { (int, int, int a) M() { return (1, 2, 3); } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [(int, int) M() { return (1, 2); }]@10 -> [(int, int, int a) M() { return (1, 2, 3); }]@10");

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.M"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("C.M"))
                },
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingTypeNotSupportedByRuntime, "(int, int, int a) M()", FeaturesResources.method) },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void Indexer_ParameterUpdate()
        {
            var src1 = "class C { int this[int a] { get; set; } }";
            var src2 = "class C { int this[string a] { get; set; } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMembers("C.get_Item").FirstOrDefault(m => m.GetParameterTypes()[0].SpecialType == SpecialType.System_Int32)?.ISymbol, deletedSymbolContainerProvider: c => c.GetMember("C")),
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMembers("C.set_Item").FirstOrDefault(m => m.GetParameterTypes()[0].SpecialType == SpecialType.System_Int32)?.ISymbol, deletedSymbolContainerProvider: c => c.GetMember("C")),
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMembers("C.this[]").FirstOrDefault(m => m.GetParameterTypes()[0].SpecialType == SpecialType.System_String)?.ISymbol),
                },
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
        }

        [Fact]
        public void Indexer_ParameterInsert()
        {
            var src1 = "class C { int this[int a] { get; set; } }";
            var src2 = "class C { int this[int a, string b] { get; set; } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMembers("C.get_Item").FirstOrDefault(m => m.GetParameterCount() == 1)?.ISymbol, deletedSymbolContainerProvider: c => c.GetMember("C")),
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMembers("C.set_Item").FirstOrDefault(m => m.GetParameterCount() == 2)?.ISymbol, deletedSymbolContainerProvider: c => c.GetMember("C")),
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMembers("C.this[]").FirstOrDefault(m => m.GetParameterCount() == 2)?.ISymbol),
                },
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
        }

        [Fact]
        public void Indexer_ParameterDelete()
        {
            var src1 = "class C { int this[int a, string b] { get; set; } }";
            var src2 = "class C { int this[int a] { get; set; } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMembers("C.get_Item").FirstOrDefault(m => m.GetParameterCount() == 2)?.ISymbol, deletedSymbolContainerProvider: c => c.GetMember("C")),
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMembers("C.set_Item").FirstOrDefault(m => m.GetParameterCount() == 3)?.ISymbol, deletedSymbolContainerProvider: c => c.GetMember("C")),
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMembers("C.this[]").FirstOrDefault(m => m.GetParameterCount() == 1)?.ISymbol),
                },
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
        }

        [Fact]
        public void Indexer_AddGetAccessor()
        {
            var src1 = @"
class Test
{
    static void Main(string[] args)
    {
        SampleCollection<string> stringCollection = new SampleCollection<string>();
        stringCollection[0] = ""hello"";
    }
}

class SampleCollection<T>
{
    private T[] arr = new T[100];
    public T this[int i]
    {
        set { arr[i] = value; }
    }
}";
            var src2 = @"
class Test
{
    static void Main(string[] args)
    {
        SampleCollection<string> stringCollection = new SampleCollection<string>();
        stringCollection[0] = ""hello"";
    }
}

class SampleCollection<T>
{
    private T[] arr = new T[100];
    public T this[int i]
    {
        get { return arr[i]; }
        set { arr[i] = value; }
    }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Insert [get { return arr[i]; }]@304");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.InsertIntoGenericType, "get", CSharpFeaturesResources.indexer_getter));
        }

        [Fact]
        public void Indexer_AddSetAccessor()
        {
            var src1 = @"
class C
{
    public int this[int i] { get { return default; } }
}";
            var src2 = @"
class C
{
    public int this[int i] { get { return default; } set { } }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Insert [set { }]@67");

            edits.VerifySemantics(
                new[] { SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IPropertySymbol>("this[]").SetMethod) },
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
        }

        [Fact]
        public void Indexer_AddSetAccessor_GenericType()
        {
            var src1 = @"
class C<T>
{
    public T this[int i] { get { return default; } }
}";
            var src2 = @"
class C<T>
{
    public T this[int i] { get { return default; } set { } }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Insert [set { }]@68");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.InsertIntoGenericType, "set", CSharpFeaturesResources.indexer_setter));
        }

        [Fact]
        public void Indexer_Delete()
        {
            var src1 = @"
class C<T>
{
    public T this[int i]
    {
        get { return arr[i]; }
        set { arr[i] = value; }
    }
}";
            var src2 = @"
class C<T>
{
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.get_Item"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.set_Item"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/750109")]
        public void Indexer_DeleteGetAccessor()
        {
            var src1 = @"
class C<T>
{
    public T this[int i]
    {
        get { return arr[i]; }
        set { arr[i] = value; }
    }
}";
            var src2 = @"
class C<T>
{
    public T this[int i]
    {
        set { arr[i] = value; }
    }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.get_Item"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void Indexer_DeleteSetAccessor()
        {
            var src1 = @"
class C
{
    public int this[int i] { get { return 0; } set { } }
}";
            var src2 = @"
class C
{
    public int this[int i] { get { return 0; } }
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.set_Item"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1174850")]
        public void Indexer_Insert()
        {
            var src1 = "struct C { }";
            var src2 = "struct C { public int this[int x, int y] { get { return x + y; } } }";

            var edits = GetTopEdits(src1, src2);
            edits.VerifySemanticDiagnostics(
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
        }

        [Fact]
        public void Indexer_ReadOnlyRef_Parameter_InsertWhole()
        {
            var src1 = "class Test { }";
            var src2 = "class Test { int this[in int i] => throw null; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [int this[in int i] => throw null;]@13",
                "Insert [[in int i]]@21",
                "Insert [=> throw null]@32",
                "Insert [in int i]@22");

            edits.VerifySemanticDiagnostics(
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
        }

        [Fact]
        public void Indexer_ReadOnlyRef_Parameter_Update()
        {
            var src1 = "class Test { int this[int i] => throw null; }";
            var src2 = "class Test { int this[in int i] => throw null; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [int i]@22 -> [in int i]@22");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "in int i", FeaturesResources.parameter));
        }

        [Fact]
        public void Indexer_ReadOnlyRef_ReturnType_Insert()
        {
            var src1 = "class Test { }";
            var src2 = "class Test { ref readonly int this[int i] => throw null; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [ref readonly int this[int i] => throw null;]@13",
                "Insert [[int i]]@34",
                "Insert [=> throw null]@42",
                "Insert [int i]@35");

            edits.VerifySemanticDiagnostics(
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
        }

        [Fact]
        public void Indexer_ReadOnlyRef_ReturnType_Update()
        {
            var src1 = "class Test { int this[int i] => throw null; }";
            var src2 = "class Test { ref readonly int this[int i] => throw null; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [int this[int i] => throw null;]@13 -> [ref readonly int this[int i] => throw null;]@13");

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("Test.get_Item"), deletedSymbolContainerProvider: c => c.GetMember("Test")),
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("Test.this[]")),
                },
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType | EditAndContinueCapabilities.AddInstanceFieldToExistingType);

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingTypeNotSupportedByRuntime, "ref readonly int this[int i]", FeaturesResources.indexer_) },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void Indexer_Partial_InsertDelete()
        {
            var srcA1 = "partial class C { }";
            var srcB1 = "partial class C { int this[int x] { get => 1; set { } } }";

            var srcA2 = "partial class C { int this[int x] { get => 1; set { } } }";
            var srcB2 = "partial class C { }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(
                        semanticEdits: new[]
                        {
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IPropertySymbol>("this[]").GetMethod),
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IPropertySymbol>("this[]").SetMethod)
                        }),

                    DocumentResults(),
                });
        }

        [Fact]
        public void IndexerInit_Partial_InsertDelete()
        {
            var srcA1 = "partial class C { }";
            var srcB1 = "partial class C { int this[int x] { get => 1; init { } }}";

            var srcA2 = "partial class C { int this[int x] { get => 1; init { } }}";
            var srcB2 = "partial class C { }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(
                        semanticEdits: new[]
                        {
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IPropertySymbol>("this[]").GetMethod),
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IPropertySymbol>("this[]").SetMethod)
                        }),

                    DocumentResults(),
                });
        }

        [Fact]
        public void AutoIndexer_Partial_InsertDelete()
        {
            var srcA1 = "partial class C { }";
            var srcB1 = "partial class C { int this[int x] { get; set; } }";

            var srcA2 = "partial class C { int this[int x] { get; set; } }";
            var srcB2 = "partial class C { }";

            // Accessors need to be updated even though they do not have an explicit body. 
            // There is still a sequence point generated for them whose location needs to be updated.
            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(
                        semanticEdits: new[]
                        {
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IPropertySymbol>("this[]").GetMethod),
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IPropertySymbol>("this[]").SetMethod),
                        }),
                    DocumentResults(),
                });
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/51297")]
        public void IndexerWithExpressionBody_Partial_InsertDeleteUpdate_LiftedParameter()
        {
            var srcA1 = @"
partial class C
{
}";
            var srcB1 = @"
partial class C
{
    int this[int a] => new System.Func<int>(() => a + 1);
}";

            var srcA2 = @"
partial class C
{
    int this[int a] => new System.Func<int>(() => 2); // no capture
}";
            var srcB2 = @"
partial class C
{
}";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(diagnostics: new[] { Diagnostic(RudeEditKind.NotCapturingVariable, "a", "a") }),
                    DocumentResults(),
                });
        }

        [Fact]
        public void AutoIndexer_ReadOnly_Add()
        {
            var src1 = @"
struct S
{
    int this[int x] { get; }
}";
            var src2 = @"
struct S
{
    readonly int this[int x] { get; }
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "readonly int this[int x]", CSharpFeaturesResources.indexer_getter));
        }

        [Fact]
        public void Indexer_InMutableStruct_ReadOnly_Add()
        {
            var src1 = @"
struct S
{
     int this[int x] { get => 1; }
     int this[uint x] { get => 1; set {}}
     int this[byte x] { get => 1; set {}}
     int this[sbyte x] { get => 1; set {}}
}";
            var src2 = @"
struct S
{
     readonly int this[int x] { get => 1; }
     int this[uint x] { readonly get => 1; set {}}
     int this[byte x] { get => 1; readonly set {}}
     readonly int this[sbyte x] { get => 1; set {}}
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "readonly int this[int x]", CSharpFeaturesResources.indexer_getter),
                Diagnostic(RudeEditKind.ModifiersUpdate, "readonly int this[sbyte x]", CSharpFeaturesResources.indexer_getter),
                Diagnostic(RudeEditKind.ModifiersUpdate, "readonly int this[sbyte x]", CSharpFeaturesResources.indexer_setter),
                Diagnostic(RudeEditKind.ModifiersUpdate, "readonly get", CSharpFeaturesResources.indexer_getter),
                Diagnostic(RudeEditKind.ModifiersUpdate, "readonly set", CSharpFeaturesResources.indexer_setter));
        }

        [Fact]
        public void Indexer_InReadOnlyStruct_ReadOnly_Add()
        {
            // indent to align accessor bodies and avoid updates caused by sequence point location changes

            var src1 = @"
readonly struct S
{
              int this[int x] { get => 1; }
     int this[uint x] {          get => 1; set {}}
     int this[byte x] { get => 1;          set {}}
              int this[sbyte x] { get => 1; set {}}
}";
            var src2 = @"
readonly struct S
{
     readonly int this[int x] { get => 1; }
     int this[uint x] { readonly get => 1; set {}}
     int this[byte x] { get => 1; readonly set {}}
     readonly int this[sbyte x] { get => 1; set {}}
}";
            var edits = GetTopEdits(src1, src2);

            // updates only for accessors whose modifiers were explicitly updated
            edits.VerifySemantics(new[]
            {
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("S").GetMembers("this[]").Cast<IPropertySymbol>().Single(m => m.Parameters.Single().Type.Name == "UInt32").GetMethod, preserveLocalVariables: false),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("S").GetMembers("this[]").Cast<IPropertySymbol>().Single(m => m.Parameters.Single().Type.Name == "Byte").SetMethod, preserveLocalVariables: false)
            });
        }

        #endregion

        #region Events

        [Theory]
        [InlineData("static")]
        [InlineData("virtual")]
        [InlineData("abstract")]
        [InlineData("override")]
        [InlineData("sealed override", "override")]
        public void Event_Modifiers_Update(string oldModifiers, string newModifiers = "")
        {
            if (oldModifiers != "")
            {
                oldModifiers += " ";
            }

            if (newModifiers != "")
            {
                newModifiers += " ";
            }

            var src1 = "class C { " + oldModifiers + "event Action F { add {} remove {} } }";
            var src2 = "class C { " + newModifiers + "event Action F { add {} remove {} } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [" + oldModifiers + "event Action F { add {} remove {} }]@10 -> [" + newModifiers + "event Action F { add {} remove {} }]@10");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, newModifiers + "event Action F", FeaturesResources.event_));
        }

        [Fact]
        public void Event_Accessor_Reorder1()
        {
            var src1 = "class C { event int E { add { } remove { } } }";
            var src2 = "class C { event int E { remove { } add { } } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Reorder [remove { }]@32 -> @24");

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void Event_Accessor_Reorder2()
        {
            var src1 = "class C { event int E1 { add { } remove { } }    event int E1 { add { } remove { } } }";
            var src2 = "class C { event int E2 { remove { } add { } }    event int E2 { remove { } add { } } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [event int E1 { add { } remove { } }]@10 -> [event int E2 { remove { } add { } }]@10",
                "Update [event int E1 { add { } remove { } }]@49 -> [event int E2 { remove { } add { } }]@49",
                "Reorder [remove { }]@33 -> @25",
                "Reorder [remove { }]@72 -> @64");
        }

        [Fact]
        public void Event_Accessor_Reorder3()
        {
            var src1 = "class C { event int E1 { add { } remove { } }    event int E2 { add { } remove { } } }";
            var src2 = "class C { event int E2 { remove { } add { } }    event int E1 { remove { } add { } } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Reorder [event int E2 { add { } remove { } }]@49 -> @10",
                "Reorder [remove { }]@72 -> @25",
                "Reorder [remove { }]@33 -> @64");
        }

        [Fact]
        public void Event_Insert()
        {
            var src1 = "class C { }";
            var src2 = "class C { event int E { remove { } add { } } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMember<INamedTypeSymbol>("C").GetMember("E"))
                },
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
        }

        [Fact]
        public void Event_Delete1()
        {
            var src1 = "class C { event int E { remove { } add { } } }";
            var src2 = "class C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.add_E"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.remove_E"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void Event_Delete2()
        {
            var src1 = "class C { event int E; }";
            var src2 = "class C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.add_E"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.remove_E"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void Event_Insert_IntoLayoutClass_Sequential()
        {
            var src1 = @"
using System;
using System.Runtime.InteropServices;

[StructLayoutAttribute(LayoutKind.Sequential)]
class C 
{ 
}
";
            var src2 = @"
using System;
using System.Runtime.InteropServices;

[StructLayoutAttribute(LayoutKind.Sequential)]
class C 
{ 
    private event Action c { add { } remove { } } 
}
";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17681")]
        public void Event_ExpressionBodyToBlockBody()
        {
            var src1 = @"
using System;
public class C
{
    event Action E { add => F(); remove => F(); }
}
";
            var src2 = @"
using System;
public class C
{
   event Action E { add { F(); } remove { } }
}
";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [add => F();]@57 -> [add { F(); }]@56",
                "Update [remove => F();]@69 -> [remove { }]@69"
                );

            edits.VerifySemanticDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17681")]
        public void Event_BlockBodyToExpressionBody()
        {
            var src1 = @"
using System;
public class C
{
   event Action E { add { F(); } remove { } }
}
";
            var src2 = @"
using System;
public class C
{
    event Action E { add => F(); remove => F(); }
}
";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [add { F(); }]@56 -> [add => F();]@57",
                "Update [remove { }]@69 -> [remove => F();]@69"
                );

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void Event_Partial_InsertDelete()
        {
            var srcA1 = "partial class C { }";
            var srcB1 = "partial class C { event int E { add { } remove { } } }";

            var srcA2 = "partial class C { event int E { add { } remove { } } }";
            var srcB2 = "partial class C { }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(
                        semanticEdits: new[]
                        {
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IEventSymbol>("E").AddMethod),
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember<IEventSymbol>("E").RemoveMethod)
                        }),

                    DocumentResults(),
                });
        }

        [Fact]
        public void Event_InMutableStruct_ReadOnly_Add()
        {
            var src1 = @"
struct S
{
    public event Action E { add {} remove {} }
}";
            var src2 = @"
struct S
{
    public readonly event Action E { add {} remove {} }
}";
            var edits = GetTopEdits(src1, src2);
            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "public readonly event Action E", FeaturesResources.event_));
        }

        [Fact]
        public void Event_InReadOnlyStruct_ReadOnly_Add1()
        {
            var src1 = @"
readonly struct S
{
    public event Action E { add {} remove {} }
}";
            var src2 = @"
readonly struct S
{
    public readonly event Action E { add {} remove {} }
}";
            var edits = GetTopEdits(src1, src2);

            // Currently, an edit is produced eventhough bodies nor IsReadOnly attribute have changed. Consider improving.
            edits.VerifySemantics(
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("S").GetMember<IEventSymbol>("E").AddMethod),
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("S").GetMember<IEventSymbol>("E").RemoveMethod));
        }

        [Fact]
        public void Field_Event_Attribute_Add()
        {
            var src1 = @"
class C
{
    event Action F;
}";
            var src2 = @"
class C
{
    [System.Obsolete]event Action F;
}";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [event Action F;]@18 -> [[System.Obsolete]event Action F;]@18");

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "event Action F", FeaturesResources.event_) },
                capabilities: EditAndContinueCapabilities.Baseline);

            edits.VerifySemantics(
               new[]
               {
                   SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IEventSymbol>("C.F"))
               },
               capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
        }

        [Fact]
        public void Event_Attribute_Add()
        {
            var src1 = @"
class C
{
    event Action F { add {} remove {} }
}";
            var src2 = @"
class C
{
    [System.Obsolete]event Action F { add {} remove {} }
}";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [event Action F { add {} remove {} }]@18 -> [[System.Obsolete]event Action F { add {} remove {} }]@18");

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "event Action F", FeaturesResources.event_) },
                capabilities: EditAndContinueCapabilities.Baseline);

            edits.VerifySemantics(
               new[]
               {
                   SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IEventSymbol>("C.F")),
                   SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IEventSymbol>("C.F").AddMethod),
                   SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IEventSymbol>("C.F").RemoveMethod)
               },
               capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
        }

        [Fact]
        public void Event_Accessor_Attribute_Add()
        {
            var src1 = @"
class C
{
    event Action F { add {} remove {} }
}";
            var src2 = @"
class C
{
    event Action F { add {} [System.Obsolete]remove {} }
}";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [remove {}]@42 -> [[System.Obsolete]remove {}]@42");

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "remove", FeaturesResources.event_accessor) },
                capabilities: EditAndContinueCapabilities.Baseline);

            edits.VerifySemantics(
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IEventSymbol>("C.F").RemoveMethod) },
                capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
        }

        [Fact]
        public void Field_Event_Attribute_Delete()
        {
            var src1 = @"
class C
{
    [System.Obsolete]event Action F;
}";
            var src2 = @"
class C
{
    event Action F;
}";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [[System.Obsolete]event Action F;]@18 -> [event Action F;]@18");

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "event Action F", FeaturesResources.event_) },
                capabilities: EditAndContinueCapabilities.Baseline);

            edits.VerifySemantics(
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IEventSymbol>("C.F")) },
                capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
        }

        [Fact]
        public void Event_Attribute_Delete()
        {
            var src1 = @"
class C
{
    [System.Obsolete]event Action F { add {} remove {} }
}";
            var src2 = @"
class C
{
    event Action F { add {} remove {} }
}";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [[System.Obsolete]event Action F { add {} remove {} }]@18 -> [event Action F { add {} remove {} }]@18");

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "event Action F", FeaturesResources.event_) },
                capabilities: EditAndContinueCapabilities.Baseline);

            edits.VerifySemantics(
               new[]
               {
                   SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IEventSymbol>("C.F")),
                   SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IEventSymbol>("C.F").AddMethod),
                   SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IEventSymbol>("C.F").RemoveMethod)
               },
               capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
        }

        [Fact]
        public void Event_Accessor_Attribute_Delete()
        {
            var src1 = @"
class C
{
    event Action F { add {} [System.Obsolete]remove {} }
}";
            var src2 = @"
class C
{
    event Action F { add {} remove {} }
}";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [[System.Obsolete]remove {}]@42 -> [remove {}]@42");

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "remove", FeaturesResources.event_accessor) },
                capabilities: EditAndContinueCapabilities.Baseline);

            edits.VerifySemantics(
               new[]
               {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember<IEventSymbol>("C.F").RemoveMethod)
               },
               capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
        }

        #endregion

        #region Parameter

        [Fact]
        public void ParameterRename_Method1()
        {
            var src1 = @"class C { public void M(int a) { a.ToString(); } }";
            var src2 = @"class C { public void M(int b) { b.ToString(); } } ";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyEdits(
                "Update [public void M(int a) { a.ToString(); }]@10 -> [public void M(int b) { b.ToString(); }]@10",
                "Update [int a]@24 -> [int b]@24");

            edits.VerifySemantics(
                new[] {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.M"))
                },
                capabilities: EditAndContinueCapabilities.UpdateParameters);

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.RenamingNotSupportedByRuntime, "int b", FeaturesResources.parameter) },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void ParameterRename_Ctor1()
        {
            var src1 = @"class C { public C(int a) {} }";
            var src2 = @"class C { public C(int b) {} } ";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyEdits(
                "Update [int a]@19 -> [int b]@19");
        }

        [Fact]
        public void ParameterRename_Operator1()
        {
            var src1 = @"class C { public static implicit operator int(C a) {} }";
            var src2 = @"class C { public static implicit operator int(C b) {} } ";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyEdits(
                "Update [C a]@46 -> [C b]@46");
        }

        [Fact]
        public void ParameterRename_Operator2()
        {
            var src1 = @"class C { public static int operator +(C a, C b) { return 0; } }";
            var src2 = @"class C { public static int operator +(C a, C x) { return 0; } } ";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyEdits(
                "Update [C b]@44 -> [C x]@44");
        }

        [Fact]
        public void ParameterRename_Indexer2()
        {
            var src1 = @"class C { public int this[int a, int b] { get { return 0; } } }";
            var src2 = @"class C { public int this[int a, int x] { get { return 0; } } }";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyEdits(
                "Update [int b]@33 -> [int x]@33");
        }

        [Fact]
        public void ParameterInsert1()
        {
            var src1 = @"class C { public void M() {} }";
            var src2 = @"class C { public void M(int a) { a.ToString(); } } ";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyEdits(
                "Update [public void M() {}]@10 -> [public void M(int a) { a.ToString(); }]@10",
                "Insert [int a]@24");

            edits.VerifySemantics(
                new[] {
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMembers("C.M").FirstOrDefault(m => m.GetParameterCount() == 0)?.ISymbol, deletedSymbolContainerProvider: c => c.GetMember("C")),
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMembers("C.M").FirstOrDefault(m => m.GetParameterCount() == 1)?.ISymbol)
                },
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
        }

        [Fact]
        public void ParameterInsert2()
        {
            var src1 = @"class C { public void M(int a) {} }";
            var src2 = @"class C { public void M(int a, ref int b) {} } ";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyEdits(
                "Update [(int a)]@23 -> [(int a, ref int b)]@23",
                "Insert [ref int b]@31");

            edits.VerifySemantics(
                new[] {
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMembers("C.M").FirstOrDefault(m => m.GetParameterCount() == 1)?.ISymbol, deletedSymbolContainerProvider: c => c.GetMember("C")),
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMembers("C.M").FirstOrDefault(m => m.GetParameterCount() == 2)?.ISymbol)
                },
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
        }

        [Fact]
        public void ParameterInsert3()
        {
            var src1 = @"class C { public void M() {} }";
            var src2 = @"class C { public int M(int a) { return a; } } ";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyEdits(
                "Update [public void M() {}]@10 -> [public int M(int a) { return a; }]@10",
                "Insert [int a]@23");

            edits.VerifySemantics(
                new[] {
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMembers("C.M").FirstOrDefault(m => m.GetParameterCount() == 0)?.ISymbol, deletedSymbolContainerProvider: c => c.GetMember("C")),
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMembers("C.M").FirstOrDefault(m => m.GetParameterCount() == 1)?.ISymbol)
                },
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
        }

        [Fact]
        public void ParameterDelete1()
        {
            var src1 = @"class C { public void M(int a) {} }";
            var src2 = @"class C { public void M() {} } ";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyEdits(
                "Delete [int a]@24");

            edits.VerifySemantics(
                new[] {
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMembers("C.M").FirstOrDefault(m => m.GetParameterCount() == 1)?.ISymbol, deletedSymbolContainerProvider: c => c.GetMember("C")),
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMembers("C.M").FirstOrDefault(m => m.GetParameterCount() == 0)?.ISymbol)
                },
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
        }

        [Fact]
        public void ParameterDelete2()
        {
            var src1 = @"class C { public void M(int a, int b) {} }";
            var src2 = @"class C { public void M(int b) {} } ";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyEdits(
                "Update [(int a, int b)]@23 -> [(int b)]@23",
                "Delete [int a]@24");

            edits.VerifySemantics(
                new[] {
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMembers("C.M").FirstOrDefault(m => m.GetParameterCount() == 2)?.ISymbol, deletedSymbolContainerProvider: c => c.GetMember("C")),
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMembers("C.M").FirstOrDefault(m => m.GetParameterCount() == 1)?.ISymbol)
                },
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
        }

        [Fact]
        public void ParameterUpdate()
        {
            var src1 = @"class C { public void M(int a) {} }";
            var src2 = @"class C { public void M(int b) {} } ";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyEdits(
                "Update [int a]@24 -> [int b]@24");
        }

        [Fact]
        public void ParameterReorder()
        {
            var src1 = @"class C { public void M(int a, int b) {} }";
            var src2 = @"class C { public void M(int b, int a) {} } ";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyEdits(
                "Reorder [int b]@31 -> @24");

            edits.VerifySemantics(
               new[] {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.M"))
               },
               capabilities: EditAndContinueCapabilities.UpdateParameters);

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.RenamingNotSupportedByRuntime, "int b", FeaturesResources.parameter) },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void ParameterReorderAndUpdate()
        {
            var src1 = @"class C { public void M(int a, int b) { a.ToString(); } }";
            var src2 = @"class C { public void M(int b, int a) { b.ToString(); } } ";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyEdits(
                "Update [public void M(int a, int b) { a.ToString(); }]@10 -> [public void M(int b, int a) { b.ToString(); }]@10",
                "Reorder [int b]@31 -> @24");

            edits.VerifySemantics(
               new[] {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.M"))
               },
               capabilities: EditAndContinueCapabilities.UpdateParameters);

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.RenamingNotSupportedByRuntime, "int b", FeaturesResources.parameter) },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void ParameterReorderAndChangeTypes()
        {
            var src1 = @"class C { public void M(string a, int b) { a.ToString(); } }";
            var src2 = @"class C { public void M(int b, string a) { b.ToString(); } } ";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyEdits(
                "Update [public void M(string a, int b) { a.ToString(); }]@10 -> [public void M(int b, string a) { b.ToString(); }]@10",
                "Reorder [int b]@34 -> @24");

            edits.VerifySemantics(
               new[] {
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMembers("C.M").FirstOrDefault(m => m.GetParameterTypes()[0].SpecialType == SpecialType.System_String)?.ISymbol, deletedSymbolContainerProvider: c => c.GetMember("C")),
                    SemanticEdit(SemanticEditKind.Insert, c => c.GetMembers("C.M").FirstOrDefault(m => m.GetParameterTypes()[0].SpecialType == SpecialType.System_Int32)?.ISymbol)
               },
               capabilities: EditAndContinueCapabilities.AddMethodToExistingType);

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.RenamingNotSupportedByRuntime, "int b", FeaturesResources.parameter) },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void ParameterReorderAndRename()
        {
            var src1 = @"class C { public void M(int a, int b) {} }";
            var src2 = @"class C { public void M(int b, int c) {} } ";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyEdits(
                "Reorder [int b]@31 -> @24",
                "Update [int a]@24 -> [int c]@31");

            edits.VerifySemantics(
                new[] {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.M"))
                },
                capabilities: EditAndContinueCapabilities.UpdateParameters);

            edits.VerifySemanticDiagnostics(
                new[] {
                    Diagnostic(RudeEditKind.RenamingNotSupportedByRuntime, "int b", FeaturesResources.parameter),
                    Diagnostic(RudeEditKind.RenamingNotSupportedByRuntime, "int c", FeaturesResources.parameter)
                },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Theory]
        [InlineData("string", "string?")]
        [InlineData("object", "dynamic")]
        [InlineData("(int a, int b)", "(int a, int c)")]
        public void Parameter_Type_Update_RuntimeTypeUnchanged(string oldType, string newType)
        {
            var src1 = "class C { static void M(" + oldType + " a) {} }";
            var src2 = "class C { static void M(" + newType + " a) {} }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(
                SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.M")));
        }

        [Fact]
        public void Parameter_Type_Nullable()
        {
            var src1 = @"
#nullable enable
class C { static void M(string a) { } }
";
            var src2 = @"
#nullable disable
class C { static void M(string a) { } }
";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics();
        }

        [Theory]
        [InlineData("this")]
        [InlineData("ref")]
        [InlineData("out")]
        [InlineData("params")]
        public void Parameter_Modifier_Remove(string modifier)
        {
            var src1 = @"static class C { static void F(" + modifier + " int[] a) { } }";
            var src2 = @"static class C { static void F(int[] a) { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ModifiersUpdate, "int[] a", FeaturesResources.parameter));
        }

        [Theory]
        [InlineData("int a = 1", "int a = 2")]
        [InlineData("int a = 1", "int a")]
        [InlineData("int a", "int a = 2")]
        [InlineData("object a = null", "object a")]
        [InlineData("object a", "object a = null")]
        [InlineData("double a = double.NaN", "double a = 1.2")]
        public void Parameter_Initializer_Update(string oldParameter, string newParameter)
        {
            var src1 = @"static class C { static void F(" + oldParameter + ") { } }";
            var src2 = @"static class C { static void F(" + newParameter + ") { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.InitializerUpdate, newParameter, FeaturesResources.parameter));
        }

        [Fact]
        public void Parameter_Initializer_NaN()
        {
            var src1 = @"static class C { static void F(double a = System.Double.NaN) { } }";
            var src2 = @"static class C { static void F(double a = double.NaN) { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void Parameter_Initializer_InsertDeleteUpdate()
        {
            var srcA1 = @"partial class C { }";
            var srcB1 = @"partial class C { public static void F(int x = 1) {} }";

            var srcA2 = @"partial class C { public static void F(int x = 2) {} }";
            var srcB2 = @"partial class C { }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(
                        diagnostics: new[]
                        {
                            Diagnostic(RudeEditKind.InitializerUpdate, "int x = 2", FeaturesResources.parameter)
                        }),
                    DocumentResults(),
                });
        }

        [Fact]
        public void Parameter_Attribute_Insert()
        {
            var attribute = "public class A : System.Attribute { }\n\n";

            var src1 = attribute + @"class C { public void M(int a)    {} }";
            var src2 = attribute + @"class C { public void M([A]int a) {} } ";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [int a]@63 -> [[A]int a]@63");

            edits.VerifySemantics(
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.M")) },
                capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
        }

        [Fact]
        public void Parameter_Attribute_Insert_SupportedByRuntime_SecurityAttribute1()
        {
            var attribute = "public class AAttribute : System.Security.Permissions.SecurityAttribute { }\n\n";

            var src1 = attribute + @"class C { public void M(int a) {} }";
            var src2 = attribute + @"class C { public void M([A]int a) {} } ";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [int a]@101 -> [[A]int a]@101");

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingNonCustomAttribute, "int a", "AAttribute", FeaturesResources.parameter) },
                capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
        }

        [Fact]
        public void Parameter_Attribute_Insert_SupportedByRuntime_SecurityAttribute2()
        {
            var attribute = "public class BAttribute : System.Security.Permissions.SecurityAttribute { }\n\n" +
                            "public class AAttribute : BAttribute { }\n\n";

            var src1 = attribute + @"class C { public void M(int a) {} }";
            var src2 = attribute + @"class C { public void M([A]int a) {} } ";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [int a]@143 -> [[A]int a]@143");

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingNonCustomAttribute, "int a", "AAttribute", FeaturesResources.parameter) },
                capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
        }

        [Fact]
        public void Parameter_Attribute_Insert_NotSupportedByRuntime1()
        {
            var attribute = "public class AAttribute : System.Attribute { }\n\n";

            var src1 = attribute + @"class C { public void M(int a) {} }";
            var src2 = attribute + @"class C { public void M([A]int a) {} } ";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [int a]@72 -> [[A]int a]@72");

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "int a", FeaturesResources.parameter) },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void Parameter_Attribute_Insert_NotSupportedByRuntime2()
        {
            var attribute = "public class AAttribute : System.Attribute { }\n\n" +
                            "public class BAttribute : System.Attribute { }\n\n";

            var src1 = attribute + @"class C { public void M([A]int a) {} }";
            var src2 = attribute + @"class C { public void M([A, B]int a) {} } ";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [[A]int a]@120 -> [[A, B]int a]@120");

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "int a", FeaturesResources.parameter) },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void Parameter_Attribute_Delete_NotSupportedByRuntime()
        {
            var attribute = "public class AAttribute : System.Attribute { }\n\n";

            var src1 = attribute + @"class C { public void M([A]int a) {} }";
            var src2 = attribute + @"class C { public void M(int a) {} } ";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [[A]int a]@72 -> [int a]@72");

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "int a", FeaturesResources.parameter) },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void Parameter_Attribute_Update_NotSupportedByRuntime()
        {
            var attribute = "public class AAttribute : System.Attribute { }\n\n" +
                            "public class BAttribute : System.Attribute { }\n\n";

            var src1 = attribute + @"class C { public void M([System.Obsolete(""1""), B]int a) {} }";
            var src2 = attribute + @"class C { public void M([System.Obsolete(""2""), A]int a) {} } ";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [[System.Obsolete(\"1\"), B]int a]@120 -> [[System.Obsolete(\"2\"), A]int a]@120");

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "int a", FeaturesResources.parameter) },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void Parameter_Attribute_Update()
        {
            var attribute = "class A : System.Attribute { public A(int x) {} } ";

            var src1 = attribute + "class C { void F([A(0)]int a) {} }";
            var src2 = attribute + "class C { void F([A(1)]int a) {} }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [[A(0)]int a]@67 -> [[A(1)]int a]@67");

            edits.VerifySemantics(
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.F")) },
                capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
        }

        [Fact]
        public void Parameter_Attribute_Update_WithBodyUpdate()
        {
            var attribute = "class A : System.Attribute { public A(int x) {} } ";

            var src1 = attribute + "class C { void F([A(0)]int a) { F(0); } }";
            var src2 = attribute + "class C { void F([A(1)]int a) { F(1); } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [void F([A(0)]int a) { F(0); }]@60 -> [void F([A(1)]int a) { F(1); }]@60",
                "Update [[A(0)]int a]@67 -> [[A(1)]int a]@67");

            edits.VerifySemantics(
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.F")) },
                capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
        }

        #endregion

        #region Method Type Parameter

        [Fact]
        public void MethodTypeParameterInsert1()
        {
            var src1 = @"class C { public void M() {} }";
            var src2 = @"class C { public void M<A>() {} } ";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyEdits(
                "Insert [<A>]@23",
                "Insert [A]@24");
        }

        [Fact]
        public void MethodTypeParameterInsert2()
        {
            var src1 = @"class C { public void M<A>() {} }";
            var src2 = @"class C { public void M<A,B>() {} } ";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyEdits(
                "Update [<A>]@23 -> [<A,B>]@23",
                "Insert [B]@26");
        }

        [Fact]
        public void MethodTypeParameterDelete1()
        {
            var src1 = @"class C { public void M<A>() {} }";
            var src2 = @"class C { public void M() {} } ";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyEdits(
                "Delete [<A>]@23",
                "Delete [A]@24");
        }

        [Fact]
        public void MethodTypeParameterDelete2()
        {
            var src1 = @"class C { public void M<A,B>() {} }";
            var src2 = @"class C { public void M<B>() {} } ";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyEdits(
                "Update [<A,B>]@23 -> [<B>]@23",
                "Delete [A]@24");
        }

        [Fact]
        public void MethodTypeParameterUpdate()
        {
            var src1 = @"class C { public void M<A>() {} }";
            var src2 = @"class C { public void M<B>() {} } ";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyEdits(
                "Update [A]@24 -> [B]@24");
        }

        [Fact]
        public void MethodTypeParameterReorder()
        {
            var src1 = @"class C { public void M<A,B>() {} }";
            var src2 = @"class C { public void M<B,A>() {} } ";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyEdits(
                "Reorder [B]@26 -> @24");
        }

        [Fact]
        public void MethodTypeParameterReorderAndUpdate()
        {
            var src1 = @"class C { public void M<A,B>() {} }";
            var src2 = @"class C { public void M<B,C>() {} } ";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyEdits(
                "Reorder [B]@26 -> @24",
                "Update [A]@24 -> [C]@26");
        }

        [Fact]
        public void MethodTypeParameter_Attribute_Insert1()
        {
            var attribute = "public class AAttribute : System.Attribute { }\n\n";

            var src1 = attribute + @"class C { public void M<T>() {} }";
            var src2 = attribute + @"class C { public void M<[A]T>() {} } ";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [T]@72 -> [[A]T]@72");

            edits.VerifySemanticDiagnostics(
                new[]
                {
                    Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "T", FeaturesResources.type_parameter),
                    Diagnostic(RudeEditKind.GenericMethodUpdate, "T"),
                    Diagnostic(RudeEditKind.GenericMethodTriviaUpdate, "", FeaturesResources.method)
                },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void MethodTypeParameter_Attribute_Insert2()
        {
            var attribute = "public class AAttribute : System.Attribute { }\n\n" +
                            "public class BAttribute : System.Attribute { }\n\n";

            var src1 = attribute + @"class C { public void M<[A]T>() {} }";
            var src2 = attribute + @"class C { public void M<[A, B]T>() {} } ";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [[A]T]@120 -> [[A, B]T]@120");

            edits.VerifySemanticDiagnostics(
                new[]
                {
                    Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "T", FeaturesResources.type_parameter),
                    Diagnostic(RudeEditKind.GenericMethodUpdate, "T"),
                    Diagnostic(RudeEditKind.GenericMethodTriviaUpdate, "", FeaturesResources.method)
                },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void MethodTypeParameter_Attribute_Delete()
        {
            var attribute = "public class AAttribute : System.Attribute { }\n\n";

            var src1 = attribute + @"class C { public void M<[A]T>() {} }";
            var src2 = attribute + @"class C { public void M<T>() {} } ";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [[A]T]@72 -> [T]@72");

            edits.VerifySemanticDiagnostics(
                new[]
                {
                    Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "T", FeaturesResources.type_parameter),
                    Diagnostic(RudeEditKind.GenericMethodTriviaUpdate, "", FeaturesResources.method),
                    Diagnostic(RudeEditKind.GenericMethodUpdate, "T")
                },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void MethodTypeParameter_Attribute_Update_NotSupportedByRuntime()
        {
            var attribute = "public class AAttribute : System.Attribute { }\n\n" +
                            "public class BAttribute : System.Attribute { }\n\n";

            var src1 = attribute + @"class C { public void M<[System.Obsolete(""1""), B]T>() {} }";
            var src2 = attribute + @"class C { public void M<[System.Obsolete(""2""), A]T>() {} } ";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [[System.Obsolete(\"1\"), B]T]@120 -> [[System.Obsolete(\"2\"), A]T]@120");

            edits.VerifySemanticDiagnostics(
                new[]
                {
                    Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "T", FeaturesResources.type_parameter),
                    Diagnostic(RudeEditKind.GenericMethodUpdate, "T")
                },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void MethodTypeParameter_Attribute_Update()
        {
            var attribute = "class A : System.Attribute { public A(int x) {} } ";

            var src1 = attribute + "class C { void F<[A(0)]T>(T a) {} }";
            var src2 = attribute + "class C { void F<[A(1)]T>(T a) {} }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [[A(0)]T]@67 -> [[A(1)]T]@67");

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.GenericMethodUpdate, "T") },
                capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
        }

        [Fact]
        public void MethodTypeParameter_Attribute_Update_WithBodyUpdate()
        {
            var attribute = "class A : System.Attribute { public A(int x) {} } ";

            var src1 = attribute + "class C { void F<[A(0)]T>(T a) { F(0); } }";
            var src2 = attribute + "class C { void F<[A(1)]T>(T a) { F(1); } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [void F<[A(0)]T>(T a) { F(0); }]@60 -> [void F<[A(1)]T>(T a) { F(1); }]@60",
                "Update [[A(0)]T]@67 -> [[A(1)]T]@67");

            edits.VerifySemanticDiagnostics(
                new[]
                {
                    Diagnostic(RudeEditKind.GenericMethodUpdate, "void F<[A(1)]T>(T a)"),
                    Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "T", FeaturesResources.type_parameter),
                    Diagnostic(RudeEditKind.GenericMethodUpdate, "T")
                },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        #endregion

        #region Type Type Parameter

        [Fact]
        public void TypeTypeParameterInsert1()
        {
            var src1 = @"class C {}";
            var src2 = @"class C<A> {}";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [<A>]@7",
                "Insert [A]@8");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Insert, "A", FeaturesResources.type_parameter));
        }

        [Fact]
        public void TypeTypeParameterInsert2()
        {
            var src1 = @"class C<A> {}";
            var src2 = @"class C<A,B> {}";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [<A>]@7 -> [<A,B>]@7",
                "Insert [B]@10");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Insert, "B", FeaturesResources.type_parameter));
        }

        [Fact]
        public void TypeTypeParameterDelete1()
        {
            var src1 = @"class C<A> { }";
            var src2 = @"class C { } ";

            var edits = GetTopEdits(src1, src2);
            edits.VerifyEdits(
                "Delete [<A>]@7",
                "Delete [A]@8");
        }

        [Fact]
        public void TypeTypeParameterDelete2()
        {
            var src1 = @"class C<A,B> {}";
            var src2 = @"class C<B> {}";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [<A,B>]@7 -> [<B>]@7",
                "Delete [A]@8");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Delete, "class C<B>", DeletedSymbolDisplay(FeaturesResources.type_parameter, "A")));
        }

        [Fact]
        public void TypeTypeParameterUpdate()
        {
            var src1 = @"class C<A> {}";
            var src2 = @"class C<B> {} ";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [A]@8 -> [B]@8");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Renamed, "B", FeaturesResources.type_parameter),
                Diagnostic(RudeEditKind.GenericTypeUpdate, "B"));
        }

        [Fact]
        public void TypeTypeParameterReorder()
        {
            var src1 = @"class C<A,B> { }";
            var src2 = @"class C<B,A> { } ";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Reorder [B]@10 -> @8");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Move, "B", FeaturesResources.type_parameter));
        }

        [Fact]
        public void TypeTypeParameterReorderAndUpdate()
        {
            var src1 = @"class C<A,B> {}";
            var src2 = @"class C<B,C> {} ";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Reorder [B]@10 -> @8",
                "Update [A]@8 -> [C]@10");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Move, "B", FeaturesResources.type_parameter),
                Diagnostic(RudeEditKind.Renamed, "C", FeaturesResources.type_parameter),
                Diagnostic(RudeEditKind.GenericTypeUpdate, "C"));
        }

        [Fact]
        public void TypeTypeParameterAttributeInsert1()
        {
            var attribute = "public class AAttribute : System.Attribute { }\n\n";

            var src1 = attribute + @"class C<T> {}";
            var src2 = attribute + @"class C<[A]T> {}";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [T]@56 -> [[A]T]@56");

            edits.VerifySemanticDiagnostics(
                new[]
                {
                    Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "T", FeaturesResources.type_parameter),
                    Diagnostic(RudeEditKind.GenericTypeUpdate, "T")
                },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void TypeTypeParameterAttributeInsert2()
        {
            var attribute = "public class AAttribute : System.Attribute { }\n\n" +
                            "public class BAttribute : System.Attribute { }\n\n";

            var src1 = attribute + @"class C<[A]T> {}";
            var src2 = attribute + @"class C<[A, B]T> {}";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [[A]T]@104 -> [[A, B]T]@104");

            edits.VerifySemanticDiagnostics(
                new[]
                {
                    Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "T", FeaturesResources.type_parameter),
                    Diagnostic(RudeEditKind.GenericTypeUpdate, "T")
                },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void TypeTypeParameterAttributeInsert_SupportedByRuntime()
        {
            var attribute = "public class AAttribute : System.Attribute { }\n\n";

            var src1 = attribute + @"class C<T> {}";
            var src2 = attribute + @"class C<[A]T> {}";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [T]@56 -> [[A]T]@56");

            edits.VerifySemanticDiagnostics(
                new[] { Diagnostic(RudeEditKind.GenericTypeUpdate, "T") },
                capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
        }

        [Fact]
        public void TypeTypeParameterAttributeDelete()
        {
            var attribute = "public class AAttribute : System.Attribute { }\n\n";

            var src1 = attribute + @"class C<[A]T> {}";
            var src2 = attribute + @"class C<T> {}";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [[A]T]@56 -> [T]@56");

            edits.VerifySemanticDiagnostics(
                new[]
                {
                    Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "T", FeaturesResources.type_parameter),
                    Diagnostic(RudeEditKind.GenericTypeUpdate, "T")
                },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void TypeTypeParameterAttributeUpdate()
        {
            var attribute = "public class AAttribute : System.Attribute { }\n\n" +
                            "public class BAttribute : System.Attribute { }\n\n";

            var src1 = attribute + @"class C<[System.Obsolete(""1""), B]T> {}";
            var src2 = attribute + @"class C<[System.Obsolete(""2""), A]T> {} ";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [[System.Obsolete(\"1\"), B]T]@104 -> [[System.Obsolete(\"2\"), A]T]@104");

            edits.VerifySemanticDiagnostics(
                new[]
                {
                    Diagnostic(RudeEditKind.ChangingAttributesNotSupportedByRuntime, "T", FeaturesResources.type_parameter),
                    Diagnostic(RudeEditKind.GenericTypeUpdate, "T")
                },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void TypeTypeParameter_Partial_Attribute_AddMultiple()
        {
            var attributes = @"
class A : System.Attribute {}
class B : System.Attribute {}
";

            var srcA1 = "partial class C<T> { }" + attributes;
            var srcB1 = "partial class C<T> { }";

            var srcA2 = "partial class C<[A]T> { }" + attributes;
            var srcB2 = "partial class C<[B]T> { }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(diagnostics: new[]
                    {
                        Diagnostic(RudeEditKind.GenericTypeUpdate, "T"),
                    }),
                    DocumentResults(diagnostics: new[]
                    {
                        Diagnostic(RudeEditKind.GenericTypeUpdate, "T"),
                    }),
                },
                capabilities: EditAndContinueCapabilities.ChangeCustomAttributes);
        }

        [Fact]
        public void TypeTypeParameter_Partial_Attribute_AddMultiple_Reloadable()
        {
            var attributes = @"
class A : System.Attribute {}
class B : System.Attribute {}
";

            var srcA1 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]partial class C<T> { }" + attributes;
            var srcB1 = "partial class C<T> { }";

            var srcA2 = ReloadableAttributeSrc + "[CreateNewOnMetadataUpdate]partial class C<[A]T> { }" + attributes;
            var srcB2 = "partial class C<[B]T> { }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(semanticEdits: new[]
                    {
                        SemanticEdit(SemanticEditKind.Replace, c => c.GetMember("C"), partialType: "C")
                    }),
                    DocumentResults(semanticEdits: new[]
                    {
                        SemanticEdit(SemanticEditKind.Replace, c => c.GetMember("C"), partialType: "C")
                    }),
                },
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        #endregion

        #region Type Parameter Constraints

        [Theory]
        [InlineData("nonnull")]
        [InlineData("struct")]
        [InlineData("class")]
        [InlineData("new()")]
        [InlineData("unmanaged")]
        [InlineData("System.IDisposable")]
        [InlineData("System.Delegate")]
        public void TypeConstraint_Insert(string newConstraint)
        {
            var src1 = "class C<S,T> { }";
            var src2 = "class C<S,T> where T : " + newConstraint + " { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [where T : " + newConstraint + "]@13");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingConstraints, "where T : " + newConstraint, FeaturesResources.type_parameter),
                Diagnostic(RudeEditKind.GenericTypeUpdate, "where T : " + newConstraint));
        }

        [Theory]
        [InlineData("nonnull")]
        [InlineData("struct")]
        [InlineData("class")]
        [InlineData("new()")]
        [InlineData("unmanaged")]
        [InlineData("System.IDisposable")]
        [InlineData("System.Delegate")]
        public void TypeConstraint_Delete(string oldConstraint)
        {
            var src1 = "class C<S,T> where T : " + oldConstraint + " { }";
            var src2 = "class C<S,T> { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Delete [where T : " + oldConstraint + "]@13");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingConstraints, "T", FeaturesResources.type_parameter),
                Diagnostic(RudeEditKind.GenericTypeUpdate, "T"));
        }

        [Theory]
        [InlineData("string", "string?")]
        [InlineData("(int a, int b)", "(int a, int c)")]
        public void TypeConstraint_Update_RuntimeTypeUnchanged(string oldType, string newType)
        {
            // note: dynamic is not allowed in constraints
            var src1 = "class C<T> where T : System.Collections.Generic.List<" + oldType + "> {}";
            var src2 = "class C<T> where T : System.Collections.Generic.List<" + newType + "> {}";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.GenericTypeUpdate, "where T : System.Collections.Generic.List<" + newType + ">"));
        }

        [Theory]
        [InlineData("int", "string")]
        [InlineData("int", "int?")]
        [InlineData("(int a, int b)", "(int a, double b)")]
        public void TypeConstraint_Update_RuntimeTypeChanged(string oldType, string newType)
        {
            var src1 = "class C<T> where T : System.Collections.Generic.List<" + oldType + "> {}";
            var src2 = "class C<T> where T : System.Collections.Generic.List<" + newType + "> {}";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingConstraints, "where T : System.Collections.Generic.List<" + newType + ">", FeaturesResources.type_parameter),
                Diagnostic(RudeEditKind.GenericTypeUpdate, "where T : System.Collections.Generic.List<" + newType + ">"));
        }

        [Fact]
        public void TypeConstraint_Delete_WithParameter()
        {
            var src1 = "class C<S,T> where S : new() where T : class  { }";
            var src2 = "class C<S> where S : new() { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Delete, "class C<S>", DeletedSymbolDisplay(FeaturesResources.type_parameter, "T")));
        }

        [Fact]
        public void TypeConstraint_MultipleClauses_Insert()
        {
            var src1 = "class C<S,T> where T : class { }";
            var src2 = "class C<S,T> where S : unmanaged where T : class { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Insert [where S : unmanaged]@13");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingConstraints, "where S : unmanaged", FeaturesResources.type_parameter),
                Diagnostic(RudeEditKind.GenericTypeUpdate, "where S : unmanaged"));
        }

        [Fact]
        public void TypeConstraint_MultipleClauses_Delete()
        {
            var src1 = "class C<S,T> where S : new() where T : class  { }";
            var src2 = "class C<S,T> where T : class { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Delete [where S : new()]@13");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangingConstraints, "S", FeaturesResources.type_parameter),
                Diagnostic(RudeEditKind.GenericTypeUpdate, "S"));
        }

        [Fact]
        public void TypeConstraint_MultipleClauses_Reorder()
        {
            var src1 = "class C<S,T> where S : struct where T : class  { }";
            var src2 = "class C<S,T> where T : class where S : struct { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Reorder [where T : class]@30 -> @13");

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void TypeConstraint_MultipleClauses_UpdateAndReorder()
        {
            var src1 = "class C<S,T> where S : new() where T : class  { }";
            var src2 = "class C<T,S> where T : class, I where S : class, new() { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Reorder [where T : class]@29 -> @13",
                "Reorder [T]@10 -> @8",
                "Update [where T : class]@29 -> [where T : class, I]@13",
                "Update [where S : new()]@13 -> [where S : class, new()]@32");

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Move, "T", FeaturesResources.type_parameter),
                Diagnostic(RudeEditKind.ChangingConstraints, "where T : class, I", FeaturesResources.type_parameter),
                Diagnostic(RudeEditKind.GenericTypeUpdate, "where T : class, I"),
                Diagnostic(RudeEditKind.ChangingConstraints, "where S : class, new()", FeaturesResources.type_parameter),
                Diagnostic(RudeEditKind.GenericTypeUpdate, "where S : class, new()"));
        }

        #endregion

        #region Top Level Statements

        [Fact]
        public void TopLevelStatements_Update()
        {
            var src1 = @"
using System;

Console.WriteLine(""Hello"");
";
            var src2 = @"
using System;

Console.WriteLine(""Hello World"");
";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Update [Console.WriteLine(\"Hello\");]@19 -> [Console.WriteLine(\"Hello World\");]@19");

            edits.VerifySemantics(SemanticEdit(SemanticEditKind.Update, c => c.GetMember("Program.<Main>$")));
        }

        [Fact]
        public void TopLevelStatements_InsertAndUpdate()
        {
            var src1 = @"
using System;

Console.WriteLine(""Hello"");
";
            var src2 = @"
using System;

Console.WriteLine(""Hello World"");
Console.WriteLine(""What is your name?"");
var name = Console.ReadLine();
";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [Console.WriteLine(\"Hello\");]@19 -> [Console.WriteLine(\"Hello World\");]@19",
                "Insert [Console.WriteLine(\"What is your name?\");]@54",
                "Insert [var name = Console.ReadLine();]@96");

            edits.VerifySemantics(SemanticEdit(SemanticEditKind.Update, c => c.GetMember("Program.<Main>$")));
        }

        [Fact]
        public void TopLevelStatements_Insert_NoImplicitMain()
        {
            var src1 = @"
using System;
";
            var src2 = @"
using System;

Console.WriteLine(""Hello World"");
";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Insert [Console.WriteLine(\"Hello World\");]@19");

            edits.VerifySemantics(
                new[] { SemanticEdit(SemanticEditKind.Insert, c => c.GetMember("Program.<Main>$")) },
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void TopLevelStatements_Insert_ImplicitMain()
        {
            var src1 = @"
using System;

Console.WriteLine(""Hello"");
";
            var src2 = @"
using System;

Console.WriteLine(""Hello"");
Console.WriteLine(""World"");
";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Insert [Console.WriteLine(\"World\");]@48");

            edits.VerifySemantics(SemanticEdit(SemanticEditKind.Update, c => c.GetMember("Program.<Main>$")));
        }

        [Fact]
        public void TopLevelStatements_Delete_NoImplicitMain()
        {
            var src1 = @"
using System;

Console.WriteLine(""Hello World"");
";
            var src2 = @"
using System;

";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Delete [Console.WriteLine(\"Hello World\");]@19");

            edits.VerifySemanticDiagnostics(Diagnostic(RudeEditKind.Delete, null, CSharpFeaturesResources.global_statement));
        }

        [Fact]
        public void TopLevelStatements_Delete_ImplicitMain()
        {
            var src1 = @"
using System;

Console.WriteLine(""Hello"");
Console.WriteLine(""World"");
";
            var src2 = @"
using System;

Console.WriteLine(""Hello"");
";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Delete [Console.WriteLine(\"World\");]@48");

            edits.VerifySemantics(SemanticEdit(SemanticEditKind.Update, c => c.GetMember("Program.<Main>$")));
        }

        [Fact]
        public void TopLevelStatements_StackAllocInUnsafeBlock()
        {
            var src1 = @"unsafe { var x = stackalloc int[3]; System.Console.Write(1); }";
            var src2 = @"unsafe { var x = stackalloc int[3]; System.Console.Write(2); }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.StackAllocUpdate, "stackalloc", CSharpFeaturesResources.global_statement));
        }

        [Fact]
        public void TopLevelStatements_StackAllocInTopBlock()
        {
            var src1 = @"{ var x = stackalloc int[3]; System.Console.Write(1); }";
            var src2 = @"{ var x = stackalloc int[3]; System.Console.Write(2); }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.StackAllocUpdate, "stackalloc", CSharpFeaturesResources.global_statement));
        }

        [Fact]
        public void TopLevelStatements_VoidToInt1()
        {
            var src1 = @"
using System;

Console.Write(1);
";
            var src2 = @"
using System;

Console.Write(1);
return 1;
";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangeImplicitMainReturnType, "return 1;"));
        }

        [Fact]
        public void TopLevelStatements_VoidToInt2()
        {
            var src1 = @"
using System;

Console.Write(1);

return;
";
            var src2 = @"
using System;

Console.Write(1);
return 1;
";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangeImplicitMainReturnType, "return 1;"));
        }

        [Fact]
        public void TopLevelStatements_VoidToInt3()
        {
            var src1 = @"
using System;

Console.Write(1);

int Goo()
{
    return 1;
}
";
            var src2 = @"
using System;

Console.Write(1);
return 1;

int Goo()
{
    return 1;
}
";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangeImplicitMainReturnType, "return 1;"));
        }

        [Fact]
        public void TopLevelStatements_AddAwait()
        {
            var src1 = @"
using System.Threading.Tasks;

await Task.Delay(100);
";
            var src2 = @"
using System.Threading.Tasks;

await Task.Delay(100);
await Task.Delay(200);
";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(SemanticEdit(SemanticEditKind.Update, c => c.GetMember("Program.<Main>$"), preserveLocalVariables: true));
        }

        [Fact]
        public void TopLevelStatements_DeleteAwait()
        {
            var src1 = @"
using System.Threading.Tasks;

await Task.Delay(100);
await Task.Delay(200);
";
            var src2 = @"
using System.Threading.Tasks;

await Task.Delay(100);
";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemantics(SemanticEdit(SemanticEditKind.Update, c => c.GetMember("Program.<Main>$"), preserveLocalVariables: true));
        }

        [Fact]
        public void TopLevelStatements_VoidToTask()
        {
            var src1 = @"
using System;
using System.Threading.Tasks;

Console.Write(1);
";
            var src2 = @"
using System;
using System.Threading.Tasks;

await Task.Delay(100);
Console.Write(1);
";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangeImplicitMainReturnType, "await Task.Delay(100);"));
        }

        [Fact]
        public void TopLevelStatements_TaskToTaskInt()
        {
            var src1 = @"
using System;
using System.Threading.Tasks;

await Task.Delay(100);
Console.Write(1);
";
            var src2 = @"
using System;
using System.Threading.Tasks;

await Task.Delay(100);
Console.Write(1);
return 1;
";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangeImplicitMainReturnType, "return 1;"));
        }

        [Fact]
        public void TopLevelStatements_VoidToTaskInt()
        {
            var src1 = @"
using System;
using System.Threading.Tasks;

Console.Write(1);
";
            var src2 = @"
using System;
using System.Threading.Tasks;

Console.Write(1);
return await GetInt();

Task<int> GetInt()
{
    return Task.FromResult(1);
}
";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangeImplicitMainReturnType, "return await GetInt();"));
        }

        [Fact]
        public void TopLevelStatements_IntToVoid1()
        {
            var src1 = @"
using System;

Console.Write(1);

return 1;
";
            var src2 = @"
using System;

Console.Write(1);
";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
               Diagnostic(RudeEditKind.ChangeImplicitMainReturnType, "Console.Write(1);"));
        }

        [Fact]
        public void TopLevelStatements_IntToVoid2()
        {
            var src1 = @"
using System;

Console.Write(1);

return 1;
";
            var src2 = @"
using System;

Console.Write(1);
return;
";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangeImplicitMainReturnType, "return;"));
        }

        [Fact]
        public void TopLevelStatements_IntToVoid3()
        {
            var src1 = @"
using System;

Console.Write(1);
return 1;

int Goo()
{
    return 1;
}
";
            var src2 = @"
using System;

Console.Write(1);

int Goo()
{
    return 1;
}
";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangeImplicitMainReturnType, "int Goo()\r\n{\r\n    return 1;\r\n}"));
        }

        [Fact]
        public void TopLevelStatements_IntToVoid4()
        {
            var src1 = @"
using System;

Console.Write(1);
return 1;

public class C
{
    public int Goo()
    {
        return 1;
    }
}
";
            var src2 = @"
using System;

Console.Write(1);

public class C
{
    public int Goo()
    {
        return 1;
    }
}
";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangeImplicitMainReturnType, "Console.Write(1);"));
        }

        [Fact]
        public void TopLevelStatements_TaskToVoid()
        {
            var src1 = @"
using System;
using System.Threading.Tasks;

await Task.Delay(100);
Console.Write(1);
";
            var src2 = @"
using System;
using System.Threading.Tasks;

Console.Write(1);
";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangeImplicitMainReturnType, "Console.Write(1);"));
        }

        [Fact]
        public void TopLevelStatements_TaskIntToTask()
        {
            var src1 = @"
using System;
using System.Threading.Tasks;

await Task.Delay(100);
Console.Write(1);
return 1;
";
            var src2 = @"
using System;
using System.Threading.Tasks;

await Task.Delay(100);
Console.Write(1);
";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangeImplicitMainReturnType, "Console.Write(1);"));
        }

        [Fact]
        public void TopLevelStatements_TaskIntToVoid()
        {
            var src1 = @"
using System;
using System.Threading.Tasks;

Console.Write(1);
return await GetInt();

Task<int> GetInt()
{
    return Task.FromResult(1);
}
";
            var src2 = @"
using System;
using System.Threading.Tasks;

Console.Write(1);
";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.ChangeImplicitMainReturnType, "Console.Write(1);"));
        }

        [Fact]
        public void TopLevelStatements_WithLambda_Insert()
        {
            var src1 = @"
using System;

Func<int> a = () => { <N:0.0>return 1;</N:0.0> };
Func<Func<int>> b = () => () => { <N:0.1>return 1;</N:0.1> };
";
            var src2 = @"
using System;

Func<int> a = () => { <N:0.0>return 1;</N:0.0> };
Func<Func<int>> b = () => () => { <N:0.1>return 1;</N:0.1> };

Console.WriteLine(1);
";
            var edits = GetTopEdits(src1, src2);
            var syntaxMap = GetSyntaxMap(src1, src2);

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember("Program.<Main>$"), syntaxMap[0]) });
        }

        [Fact]
        public void TopLevelStatements_WithLambda_Update()
        {
            var src1 = @"
using System;

Func<int> a = () => { <N:0.0>return 1;</N:0.0> };
Func<Func<int>> b = () => () => { <N:0.1>return 1;</N:0.1> };

Console.WriteLine(1);

public class C { }
";
            var src2 = @"
using System;

Func<int> a = () => { <N:0.0>return 1;</N:0.0> };
Func<Func<int>> b = () => () => { <N:0.1>return 1;</N:0.1> };

Console.WriteLine(2);

public class C { }
";
            var edits = GetTopEdits(src1, src2);
            var syntaxMap = GetSyntaxMap(src1, src2);

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember("Program.<Main>$"), syntaxMap[0]) });
        }

        [Fact]
        public void TopLevelStatements_WithLambda_Delete()
        {
            var src1 = @"
using System;

Func<int> a = () => { <N:0.0>return 1;</N:0.0> };
Func<Func<int>> b = () => () => { <N:0.1>return 1;</N:0.1> };

Console.WriteLine(1);

public class C { }
";
            var src2 = @"
using System;

Func<int> a = () => { <N:0.0>return 1;</N:0.0> };
Func<Func<int>> b = () => () => { <N:0.1>return 1;</N:0.1> };

public class C { }
";
            var edits = GetTopEdits(src1, src2);
            var syntaxMap = GetSyntaxMap(src1, src2);

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember("Program.<Main>$"), syntaxMap[0]) });
        }

        [Fact]
        public void TopLevelStatements_UpdateMultiple()
        {
            var src1 = @"
using System;

Console.WriteLine(1);
Console.WriteLine(2);

public class C { }
";
            var src2 = @"
using System;

Console.WriteLine(3);
Console.WriteLine(4);

public class C { }
";
            var edits = GetTopEdits(src1, src2);

            // Since each individual statement is a separate update to a separate node, this just validates we correctly
            // only analyze the things once
            edits.VerifySemantics(SemanticEdit(SemanticEditKind.Update, c => c.GetMember("Program.<Main>$")));
        }

        [Fact]
        public void TopLevelStatements_MoveToOtherFile()
        {
            var srcA1 = @"
using System;

Console.WriteLine(1);

public class A
{
}";
            var srcB1 = @"
using System;

public class B
{
}";

            var srcA2 = @"
using System;

public class A
{
}";
            var srcB2 = @"
using System;

Console.WriteLine(2);

public class B
{
}";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(),
                    DocumentResults(semanticEdits: new [] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember("Program.<Main>$")) }),
                });
        }

        [Fact]
        public void TopLevelStatements_BlockReorder()
        {
            var src1 = @"
{ int a; }
{ int b; }
";
            var src2 = @"
{ int b; }
{ int a; }
";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Reorder [{ int b; }]@14 -> @2");

            edits.VerifySemantics(SemanticEdit(SemanticEditKind.Update, c => c.GetMember("Program.<Main>$")));
        }

        [Fact]
        public void TopLevelStatements_Reorder()
        {
            var src1 = @"
System.Console.Write(1);
System.Console.Write(2);
";
            var src2 = @"
System.Console.Write(2);
System.Console.Write(1);
";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits("Reorder [System.Console.Write(2);]@28 -> @2");

            edits.VerifySemantics(SemanticEdit(SemanticEditKind.Update, c => c.GetMember("Program.<Main>$")));
        }

        #endregion

        #region Primary Constructors

        [Theory]
        [CombinatorialData]
        public void PrimaryConstructors_01([CombinatorialValues("class", "struct")] string keyword)
        {
            var src1 = keyword + " C() { void M() { } }";
            var src2 = keyword + " C() { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Delete, keyword + " C", FeaturesResources.method));
        }

        [Theory]
        [CombinatorialData]
        public void PrimaryConstructors_02([CombinatorialValues("class", "struct")] string keyword)
        {
            var src1 = keyword + " C() { }";
            var src2 = keyword + " C() { void M() { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Insert, "void M()", FeaturesResources.method));
        }

        [Theory]
        [CombinatorialData]
        public void PrimaryConstructors_03([CombinatorialValues("class", "struct")] string keyword)
        {
            var src1 = keyword + " C() { void M() { } }";
            var src2 = keyword + " C() { void M() { ToString(); } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Update, "void M()", FeaturesResources.method));
        }

        [Theory]
        [CombinatorialData]
        public void PrimaryConstructors_04([CombinatorialValues("class", "struct")] string keyword)
        {
            var src1 = keyword + " C(int a) { }";
            var src2 = keyword + " C(int a, int b) { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Insert, "int b", FeaturesResources.parameter));
        }

        [Theory]
        [CombinatorialData]
        public void PrimaryConstructors_05([CombinatorialValues("class", "struct")] string keyword)
        {
            var src1 = keyword + " C(int a, int b) { }";
            var src2 = keyword + " C(int a) { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Delete, keyword + " C", FeaturesResources.parameter));
        }

        [Theory]
        [CombinatorialData]
        public void PrimaryConstructors_06([CombinatorialValues("class", "struct")] string keyword)
        {
            var src1 = keyword + " C { }";
            var src2 = keyword + " C() { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void PrimaryConstructors_07([CombinatorialValues("class", "struct")] string keyword)
        {
            var src1 = keyword + " C() { }";
            var src2 = keyword + " C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void PrimaryConstructors_08([CombinatorialValues("class", "struct")] string keyword)
        {
            var src1 = keyword + " C() { }";
            var src2 = keyword + " C(int b) { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Insert, "int b", FeaturesResources.parameter));
        }

        [Theory]
        [CombinatorialData]
        public void PrimaryConstructors_09([CombinatorialValues("class", "struct")] string keyword)
        {
            var src1 = keyword + " C(int b) { }";
            var src2 = keyword + " C() { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Delete, keyword + " C", FeaturesResources.parameter));
        }

        [Theory]
        [CombinatorialData]
        public void PrimaryConstructors_10([CombinatorialValues("class", "struct")] string keyword)
        {
            var src1 = keyword + " C { }";
            var src2 = keyword + " C(int b) { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Insert, "int b", FeaturesResources.parameter));
        }

        [Theory]
        [CombinatorialData]
        public void PrimaryConstructors_11([CombinatorialValues("class", "struct")] string keyword)
        {
            var src1 = keyword + " C(int b) { }";
            var src2 = keyword + " C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Delete, keyword + " C", FeaturesResources.parameter));
        }

        [Theory]
        [CombinatorialData]
        public void PrimaryConstructors_12([CombinatorialValues("class", "struct")] string keyword)
        {
            var src1 = "partial " + keyword + " C(); partial " + keyword + " C { void M() { } }";
            var src2 = "partial " + keyword + " C(); partial " + keyword + " C { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Delete, "partial " + keyword + " C", FeaturesResources.method));
        }

        [Theory]
        [CombinatorialData]
        public void PrimaryConstructors_13([CombinatorialValues("class", "struct")] string keyword)
        {
            var src1 = "partial " + keyword + " C(); partial " + keyword + " C { }";
            var src2 = "partial " + keyword + " C(); partial " + keyword + " C { void M() { } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Insert, "void M()", FeaturesResources.method));
        }

        [Theory]
        [CombinatorialData]
        public void PrimaryConstructors_14([CombinatorialValues("class", "struct")] string keyword)
        {
            var src1 = "partial " + keyword + " C(); partial " + keyword + " C { void M() { } }";
            var src2 = "partial " + keyword + " C(); partial " + keyword + " C { void M() { ToString(); } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Update, "void M()", FeaturesResources.method));
        }

        [Theory]
        [CombinatorialData]
        public void PrimaryConstructors_15([CombinatorialValues("class", "struct")] string keyword)
        {
            var src1 = "partial " + keyword + " C(); partial " + keyword + " C { void M() { } }";
            var src2 = "partial " + keyword + " C {} partial " + keyword + " C { void M() { ToString(); } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Update, "void M()", FeaturesResources.method));
        }

        [Theory]
        [CombinatorialData]
        public void PrimaryConstructors_16([CombinatorialValues("class", "struct")] string keyword)
        {
            var src1 = "partial " + keyword + " C {} partial " + keyword + " C { void M() { } }";
            var src2 = "partial " + keyword + " C(); partial " + keyword + " C { void M() { ToString(); } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Update, "void M()", FeaturesResources.method));
        }

        [Theory]
        [CombinatorialData]
        public void PrimaryConstructors_17([CombinatorialValues("class", "struct")] string keyword)
        {
            var src1 = keyword + " C(int a) { }";
            var src2 = keyword + " C(long a) { }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Update, "long a", FeaturesResources.parameter));
        }

        [Theory]
        [CombinatorialData]
        public void PrimaryConstructors_18([CombinatorialValues("class", "struct")] string keyword)
        {
            var src1 = keyword + " C(int x, int y) { void M() { x++; } System.Func<int> z = () => y; }";
            var src2 = keyword + " C(int x, int y) { void M() { x++; } System.Func<int> z = () => x + y; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Update, "z = () => x + y", FeaturesResources.field));
        }

        [Theory]
        [CombinatorialData]
        public void PrimaryConstructors_19([CombinatorialValues("class", "struct")] string keyword)
        {
            var src1 = keyword + " C(int x, int y) { void M() { x++; } System.Func<int> z = () => x + y; }";
            var src2 = keyword + " C(int x, int y) { void M() { x++; } System.Func<int> z = () => y; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Update, "z = () => y", FeaturesResources.field));
        }

        [Theory]
        [CombinatorialData]
        public void PrimaryConstructors_20([CombinatorialValues("class", "struct")] string keyword)
        {
            var src1 = keyword + " C(int x) { void M() { x++; } }";
            var src2 = keyword + " C(int x) { void M() { x++; } System.Func<int> z = () => x; }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Insert, "z = () => x", FeaturesResources.field));
        }

        [Theory]
        [CombinatorialData]
        public void PrimaryConstructors_21([CombinatorialValues("class", "struct")] string keyword)
        {
            var src1 = keyword + " C(int x) { void M() { x++; } System.Func<int> z = () => x; }";
            var src2 = keyword + " C(int x) { void M() { x++; } }";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Delete, keyword + " C", FeaturesResources.field));
        }

        [Fact]
        public void PrimaryConstructors_22()
        {
            var src1 = "class C2(int x, int y) : C1(() => y) { void M() { x++; } } class C1(System.Func<int> x);";
            var src2 = "class C2(int x, int y) : C1(() => x + y) { void M() { x++; } } class C1(System.Func<int> x);";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Update, "class C2", FeaturesResources.class_));
        }

        [Fact]
        public void PrimaryConstructors_23()
        {
            var src1 = "class C2(int x, int y) : C1(() => x + y) { void M() { x++; } } class C1(System.Func<int> x);";
            var src2 = "class C2(int x, int y) : C1(() => y) { void M() { x++; } } class C1(System.Func<int> x);";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Update, "class C2", FeaturesResources.class_));
        }

        [Fact]
        public void PrimaryConstructors_24()
        {
            var src1 = "class C2(int x) : C1(null) { void M() { x++; } } class C1(System.Func<int> x);";
            var src2 = "class C2(int x) : C1(() => x) { void M() { x++; } } class C1(System.Func<int> x);";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Update, "class C2", FeaturesResources.class_));
        }

        [Fact]
        public void PrimaryConstructors_25()
        {
            var src1 = "class C2(int x) : C1(() => x) { void M() { x++; } } class C1(System.Func<int> x);";
            var src2 = "class C2(int x) : C1(null) { void M() { x++; } } class C1(System.Func<int> x);";

            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Update, "class C2", FeaturesResources.class_));
        }

        #endregion
    }
}

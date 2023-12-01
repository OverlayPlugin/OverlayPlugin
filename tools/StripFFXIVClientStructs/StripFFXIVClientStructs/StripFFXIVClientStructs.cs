using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StripFFXIVClientStructs
{
    class StripFFXIVClientStructs
    {
        // SyntaxKinds in this array are explicitly discarded, e.g. we won't ever keep a property or method no matter the contents
        private static SyntaxKind[] DiscardKinds = new SyntaxKind[] {
            SyntaxKind.PropertyDeclaration,
            SyntaxKind.MethodDeclaration,
            SyntaxKind.ConstructorDeclaration,
            SyntaxKind.AttributeTargetSpecifier,
            SyntaxKind.ClassDeclaration,
            SyntaxKind.IncompleteMember,
            SyntaxKind.GlobalStatement,
            SyntaxKind.ConversionOperatorDeclaration,
            SyntaxKind.RecordDeclaration,
            SyntaxKind.IndexerDeclaration,
            SyntaxKind.OperatorDeclaration,
            SyntaxKind.DelegateDeclaration,
            SyntaxKind.ImplicitObjectCreationExpression,
        };

        // Attributes in this array are discarded, but the underlying type is kept
        private static string[] DiscardAttributes = new string[] {
            "DisableRuntimeMarshalling",
            "Flags",
            "",
            "Addon",
            "Agent",
            "Obsolete",
            "FixedArray",
            "FixedSizeArray",
            "AssemblyCompany",
            "AssemblyProduct",
            "AssemblyTitle",
            "AssemblyConfiguration",
            "InfoProxy",
            "VTableAddress",
            "FixedString",
            "CExportIgnore",
        };

        // Files whose relative path start with an entry in this array are skipped for transformation
        // or deleted if the source and destination are the same
        private static string[] DiscardFiles = new string[] {
            @"\FFXIVClientStructs\GlobalUsings.cs",
            @"\FFXIVClientStructs\AssemblyAttributes.cs",
            @"\FFXIVClientStructs\Interop",
            @"\FFXIVClientStructs\Attributes",
            @"\FFXIVClientStructs.InteropSourceGenerators",
            @"\FFXIVClientStructs.ResolverTester",
            @"\FFXIVClientStructs\Havok",
            @"\FFXIVClientStructs\FFXIV\Client\Graphics\Kernel\CVector.cs",
            @"\FFXIVClientStructs\FFXIV\Component\GUI\AtkLinkedList.cs",
            @"\FFXIVClientStructs\STD\Deque.cs",
            @"\FFXIVClientStructs\STD\Map.cs",
            @"\FFXIVClientStructs\STD\Pair.cs",
            @"\FFXIVClientStructs\STD\Set.cs",
            @"\FFXIVClientStructs\STD\Vector.cs",
            @"\ida",
        };

        // Namespaces in this array are prepended to every file unless they're already present.
        //{0} is replaced with the namespace, e.g. "Global"
        private static string[] GlobalUsings = new string[] {
            "System.Runtime.InteropServices",
            "FFXIVClientStructs.{0}.STD",
            "FFXIVClientStructs.{0}.FFXIV.Client.Graphics",
        };

        // Entries in this dictionary will have their types remapped to concrete types.
        // Even format numbers (e.g. {0}, {2}) will have a stripped version of the type that's safe to use in identifiers
        // Odd format numbers will have the original type
        private static Dictionary<string, string> GenericMaps = new Dictionary<string, string>(){
            {
                "StdPair",
                @"
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public unsafe struct StdPair{0}{2}
    {{
        public {1} Item1;
        public {3} Item2;
    }}
"},
            {
                "StdSet",
                @"
    [StructLayout(LayoutKind.Sequential, Size = 0x10)]
    public unsafe struct StdSet{0}
    {{
        public Node* Head;
        public ulong Count;
        public ref struct Enumerator
        {{
            private readonly Node* _head;
            private Node* _current;
        }}

        [StructLayout(LayoutKind.Sequential)]
        public struct Node
        {{
            public Node* Left;
            public Node* Parent;
            public Node* Right;
            public byte Color;
            public bool IsNil;
            public byte _18;
            public byte _19;
            public {1} Key;
        }}
    }}
" },
            {
                "StdVector",
                @"
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct StdVector{0}
    {{
        public {1}* First;
        public {1}* Last;
        public {1}* End;
    }}
" },
            {
                "StdMap",
                @"
    [StructLayout(LayoutKind.Sequential, Size = 0x10)]
    public unsafe struct StdMap{0}{2}
    {{
        public Node* Head;
        public ulong Count;
        public ref struct Enumerator
        {{
            private readonly Node* _head;
            private Node* _current;
        }}

        [StructLayout(LayoutKind.Sequential)]
        public struct Node
        {{
            public Node* Left;
            public Node* Parent;
            public Node* Right;
            public byte Color;
            public bool IsNil;
            public byte _18;
            public byte _19;
            public StdPair<{1}, {3}> KeyValuePair;
        }}
    }}
" },
            {
                "StdDeque",
                @"
    [StructLayout(LayoutKind.Sequential, Size = 0x28)]
    public unsafe struct StdDeque{0}
    {{
        public void* ContainerBase; // iterator base nonsense
        public {1}* Map; // pointer to array of pointers (size MapSize) to arrays of T (size BlockSize)
        public ulong MapSize; // size of map
        public ulong MyOff; // offset of current first element
        public ulong MySize; // current length 
    }}
" },
            {
                "AtkLinkedList",
                @"
    [StructLayout(LayoutKind.Sequential, Size = 0x18)]
    public unsafe struct AtkLinkedList{0}
    {{
        [StructLayout(LayoutKind.Sequential)]
        public struct Node
        {{
            public {1} Value;
            public Node* Next;
            public Node* Previous;
        }}

        public Node* End;
        public Node* Start;
        public uint Count;
    }}
" },
            {
                "CVector",
                @"
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct CVector{0}
    {{
        public void* vtbl;
        public StdVector<{1}> Vector;
    }}
" },
        };

        // Regex used to strip non-safe characters from types for use as type names
        private static Regex GenericTypeRenamer = new Regex("[^a-zA-Z0-9]");

        static void Main(string[] args)
        {
            // @TODO: Maybe we can use an args parser here instead?
            if (args.Length < 1)
            {
                Console.WriteLine("Missing required namespace");
                return;
            }

            var ns = args[0];

            if (args.Length < 2)
            {
                Console.WriteLine("Missing required path");
                return;
            }

            var path = args[1];
            if (!Directory.Exists(path))
            {
                Console.WriteLine($"Path {path} does not exist");
                return;
            }
            // Use `Path.GetFullPath` here (and for `dest`) to get a normalized path name for comparison
            path = Path.GetFullPath(path);

            string dest;
            if (args.Length > 2)
            {
                dest = Path.GetFullPath(args[2]);
                if (!Directory.Exists(dest))
                {
                    Directory.CreateDirectory(dest);
                }
            }
            else
            {
                dest = path;
            }

            foreach (var file in Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories))
            {
                // Relative path to the current file
                var relFile = file.Replace(path, "");
                if (DiscardFiles.Any((discard) => relFile.StartsWith(discard)))
                {
                    // If we're transforming in-place, delete files that we don't want to keep
                    if (path == dest)
                    {
                        File.Delete(file);
                    }
                    continue;
                }

                try
                {
                    var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(file));
                    var root = tree.GetRoot();
                    // SyntaxRewriter must be on a per-file basis
                    var rewriter = new SyntaxRewriter(ns);
                    // We know that this will always be CompilationUnitSyntax because that's what we get back from
                    // `CSharpSyntaxTree.ParseText` for any given fully valid C# file
                    CompilationUnitSyntax outTree = (CompilationUnitSyntax)rewriter.Visit(root);

                    // Add any missing global-level using statements
                    foreach (var usingValTemplate in GlobalUsings)
                    {
                        var usingVal = string.Format(usingValTemplate, ns);

                        if (!rewriter.usings.Contains(usingVal))
                        {
                            outTree = outTree.AddUsings(new[] {
                                SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(usingVal))
                            });
                        }
                    }

                    var destFile = file.Replace(path, dest);
                    Directory.CreateDirectory(Directory.GetParent(destFile).FullName);
                    // Call `NormalizeWhitespace()`, otherwise Roslyn will omit whitespace between nodes randomly 🤷🏼‍
                    // Also call `ToFullString()` because sometimes it'll use the wrong output. I never figured out why this is needed.
                    // The documentation doesn't indicate why, but sometimes it randomly returned old node text instead of replaced/updated text?
                    File.WriteAllText(destFile, outTree.NormalizeWhitespace().ToFullString());
                }
                catch (Exception)
                {
                    Console.WriteLine(file);
                    throw;
                }
            }
            return;
        }

        class SyntaxRewriter : CSharpSyntaxRewriter
        {
            /// <summary>
            /// `using` statements detected in this file
            /// </summary>
            public List<string> usings = new List<string>();

            /// <summary>
            /// The namespace that this file should exist in, e.g. `Global`, `Korean`, `Chinese`
            /// </summary>
            private string structsNS;

            /// <summary>
            /// Collector for nodes which need added to the top-level Struct
            /// </summary>
            private Dictionary<string, SyntaxNode> PendingNodes = new Dictionary<string, SyntaxNode>();

            /// <summary>
            /// Holder for the current top-level struct. Technically could be a boolean instead, but it's sometimes
            /// useful to be able to see this when debugging
            /// </summary>
            private StructDeclarationSyntax topStruct = null;

            /// <summary>
            /// Collector for `using Abc = Ghi.Xyz` statements, to remap them to their full values.
            /// This is a lot easier than trying to work from both directions when rewriting generics to concrete types.
            /// </summary>
            private Dictionary<string, string> remapUsingAliases = new Dictionary<string, string>();

            /// <param name="ns">Top-level namespace, e.g. `Global`</param>
            public SyntaxRewriter(string ns) : base(true)
            {
                structsNS = ns;
            }

            /// <summary>
            /// This function is called for any node first, before a node-specific function (e.g. `VisitUsingDirective`) is called
            /// Note that this function should be called for ALL CASES where a node is modified,
            /// this function should be called and the result returned, instead of returning the modified node directly.
            /// 
            /// Technically this isn't required for `return null` cases, but in any situation where that doesn't result
            /// in an NPE it should be done as well.
            /// </summary>
            [return: NotNullIfNotNull("node")]
            public override SyntaxNode Visit(SyntaxNode node)
            {
                if (node == null)
                {
                    return base.Visit(node);
                }
#if false
                // For debugging/testing, since conditional breakpoints on `ToString`'d results don't work
                // And checking `rawText.Contains` in a conditional breakpoint is incredibly slow
                var rawText = node.ToString();
                if (rawText.Equals("CategoryMap"))
                {
                    var b = "";
                }
#endif
                // If this node's type is in the discard list, get rid of it
                if (DiscardKinds.Contains(node.Kind()))
                {
                    return base.Visit(null);
                }

                return base.Visit(node);
            }

            public override SyntaxNode VisitRegionDirectiveTrivia(RegionDirectiveTriviaSyntax node)
            {
                return SyntaxFactory.SkippedTokensTrivia();
            }

            public override SyntaxNode VisitEndRegionDirectiveTrivia(EndRegionDirectiveTriviaSyntax node)
            {
                return SyntaxFactory.SkippedTokensTrivia();
            }

            public override SyntaxNode VisitUsingDirective(UsingDirectiveSyntax node)
            {
                // Store off any `using Abc = ...` statements for replacement later, then remove it
                if (node.ToString().Contains("="))
                {
                    remapUsingAliases.Add(node.Alias.Name.Identifier.ValueText, node.Name.ToString());
                    return Visit(null);
                }

                // Remove the Havok using statements entirely
                if (node.Name.ToString().EndsWith("Havok"))
                {
                    return Visit(null);
                }

                // Store off this using statement's actual value for deduplication later
                usings.Add(node.ChildNodes().First().ToString());

                // If this using statement points at an FFXIVClientStructs namespace, and it's not using the `structsNS` namespace,
                // insert it into the name
                var newNameString = node.Name.ToString();
                if (newNameString.StartsWith("FFXIVClientStructs") && !newNameString.StartsWith("FFXIVClientStructs." + structsNS))
                {
                    return SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("FFXIVClientStructs." + structsNS + newNameString.Substring(18))).NormalizeWhitespace();
                }

                return base.VisitUsingDirective(node);
            }

            public override SyntaxNode VisitAttribute(AttributeSyntax node)
            {
                // Check if we should discard this attribute and then do so
                var attributeName = node.ChildNodes().First().GetFirstToken().ToString();
                if (DiscardAttributes.Contains(attributeName))
                {
                    return Visit(null);
                }

                return base.VisitAttribute(node);
            }

            public override SyntaxNode VisitFieldDeclaration(FieldDeclarationSyntax node)
            {
                // Check to see if we should remove this field entirely
                var nodeString = node.ToString();
                if (nodeString.Contains(" = new("))
                {
                    return Visit(null);
                }
                if (node.AttributeLists.Any((list) => list.Attributes.Any((attr) => attr.Name.ToString().Equals("Obsolete"))))
                {
                    return Visit(null);
                }

                // Technically this field and `Rad2Deg` could exist, but their type would need changed from `float` to `double`
                // which is a bit messy to do here.
                if (nodeString.StartsWith("public const float Deg2Rad"))
                {
                    return Visit(null);
                }
                if (nodeString.StartsWith("public const float Rad2Deg"))
                {
                    return Visit(null);
                }

                return base.VisitFieldDeclaration(node);
            }

            public override SyntaxNode VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
            {
                // Check to see if we need to insert the `structsNS` into this namespace declaration
                var newName = node.Name;
                var newNameString = newName.ToString();
                if (newNameString.StartsWith("FFXIVClientStructs") && !newNameString.StartsWith("FFXIVClientStructs." + structsNS))
                {
                    newName = SyntaxFactory.ParseName("FFXIVClientStructs." + structsNS + newNameString.Substring(18));
                    return Visit(SyntaxFactory.NamespaceDeclaration(
                        node.AttributeLists, node.Modifiers, node.NamespaceKeyword,
                        newName, SyntaxFactory.Token(SyntaxKind.OpenBraceToken), node.Externs, node.Usings,
                        node.Members, SyntaxFactory.Token(SyntaxKind.CloseBraceToken), node.SemicolonToken));
                }

                return base.VisitNamespaceDeclaration(node);
            }

            public override SyntaxNode VisitFileScopedNamespaceDeclaration(FileScopedNamespaceDeclarationSyntax node)
            {
                // Completely remap newer file-scoped namespace declarations to older braced namespace declarations
                return Visit(SyntaxFactory.NamespaceDeclaration(
                    node.AttributeLists, node.Modifiers, node.NamespaceKeyword,
                    node.Name, SyntaxFactory.Token(SyntaxKind.OpenBraceToken), node.Externs, node.Usings,
                    node.Members, SyntaxFactory.Token(SyntaxKind.CloseBraceToken), node.SemicolonToken));
            }

            public override SyntaxNode VisitAttributeList(AttributeListSyntax node)
            {
                // Call base.VisitAttributeList first so that any attributes can be removed as needed
                var newNode = base.VisitAttributeList(node);

                // Check if the list is empty, if so then remove it entirely
                if (newNode.ChildNodes().Count() == 0)
                {
                    return Visit(null);
                }

                // To prevent a bug with Roslyn not prepending newline properly in the case of a block comment right before
                // an attribute list, convert leading trivia from a block comment to a set of single-line comments
                if (newNode.HasLeadingTrivia)
                {
                    var origTrivia = newNode.GetLeadingTrivia();
                    var newTrivia = SyntaxFactory.TriviaList();
                    foreach (var trivia in origTrivia)
                    {
                        if (!trivia.IsKind(SyntaxKind.MultiLineCommentTrivia))
                        {
                            newTrivia.Add(trivia);
                        }
                        else
                        {
                            var text = trivia.ToString();
                            var lines = text.Substring(2, text.Length - 4).Split('\n');
                            foreach (var line in lines)
                            {
                                var trimmedLine = line.Trim().TrimStart('/');
                                if (trimmedLine.Length > 0)
                                {
                                    newTrivia.Add(SyntaxFactory.Comment(trimmedLine));
                                }
                            }
                        }
                    }
                    newNode = newNode.WithLeadingTrivia(newTrivia);
                }

                return newNode;
            }

            public override SyntaxNode VisitFunctionPointerType(FunctionPointerTypeSyntax node)
            {
                // Replace delegate function pointers with void pointers
                if (node.ToString().Trim().StartsWith("delegate*"))
                {
                    return Visit(SyntaxFactory.PointerType(SyntaxFactory.ParseTypeName("void")));
                }

                return base.VisitFunctionPointerType(node);
            }
            public override SyntaxNode VisitGenericName(GenericNameSyntax node)
            {
                var gnIdentifier = node.Identifier.ToString();

                // Remap `Pointer<T>` to `T*`
                if (gnIdentifier == "Pointer")
                {
                    return Visit(SyntaxFactory.PointerType((TypeSyntax)node.ChildNodes().First().ChildNodes().First()));
                }

                // If this name is in the generic maps key, do the remap here to a concrete type.
                // This logic is recursive, so it handles nested generics properly.
                if (GenericMaps.ContainsKey(gnIdentifier))
                {
                    var typeStringFormatParams = new List<string>();
                    var newName = gnIdentifier;
                    foreach (var t in node.TypeArgumentList.Arguments)
                    {
                        var strippedString = GenericTypeRenamer.Replace(t.ToString(), "");
                        // Format params are always in pairs, the stripped name and then the original name.
                        typeStringFormatParams.Add(strippedString);
                        typeStringFormatParams.Add(t.ToString());
                        newName += strippedString;
                    }
                    // Only add this entry if it's not already mapped.
                    // Handles cases where the same generic is used for multiple fields.
                    if (!PendingNodes.ContainsKey(newName))
                    {
                        var typeString = string.Format(GenericMaps[gnIdentifier], typeStringFormatParams.ToArray());
                        PendingNodes.Add(newName, base.Visit(SyntaxFactory.ParseSyntaxTree(typeString).GetRoot().ChildNodes().First()));
                    }

                    return base.Visit(SyntaxFactory.ParseName(newName));
                }

                return base.VisitGenericName(node);
            }

            public override SyntaxNode VisitStructDeclaration(StructDeclarationSyntax node)
            {
                // For the topmost struct, visit all children first, so we can append the new subtypes if needed
                if (topStruct == null)
                {
                    topStruct = node;
                    StructDeclarationSyntax newNode = (StructDeclarationSyntax)base.VisitStructDeclaration(node);
                    if (PendingNodes.Count > 0)
                    {
                        newNode = newNode.InsertNodesAfter(newNode.ChildNodes().Last(), PendingNodes.Values.ToArray());
                        PendingNodes.Clear();
                    }

                    // This is intentionally a call to `base.VisitStructDeclaration` to avoid issues with recursion
                    var newStruct = base.VisitStructDeclaration(newNode);

                    // Clear `topStruct` after we've handled PendingNodes, to allow additional structs in the current
                    // file's namespace to be processed properly
                    topStruct = null;

                    return newStruct;
                }

                return base.VisitStructDeclaration(node);
            }

            public override SyntaxNode VisitTypeArgumentList(TypeArgumentListSyntax node)
            {
                // Call base.VisitTypeArgumentList first so that any types can be removed or remapped as needed
                var newNode = base.VisitTypeArgumentList(node);

                // Check if the list is empty, if so then remove it entirely
                if (newNode.ChildNodes().Count() == 0)
                {
                    return Visit(null);
                }
                return newNode;
            }

            public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
            {
                switch (node.Identifier.ValueText)
                {
                    case "nint":
                        // nint = native int, only available in newer C# lang versions
                        return Visit(SyntaxFactory.ParseName("long"));
                    case "hkaSkeleton":
                    case "hkLoader":
                    case "hkaSkeletonMapper":
                        // Remove all havok-related types
                        return Visit(SyntaxFactory.ParseName("void"));
                    case "MathF":
                        // Remap `MathF` to `Math`. Need to use the `System` qualifier here to avoid it trying to
                        // use FFXIV.Common.Math
                        return Visit(SyntaxFactory.ParseName("System.Math"));
                }

                // If this type name was an alias via `using`, remap it to its underlying type here
                if (remapUsingAliases.ContainsKey(node.Identifier.ValueText))
                {
                    return Visit(SyntaxFactory.ParseName(remapUsingAliases[node.Identifier.ValueText]));
                }

                return base.VisitIdentifierName(node);
            }

            public override SyntaxNode VisitBaseList(BaseListSyntax node)
            {
                // Strip off base types that don't exist on older language versions
                var types = node.Types.Where((type) =>
                {
                    switch (type.ToString().Split("<")[0])
                    {
                        case "IEquatable":
                        case "IFormattable":
                            return false;
                    }
                    return true;
                }).ToArray();

                // If we removed any, rebuild the list. This recurses back to our VisitBaseList, so it handles an empty count properly.
                if (types.Length != node.Types.Count)
                {
                    return Visit(SyntaxFactory.BaseList(node.ColonToken, SyntaxFactory.SeparatedList(types)));
                }

                // If we don't have any types in the base type list, remove it entirely.
                // Otherwise you end up with `struct Something : {`
                if (node.Types.Count == 0)
                {
                    return Visit(null);
                }

                return base.VisitBaseList(node);
            }
        }
    }
}

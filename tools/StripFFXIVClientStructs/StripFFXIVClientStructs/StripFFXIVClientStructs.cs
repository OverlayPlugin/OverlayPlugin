using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace StripFFXIVClientStructs
{
    class StripFFXIVClientStructs
    {
        private static SyntaxKind[] KeepKinds = new SyntaxKind[] {
            SyntaxKind.CompilationUnit, // Top-level/root object
            SyntaxKind.UsingDirective, // `using` statements
            SyntaxKind.FileScopedNamespaceDeclaration, // `namespace` statements + child objects
            SyntaxKind.NamespaceDeclaration,
            SyntaxKind.QualifiedName, // `namespace` name values
            SyntaxKind.IdentifierName, // various name tokens
            SyntaxKind.StructDeclaration, // actual struct declaration
            SyntaxKind.AttributeList, // Attribute lists e.g. `[StructLayout], []...`
            SyntaxKind.Attribute, // Attribute declaration
            SyntaxKind.FieldDeclaration,
            SyntaxKind.EnumDeclaration,
            SyntaxKind.BaseList,
            SyntaxKind.SimpleBaseType,
            SyntaxKind.PredefinedType,
            SyntaxKind.EnumMemberDeclaration,
            SyntaxKind.EqualsValueClause,
            SyntaxKind.NumericLiteralExpression,
            SyntaxKind.UnaryMinusExpression,
            SyntaxKind.GenericName,
            SyntaxKind.TypeArgumentList,
            SyntaxKind.LeftShiftExpression,
            SyntaxKind.BitwiseOrExpression,
            SyntaxKind.InterfaceDeclaration,
            SyntaxKind.ParenthesizedExpression,
            SyntaxKind.VariableDeclaration,
            SyntaxKind.PointerType,
            SyntaxKind.VariableDeclarator,
            SyntaxKind.BracketedArgumentList,
            SyntaxKind.Argument,
            SyntaxKind.MultiplyExpression,
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxKind.ArgumentList,
            SyntaxKind.ObjectInitializerExpression,
            SyntaxKind.SimpleAssignmentExpression,
            SyntaxKind.DivideExpression,
            SyntaxKind.CastExpression,
            SyntaxKind.FunctionPointerType,
            SyntaxKind.FunctionPointerParameterList,
            SyntaxKind.FunctionPointerParameter,
        };
        private static SyntaxKind[] DiscardKinds = new SyntaxKind[] {
            SyntaxKind.TypeParameterList, // Type parameters for objects and funcs, e.g. `<T>` in `struct A<T>`
            SyntaxKind.TypeParameterConstraintClause, // Type parameters for objects and funcs, e.g. `where T : unmanaged` in `struct A<T> where T : unmanaged`
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
        private static string[] KeepAttributes = new string[] {
            "StructLayout",
            "FieldOffset",
        };
        private static string[] DiscardAttributes = new string[] {
            "DisableRuntimeMarshalling",
            "Flags",
            "",
            "Addon",
            "Agent",
            "Obsolete",
            "FixedArray",
            "FixedSizeArray",
        };

        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                System.Console.WriteLine("Missing required path");
                return;
            }
            var path = args[0];
            if (!Directory.Exists(path))
            {
                System.Console.WriteLine($"Path {path} does not exist");
                return;
            }
            foreach (var file in Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories))
            {
                var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(file));
                var root = tree.GetRoot();
                var outTree = recurseNode(root);
                File.WriteAllText(file, outTree.ToString());
            }
            return;
        }

        private static SyntaxNode recurseNode(SyntaxNode root)
        {
            var rootKind = root.Kind();
            if (!KeepKinds.Contains(rootKind))
            {
                if (!DiscardKinds.Contains(rootKind))
                {
                    throw new System.Exception($"Unknown type {rootKind}");
                }
                return null;
            }
            switch (rootKind)
            {
                case SyntaxKind.UsingDirective:
                case SyntaxKind.QualifiedName:
                case SyntaxKind.IdentifierName:
                    return root;
                case SyntaxKind.Attribute:
                    var attributeName = root.ChildNodes().First().GetFirstToken().ToString();
                    if (!KeepAttributes.Contains(attributeName))
                    {
                        if (!DiscardAttributes.Contains(attributeName))
                        {
                            throw new System.Exception($"Unknown attribute {attributeName}");
                        }
                        return null;
                    }
                    return root;
                case SyntaxKind.FieldDeclaration:
                    if (root.ToString().Contains(" = new("))
                    {
                        return null;
                    }
                    break;
            }
            // There's probably a better way to handle this
            var dirty = true;
            while (dirty)
            {
                dirty = false;
                foreach (var node in root.ChildNodes().ToList())
                {
                    var newNode = recurseNode(node);
                    if (newNode != node)
                    {
                        if (newNode != null)
                        {
                            if (newNode.ToString() == node.ToString())
                            {
                                continue;
                            }
                            root = root.ReplaceNode(node, newNode);
                        }
                        else
                        {
                            root = root.RemoveNode(node, SyntaxRemoveOptions.KeepNoTrivia);
                        }
                        // Re-loop, this node set has been modified
                        dirty = true;
                        break;
                    }
                }
            }

            return root;
        }
    }
}

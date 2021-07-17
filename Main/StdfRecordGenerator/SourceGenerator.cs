using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace StdfRecordGenerator
{
    static class Extensions
    {
        public static bool TryGetFieldLayoutAttribute(this GeneratorSyntaxContext context, [NotNullWhen(true)] out AttributeSyntax? attribute)
        {
            attribute = null;
            if (context.Node is AttributeSyntax att)
            {
                var typeInfo = context.SemanticModel.GetTypeInfo(context.Node);
                var isFieldLayout = typeInfo.Type?.IsFieldLayoutAttribute() ?? false;
                if (isFieldLayout)
                {
                    attribute = att;
                    return true;
                }
            }
            return false;
        }

        public static bool IsFieldLayoutAttribute(this ITypeSymbol symbol)
        {
            return symbol.GetFullName() switch
            {
                "System.Attribute" => false,
                "LinqToStdf.Attributes.FieldLayoutAttribute" => true,
                _ => symbol.BaseType?.IsFieldLayoutAttribute() ?? false,
            };
        }

        public static string GetFullName(this INamespaceSymbol symbol)
        {
            if (symbol.ContainingNamespace.IsGlobalNamespace)
            {
                return symbol.Name;
            }
            else
            {
                return $"{symbol.ContainingNamespace.GetFullName()}.{symbol.Name}";
            }
        }

        public static string GetFullName(this ITypeSymbol symbol)
        {
            if (symbol.ContainingNamespace.IsGlobalNamespace)
            {
                return symbol.Name;
            }
            else
            {
                return $"{symbol.ContainingNamespace.GetFullName()}.{symbol.Name}";
            }
        }

        public static int GetIntLiteral(this AttributeArgumentSyntax argument, SemanticModel semanticModel)
        {
            return (int)argument.GetObject(semanticModel);
        }
        public static string GetFieldType(this AttributeArgumentSyntax argument, SemanticModel semanticModel)
        {
            return "";
        }
        public static string GetString(this AttributeArgumentSyntax argument, SemanticModel semanticModel)
        {
            return (string)argument.GetObject(semanticModel);
        }
        public static bool GetBool(this AttributeArgumentSyntax argument, SemanticModel semanticModel)
        {
            return (bool)argument.GetObject(semanticModel);
        }
        static object GetObject(this ExpressionSyntax expression, SemanticModel semanticModel)
        {
            return expression switch
            {
                LiteralExpressionSyntax literal => literal.Token.Value ?? throw new InvalidOperationException("Literal token has no value"),
                CastExpressionSyntax cast => cast.Expression.GetObject(semanticModel),
                //since these are in attributes, we know that these are symbols we can resolve. let's carry them around that way and see
                //how that works out.
                MemberAccessExpressionSyntax memberAccess => semanticModel.GetSymbolInfo(expression).Symbol ?? throw new InvalidOperationException("Couldn't get symbol for member access"),
                _ => throw new InvalidOperationException($"Unsupported expression {expression.Kind()}"),
            };
        }

        public static object GetObject(this AttributeArgumentSyntax argument, SemanticModel semanticModel)
        {
            return argument.Expression.GetObject(semanticModel);
        }
    }

    [Generator]
    public class SourceGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            var recordReceiver = context.SyntaxContextReceiver as RecordSyntaxReceiver ?? throw new InvalidOperationException("Could not get the record receiver.");
            //at this point, we have the records and field layout attributes, we need to convert to definitions

            foreach (var record in recordReceiver.RecordClasses)
            {
                var generator = new ConverterGenerator(record.Key, record.Value);
            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            System.Diagnostics.Debugger.Launch();
            context.RegisterForSyntaxNotifications(() => new RecordSyntaxReceiver());
        }
    }

    class RecordSyntaxReceiver : ISyntaxContextReceiver
    {
        public ConcurrentDictionary<ClassDeclarationSyntax, List<FieldLayoutDefinition>> RecordClasses { get; } = new();

        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            if (context.TryGetFieldLayoutAttribute(out var attribute))
            {
                var parent = attribute.Parent?.Parent as ClassDeclarationSyntax ?? throw new InvalidOperationException("");

                var definition = GetFieldLayoutDefinitionFromAttribute(attribute, context.SemanticModel);
                RecordClasses.AddOrUpdate(parent, key => new List<FieldLayoutDefinition> { definition }, (key, list) => { list.Add(definition); return list; });
            }
        }

        static FieldLayoutDefinition GetFieldLayoutDefinitionFromAttribute(AttributeSyntax attribute, SemanticModel semanticModel)
        {
            var typeInfo = semanticModel.GetTypeInfo(attribute);
            var name = typeInfo.Type?.GetFullName() ?? throw new InvalidOperationException("Could not get the attribute name.");
            var layout = name switch
            {
                "LinqToStdf.Attributes.FieldLayoutAttribute" => GenerateFieldLayoutAttribute(attribute, semanticModel),
                "LinqToStdf.Attributes.TimeFieldLayoutAttribute" => GenerateTimeFieldLayoutAttribute(attribute, semanticModel),
                "LinqToStdf.Attributes.StringFieldLayoutAttribute" => GenerateStringFieldLayoutAttribute(attribute, semanticModel),
                "LinqToStdf.Attributes.FlaggedFieldLayoutAttribute" => GenerateFlaggedFieldLayoutAttribute(attribute, semanticModel),
                "LinqToStdf.Attributes.ArrayFieldLayoutAttribute" => GenerateArrayFieldLayoutAttribute(attribute, semanticModel),
                "LinqToStdf.Attributes.NibbleArrayFieldLayoutAttribute" => GenerateNibbleArrayFieldLayoutAttribute(attribute, semanticModel),
                "LinqToStdf.Attributes.DependencyProperty" => GenerateDependencyPropertyAttribute(attribute, semanticModel),
                _ => throw new InvalidOperationException($"Unsupported attribute {name}"),
            };
            if (layout.FieldIndex is null) throw new InvalidOperationException("Field index was not specified");
            if (layout.FieldType is null) throw new InvalidOperationException("Field type was not specified");
            return layout;
        }

        static FieldLayoutDefinition GenerateFieldLayoutAttribute(AttributeSyntax attribute, SemanticModel semanticModel)
        {
            var layout = new FieldLayoutDefinition();
            foreach (var argument in attribute.ArgumentList?.Arguments ?? throw new InvalidOperationException("Layout attribute without arguments."))
            {
                if (argument is null) throw new InvalidOperationException("Null argument syntax");
                var name = argument.NameEquals?.Name.ToString() ?? throw new InvalidOperationException("Could not get argument name");
                switch (name)
                {
                    case "FieldIndex":
                        layout = layout with { FieldIndex = argument.GetIntLiteral(semanticModel) };
                        break;
                    case "FieldType":
                        layout = layout with { FieldType = argument.GetFieldType(semanticModel) };
                        break;
                    case "MissingValue":
                        layout = layout with { MissingValue = argument.GetObject(semanticModel) };
                        break;
                    case "PersistMissingValue":
                        layout = layout with { PersistMissingValue = argument.GetBool(semanticModel) };
                        break;
                    case "RecordProperty":
                        layout = layout with { RecordProperty = argument.GetString(semanticModel) };
                        break;
                    case "IsOptional":
                        layout = layout with { IsOptional = argument.GetBool(semanticModel) };
                        break;
                    default: throw new InvalidOperationException($"Unsupported name {name}");
                }
            }
            return layout;
        }
        static DependencyPropertyDefinition GenerateDependencyPropertyAttribute(AttributeSyntax attribute, SemanticModel semanticModel)
        {
            var layout = new DependencyPropertyDefinition();
            foreach (var argument in attribute.ArgumentList?.Arguments ?? throw new InvalidOperationException("Layout attribute without arguments."))
            {
                if (argument is null) throw new InvalidOperationException("Null argument syntax");
                var name = argument.NameEquals?.Name.ToString() ?? throw new InvalidOperationException("Could not get argument name");
                switch (name)
                {
                    case "FieldIndex":
                        layout = layout with { FieldIndex = argument.GetIntLiteral(semanticModel) };
                        break;
                    case "FieldType":
                        layout = layout with { FieldType = argument.GetFieldType(semanticModel) };
                        break;
                    case "MissingValue":
                        layout = layout with { MissingValue = argument.GetObject(semanticModel) };
                        break;
                    case "PersistMissingValue":
                        layout = layout with { PersistMissingValue = argument.GetBool(semanticModel) };
                        break;
                    case "RecordProperty":
                        layout = layout with { RecordProperty = argument.GetString(semanticModel) };
                        break;
                    case "IsOptional":
                        layout = layout with { IsOptional = argument.GetBool(semanticModel) };
                        break;
                    case "DependentOnIndex":
                        layout = layout with { DependentOnIndex = argument.GetIntLiteral(semanticModel) };
                        break;
                    default: throw new InvalidOperationException($"Unsupported name {name}");
                }
            }
            return layout;
        }
        static ArrayFieldLayoutDefinition GenerateArrayFieldLayoutAttribute(AttributeSyntax attribute, SemanticModel semanticModel)
        {
            var layout = new ArrayFieldLayoutDefinition();
            foreach (var argument in attribute.ArgumentList?.Arguments ?? throw new InvalidOperationException("Layout attribute without arguments."))
            {
                if (argument is null) throw new InvalidOperationException("Null argument syntax");
                var name = argument.NameEquals?.Name.ToString() ?? throw new InvalidOperationException("Could not get argument name");
                switch (name)
                {
                    case "FieldIndex":
                        layout = layout with { FieldIndex = argument.GetIntLiteral(semanticModel) };
                        break;
                    case "FieldType":
                        layout = layout with { FieldType = argument.GetFieldType(semanticModel) };
                        break;
                    case "MissingValue":
                        layout = layout with { MissingValue = argument.GetObject(semanticModel) };
                        break;
                    case "PersistMissingValue":
                        layout = layout with { PersistMissingValue = argument.GetBool(semanticModel) };
                        break;
                    case "RecordProperty":
                        layout = layout with { RecordProperty = argument.GetString(semanticModel) };
                        break;
                    case "IsOptional":
                        layout = layout with { IsOptional = argument.GetBool(semanticModel) };
                        break;
                    case "ArrayLengthFieldIndex":
                        layout = layout with { ArrayLengthFieldIndex = argument.GetIntLiteral(semanticModel) };
                        break;
                    case "AllowTruncation":
                        layout = layout with { AllowTruncation = argument.GetBool(semanticModel) };
                        break;
                    default: throw new InvalidOperationException($"Unsupported name {name}");
                }
            }
            return layout;
        }
        static NibbleArrayFieldLayoutDefinition GenerateNibbleArrayFieldLayoutAttribute(AttributeSyntax attribute, SemanticModel semanticModel)
        {
            var layout = new NibbleArrayFieldLayoutDefinition();
            foreach (var argument in attribute.ArgumentList?.Arguments ?? throw new InvalidOperationException("Layout attribute without arguments."))
            {
                if (argument is null) throw new InvalidOperationException("Null argument syntax");
                var name = argument.NameEquals?.Name.ToString() ?? throw new InvalidOperationException("Could not get argument name");
                switch (name)
                {
                    case "FieldIndex":
                        layout = layout with { FieldIndex = argument.GetIntLiteral(semanticModel) };
                        break;
                    case "MissingValue":
                        layout = layout with { MissingValue = argument.GetObject(semanticModel) };
                        break;
                    case "PersistMissingValue":
                        layout = layout with { PersistMissingValue = argument.GetBool(semanticModel) };
                        break;
                    case "RecordProperty":
                        layout = layout with { RecordProperty = argument.GetString(semanticModel) };
                        break;
                    case "IsOptional":
                        layout = layout with { IsOptional = argument.GetBool(semanticModel) };
                        break;
                    case "ArrayLengthFieldIndex":
                        layout = layout with { ArrayLengthFieldIndex = argument.GetIntLiteral(semanticModel) };
                        break;
                    case "AllowTruncation":
                        layout = layout with { AllowTruncation = argument.GetBool(semanticModel) };
                        break;
                    default: throw new InvalidOperationException($"Unsupported name {name}");
                }
            }
            return layout;
        }
        static FlaggedFieldLayoutDefinition GenerateFlaggedFieldLayoutAttribute(AttributeSyntax attribute, SemanticModel semanticModel)
        {
            var layout = new FlaggedFieldLayoutDefinition();
            foreach (var argument in attribute.ArgumentList?.Arguments ?? throw new InvalidOperationException("Layout attribute without arguments."))
            {
                if (argument is null) throw new InvalidOperationException("Null argument syntax");
                var name = argument.NameEquals?.Name.ToString() ?? throw new InvalidOperationException("Could not get argument name");
                switch (name)
                {
                    case "FieldIndex":
                        layout = layout with { FieldIndex = argument.GetIntLiteral(semanticModel) };
                        break;
                    case "FieldType":
                        layout = layout with { FieldType = argument.GetFieldType(semanticModel) };
                        break;
                    case "MissingValue":
                        layout = layout with { MissingValue = argument.GetObject(semanticModel) };
                        break;
                    case "PersistMissingValue":
                        layout = layout with { PersistMissingValue = argument.GetBool(semanticModel) };
                        break;
                    case "RecordProperty":
                        layout = layout with { RecordProperty = argument.GetString(semanticModel) };
                        break;
                    case "IsOptional":
                        layout = layout with { IsOptional = argument.GetBool(semanticModel) };
                        break;
                    case "FlagIndex":
                        layout = layout with { FlagIndex = argument.GetIntLiteral(semanticModel) };
                        break;
                    case "FlagMask":
                        layout = layout with { FlagMask = (byte)argument.GetIntLiteral(semanticModel) };
                        break;
                    default: throw new InvalidOperationException($"Unsupported name {name}");
                }
            }
            return layout;
        }
        static TimeFieldLayoutDefinition GenerateTimeFieldLayoutAttribute(AttributeSyntax attribute, SemanticModel semanticModel)
        {
            var layout = new TimeFieldLayoutDefinition();
            foreach (var argument in attribute.ArgumentList?.Arguments ?? throw new InvalidOperationException("Layout attribute without arguments."))
            {
                if (argument is null) throw new InvalidOperationException("Null argument syntax");
                var name = argument.NameEquals?.Name.ToString() ?? throw new InvalidOperationException("Could not get argument name");
                switch (name)
                {
                    case "FieldIndex":
                        layout = layout with { FieldIndex = argument.GetIntLiteral(semanticModel) };
                        break;
                    case "RecordProperty":
                        layout = layout with { RecordProperty = argument.GetString(semanticModel) };
                        break;
                    default: throw new InvalidOperationException($"Unsupported name {name}");
                }
            }
            return layout;
        }
        static StringFieldLayoutDefinition GenerateStringFieldLayoutAttribute(AttributeSyntax attribute, SemanticModel semanticModel)
        {
            var layout = new StringFieldLayoutDefinition();
            foreach (var argument in attribute.ArgumentList?.Arguments ?? throw new InvalidOperationException("Layout attribute without arguments."))
            {
                if (argument is null) throw new InvalidOperationException("Null argument syntax");
                var name = argument.NameEquals?.Name.ToString() ?? throw new InvalidOperationException("Could not get argument name");
                switch (name)
                {
                    case "FieldIndex":
                        layout = layout with { FieldIndex = argument.GetIntLiteral(semanticModel) };
                        break;
                    case "RecordProperty":
                        layout = layout with { RecordProperty = argument.GetString(semanticModel) };
                        break;
                    case "IsOptional":
                        layout = layout with { IsOptional = argument.GetBool(semanticModel) };
                        break;
                    case "Length":
                        layout = layout with { Length = argument.GetIntLiteral(semanticModel) };
                        break;
                    case "MissingValue":
                        layout = layout with { MissingValue = argument.GetString(semanticModel) }; //NOTE: we force string at this level
                        break;
                    default: throw new InvalidOperationException($"Unsupported name {name}");
                }
            }
            return layout;
        }
    }
}

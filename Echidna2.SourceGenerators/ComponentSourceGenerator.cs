﻿using System.Collections.Immutable;
using Echidna2.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Echidna2.SourceGenerators;

[Generator]
public class ComponentSourceGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext initContext)
	{
		IncrementalValueProvider<Compilation> compilationProvider = initContext.CompilationProvider;
		IncrementalValuesProvider<ParameterSyntax> syntaxProvider = initContext.SyntaxProvider.CreateSyntaxProvider(
			(node, _) => NodeIsPrimaryConstructorParameterWithComponentAttribute(node),
			(syntaxContext, _) => (ParameterSyntax)syntaxContext.Node
		);
		IncrementalValueProvider<(Compilation Left, ImmutableArray<ParameterSyntax> Right)> tuple = compilationProvider.Combine(syntaxProvider.Collect());
		initContext.RegisterImplementationSourceOutput(tuple, (context, provider) => OnExecute(context, provider.Left, provider.Right));
	}
	
	private static bool NodeIsPrimaryConstructorParameterWithComponentAttribute(SyntaxNode node)
	{
		if (node is not ParameterSyntax parameter) return false;
		if (parameter.Parent?.Parent is not ClassDeclarationSyntax and not ConstructorDeclarationSyntax) return false;
		if (parameter.AttributeLists.Count == 0) return false;
		return parameter.AttributeLists.SelectMany(attributeList => attributeList.Attributes).Any(attribute => attribute.Name.ToString() == "Component");
	}
	
	private static void OnExecute(SourceProductionContext context, Compilation compilation, ImmutableArray<ParameterSyntax> nodes)
	{
		context.AddSource($"debug.g.cs", "// " + string.Join(", ", nodes).Replace("\n", "\n// "));
		foreach (ParameterSyntax node in nodes.Distinct())
		{
			if (context.CancellationToken.IsCancellationRequested)
				return;
			
			SemanticModel model = compilation.GetSemanticModel(node.SyntaxTree);
			if (model.GetDeclaredSymbol(node) is not IParameterSymbol symbol) continue;
			
			AttributeData? attributeData = symbol.GetAttributes().SingleOrDefault(attribute => attribute.AttributeClass?.Name == nameof(ComponentAttribute));
			if (attributeData is null) continue;
			
			string generatedCode = GenerateCode(symbol, node);
			context.AddSource($"{symbol.ContainingType}_{symbol.Type.Name}.g.cs", generatedCode);
		}
	}
	
	private static string GenerateCode(IParameterSymbol symbol, ParameterSyntax node)
	{
		bool isPrimaryConstructorParameter = node.Parent?.Parent is ClassDeclarationSyntax;
		string parameterName = symbol.Name;
		string parameterFieldName = parameterName;//parameterName.Substring(0, 1).ToUpperInvariant() + parameterName.Substring(1);
		INamedTypeSymbol classType = symbol.ContainingType;
		INamedTypeSymbol interfaceType = (INamedTypeSymbol)symbol.Type.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
		ITypeSymbol? interfaceImplementationType = interfaceType.GetAttributes().Where(implAttribute => implAttribute.AttributeClass?.Name == "ComponentImplementationAttribute").Select(implAttribute => implAttribute.AttributeClass?.TypeArguments[0]).FirstOrDefault();
		IMethodSymbol? interfaceImplementationConstructor = interfaceImplementationType?.GetMembers(".ctor").OfType<IMethodSymbol>().FirstOrDefault(method => method.Parameters.All(parameter => parameter.IsOptional));
		
		string source = "#nullable enable\n";
		source += $"namespace {symbol.ContainingNamespace};\n";
		source += "\n";
		source += $"partial class {classType.Name} : {interfaceType}\n";
		source += "{\n";
		
		if (interfaceImplementationType is not null && interfaceImplementationConstructor is not null)
		{
			if (isPrimaryConstructorParameter)
				source += $"\tprivate {interfaceType} {parameterFieldName} = {parameterName} ?? new {interfaceImplementationType}();\n\n";
			else
				source += $"\tprivate {interfaceType} {parameterFieldName};\n\n";
		}
		
		foreach (ISymbol member in GetAllUnimplementedInterfaceMembers(classType, interfaceType))
		{
			switch (member)
			{
				case IMethodSymbol method:
					source += $"\t{method.DeclaredAccessibility.ToString().ToLower()} {method.ReturnType} {method.Name}{(method.IsGenericMethod ? "<" + string.Join(", ", method.TypeArguments) + ">" : "")}({string.Join(", ", method.Parameters.Select(parameter => $"{(parameter.IsParams ? "params " : "")}{parameter.Type} {parameter.Name}{(parameter.HasExplicitDefaultValue ? $" = {parameter.ExplicitDefaultValue}" : "")}"))}) => {parameterFieldName}.{method.Name}({string.Join(", ", method.Parameters.Select(parameter => parameter.Name))});\n";
					break;
				case IPropertySymbol property:
					if (property is { GetMethod: not null, SetMethod: not null })
						source += $"\t{property.DeclaredAccessibility.ToString().ToLower()} {property.Type} {property.Name} {{ get => {parameterFieldName}.{property.Name}; set => {parameterFieldName}.{property.Name} = value; }}\n";
					else if (property is { GetMethod: not null })
						source += $"\t{property.DeclaredAccessibility.ToString().ToLower()} {property.Type} {property.Name} => {parameterFieldName}.{property.Name};\n";
					else if (property is { SetMethod: not null })
						source += $"\t{property.DeclaredAccessibility.ToString().ToLower()} {property.Type} {property.Name} {{ set => {parameterFieldName}.{property.Name} = value; }}\n";
					break;
			}
		}
		
		source += "}";
		return source;
	}
	
	private static IEnumerable<ISymbol> GetAllUnimplementedInterfaceMembers(INamedTypeSymbol classType, INamedTypeSymbol interfaceType) =>
		interfaceType.AllInterfaces.Append(interfaceType).SelectMany(inter => inter.GetMembers().Where(member => member.IsAbstract && !member.Name.StartsWith("get_") && !member.Name.StartsWith("set_") && !classType.GetMembers(member.Name).Any()));
}
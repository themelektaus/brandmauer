using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

using System.Reflection;
using System.Reflection.Metadata;

namespace Brandmauer.LiveCode;

public class CSharpCompiler : IAsyncCompiler
{
    public LanguageVersion languageVersion = LanguageVersion.CSharp10;

    public string sourceCode;

    public async Task<CompilerResult> CompileAsync()
    {
        if (!sourceCode.Contains("static class "))
        {
            var nl = Environment.NewLine;

            sourceCode = "" +
                $"static class Program{nl}" +
                $"{{{nl}" +
                $"    static object Main(){nl}" +
                $"    {{{nl}" +
                $"        {sourceCode}{nl}" +
                $"        return null;{nl}" +
                $"    }}{nl}" +
                $"}}";
        }

        var sourceText = SourceText.From(sourceCode);
        var options = CSharpParseOptions.Default.WithLanguageVersion(languageVersion);
        var syntaxTree = SyntaxFactory.ParseSyntaxTree(sourceText, options);

        var compilation = CSharpCompilation.Create(
            null,
            new[] { syntaxTree },
            references: GetReferences(),
            options: new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: OptimizationLevel.Release,
                assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default
            )
        );

        using var stream = new MemoryStream();

        var result = await Task.Run(() => compilation.Emit(stream));
        if (!result.Success)
        {
            return new()
            {
                sourceCode = sourceCode,
                rawAssembly = null,
                errors = result.Diagnostics
                    .Where(x => x.IsWarningAsError || x.Severity == DiagnosticSeverity.Error)
                    .Select(x => new CompilerResult.Error
                    {
                        id = x.Id,
                        message = x.GetMessage(),
                        line = x.Location.GetLineSpan().StartLinePosition.Line + 1
                    })
                    .ToArray()
            };
        }

        stream.Seek(0, SeekOrigin.Begin);

        return new()
        {
            sourceCode = sourceCode,
            rawAssembly = stream.ToArray(),
            errors = []
        };
    }

    static List<PortableExecutableReference> references;

    static List<PortableExecutableReference> GetReferences()
    {
        if (references is null)
        {
            var assemblies = new Dictionary<string, Assembly>();
            var checkedAssemblies = new HashSet<string>();

            foreach (var name in Assembly.GetEntryAssembly()?.GetReferencedAssemblies())
                assemblies.TryAdd(name.FullName, Assembly.Load(name));

            while (checkedAssemblies.Count < assemblies.Count)
            {
                var uncheckedAssemblies = assemblies
                    .Where(x => !checkedAssemblies.Contains(x.Key))
                    .ToList();

                foreach (var (assemblyName, assembly) in uncheckedAssemblies)
                {
                    checkedAssemblies.Add(assemblyName);
                    foreach (var name in assembly.GetReferencedAssemblies())
                        assemblies.TryAdd(name.FullName, Assembly.Load(name));
                }
            }

            references = assemblies.Values
                .Select(CreatePortableExecutableReference)
                .ToList();
        }

        return references;
    }

    unsafe static PortableExecutableReference CreatePortableExecutableReference(Assembly @this)
    {
        if (@this.TryGetRawMetadata(out byte* blob, out int length))
        {
            var moduleMetadata = ModuleMetadata.CreateFromMetadata((nint) blob, length);
            var assemblyMetadata = AssemblyMetadata.Create(moduleMetadata);
            return assemblyMetadata.GetReference();
        }
        return null;
    }
}

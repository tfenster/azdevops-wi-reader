using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CSharp;

namespace AzDevOpsWiReader.Shared
{
    public class DynamicClassCreator
    {

        public static Assembly GenerateType(IDictionary<string, object> columns)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using System; namespace AzDevOpsWiReader.Shared { public class DynamicTable {");
            foreach (var key in columns.Keys)
            {
                if (key == "ID")
                    sb.AppendLine($"public Int64 {key} {{ get; set; }}");
                else
                    sb.AppendLine($"public string {key} {{ get; set; }}");

                if (key == "System_Title" || key == "ParentTitle")
                {
                    sb.AppendLine($"public string {key}_Extended {{ get; set; }}");
                }

            }
            sb.AppendLine("}}");

            var src = sb.ToString();

            var syntaxTree = SyntaxFactory.ParseSyntaxTree(SourceText.From(src));
            var assemblyPath = Path.ChangeExtension(Path.GetTempFileName(), "dll");
            var dotNetCoreDir = Path.GetDirectoryName(typeof(object).GetTypeInfo().Assembly.Location);

            var compilation = CSharpCompilation.Create(Path.GetFileName(assemblyPath))
                .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .AddReferences(
                    MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Console).GetTypeInfo().Assembly.Location),
                    MetadataReference.CreateFromFile(Path.Combine(dotNetCoreDir, "System.Runtime.dll"))
                )
                .AddSyntaxTrees(syntaxTree);

            using (var ms = new MemoryStream())
            {
                var result = compilation.Emit(ms);

                if (result.Success)
                {
                    ms.Seek(0, SeekOrigin.Begin);
                    Assembly assembly = AssemblyLoadContext.Default.LoadFromStream(ms);
                    return assembly;
                }
                else
                {
                    Debug.Write(string.Join(
                        Environment.NewLine,
                        result.Diagnostics.Select(diagnostic => diagnostic.ToString())
                    ));
                    return null;
                }
            }
        }
    }
}
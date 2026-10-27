$ErrorActionPreference='Stop'
$root = Split-Path -Parent $PSScriptRoot
$roslyn = @((Join-Path $PSHOME 'Microsoft.CodeAnalysis.dll'),(Join-Path $PSHOME 'Microsoft.CodeAnalysis.CSharp.dll'))
Add-Type -CompilerOptions /nowarn:1701 -ReferencedAssemblies $roslyn -TypeDefinition @'
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
public static class VeyloraSourceFormatter {
    public static string Format(string source) {
        return CSharpSyntaxTree.ParseText(source).GetRoot().NormalizeWhitespace().ToFullString();
    }
}
'@
$files = @(Get-ChildItem -LiteralPath (Join-Path $root 'src') -Filter '*.cs') + @(Get-ChildItem -LiteralPath (Join-Path $root 'tests') -Filter '*.cs')
foreach($file in $files) {
    $formatted=[VeyloraSourceFormatter]::Format([IO.File]::ReadAllText($file.FullName))
    [IO.File]::WriteAllText($file.FullName,$formatted+[Environment]::NewLine,[Text.UTF8Encoding]::new($false))
}

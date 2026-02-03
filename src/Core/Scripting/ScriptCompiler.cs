using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using SuperDeck.Core.Models;
using SuperDeck.Core.Models.Enums;

namespace SuperDeck.Core.Scripting;

public class ScriptCompiler
{
    private readonly ScriptOptions _scriptOptions;
    private readonly ConcurrentDictionary<string, Script<object>> _compiledScripts = new();
    private readonly ScriptExecutionOptions _executionOptions;
    private readonly SandboxedScriptRunner _sandboxedRunner;

    public ScriptCompiler(ScriptExecutionOptions? executionOptions = null)
    {
        _executionOptions = executionOptions ?? new ScriptExecutionOptions();
        _sandboxedRunner = new SandboxedScriptRunner(_executionOptions);

        _scriptOptions = ScriptOptions.Default
            .WithReferences(
                typeof(object).Assembly,
                typeof(List<>).Assembly,
                typeof(Enumerable).Assembly,
                typeof(Character).Assembly,
                typeof(Math).Assembly
            )
            .WithImports(
                "System",
                "System.Linq",
                "System.Collections.Generic",
                "SuperDeck.Core.Models",
                "SuperDeck.Core.Models.Enums"
            );
    }

    public async Task<Script<object>> CompileAsync(string scriptCode)
    {
        if (string.IsNullOrWhiteSpace(scriptCode))
        {
            throw new ScriptCompilationException("Script code cannot be empty");
        }

        if (_compiledScripts.TryGetValue(scriptCode, out var cached))
        {
            return cached;
        }

        var script = CSharpScript.Create<object>(
            scriptCode,
            _scriptOptions,
            typeof(ScriptGlobals)
        );

        // Compile and validate
        var diagnostics = script.Compile();
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();

        if (errors.Any())
        {
            var errorMessages = string.Join("; ", errors.Select(e => e.GetMessage()));
            throw new ScriptCompilationException($"Script compilation failed: {errorMessages}");
        }

        _compiledScripts[scriptCode] = script;
        return script;
    }

    public async Task<Action<ScriptGlobals>> CompileToActionAsync(string scriptCode)
    {
        var script = await CompileAsync(scriptCode);

        return globals =>
        {
            try
            {
                _sandboxedRunner.Run(script, globals);
            }
            catch (Exception ex) when (ex is not ScriptTimeoutException and not ScriptMemoryException)
            {
                throw new ScriptExecutionException($"Script execution failed: {ex.Message}", ex);
            }
        };
    }

    public Action<ScriptGlobals> CompileToActionSync(string scriptCode)
    {
        return CompileToActionAsync(scriptCode).GetAwaiter().GetResult();
    }

    public void PrecompileScript(string scriptCode)
    {
        _ = CompileAsync(scriptCode).GetAwaiter().GetResult();
    }

    public bool TryCompile(string scriptCode, out string? errorMessage)
    {
        try
        {
            _ = CompileAsync(scriptCode).GetAwaiter().GetResult();
            errorMessage = null;
            return true;
        }
        catch (ScriptCompilationException ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }

    public void ClearCache()
    {
        _compiledScripts.Clear();
    }
}

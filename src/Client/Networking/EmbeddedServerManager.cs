using System.Diagnostics;

namespace SuperDeck.Client.Networking;

public class EmbeddedServerManager : IDisposable
{
    private Process? _serverProcess;
    private readonly int _port;
    private bool _isRunning;
    private string? _serverPath;
    private string? _lastError;

    public string BaseUrl => $"http://localhost:{_port}";
    public bool IsRunning => _isRunning;
    public string? LastError => _lastError;

    public EmbeddedServerManager(int port = 5000)
    {
        _port = port;
        FindServerPath();
    }

    private void FindServerPath()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;

        // Try to find the built DLL first (faster startup)
        var possibleDllPaths = new[]
        {
            Path.Combine(baseDir, "SuperDeck.Server.dll"),
            Path.Combine(baseDir, "..", "Server", "SuperDeck.Server.dll"),
            Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "Server", "bin", "Debug", "net10.0", "SuperDeck.Server.dll")),
            Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "Server", "bin", "Release", "net10.0", "SuperDeck.Server.dll")),
        };

        foreach (var path in possibleDllPaths)
        {
            if (File.Exists(path))
            {
                _serverPath = path;
                return;
            }
        }

        // Fallback to project file (will compile on run)
        var possibleProjectPaths = new[]
        {
            Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "Server", "SuperDeck.Server.csproj")),
            Path.GetFullPath(Path.Combine(baseDir, "..", "Server", "SuperDeck.Server.csproj")),
            "src/Server/SuperDeck.Server.csproj",
        };

        foreach (var path in possibleProjectPaths)
        {
            if (File.Exists(path))
            {
                _serverPath = path;
                return;
            }
        }
    }

    public async Task StartAsync(Action<string>? onOutput = null)
    {
        if (_isRunning) return;

        if (string.IsNullOrEmpty(_serverPath))
        {
            throw new InvalidOperationException("Could not find server. Make sure the server project is built.");
        }

        ProcessStartInfo startInfo;

        if (_serverPath.EndsWith(".dll"))
        {
            // Run the built DLL directly (faster)
            startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"\"{_serverPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(_serverPath) ?? Environment.CurrentDirectory
            };
            onOutput?.Invoke("Starting server from built DLL...");
        }
        else
        {
            // Use dotnet run (compiles first - slower)
            startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --project \"{_serverPath}\" --no-launch-profile",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(_serverPath) ?? Environment.CurrentDirectory
            };
            onOutput?.Invoke("Building and starting server (this may take a moment on first run)...");
        }

        // Set the URL via environment variable (more reliable than command line args)
        startInfo.EnvironmentVariables["ASPNETCORE_URLS"] = $"http://localhost:{_port}";
        startInfo.EnvironmentVariables["ASPNETCORE_ENVIRONMENT"] = "Development";

        _serverProcess = new Process { StartInfo = startInfo };
        _lastError = null;

        _serverProcess.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                onOutput?.Invoke(e.Data);
            }
        };

        _serverProcess.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                _lastError = e.Data;
                onOutput?.Invoke($"[Error] {e.Data}");
            }
        };

        try
        {
            _serverProcess.Start();
            _serverProcess.BeginOutputReadLine();
            _serverProcess.BeginErrorReadLine();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to start server process: {ex.Message}");
        }

        // Wait for server to be ready (longer timeout for compilation)
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var maxAttempts = 120;  // 60 seconds timeout (for compilation)
        var healthUrl = $"{BaseUrl}/api/health";

        onOutput?.Invoke($"Waiting for server at {healthUrl}...");

        for (int i = 0; i < maxAttempts; i++)
        {
            // Check if process died
            if (_serverProcess.HasExited)
            {
                var error = _lastError ?? "Unknown error";
                throw new InvalidOperationException($"Server process exited unexpectedly (exit code {_serverProcess.ExitCode}): {error}");
            }

            try
            {
                var response = await client.GetAsync(healthUrl);
                if (response.IsSuccessStatusCode)
                {
                    _isRunning = true;
                    onOutput?.Invoke("Server started successfully!");
                    return;
                }
                else
                {
                    onOutput?.Invoke($"Health check returned {response.StatusCode}, retrying...");
                }
            }
            catch (HttpRequestException ex)
            {
                // Server not ready yet, keep waiting
                if (i > 0 && i % 10 == 0)
                {
                    onOutput?.Invoke($"Still waiting for server... ({i / 2}s) - {ex.Message}");
                }
            }
            catch (TaskCanceledException)
            {
                // Timeout, keep waiting
            }

            await Task.Delay(500);
        }

        Stop();
        throw new InvalidOperationException($"Server failed to respond at {healthUrl} within timeout. Last error: {_lastError ?? "None"}");
    }

    public void Stop()
    {
        if (_serverProcess != null && !_serverProcess.HasExited)
        {
            try
            {
                _serverProcess.Kill(true);
            }
            catch
            {
                // Process may have already exited
            }
            _serverProcess.Dispose();
            _serverProcess = null;
        }
        _isRunning = false;
    }

    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }
}

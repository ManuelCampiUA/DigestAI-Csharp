using System.Text;
using Microsoft.Extensions.Logging;

namespace DigestAICsharp;

public class ProjectDigestGenerator(ILogger<ProjectDigestGenerator> logger)
{

    // Dimensione massima del buffer per lettura file (128KB)
    private const int BufferSize = 128 * 1024;

    // File importanti da includere sempre (case-insensitive)
    private readonly HashSet<string> _priorityFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        "README.md", "README.txt", "README.rst", "README", "ReadMe.md",
        "CHANGELOG.md", "CHANGELOG.txt", "LICENSE", "LICENSE.md", "LICENSE.txt"
    };

    // Estensioni file da includere NEL DIGEST
    private readonly HashSet<string> _allowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        // .NET/C#
        ".cs", ".csx", ".csproj", ".sln", ".props", ".targets",
        
        // Web Frontend
        ".js", ".ts", ".jsx", ".tsx", ".vue", ".svelte", ".html", ".htm",
        ".css", ".scss", ".sass", ".less",
        
        // Backend Languages
        ".py", ".java", ".php", ".rb", ".go", ".rs", ".kt",
        ".cpp", ".cxx", ".cc", ".c++", ".h", ".hpp", ".hh", ".c",
        ".fs", ".fsi", ".fsx",
        
        // Config/Data
        ".json", ".xml", ".yaml", ".yml", ".toml", ".ini", ".cfg",
        ".sql", ".graphql", ".proto",
        
        // Documentation/Scripts
        ".md", ".markdown", ".txt", ".rst", ".sh", ".bash", ".zsh",
        ".ps1", ".bat", ".cmd",
        
        // Docker/Container
        ".dockerfile", ".dockerignore"
    };

    // Cartelle da ignorare
    private readonly HashSet<string> _ignoredFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "bin", "obj", "packages", "TestResults", ".vs", "publish",
        "artifacts", "output", "release", "debug", "node_modules",
        "dist", "build", "out", ".next", ".nuxt", ".vscode", ".idea",
        "temp", "tmp", ".cache", ".git", ".svn", ".hg", "coverage",
        "logs", "log", "target", "__pycache__", ".pytest_cache", ".nuget"
    };

    // Pattern file da ignorare
    private readonly HashSet<string> _ignoredFilePatterns = new(StringComparer.OrdinalIgnoreCase)
    {
        "digestedCode.txt", "appsettings.json", "appsettings.*.json",
        "secrets.json", "web.config", "app.config", "*.dll", "*.pdb",
        "*.exe", "*.log", ".env", ".env.*", ".gitignore", "*.swp",
        "*.tmp", "package-lock.json", "yarn.lock", "*.min.js", "*.min.css"
    };

    // Cartelle nascoste dalla struttura
    private readonly HashSet<string> _hiddenFromTree = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git", ".vs", ".vscode", ".idea", "bin", "obj", "node_modules"
    };

    /// <summary>
    /// Genera il digest del progetto in modo asincrono con gestione ottimizzata della memoria.
    /// </summary>
    public async Task GenerateAsync(ProjectOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!Directory.Exists(options.ProjectPath))
        {
            throw new DirectoryNotFoundException($"Project directory not found: {options.ProjectPath}");
        }

        await HandleExistingOutputFileAsync(options.OutputFilePath);

        logger.LogInformation("🔍 Scanning project: {ProjectPath}", options.ProjectPath);

        // Trova tutti i file per il digest
        var filesToDigest = await FindFilesToDigestAsync(options);

        if (filesToDigest.Count == 0)
        {
            logger.LogWarning("⚠️ No relevant files found to generate the digest.");
            return;
        }

        logger.LogInformation("📁 Found {FileCount} relevant files to include in digest.", filesToDigest.Count);

        // Genera il contenuto usando stream per ottimizzare la memoria
        await GenerateDigestFileAsync(filesToDigest, options);
    }

    private async Task<List<FileInfo>> FindFilesToDigestAsync(ProjectOptions options)
    {
        var filesToDigest = new List<FileInfo>();
        var dynamicIgnoredPatterns = new HashSet<string>(_ignoredFilePatterns, StringComparer.OrdinalIgnoreCase);
        var dynamicAllowedExtensions = new HashSet<string>(_allowedExtensions, StringComparer.OrdinalIgnoreCase);

        // Aggiungi pattern personalizzati
        if (options.ExcludePatterns.Count > 0)
        {
            foreach (var pattern in options.ExcludePatterns)
            {
                dynamicIgnoredPatterns.Add(pattern);
            }
        }

        // Aggiungi estensioni personalizzate
        if (options.IncludeExtensions.Count > 0)
        {
            foreach (var ext in options.IncludeExtensions)
            {
                var normalizedExt = ext.StartsWith('.') ? ext : $".{ext}";
                dynamicAllowedExtensions.Add(normalizedExt);
            }
        }

        var maxFileSizeBytes = options.MaxFileSizeMb * 1024 * 1024;

        await foreach (var file in EnumerateFilesAsync(options.ProjectPath))
        {
            if (ShouldIncludeInDigest(file, options.ProjectPath, dynamicIgnoredPatterns, dynamicAllowedExtensions))
            {
                // Verifica dimensione file
                if (file.Length > maxFileSizeBytes)
                {
                    if (options.Verbose)
                    {
                        logger.LogWarning("⚠️ Skipping large file ({SizeMb:F1}MB): {FilePath}",
                            file.Length / (1024.0 * 1024.0), file.FullName);
                    }
                    continue;
                }

                filesToDigest.Add(file);
            }
        }

        return filesToDigest;
    }

    private static async IAsyncEnumerable<FileInfo> EnumerateFilesAsync(string projectPath)
    {
        await Task.Yield(); // Make it actually async

        var directoryInfo = new DirectoryInfo(projectPath);
        var files = directoryInfo.EnumerateFiles("*.*", SearchOption.AllDirectories);

        foreach (var file in files)
        {
            yield return file;
        }
    }

    private async Task GenerateDigestFileAsync(List<FileInfo> filesToDigest, ProjectOptions options)
    {
        using var fileStream = new FileStream(options.OutputFilePath, FileMode.Create, FileAccess.Write, FileShare.Read, BufferSize);
        using var writer = new StreamWriter(fileStream, Encoding.UTF8, BufferSize);

        // Header
        await writer.WriteLineAsync($"Total files included: {filesToDigest.Count}");
        await writer.WriteLineAsync();

        // Struttura del progetto
        var projectTree = GenerateProjectTree(options.ProjectPath, [.. filesToDigest.Select(f => f.FullName)]);
        await writer.WriteLineAsync("## Project Structure");
        await writer.WriteLineAsync();
        await writer.WriteLineAsync("```");
        await writer.WriteLineAsync(projectTree);
        await writer.WriteLineAsync("```");
        await writer.WriteLineAsync();
        await writer.WriteLineAsync("---");

        // Processa ogni file
        for (int i = 0; i < filesToDigest.Count; i++)
        {
            var file = filesToDigest[i];
            var relativePath = Path.GetRelativePath(options.ProjectPath, file.FullName);

            if (options.Verbose)
            {
                logger.LogInformation("📄 Processing ({Current}/{Total}): {FilePath}",
                    i + 1, filesToDigest.Count, relativePath);
            }

            try
            {
                var fileContent = await File.ReadAllTextAsync(file.FullName);
                var language = GetLanguageFromExtension(file.Extension);

                await writer.WriteLineAsync();
                await writer.WriteLineAsync($"## File: `{relativePath}`");
                await writer.WriteLineAsync($"Language: `{language}`");
                await writer.WriteLineAsync();
                await writer.WriteLineAsync($"```{language}");
                await writer.WriteLineAsync(fileContent);
                await writer.WriteLineAsync("```");
                await writer.WriteLineAsync();
                await writer.WriteLineAsync("---");

                // Flush periodicamente per liberare memoria
                if (i % 10 == 0)
                {
                    await writer.FlushAsync();
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning("⚠️ Skipping file {FilePath} due to read error: {Error}",
                    relativePath, ex.Message);
            }
        }

        await writer.FlushAsync();
    }

    // Altri metodi rimangono sostanzialmente gli stessi...
    // (ShouldIncludeInDigest, GenerateProjectTree, ecc.)

    private bool ShouldIncludeInDigest(FileInfo file, string projectPath,
        HashSet<string> ignoredPatterns, HashSet<string> allowedExtensions)
    {
        var fileName = file.Name;
        var extension = file.Extension.ToLowerInvariant();

        // File prioritari sempre inclusi (se non in cartelle ignorate)
        if (IsPriorityFile(fileName) && !IsInIgnoredFolder(file.FullName, projectPath))
        {
            return true;
        }

        // Ignora se in cartella ignorata
        if (IsInIgnoredFolder(file.FullName, projectPath))
            return false;

        // Ignora se corrisponde a pattern ignorati
        if (MatchesIgnoredPattern(fileName, ignoredPatterns))
            return false;

        // Includi solo se ha estensione valida
        return allowedExtensions.Contains(extension);
    }

    private async Task HandleExistingOutputFileAsync(string outputFilePath)
    {
        if (File.Exists(outputFilePath))
        {
            logger.LogInformation("🗑️ Removing existing output file: {OutputFile}", outputFilePath);

            try
            {
                File.Delete(outputFilePath);
            }
            catch (Exception ex)
            {
                throw new IOException($"Failed to delete existing output file '{outputFilePath}': {ex.Message}", ex);
            }
        }
    }

    // Mantieni gli altri metodi helper esistenti...
    private bool IsPriorityFile(string fileName) =>
        _priorityFiles.Contains(fileName);

    private bool IsInIgnoredFolder(string filePath, string projectPath)
    {
        var relativePath = Path.GetRelativePath(projectPath, filePath);
        var pathParts = relativePath.Split(Path.DirectorySeparatorChar);

        return pathParts.Any(part =>
            _ignoredFolders.Contains(part));
    }

    private bool MatchesIgnoredPattern(string fileName, HashSet<string> patterns)
    {
        return patterns.Any(pattern => SimplePatternMatch(fileName, pattern));
    }

    private static bool SimplePatternMatch(string fileName, string pattern)
    {
        if (fileName.Equals(pattern, StringComparison.OrdinalIgnoreCase))
            return true;

        if (!pattern.Contains('*'))
            return false;

        // Pattern matching logic rimane uguale...
        if (pattern.StartsWith("*."))
        {
            var extension = pattern.Substring(1);
            return fileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase);
        }

        if (pattern.EndsWith(".*"))
        {
            var baseName = pattern.Substring(0, pattern.Length - 2);
            return fileName.StartsWith(baseName, StringComparison.OrdinalIgnoreCase);
        }

        // Altri pattern...
        return false;
    }

    // Altri metodi helper rimangono gli stessi...
    private string GenerateProjectTree(string projectPath, List<string> includedFiles)
    {
        var projectName = Path.GetFileName(projectPath);
        var tree = new StringBuilder();
        tree.AppendLine($"{projectName}/");

        try
        {
            // Crea un set dei percorsi dei file inclusi per lookup veloce
            var includedFilesSet = includedFiles.Select(Path.GetFullPath).ToHashSet();

            // Ottieni tutte le directory che contengono file inclusi
            var dirsWithFiles = includedFiles
                .Select(file => Path.GetDirectoryName(Path.GetFullPath(file)))
                .Where(dir => !string.IsNullOrEmpty(dir))
                .ToHashSet();

            GenerateDirectoryTree(projectPath, tree, "", includedFilesSet, dirsWithFiles);
        }
        catch (Exception ex)
        {
            tree.AppendLine($"Error generating tree: {ex.Message}");
        }

        return tree.ToString();
    }


    private void GenerateDirectoryTree(string currentPath, StringBuilder tree, string indent,
    HashSet<string> includedFiles, HashSet<string> dirsWithFiles)
    {
        var entries = new List<(string path, bool isFile, string name)>();

        // Aggiungi le directory che contengono file inclusi
        if (Directory.Exists(currentPath))
        {
            var directories = Directory.GetDirectories(currentPath)
                .Where(dir => dirsWithFiles.Any(includedDir =>
                    includedDir.StartsWith(Path.GetFullPath(dir), StringComparison.OrdinalIgnoreCase)))
                .OrderBy(Path.GetFileName);

            foreach (var dir in directories)
            {
                entries.Add((dir, false, Path.GetFileName(dir)));
            }

            // Aggiungi i file inclusi in questa directory
            var files = Directory.GetFiles(currentPath)
                .Where(file => includedFiles.Contains(Path.GetFullPath(file)))
                .OrderBy(Path.GetFileName);

            foreach (var file in files)
            {
                entries.Add((file, true, Path.GetFileName(file)));
            }
        }

        // Genera l'output per questa directory
        for (int i = 0; i < entries.Count; i++)
        {
            var (path, isFile, name) = entries[i];
            var isLast = i == entries.Count - 1;
            var connector = isLast ? "└── " : "├── ";

            tree.AppendLine($"{indent}{connector}{name}");

            // Se è una directory, ricorsivamente genera i suoi contenuti
            if (!isFile)
            {
                var newIndent = indent + (isLast ? "    " : "│   ");
                GenerateDirectoryTree(path, tree, newIndent, includedFiles, dirsWithFiles);
            }
        }
    }

    private void GenerateTreeRecursive(StringBuilder sb, string currentPath, string rootPath,
        string indent, HashSet<string> includedFiles, HashSet<string> dirsWithFiles)
    {
        var entries = new List<(string path, bool isFile, string name)>();

        // Aggiungi le directory che contengono file inclusi
        if (Directory.Exists(currentPath))
        {
            var directories = Directory.GetDirectories(currentPath)
                .Where(dir => dirsWithFiles.Any(includedDir =>
                    includedDir.StartsWith(Path.GetFullPath(dir), StringComparison.OrdinalIgnoreCase)))
                .OrderBy(Path.GetFileName);

            foreach (var dir in directories)
            {
                entries.Add((dir, false, Path.GetFileName(dir)));
            }

            // Aggiungi i file inclusi in questa directory
            var files = Directory.GetFiles(currentPath)
                .Where(file => includedFiles.Contains(Path.GetFullPath(file)))
                .OrderBy(Path.GetFileName);

            foreach (var file in files)
            {
                entries.Add((file, true, Path.GetFileName(file)));
            }
        }

        // Genera l'output per questa directory
        for (int i = 0; i < entries.Count; i++)
        {
            var (path, isFile, name) = entries[i];
            var isLast = i == entries.Count - 1;
            var connector = isLast ? "└── " : "├── ";

            sb.AppendLine($"{indent}{connector}{name}");

            // Se è una directory, ricorsivamente genera i suoi contenuti
            if (!isFile)
            {
                var newIndent = indent + (isLast ? "    " : "│   ");
                GenerateTreeRecursive(sb, path, rootPath, newIndent, includedFiles, dirsWithFiles);
            }
        }
    }

    private bool ShouldIncludeInTree(string path, string projectPath)
    {
        var itemName = Path.GetFileName(path);

        if (Directory.Exists(path))
        {
            return !_hiddenFromTree.Contains(itemName);
        }

        return true;
    }

    private static string GetLanguageFromExtension(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".cs" => "csharp",
            ".js" => "javascript",
            ".ts" => "typescript",
            ".jsx" => "jsx",
            ".tsx" => "tsx",
            ".py" => "python",
            ".java" => "java",
            ".cpp" or ".cxx" or ".cc" or ".c++" => "cpp",
            ".h" or ".hpp" or ".hh" => "cpp",
            ".c" => "c",
            ".html" or ".htm" => "html",
            ".css" => "css",
            ".scss" or ".sass" => "scss",
            ".vue" => "vue",
            ".php" => "php",
            ".rb" => "ruby",
            ".go" => "go",
            ".rs" => "rust",
            ".kt" or ".kts" => "kotlin",
            ".swift" => "swift",
            ".sql" => "sql",
            ".json" => "json",
            ".xml" => "xml",
            ".yaml" or ".yml" => "yaml",
            ".md" or ".markdown" => "markdown",
            ".sh" => "bash",
            ".ps1" => "powershell",
            ".fs" or ".fsi" or ".fsx" => "fsharp",
            ".csproj" => "xml",
            ".sln" => "text",
            ".txt" => "text",
            _ => "text"
        };
    }
}

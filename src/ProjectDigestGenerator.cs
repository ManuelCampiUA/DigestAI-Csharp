using System.Text;

namespace DigestAICsharp;

public class ProjectDigestGenerator
{
    // File importanti da includere sempre (case-insensitive)
    private readonly HashSet<string> _priorityFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        "README.md", "README.txt", "README.rst", "README", "ReadMe.md",
    };

    // Estensioni file da includere
    private readonly HashSet<string> _allowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        // .NET/C#
        ".cs", ".csx", ".csproj", ".sln", ".props", ".targets",
        
        // Web Frontend
        ".js", ".ts", ".jsx", ".tsx", ".vue", ".svelte", ".html", ".htm", ".css", ".scss", ".sass", ".less",
        
        // Backend Languages
        ".py", ".java", ".php", ".rb", ".go", ".rs", ".kt", ".cpp", ".cxx", ".cc", ".c++", ".h", ".hpp", ".hh", ".c", ".fs", ".fsi", ".fsx",
        
        // Config/Data
        ".json", ".xml", ".yaml", ".yml", ".toml", ".ini", ".cfg", ".sql", ".graphql", ".proto",
        
        // Documentation/Scripts
        ".md", ".markdown", ".txt", ".rst", ".sh", ".bash", ".zsh", ".ps1", ".bat", ".cmd",
        
        // Docker/Container
        ".dockerfile", ".dockerignore"
    };

    // Cartelle da ignorare
    private readonly HashSet<string> _ignoredFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        // .NET Build/Packages
        "bin", "obj", "packages", "TestResults", ".vs", "publish", "artifacts", "output", "release", "debug",
        
        // Node.js/Frontend
        "node_modules", "dist", "build", "out", ".next", ".nuxt", "public/build", "wwwroot/dist", "assets/build",
        
        // IDEs/Editors
        ".vscode", ".idea", ".vs", "*.xcworkspace", "*.xcodeproj",
        
        // Temporary/Cache
        "temp", "tmp", ".cache", ".temp", "cache",
        
        // Version Control
        ".git", ".svn", ".hg", ".bzr",
        
        // Testing/Coverage
        "coverage", "TestCoverage", "CodeCoverage", "test-results",
        
        // Logs
        "logs", "log", "LogFiles",

        // Other common
        "target", // Rust/Java/Scala
        "__pycache__", ".pytest_cache", // Python
        ".nuget" // NuGet local cache
    };

    private readonly HashSet<string> _ignoredFilePatterns = new(StringComparer.OrdinalIgnoreCase)
    {
        // Speciali
        "digestedCode.txt",
        
        // .NET/C# - Config sensitive
        "appsettings.json", "appsettings.*.json", "secrets.json", "web.config", "app.config", "connectionstrings.json", "launchSettings.json",
        
        // .NET - Generated/Build artifacts
        "*.dll", "*.pdb", "*.exe", "*.msi", "*.nupkg", "*.snupkg", "GlobalAssemblyInfo.cs", "AssemblyInfo.cs", "*.Generated.cs", "*.g.cs", "*.g.i.cs", "TemporaryGeneratedFile_*",
        
        // Frontend - Lock files & build artifacts
        "package-lock.json", "yarn.lock", "pnpm-lock.yaml", "composer.lock", "*.min.js", "*.min.css", "*.map", "*.bundle.js", "*.chunk.js", "manifest.json", "asset-manifest.json",
        
        // Environment/Secrets
        ".env", ".env.*", "*.pem", "*.key", "*.crt", "*.pfx", "*.p12",
        
        // Git files
        ".gitignore", ".gitattributes", ".gitmodules", ".gitkeep",
        
        // Editor/IDE specifici
        "*.swp", "*.swo", "*~", "*.user", "*.suo", "*.userprefs",
        ".DS_Store", "Thumbs.db", "desktop.ini",
        
        // Testing/Coverage
        "*.coverage", "coverage.xml", "*.lcov", "*.trx", "*.runsettings",
        
        // logs
        "*.log", "npm-debug.log*", "yarn-debug.log*", "lerna-debug.log*",
        
        // Database files
        "*.db", "*.sqlite", "*.sqlite3", "*.mdf", "*.ldf", "*.bak",
        
        // Backup e temporanei
        "*.backup", "*.old", "*.orig", "*.tmp", "*~", "*.swp",
        
        // OS specific
        ".DS_Store", "Thumbs.db", "desktop.ini", "*.lnk",
        
        // Docker/Container 
        ".dockerignore", 
        
        // Archive files
        "*.zip", "*.rar", "*.7z", "*.tar", "*.gz", "*.bz2",
        
        // Media files (solitamente non utili per AI)
        "*.jpg", "*.jpeg", "*.png", "*.gif", "*.bmp", "*.ico", "*.mp4", "*.avi", "*.mov", "*.wmv", "*.mp3", "*.wav"
    };

    public async Task GenerateAsync(string projectPath, string outputFilePath)
    {
        // Verifica che la cartella esista
        if (!Directory.Exists(projectPath))
            throw new DirectoryNotFoundException($"Project directory not found: {projectPath}");

        // Elimina il file di output se esiste già
        await HandleExistingOutputFileAsync(outputFilePath);

        Console.WriteLine($"🔍 Scanning project: {projectPath}");

        // Trova tutti i file
        var allFiles = Directory.GetFiles(projectPath, "*.*", SearchOption.AllDirectories);
        var filesToDigest = new List<string>();

        // Filtra i file
        foreach (var file in allFiles)
        {
            if (ShouldIncludeFile(file, projectPath))
                filesToDigest.Add(file);
        }

        if (filesToDigest.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("⚠️ No relevant files found to generate the digest.");
            Console.ResetColor();
            return;
        }

        Console.WriteLine($"📁 Found {filesToDigest.Count} relevant files to include.");

        // Genera struttura ad albero
        var treeStructure = GenerateProjectTree(projectPath, filesToDigest);

        // Crea il contenuto markdown con la struttura ad albero
        var content = await CreateMarkdownContentAsync(filesToDigest, projectPath, treeStructure);

        // Scrivi il file
        try
        {
            await File.WriteAllTextAsync(outputFilePath, content);
        }
        catch (Exception ex)
        {
            throw new IOException($"Failed to write digest file to '{outputFilePath}'.", ex);
        }
    }

    private static async Task HandleExistingOutputFileAsync(string outputFilePath)
    {
        if (!File.Exists(outputFilePath)) return;

        try
        {
            // Elimina il file esistente
            File.Delete(outputFilePath);
            Console.WriteLine($"🗑️ Removed existing output file: {Path.GetFileName(outputFilePath)}");

            // Piccola pausa per assicurarsi che l'eliminazione sia completata
            await Task.Delay(10);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"⚠️ Warning: Could not delete existing file {outputFilePath}: {ex.Message}");
            Console.WriteLine("The file will be overwritten.");
            Console.ResetColor();
        }
    }

    private string GenerateProjectTree(string projectPath, List<string> filesToDigest)
    {
        Console.WriteLine("🌳 Generating project tree structure...");

        var projectName = Path.GetFileName(projectPath);
        var tree = new StringBuilder();

        tree.AppendLine($"{projectName}/");
        tree.AppendLine("│");

        // Crea una struttura gerarchica dei file
        var filesByDirectory = filesToDigest
            .Select(file => Path.GetRelativePath(projectPath, file))
            .OrderBy(path => path)
            .GroupBy(path => Path.GetDirectoryName(path) ?? "")
            .OrderBy(group => group.Key)
            .ToList();

        // Tieni traccia delle directory già mostrate
        var shownDirectories = new HashSet<string>();

        for (int i = 0; i < filesByDirectory.Count; i++)
        {
            var group = filesByDirectory[i];
            var directory = group.Key;
            var isLastDirectory = (i == filesByDirectory.Count - 1);

            // Se è la directory root, non mostrare il nome della directory
            if (string.IsNullOrEmpty(directory))
            {
                // File nella root
                var rootFiles = group.ToList();
                for (int j = 0; j < rootFiles.Count; j++)
                {
                    var fileName = Path.GetFileName(rootFiles[j]);
                    var isLastFile = (j == rootFiles.Count - 1) && isLastDirectory;
                    var prefix = isLastFile ? "└── " : "├── ";
                    tree.AppendLine($"{prefix}{fileName}");
                }
            }
            else
            {
                // Directory con file
                var prefix = isLastDirectory ? "└── " : "├── ";
                var dirName = directory.Replace(Path.DirectorySeparatorChar, '/');

                var dirDisplay = dirName;

                tree.AppendLine($"{prefix}{dirDisplay}");

                // Aggiungi i file nella directory
                var filesInDir = group.ToList();
                for (int j = 0; j < filesInDir.Count; j++)
                {
                    var fileName = Path.GetFileName(filesInDir[j]);
                    var isLastFile = (j == filesInDir.Count - 1);
                    var filePrefix = isLastDirectory
                        ? (isLastFile ? "    └── " : "    ├── ")
                        : (isLastFile ? "│   └── " : "│   ├── ");
                    tree.AppendLine($"{filePrefix}{fileName}");
                }

                if (!isLastDirectory)
                {
                    tree.AppendLine("│");
                }
            }
        }

        return tree.ToString();
    }

    private bool ShouldIncludeFile(string filePath, string projectPath)
    {
        var fileName = Path.GetFileName(filePath);
        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        // 1. File prioritari sono sempre inclusi (se non in cartelle ignorate)
        if (IsPriorityFile(fileName))
        {
            return !IsInIgnoredFolder(filePath, projectPath);
        }

        // 2. Ignora se in cartella ignorata
        if (IsInIgnoredFolder(filePath, projectPath))
            return false;

        // 3. Ignora se corrisponde a pattern ignorati
        if (MatchesIgnoredPattern(fileName))
            return false;

        // 4. Includi solo se ha estensione valida
        return _allowedExtensions.Contains(extension);
    }

    private bool IsPriorityFile(string fileName)
    {
        return _priorityFiles.Any(priority => priority.Equals(fileName, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsInIgnoredFolder(string filePath, string projectPath)
    {
        var relativePath = Path.GetRelativePath(projectPath, filePath);
        var pathParts = relativePath.Split(Path.DirectorySeparatorChar);

        // Controlla se qualche parte del percorso è una cartella ignorata
        return pathParts.Any(part =>
            _ignoredFolders.Any(ignored =>
                ignored.Equals(part, StringComparison.OrdinalIgnoreCase)));
    }

    private bool MatchesIgnoredPattern(string fileName)
    {
        foreach (var pattern in _ignoredFilePatterns)
        {
            if (SimplePatternMatch(fileName, pattern))
                return true;
        }
        return false;
    }

    private static bool SimplePatternMatch(string fileName, string pattern)
    {
        // Match esatto
        if (fileName.Equals(pattern, StringComparison.OrdinalIgnoreCase))
            return true;

        // Nessun wildcard = no match
        if (!pattern.Contains('*'))
            return false;

        // Pattern tipo "*.ext"
        if (pattern.StartsWith("*.") && pattern.Count(c => c == '*') == 1)
        {
            var extension = pattern.Substring(1); // rimuovi il *
            return fileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase);
        }

        // Pattern tipo "nome.*"
        if (pattern.EndsWith(".*") && pattern.Count(c => c == '*') == 1)
        {
            var baseName = pattern.Substring(0, pattern.Length - 2); // rimuovi .*
            return fileName.StartsWith(baseName, StringComparison.OrdinalIgnoreCase);
        }

        // Pattern tipo "appsettings.*.json" (BUGFIX: logica corretta)
        if (pattern.Count(c => c == '*') == 1)
        {
            var starIndex = pattern.IndexOf('*');
            var prefix = pattern.Substring(0, starIndex);
            var suffix = pattern.Substring(starIndex + 1);

            var hasPrefix = string.IsNullOrEmpty(prefix) ||
                           fileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
            var hasSuffix = string.IsNullOrEmpty(suffix) ||
                           fileName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);

            // Verifica che ci sia abbastanza spazio per entrambi prefix e suffix
            if (hasPrefix && hasSuffix)
            {
                var minLength = prefix.Length + suffix.Length;
                return fileName.Length >= minLength;
            }
        }

        return false;
    }

    private async Task<string> CreateMarkdownContentAsync(List<string> filesToDigest, string projectPath, string treeStructure)
    {
        var markdown = new StringBuilder();

        // Header con count
        markdown.AppendLine($"Total files included: {filesToDigest.Count}");
        markdown.AppendLine();

        markdown.AppendLine("## 📁 Project Structure");
        markdown.AppendLine();
        markdown.AppendLine("```");
        markdown.AppendLine(treeStructure);
        markdown.AppendLine("```");
        markdown.AppendLine();
        markdown.AppendLine("---");

        // Processa ogni file
        var tasks = filesToDigest.Select((filePath, index) =>
            ProcessFileAsync(filePath, index + 1, filesToDigest.Count, projectPath)).ToArray();

        var fileContents = await Task.WhenAll(tasks);

        // Aggiungi tutti i contenuti processati
        foreach (var content in fileContents.Where(c => !string.IsNullOrEmpty(c)))
        {
            markdown.AppendLine(content);
        }

        return markdown.ToString();
    }

    private async Task<string> ProcessFileAsync(string filePath, int currentIndex, int totalFiles, string projectPath)
    {
        var relativePath = Path.GetRelativePath(projectPath, filePath);
        Console.WriteLine($"📄 Processing ({currentIndex}/{totalFiles}): {relativePath}");

        // Leggi il contenuto del file in modo asincrono
        string fileContent;
        try
        {
            fileContent = await File.ReadAllTextAsync(filePath);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"   Skipping file {relativePath} due to read error: {ex.Message}");
            Console.ResetColor();
            return string.Empty; // Restituisci stringa vuota per file che non possono essere letti
        }

        // Determina il linguaggio
        var language = GetLanguageFromExtension(Path.GetExtension(filePath));

        // Crea il contenuto markdown per questo file
        var content = new StringBuilder();
        content.AppendLine();
        content.AppendLine($"## File: `{relativePath}`");
        content.AppendLine($"Language: `{language}`");
        content.AppendLine();
        content.AppendLine($"```{language}");
        content.AppendLine(fileContent);
        content.AppendLine("```");
        content.AppendLine();
        content.AppendLine("---");

        return content.ToString();
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

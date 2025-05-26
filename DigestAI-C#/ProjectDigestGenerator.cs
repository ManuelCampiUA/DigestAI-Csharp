using System.Text;

namespace DigestAI_Csharp
{
    public class ProjectDigestGenerator
    {
        private readonly HashSet<string> _includedExtensions =
        [
            ".cs", ".js", ".ts", ".jsx", ".tsx", ".py", ".java", ".cpp", ".h",
        ".html", ".css", ".scss", ".vue", ".php", ".rb", ".go", ".rs",
        ".sql", ".json", ".xml", ".yaml", ".yml", ".md", ".txt"
        ];

        private readonly HashSet<string> _excludedFolders =
        [
            // .NET/C#
            "bin", "obj", "packages", "TestResults", ".vs", "publish",
        
        // Frontend generale
        "node_modules", "dist", "build", "out", ".next", ".nuxt",
        
        // Cache e temp
        ".vscode", ".idea", "temp", "tmp", ".cache", ".parcel-cache",
        
        // Version control
        ".git", ".svn", ".hg",
        
        // OS
        ".DS_Store", "Thumbs.db",
        
        // Coverage/Testing
        "coverage", "nyc_output", ".nyc_output", "TestCoverage",
        
        // Logs
        "logs", "log"
        ];

        private readonly HashSet<string> _excludedFilePatterns = new()
    {
        // .NET/C# - Config sensibili
        "appsettings.json",
        "appsettings.*.json",
        "web.config",
        "app.config",
        "connectionstrings.json",
        
        // .NET - Generated/Build
        "*.dll", "*.pdb", "*.exe", "*.msi",
        "GlobalAssemblyInfo.cs",
        "AssemblyInfo.cs",
        
        // Frontend - Dipendenze/Lock files
        "package-lock.json",
        "yarn.lock",
        "pnpm-lock.yaml",
        "composer.lock",
        
        // Frontend - Build/Generated
        "*.min.js", "*.min.css",
        "*.bundle.js", "*.chunk.js",
        "manifest.json",
        
        // Frontend - Config files (ESCLUSI come richiesto)
        "tsconfig.json",
        "package.json",
        "*.csproj",
        "angular.json",
        "vue.config.js",
        "next.config.js",
        "webpack.config.js",
        "tailwind.config.js",
        "jest.config.js",
        "eslint.config.js",
        ".eslintrc.*",
        "prettier.config.js",
        ".prettierrc.*",
        
        // Environment/Secrets
        ".env", ".env.*",
        "secrets.json",
        "*.key", "*.pem", "*.crt", "*.pfx",
        
        // Git
        ".gitignore", ".gitattributes", ".gitmodules",
        
        // Editor/IDE
        "*.swp", "*.swo", "*~",
        "*.user", "*.suo", "*.userprefs",
        
        // Testing/Coverage
        "*.coverage", "coverage.xml", "*.lcov",
        
        // Logs
        "*.log", "npm-debug.log*", "yarn-debug.log*", "lerna-debug.log*",
        
        // OS
        ".DS_Store", "Thumbs.db", "ehthumbs.db",
        
        // Documentation (ESCLUSI tranne README)
        "CHANGELOG.md", "LICENSE", "LICENSE.txt",
        
        // Database
        "*.db", "*.sqlite", "*.sqlite3",
        
        // Backup files
        "*.bak", "*.backup", "*.old", "*.orig"
    };

        private readonly HashSet<string> _includedFiles =
        [
            // README files (case insensitive check nel codice)
            "readme.md", "readme.txt", "readme"
        ];

        public async Task GenerateAsync(string projectPath, string outputFile)
        {
            if (!Directory.Exists(projectPath))
            {
                throw new DirectoryNotFoundException($"Directory not found: {projectPath}");
            }

            Console.WriteLine($"🔍 Scanning project: {projectPath}");

            var files = GetRelevantFiles(projectPath).ToList();
            Console.WriteLine($"📁 Found {files.Count} relevant files");

            var markdown = new StringBuilder();

            // Header
            markdown.AppendLine("# AI Project Digest");
            markdown.AppendLine($"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            markdown.AppendLine($"Project path: `{projectPath}`");
            markdown.AppendLine($"Total files: {files.Count}\n");

            // File contents
            foreach (var file in files)
            {
                var relativePath = Path.GetRelativePath(projectPath, file);
                var content = await File.ReadAllTextAsync(file);
                var language = GetLanguageFromExtension(Path.GetExtension(file));

                Console.WriteLine($"📄 Processing: {relativePath}");

                markdown.AppendLine($"## {relativePath}");
                markdown.AppendLine($"```{language}");
                markdown.AppendLine(content);
                markdown.AppendLine("```");
                markdown.AppendLine();
            }

            await File.WriteAllTextAsync(outputFile, markdown.ToString());
        }

        private IEnumerable<string> GetRelevantFiles(string projectPath)
        {
            return Directory.EnumerateFiles(projectPath, "*", SearchOption.AllDirectories)
                .Where(file =>
                {
                    var pathParts = Path.GetRelativePath(projectPath, file).Split(Path.DirectorySeparatorChar);

                    // Escludi cartelle specifiche
                    if (pathParts.Any(part => _excludedFolders.Contains(part)))
                        return false;

                    var fileName = Path.GetFileName(file);

                    // Includi sempre i README
                    if (IsReadmeFile(fileName))
                        return true;

                    // Escludi file specifici
                    if (IsExcludedFile(fileName))
                        return false;

                    var extension = Path.GetExtension(fileName);

                    // Include solo estensioni specificate
                    return _includedExtensions.Contains(extension.ToLower());
                })
                .OrderBy(file => file);
        }

        private bool IsReadmeFile(string fileName)
        {
            return fileName.StartsWith("readme", StringComparison.CurrentCultureIgnoreCase);
        }

        private bool IsExcludedFile(string fileName)
        {
            foreach (var pattern in _excludedFilePatterns)
            {
                if (pattern.Contains('*'))
                {
                    // Gestione pattern con wildcard
                    if (pattern.StartsWith("*."))
                    {
                        var ext = pattern.Substring(1);
                        if (fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase)) return true;
                    }
                    else if (pattern.EndsWith("*"))
                    {
                        var prefix = pattern.Substring(0, pattern.Length - 1);
                        if (fileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return true;
                    }
                    else if (pattern.Contains("*"))
                    {
                        // Per pattern come "appsettings.*.json"
                        var parts = pattern.Split('*');
                        if (parts.Length == 2 &&
                            fileName.StartsWith(parts[0], StringComparison.OrdinalIgnoreCase) &&
                            fileName.EndsWith(parts[1], StringComparison.OrdinalIgnoreCase))
                            return true;
                    }
                }
                else
                {
                    // Match esatto
                    if (fileName.Equals(pattern, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            return false;
        }

        private static string GetLanguageFromExtension(string extension) => extension.ToLower() switch
        {
            ".cs" => "csharp",
            ".js" => "javascript",
            ".ts" => "typescript",
            ".jsx" => "jsx",
            ".tsx" => "tsx",
            ".py" => "python",
            ".java" => "java",
            ".cpp" or ".cc" => "cpp",
            ".h" => "c",
            ".html" => "html",
            ".css" => "css",
            ".scss" => "scss",
            ".vue" => "vue",
            ".php" => "php",
            ".rb" => "ruby",
            ".go" => "go",
            ".rs" => "rust",
            ".sql" => "sql",
            ".json" => "json",
            ".xml" => "xml",
            ".yaml" or ".yml" => "yaml",
            ".md" => "markdown",
            _ => "text"
        };
    }
}
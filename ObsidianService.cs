using System.Text;
using System.ComponentModel;
using ModelContextProtocol.Server;

[McpServerToolType]
public static class ObsidianService
{
    private static string VaultPath { get; set; } = "";

    public static void SetVaultPath(string path)
    {
        VaultPath = path;
    }

    [McpServerTool, Description("Searches markdown files in the vault by content")]
    public static async Task<string> SearchNotes(
        [Description("The search query to find in note content")] string query)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
                return "Error: Query cannot be empty";

            var results = new List<string>();
            var vaultDirectory = new DirectoryInfo(VaultPath);

            if (!vaultDirectory.Exists)
                return $"Error: Vault directory not found at {VaultPath}";

            var markdownFiles = vaultDirectory.EnumerateFiles("*.md", SearchOption.AllDirectories);

            foreach (var file in markdownFiles)
            {
                try
                {
                    var content = await File.ReadAllTextAsync(file.FullName);
                    if (content.Contains(query, StringComparison.OrdinalIgnoreCase))
                    {
                        results.Add(file.FullName);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error reading file {file.FullName}: {ex.Message}");
                }
            }

            if (results.Count == 0)
                return $"No notes found containing '{query}'";

            return string.Join("\n", results);
        }
        catch (Exception ex)
        {
            return $"Error searching notes: {ex.Message}";
        }
    }

    [McpServerTool, Description("Reads the full content of a specific markdown note")]
    public static async Task<string> ReadNote(
        [Description("The file path to the note to read")] string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
                return "Error: Path cannot be empty";

            var fileInfo = new FileInfo(path);

            if (!fileInfo.Exists)
                return $"Error: File not found at {path}";

            if (!fileInfo.FullName.StartsWith(VaultPath, StringComparison.OrdinalIgnoreCase))
                return "Error: File is outside the vault directory";

            var content = await File.ReadAllTextAsync(fileInfo.FullName);
            return content;
        }
        catch (Exception ex)
        {
            return $"Error reading note: {ex.Message}";
        }
    }

    [McpServerTool, Description("Lists the most recently modified notes in the vault")]
    public static string ListRecentNotes(
        [Description("The number of recent notes to list (default: 10)")] int count = 10)
    {
        try
        {
            if (count <= 0)
                return "Error: Count must be greater than 0";

            var vaultDirectory = new DirectoryInfo(VaultPath);

            if (!vaultDirectory.Exists)
                return $"Error: Vault directory not found at {VaultPath}";

            var markdownFiles = vaultDirectory.EnumerateFiles("*.md", SearchOption.AllDirectories)
                .OrderByDescending(f => f.LastWriteTime)
                .Take(count)
                .ToList();

            if (markdownFiles.Count == 0)
                return "No markdown files found in vault";

            var sb = new StringBuilder();
            sb.AppendLine($"Recent {markdownFiles.Count} notes:");
            foreach (var file in markdownFiles)
            {
                sb.AppendLine($"- {file.FullName} (Modified: {file.LastWriteTime:yyyy-MM-dd HH:mm:ss})");
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            return $"Error listing recent notes: {ex.Message}";
        }
    }

    [McpServerTool, Description("Gets the daily note for a specific date")]
    public static async Task<string> GetDailyNote(
        [Description("Date string: 'today', 'yesterday', or 'YYYY-MM-DD' format")] string date)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(date))
                return "Error: Date cannot be empty";

            DateTime targetDate;

            // Parse the date string
            if (date.Equals("today", StringComparison.OrdinalIgnoreCase))
            {
                targetDate = DateTime.Now.Date;
            }
            else if (date.Equals("yesterday", StringComparison.OrdinalIgnoreCase))
            {
                targetDate = DateTime.Now.Date.AddDays(-1);
            }
            else if (DateTime.TryParseExact(date, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var parsedDate))
            {
                targetDate = parsedDate.Date;
            }
            else
            {
                return "Error: Invalid date format. Use 'today', 'yesterday', or 'YYYY-MM-DD'";
            }

            // Construct the path
            string fileName = targetDate.ToString("yyyy-MM-dd");
            string filePath = Path.Combine(VaultPath, "dailies", $"{fileName}.md");

            // Read the file
            var fileInfo = new FileInfo(filePath);

            if (!fileInfo.Exists)
                return $"Error: Daily note not found for {fileName}";

            if (!fileInfo.FullName.StartsWith(VaultPath, StringComparison.OrdinalIgnoreCase))
                return "Error: File is outside the vault directory";

            var content = await File.ReadAllTextAsync(fileInfo.FullName);
            return content;
        }
        catch (Exception ex)
        {
            return $"Error reading daily note: {ex.Message}";
        }
    }

    [McpServerTool, Description("Creates a new markdown note with optional tags and folder")]
    public static async Task<string> CreateNote(
        [Description("Title for the new note")] string title,
        [Description("Content for the note")] string content,
        [Description("Optional array of tags (e.g., ['snippet', 'sql'])")] string[]? tags = null,
        [Description("Optional folder path (e.g., 'projects/work', 'other'). Defaults to vault root")] string? folder = null)
    {
        try
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(title))
                return "Error: Title cannot be empty";

            if (string.IsNullOrWhiteSpace(content))
                return "Error: Content cannot be empty";

            // Check if trying to access dailies folder
            if (!string.IsNullOrEmpty(folder) && folder.Equals("dailies", StringComparison.OrdinalIgnoreCase))
                return "Error: Cannot create notes in 'dailies' folder. Use CreateDaily() for daily notes.";

            // Construct target folder path
            string targetFolder = string.IsNullOrEmpty(folder) ? VaultPath : Path.Combine(VaultPath, folder);

            // Validate path stays within vault
            string fullTargetPath = Path.GetFullPath(targetFolder);
            string fullVaultPath = Path.GetFullPath(VaultPath);
            
            if (!fullTargetPath.StartsWith(fullVaultPath, StringComparison.OrdinalIgnoreCase))
                return "Error: Folder path must be within vault directory";

            // Create folder if it doesn't exist
            try
            {
                Directory.CreateDirectory(targetFolder);
            }
            catch (Exception ex)
            {
                return $"Error: Cannot create folder - {ex.Message}";
            }

            // Construct filename with .md extension
            string fileName = title + ".md";
            string filePath = Path.Combine(targetFolder, fileName);

            // Check if file already exists
            if (File.Exists(filePath))
                return $"Error: Note '{title}' already exists. Use EditNote() or AppendNote() to modify existing notes.";

            // Build file content with tags at the top
            var sb = new StringBuilder();
            
            // Add tags at the very top if provided
            if (tags != null && tags.Length > 0)
            {
                var tagString = string.Join(" ", tags.Select(t => t.StartsWith("#") ? t : $"#{t}"));
                sb.AppendLine(tagString);
                sb.AppendLine();
            }

            sb.Append(content);

            // Write the file
            try
            {
                await File.WriteAllTextAsync(filePath, sb.ToString());
                
                // Return relative path from vault root for user-friendly message
                string relativePath = Path.GetRelativePath(VaultPath, filePath);
                return $"Created: {relativePath}";
            }
            catch (Exception ex)
            {
                return $"Error: Cannot write file - {ex.Message}";
            }
        }
        catch (Exception ex)
        {
            return $"Error creating note: {ex.Message}";
        }
    }

    [McpServerTool, Description("Marks a task with a specific status in a note")]
    public static async Task<string> MarkTask(
        [Description("The exact task text to find (e.g., 'Call MetEd')")] string taskText,
        [Description("Task status: 'Completed', 'InProgress', 'Forwarded', 'Scheduled', or 'Open'")] string status,
        [Description("Relative path from vault root (e.g., 'dailies/2025-12-04.md' or '2025-12-04.md')")] string filePath)
    {
        try
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(taskText))
                return "Error: Task text cannot be empty";

            if (string.IsNullOrWhiteSpace(filePath))
                return "Error: File path cannot be empty";

            // Validate status
            var validStatuses = new[] { "Completed", "InProgress", "Forwarded", "Scheduled", "Open" };
            if (!validStatuses.Contains(status))
                return $"Error: Invalid status '{status}'. Must be one of: {string.Join(", ", validStatuses)}";

            // Determine the checkbox character
            char checkboxChar = status switch
            {
                "Completed" => 'x',
                "InProgress" => '/',
                "Forwarded" => '>',
                "Scheduled" => '<',
                "Open" => ' ',
                _ => ' '
            };

            // Resolve the full file path
            string fullFilePath = Path.IsPathRooted(filePath) 
                ? filePath 
                : Path.Combine(VaultPath, filePath);

            var fileInfo = new FileInfo(fullFilePath);

            // Validate file exists
            if (!fileInfo.Exists)
                return $"Error: File not found at {filePath}";

            // Validate file is within vault
            if (!fileInfo.FullName.StartsWith(VaultPath, StringComparison.OrdinalIgnoreCase))
                return "Error: File is outside the vault directory";

            // Read the file
            var content = await File.ReadAllTextAsync(fileInfo.FullName);
            var lines = content.Split('\n');

            // Find and mark all matching lines
            int matchCount = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                // Look for the task pattern: [ ], [x], [\], [>], [<] followed by the task text
                var line = lines[i];
                
                // Pattern: [?] task text where [?] is any checkbox marker
                if (System.Text.RegularExpressions.Regex.IsMatch(line, @"\[[^\]]\]\s+" + System.Text.RegularExpressions.Regex.Escape(taskText) + @"(?:\s|$)"))
                {
                    // Replace the checkbox marker
                    lines[i] = System.Text.RegularExpressions.Regex.Replace(
                        line, 
                        @"\[[^\]]\](\s+" + System.Text.RegularExpressions.Regex.Escape(taskText) + @"(?:\s|$))",
                        $"[{checkboxChar}]$1"
                    );
                    matchCount++;
                }
            }

            if (matchCount == 0)
                return $"Error: Task '{taskText}' not found in file";

            // Write the updated content back
            var updatedContent = string.Join("\n", lines);
            await File.WriteAllTextAsync(fileInfo.FullName, updatedContent);

            return $"Marked {matchCount} task(s) '{taskText}' as {status}";
        }
        catch (Exception ex)
        {
            return $"Error marking task: {ex.Message}";
        }
    }

    [McpServerTool, Description("Adds a new task to the Short List section of a daily note")]
    public static async Task<string> AddTaskToDaily(
        [Description("The task text to add (e.g., 'Call MetEd')")] string taskText,
        [Description("Date string: 'today', 'yesterday', or 'YYYY-MM-DD' format")] string date)
    {
        try
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(taskText))
                return "Error: Task text cannot be empty";

            if (string.IsNullOrWhiteSpace(date))
                return "Error: Date cannot be empty";

            // Parse the date (same logic as GetDailyNote)
            DateTime targetDate;

            if (date.Equals("today", StringComparison.OrdinalIgnoreCase))
            {
                targetDate = DateTime.Now.Date;
            }
            else if (date.Equals("yesterday", StringComparison.OrdinalIgnoreCase))
            {
                targetDate = DateTime.Now.Date.AddDays(-1);
            }
            else if (DateTime.TryParseExact(date, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var parsedDate))
            {
                targetDate = parsedDate.Date;
            }
            else
            {
                return "Error: Invalid date format. Use 'today', 'yesterday', or 'YYYY-MM-DD'";
            }

            // Construct the path
            string fileName = targetDate.ToString("yyyy-MM-dd");
            string filePath = Path.Combine(VaultPath, "dailies", $"{fileName}.md");

            // Validate file exists
            var fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists)
                return $"Error: Daily note not found for {fileName}";

            // Validate file is within vault
            if (!fileInfo.FullName.StartsWith(VaultPath, StringComparison.OrdinalIgnoreCase))
                return "Error: File is outside the vault directory";

            // Read the file
            var content = await File.ReadAllTextAsync(fileInfo.FullName);
            var lines = content.Split('\n').ToList();

            // Find "Short List" section
            int shortListIndex = -1;
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].Contains("Short List", StringComparison.OrdinalIgnoreCase))
                {
                    shortListIndex = i;
                    break;
                }
            }

            if (shortListIndex == -1)
                return "Error: 'Short List' section not found in daily note";

            // Find the end of the task list
            int taskListEndIndex = -1;
            int firstEmptyTaskIndex = -1;

            for (int i = shortListIndex + 1; i < lines.Count; i++)
            {
                string line = lines[i].Trim();

                // Check if we've hit another section header
                if (line.StartsWith("####"))
                    return "Error: Task list section is malformed or missing blank line separator";

                // Check if this is a task line (starts with "- [")
                if (line.StartsWith("- ["))
                {
                    // Check if it's an empty task (- [ ] with nothing after)
                    if (System.Text.RegularExpressions.Regex.IsMatch(line, @"^-\s+\[\s*\]\s*$"))
                    {
                        if (firstEmptyTaskIndex == -1)
                            firstEmptyTaskIndex = i;
                    }
                    taskListEndIndex = i;
                }
                else if (line == "")
                {
                    // Blank line after tasks - this is good
                    if (taskListEndIndex != -1)
                        break;
                }
                else if (!line.StartsWith("- ["))
                {
                    // Non-task, non-blank line - end of list
                    if (taskListEndIndex != -1)
                        break;
                }
            }

            if (taskListEndIndex == -1)
                return "Error: No tasks found in Short List section";

            // Add the new task
            if (firstEmptyTaskIndex != -1)
            {
                // Replace the first empty task
                lines[firstEmptyTaskIndex] = "- [ ] " + taskText;
            }
            else
            {
                // Append after the last task
                lines.Insert(taskListEndIndex + 1, "- [ ] " + taskText);
            }

            // Write the updated content back
            var updatedContent = string.Join("\n", lines);
            await File.WriteAllTextAsync(fileInfo.FullName, updatedContent);

            return $"Added: {taskText} to {fileName}";
        }
        catch (Exception ex)
        {
            return $"Error adding task to daily: {ex.Message}";
        }
    }
}
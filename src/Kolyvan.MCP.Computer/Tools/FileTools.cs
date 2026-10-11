using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace Kolyvan.MCP.Computer.Tools;

[McpServerToolType]
[Description("File system tools for searching, reading, listing and modifying files.")]
public class FileTools
{
    private const int DefaultMaxResults = 500;

    [McpServerTool]
    [Description("""
    Finds files by glob pattern (e.g. "**/*.cs", "src/**/Tools/*.cs").
    Returns full paths of matching files, one per line.
    """)]
    public string Glob(
        [Description("Glob pattern relative to the base path.")]
        string pattern,

        [Description("Base directory. If omitted, the process current directory is used.")]
        string? path = null,

        [Description("Maximum number of results to return.")]
        int maxResults = DefaultMaxResults)
    {
        string basePath = ResolveDirectory(path);

        var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
        matcher.AddInclude(pattern);

        PatternMatchingResult result = matcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(basePath)));

        var files = result.Files
            .Select(f => Path.GetFullPath(Path.Combine(basePath, f.Path)))
            .Take(maxResults)
            .ToList();

        if (files.Count == 0)
        {
            return "No files found.";
        }

        return string.Join(Environment.NewLine, files);
    }

    [McpServerTool]
    [Description("""
    Searches file contents with a regular expression (.NET syntax).
    Returns matches as "path:line: text".
    """)]
    public string Grep(
        [Description("Regular expression to search for.")]
        string pattern,

        [Description("File or directory to search. If omitted, the process current directory is used.")]
        string? path = null,

        [Description("Optional glob pattern to filter files when searching a directory (e.g. \"*.cs\").")]
        string? fileGlob = null,

        [Description("Case-insensitive search.")]
        bool ignoreCase = false,

        [Description("Maximum number of matching lines to return.")]
        int maxResults = DefaultMaxResults,

        CancellationToken cancellationToken = default)
    {
        var options = RegexOptions.Multiline | (ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);
        var regex = new Regex(pattern, options, TimeSpan.FromSeconds(5));

        string target = string.IsNullOrWhiteSpace(path)
            ? Directory.GetCurrentDirectory()
            : Path.GetFullPath(path);

        IEnumerable<string> files = File.Exists(target)
            ? new[] { target }
            : EnumerateFilesByGlob(target, fileGlob ?? "**/*");

        var sb = new StringBuilder();
        int count = 0;

        foreach (string file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string[] lines;
            try
            {
                lines = File.ReadAllLines(file);
            }
            catch (IOException)
            {
                continue;
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }

            for (int i = 0; i < lines.Length; i++)
            {
                if (!regex.IsMatch(lines[i]))
                {
                    continue;
                }

                sb.AppendLine($"{file}:{i + 1}: {lines[i]}");
                count++;

                if (count >= maxResults)
                {
                    sb.AppendLine($"... results limited to {maxResults}.");
                    return sb.ToString().TrimEnd();
                }
            }
        }

        return count == 0 ? "No matches found." : sb.ToString().TrimEnd();
    }

    [McpServerTool]
    [Description("""
    Reads a text file and returns its lines prefixed with line numbers ("   1\t...").
    Use offset and limit to read a part of a large file.
    """)]
    public string ReadFile(
        [Description("Path to the file.")]
        string path,

        [Description("Line number to start from (1-based).")]
        int offset = 1,

        [Description("Maximum number of lines to read. If omitted, reads to the end of the file.")]
        int? limit = null)
    {
        string fullPath = Path.GetFullPath(path);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("File not found.", fullPath);
        }

        if (offset < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Offset must be at least 1.");
        }

        string[] lines = File.ReadAllLines(fullPath);
        int start = offset - 1;

        if (start >= lines.Length)
        {
            return string.Empty;
        }

        int count = limit is null
            ? lines.Length - start
            : Math.Min(limit.Value, lines.Length - start);

        var sb = new StringBuilder();

        for (int i = start; i < start + count; i++)
        {
            sb.Append((i + 1).ToString().PadLeft(6))
              .Append('\t')
              .AppendLine(lines[i]);
        }

        return sb.ToString().TrimEnd();
    }

    [McpServerTool]
    [Description("""
    Lists the contents of a single directory (not recursive).
    Directories are shown with a trailing "/", files with their size in bytes.
    """)]
    public string ListDirectory(
        [Description("Directory to list. If omitted, the process current directory is used.")]
        string? path = null)
    {
        string directory = ResolveDirectory(path);

        var entries = new DirectoryInfo(directory)
            .EnumerateFileSystemInfos()
            .OrderBy(e => e is FileInfo ? 1 : 0)
            .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
            .Select(e => e is DirectoryInfo
                ? $"{e.Name}/"
                : $"{e.Name}  ({((FileInfo)e).Length} bytes)")
            .ToList();

        if (entries.Count == 0)
        {
            return "Directory is empty.";
        }

        return string.Join(Environment.NewLine, entries);
    }

    [McpServerTool]
    [Description("""
    Creates a new file with the given content. Missing parent directories are created.
    Overwrites the file if it already exists.
    """)]
    public string WriteFile(
        [Description("Path to the file to create.")]
        string path,

        [Description("Full content of the file.")]
        string content)
    {
        string fullPath = Path.GetFullPath(path);

        string? directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        bool existed = File.Exists(fullPath);
        File.WriteAllText(fullPath, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        return existed
            ? $"File overwritten: {fullPath}"
            : $"File created: {fullPath}";
    }

    [McpServerTool]
    [Description("""
    Edits a file by replacing an exact text fragment.
    old_string must match the file content exactly (including whitespace and line breaks)
    and must be unique in the file, unless replaceAll is true.
    """)]
    public string EditFile(
        [Description("Path to the file to edit.")]
        string path,

        [Description("Exact text to find.")]
        string oldString,

        [Description("Text to replace it with.")]
        string newString,

        [Description("Replace all occurrences instead of requiring a unique match.")]
        bool replaceAll = false)
    {
        string fullPath = Path.GetFullPath(path);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("File not found.", fullPath);
        }

        if (string.IsNullOrEmpty(oldString))
        {
            throw new ArgumentException("oldString must not be empty.", nameof(oldString));
        }

        string text = File.ReadAllText(fullPath);
        int occurrences = CountOccurrences(text, oldString);

        if (occurrences == 0)
        {
            throw new InvalidOperationException("oldString was not found in the file.");
        }

        if (occurrences > 1 && !replaceAll)
        {
            throw new InvalidOperationException(
                $"oldString matches {occurrences} times. Provide more context or set replaceAll to true.");
        }

        string updated = replaceAll
            ? text.Replace(oldString, newString, StringComparison.Ordinal)
            : ReplaceFirst(text, oldString, newString);

        File.WriteAllText(fullPath, updated);

        return $"File edited: {fullPath} ({(replaceAll ? occurrences : 1)} replacement(s)).";
    }

    private static string ResolveDirectory(string? path)
    {
        string fullPath = string.IsNullOrWhiteSpace(path)
            ? Directory.GetCurrentDirectory()
            : Path.GetFullPath(path);

        if (!Directory.Exists(fullPath))
        {
            throw new DirectoryNotFoundException($"Directory not found: {fullPath}");
        }

        return fullPath;
    }

    private static IEnumerable<string> EnumerateFilesByGlob(string directory, string glob)
    {
        var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
        matcher.AddInclude(glob);

        return matcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(directory)))
            .Files
            .Select(f => Path.Combine(directory, f.Path));
    }

    private static int CountOccurrences(string text, string value)
    {
        int count = 0;
        int index = 0;

        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }

    private static string ReplaceFirst(string text, string oldValue, string newValue)
    {
        int index = text.IndexOf(oldValue, StringComparison.Ordinal);
        return string.Concat(text.AsSpan(0, index), newValue, text.AsSpan(index + oldValue.Length));
    }
}

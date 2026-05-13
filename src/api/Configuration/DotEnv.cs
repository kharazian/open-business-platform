namespace OpenBusinessPlatform.Api.Configuration;

public static class DotEnv
{
    public static void LoadFromNearestFile(string fileName = ".env")
    {
        foreach (var directory in GetCandidateDirectories())
        {
            var path = Path.Combine(directory, fileName);

            if (File.Exists(path))
            {
                Load(path);
                return;
            }
        }
    }

    private static void Load(string path)
    {
        foreach (var rawLine in File.ReadAllLines(path))
        {
            var line = rawLine.Trim();

            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            var separatorIndex = line.IndexOf('=');

            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim();

            if (key.Length == 0 || Environment.GetEnvironmentVariable(key) is not null)
            {
                continue;
            }

            Environment.SetEnvironmentVariable(key, Unquote(value));
        }
    }

    private static IEnumerable<string> GetCandidateDirectories()
    {
        var seenDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var startDirectories = new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory };

        foreach (var startDirectory in startDirectories)
        {
            var directory = new DirectoryInfo(startDirectory);

            while (directory is not null)
            {
                if (seenDirectories.Add(directory.FullName))
                {
                    yield return directory.FullName;
                }

                directory = directory.Parent;
            }
        }
    }

    private static string Unquote(string value)
    {
        if (value.Length < 2)
        {
            return value;
        }

        var first = value[0];
        var last = value[^1];

        return first == last && (first == '"' || first == '\'')
            ? value[1..^1]
            : value;
    }
}

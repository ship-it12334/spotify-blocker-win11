using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

class BlockConfig
{
    public static HashSet<string> Load(string path)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            if (File.Exists(path))
            {
                foreach (var line in File.ReadAllLines(path, Encoding.UTF8))
                {
                    var t = line.Trim();
                    if (t.StartsWith("#") || t.Length == 0) continue;
                    set.Add(t.ToLowerInvariant());
                }
            }
        }
        catch { }
        return set;
    }
}

//ConfigFile.cs

using System;
using System.Collections.Generic;
using System.IO;

namespace ArchDandara
{
    public class ConfigFile
    {
        private readonly string _path;
        private readonly Dictionary<string, string> _values = new Dictionary<string, string>();

        public ConfigFile(string path)
        {
            _path = path;
        }

        // ============================================================
        //  LOAD FILE
        // ============================================================
        public void Load()
        {
            _values.Clear();

            if (!File.Exists(_path))
                return;

            var lines = File.ReadAllLines(_path);

            foreach (var line in lines)
            {
                if (string.IsNullOrEmpty(line)) continue;
                if (!line.Contains("=")) continue;

                var split = line.Split(new[] { '=' }, 2);
                string key = split[0].Trim();
                string value = split[1].Trim();

                _values[key] = value;
            }
        }

        // ============================================================
        // SAVE FILE
        // ============================================================
        public void Save(string headerComment = null)
        {
            List<string> lines = new List<string>();

            // Optional header block
            if (!string.IsNullOrEmpty(headerComment))
            {
                foreach (string line in headerComment.Split('\n'))
                    lines.Add("# " + line.TrimEnd());
                lines.Add(""); // spacer
            }

            foreach (var pair in _values)
                lines.Add($"{pair.Key}={pair.Value}");

            File.WriteAllLines(_path, lines.ToArray());
        }

        // ============================================================
        // API — Get / Set / Contains
        // ============================================================
        public bool ContainsKey(string key)
        {
            return _values.ContainsKey(key);
        }

        public string Get(string key, string defaultVal)
        {
            if (_values.TryGetValue(key, out string val))
                return val;

            _values[key] = defaultVal;
            return defaultVal;
        }

        public bool GetBool(string key, bool defaultVal)
        {
            string def = defaultVal ? "true" : "false";
            string result = Get(key, def);
            return result.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        public int GetInt(string key, int defaultVal)
        {
            string result = Get(key, defaultVal.ToString());
            if (int.TryParse(result, out var parsed))
                return parsed;

            return defaultVal;
        }

        public void Set(string key, object value)
        {
            _values[key] = value.ToString();
        }
    }
}
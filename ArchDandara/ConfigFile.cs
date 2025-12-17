//ConfigFile.cs

using System;
using System.Collections.Generic;
using System.IO;

namespace ArchDandara
{
    /// <summary>
    /// =============================================================================================
    /// CONFIG FILE HANDLER — Custom lightweight key/value config system
    /// =============================================================================================
    /// This class provides a very simple `.cfg` reader/writer designed specifically
    /// for MelonLoader mods.  
    ///
    /// It behaves like a dictionary saved to a file:
    ///
    ///     Key1=Value1
    ///     EnableFeature=true
    ///     MaxItems=50
    ///
    /// The system:
    ///   • Stores everything as strings internally  
    ///   • Loads line-by-line  
    ///   • Provides helper functions for bool, int, and normal strings  
    ///   • Writes the file back to disk with an optional header  
    ///
    /// This is intentionally **NOT** MelonPreferences — you wanted full control over
    /// creating your own files in your own directories.  
    ///
    /// =============================================================================================
    /// </summary>
    public class ConfigFile
    {
        /// <summary>
        /// Full file path (example: .../UserData/ArchDandara/ArchDandara.cfg)
        /// </summary>
        private readonly string _path;

        /// <summary>
        /// In-memory store of all loaded config values.
        /// Keys = string, Values = string
        /// </summary>
        private readonly Dictionary<string, string> _values = new Dictionary<string, string>();
        
        // ====================================================================================================
        //  CONSTRUCTOR (Option A Explanation)
        // ----------------------------------------------------------------------------------------------------
        //  • Saves the file path so other methods know where to load/save
        //  • Does NOT automatically load — Load() must be called manually
        //  Why? You may want to control the order of events (ex: create directory first).
        // ====================================================================================================
        public ConfigFile(string path)
        {
            _path = path;
        }
        
        // ====================================================================================================
        //  Purpose:
        //      Reads the config file from disk and fills the internal _values dictionary.
        //
        //  Process:
        //      1. Clear old values
        //      2. If file missing → nothing to load (all defaults will be created later)
        //      3. Read each line
        //      4. Skip blank lines and invalid lines
        //      5. Split line at '=' into "key=value"
        //      6. Store into dictionary
        //
        //  VERY SIMPLE FORMAT — there is no quoting, escaping, or arrays.
        // ====================================================================================================
        public void Load()
        {
            _values.Clear();

            if (!File.Exists(_path))
                return;

            var lines = File.ReadAllLines(_path);

            foreach (var line in lines)
            {
                // skip empty or invalid lines
                if (string.IsNullOrEmpty(line)) continue;
                if (!line.Contains("=")) continue;

                // Split ONLY on first '=' to allow values to contain '='
                var split = line.Split(new[] { '=' }, 2);

                string key   = split[0].Trim();
                string value = split[1].Trim();

                _values[key] = value;
            }
        }
        
        // ====================================================================================================
        //  Saves the entire config back to disk.
        //
        //  Features:
        //      • Optional comment header at the top (multi-line with '#')
        //      • Writes each key=value pair
        //
        //  Implementation Notes:
        //      • We convert List<string> → string[] because File.WriteAllLines expects an array
        //      • Values are written exactly as stored (no formatting)
        // ====================================================================================================
        public void Save(string headerComment = null)
        {
            List<string> lines = new List<string>();

            // Add header block if provided
            if (!string.IsNullOrEmpty(headerComment))
            {
                foreach (string line in headerComment.Split('\n'))
                    lines.Add("# " + line.TrimEnd());
                lines.Add(""); // blank line separator
            }

            // Add all stored key/value pairs
            foreach (var pair in _values)
                lines.Add($"{pair.Key}={pair.Value}");

            // Write to file
            File.WriteAllLines(_path, lines.ToArray());
        }
        
        // ====================================================================================================
        //  These helper methods provide typed access (string/bool/int) and create defaults when missing.
        //
        //  Design Notes:
        //  • All values stored as strings internally
        //  • Get() auto-creates missing keys
        //  • GetBool() and GetInt() convert safely
        // ====================================================================================================

        /// <summary>
        /// Returns TRUE if the config file already has the given key stored.
        /// </summary>
        public bool ContainsKey(string key) => _values.ContainsKey(key);
        
        /// <summary>
        /// Gets the value for a key, OR creates it using defaultVal if missing.
        /// </summary>
        public string Get(string key, string defaultVal)
        {
            if (_values.TryGetValue(key, out string val))
                return val;

            _values[key] = defaultVal;
            return defaultVal;
        }
        
        /// <summary>
        /// Typed getter for booleans.
        /// Accepted values: "true" or "false" (case-insensitive).
        /// </summary>
        public bool GetBool(string key, bool defaultVal)
        {
            string def = defaultVal ? "true" : "false";
            string result = Get(key, def);

            return result.Equals("true", StringComparison.OrdinalIgnoreCase);
        }
        
        /// <summary>
        /// Typed getter for integers.
        /// Returns defaultVal if parsing fails.
        /// </summary>
        public int GetInt(string key, int defaultVal)
        {
            string result = Get(key, defaultVal.ToString());

            if (int.TryParse(result, out var parsed))
                return parsed;

            return defaultVal;
        }
        
        /// <summary>
        /// Sets a key to a new value (stored as string).
        /// </summary>
        public void Set(string key, object value)
        {
            _values[key] = value.ToString();
        }
    }
}
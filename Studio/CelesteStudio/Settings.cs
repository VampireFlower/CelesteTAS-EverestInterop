using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using CelesteStudio.Editing;
using CelesteStudio.Util;
using Eto;
using Eto.Drawing;
using Eto.Forms;
using Tommy;
using Tommy.Serializer;

namespace CelesteStudio;

public enum ThemeType {
    Light,
    Dark,
}

[TommyTableName("Settings")]
public sealed class Settings {
    public static string BaseConfigPath => Path.Combine(EtoEnvironment.GetFolderPath(EtoSpecialFolder.ApplicationSettings), "CelesteStudio"); 
    public static string SettingsPath => Path.Combine(BaseConfigPath, "Settings.toml");
    public static string SnippetsPath => Path.Combine(BaseConfigPath, "Snippets.toml");
    
    public static Settings Instance { get; private set; } = new();
    public static readonly List<Snippet> Snippets = [new Snippet { Shortcut = Keys.C | Keys.Control | Keys.Application, Text = "Set, Player.X, "} ];
    
    public static event Action? Changed;
    public void OnChanged() => Changed?.Invoke();
    
    public static event Action? ThemeChanged;
    private void OnThemeChanged() => ThemeChanged?.Invoke();
    
    public static event Action FontChanged = FontManager.OnFontChanged;
    public void OnFontChanged() => FontChanged.Invoke();
    
    [TommyIgnore]
    public Theme Theme => ThemeType switch {
        ThemeType.Light => Theme.Light,    
        ThemeType.Dark => Theme.Dark,
        _ => throw new UnreachableException(),
    };
    
    private ThemeType themeType = ThemeType.Light;
    public ThemeType ThemeType {
        get => themeType;
        set {
            themeType = value;
            OnThemeChanged();
        }
    }
    
    public bool AutoSave { get; set; } = true;
    public bool SendInputsToCeleste { get; set; } = true;
    public bool ShowGameInfo { get; set; } = true;
    public bool AutoRemoveMutuallyExclusiveActions { get; set; } = true;
    public bool AlwaysOnTop { get; set; } = false;
    public bool AutoBackupEnabled { get; set; } = true;
    public int AutoBackupRate { get; set; } = 1;
    public int AutoBackupCount { get; set; } = 100;
    public bool FindMatchCase { get; set; }
    public bool WordWrapComments { get; set; } = true;
    
    public string FontFamily { get; set; } = FontManager.FontFamilyBuiltin;
    public float EditorFontSize { get; set; } = 12.0f;
    public float StatusFontSize { get; set; } = 9.0f;
    
    private const int MaxRecentFiles = 20;
    public List<string> RecentFiles { get; set; } = [];
    
    public void AddRecentFile(string filePath) {
        // Avoid duplicates
        RecentFiles.Remove(filePath);
        
        RecentFiles.Insert(0, filePath);
        if (RecentFiles.Count > MaxRecentFiles) {
            RecentFiles.RemoveRange(MaxRecentFiles, RecentFiles.Count - MaxRecentFiles);
        }
        
        OnChanged();
        Save();
    }
    public void ClearRecentFiles() {
        RecentFiles.Clear();
        
        OnChanged();
        Save();
    }
    
    public static void Load() {
        if (File.Exists(SettingsPath)) {
            try {
                Instance = TommySerializer.FromTomlFile<Settings>(SettingsPath);
                
                var snippetTable = TommySerializer.ReadFromDisk(SnippetsPath)["Snippets"];
                if (snippetTable.Keys.Any()) {
                    Snippets.Clear();
                    foreach (var key in snippetTable.Keys) {
                        var value = snippetTable[key];
                        if (!value.IsString)
                            continue;
                        
                        var shortcut = key.Split('+')
                            .Select(keyName => Enum.TryParse<Keys>(keyName, out var k) ? k : Keys.None)
                            .Aggregate((a, b) => a | b);
                        
                        Snippets.Add(new Snippet { Shortcut = (Keys)shortcut, Text = value});
                    }
                }
                
                var s = Snippets[0];
                Console.WriteLine($"{s.Shortcut} | '{s.Text}'");
            } catch (Exception ex) {
                Console.Error.WriteLine($"Failed to read settings file from path '{SettingsPath}'");
                Console.Error.WriteLine(ex);
            }
        }
        
        if (!File.Exists(SettingsPath)) {
            Save();
        }
    }
    
    public static void Save() {
        try {
            var dir = Path.GetDirectoryName(SettingsPath)!;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            TommySerializer.ToTomlFile([Instance], SettingsPath);
            
            var snippetTable = new TomlTable();
            var snippetTableData = new TomlTable();
            foreach (var snippet in Snippets) {
                // Create human-readable comment
                var keys = new List<Keys>();
                if (snippet.Shortcut.HasFlag(Keys.Application))
                    keys.Add(Keys.Application);
                if (snippet.Shortcut.HasFlag(Keys.Control))
                    keys.Add(Keys.Control);
                if (snippet.Shortcut.HasFlag(Keys.Alt))
                    keys.Add(Keys.Alt);
                if (snippet.Shortcut.HasFlag(Keys.Shift))
                    keys.Add(Keys.Shift);
                keys.Add(snippet.Shortcut & Keys.KeyMask);
                
                var shortcutName = string.Join("+", keys);
                snippetTableData[shortcutName] = new TomlString { Value = snippet.Text };
            }
            snippetTable["Snippets"] = snippetTableData;
            TommySerializer.WriteToDisk(snippetTable, SnippetsPath);
        } catch (Exception ex) {
            Console.Error.WriteLine($"Failed to write settings file to path '{SettingsPath}'");
            Console.Error.WriteLine(ex);
        }
    }
}
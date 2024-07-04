using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;
using CelesteStudio.Editing;
using CelesteStudio.Util;
using Eto.Drawing;
using Eto.Forms;

namespace CelesteStudio.Dialog;

public class SnippetDialog : Dialog<bool> {
    private readonly List<Snippet> snippets;
    
    private SnippetDialog() {
        // Create a copy, to not modify the list in Settings before confirming
        snippets = Settings.Instance.Snippets.Select(snippet => snippet.Clone()).ToList();

        var list = new StackLayout {
            MinimumSize = new Size(500, 0),
            Padding = 10,
            Spacing = 10,
        };
        
        GenerateListEntries(list.Items);
        
        var addButton = new Button { Text = "Add new Snippet" };
        addButton.Click += (_, _) => {
            snippets.Add(new());
            GenerateListEntries(list.Items);
        };
        
        Title = "Edit Snippets";
        Content = new StackLayout {
            Padding = 10,
            Spacing = 10,
            VerticalContentAlignment = VerticalAlignment.Center,
            Items = {
                new StackLayout {
                    Orientation = Orientation.Horizontal,
                    Spacing = 10,
                    Items = { addButton, new  LinkButton { Text = "Open documentation (TODO)" } }
                },
                new Scrollable {
                    Width = list.Width,
                    Height = 500,
                    Content = list,
                }
            }
        };
        Icon = Studio.Instance.Icon;
        
        DefaultButton = new Button((_, _) => Close(true)) { Text = "&OK" };
        AbortButton = new Button((_, _) => Close(false)) { Text = "&Cancel" };
        
        PositiveButtons.Add(DefaultButton);
        NegativeButtons.Add(AbortButton);
        
        Load += (_, _) => Studio.Instance.WindowCreationCallback(this);
    }
    
    private void GenerateListEntries(ICollection<StackLayoutItem> items) {
        items.Clear();
        
        // Hack to unfocus the selected button once a hotkey has been pressed
        var unfocuser = new Button { Visible = false };
        items.Add(unfocuser);
        
        for (int i = 0; i < snippets.Count; i++) {
            var snippet = snippets[i];

            var enabledCheckBox = new CheckBox {Checked = snippet.Enabled};
            enabledCheckBox.CheckedChanged += (_, _) => snippet.Enabled = enabledCheckBox.Checked.Value;
            
            var hotkeyButton = new Button { Text = snippet.Hotkey.ToShortcutString(), ToolTip = "Use the right mouse button to clear a hotkey!", Font = SystemFonts.Bold(), Width = 150};
            hotkeyButton.Click += (_, _) => {
                var inputDialog = new Eto.Forms.Dialog {
                    Content = new Panel {
                        Padding = 10,
                        Content = new Label { Text = "Press a hotkey...", Font = SystemFonts.Bold().WithFontStyle(FontStyle.Italic) }
                    },
                    Icon = Studio.Instance.Icon,
                };
                inputDialog.Load += (_, _) => Studio.Instance.WindowCreationCallback(inputDialog);
                inputDialog.KeyDown += (_, e) => {
                    // Don't allow binding modifiers by themselves
                    if (e.Key is Keys.LeftShift or Keys.RightShift
                        or Keys.LeftControl or Keys.RightControl
                        or Keys.LeftAlt or Keys.RightAlt
                        or Keys.LeftApplication or Keys.RightApplication) {
                        return;
                    }
                    
                    // Check for conflicts
                    if (snippets.Any(other => other.Hotkey == e.KeyData))
                    {
                        var confirm = MessageBox.Show($"Another snippet already uses this hotkey ({e.KeyData.ToShortcutString()}).{Environment.NewLine}Are you sure you to use this hotkey?", MessageBoxButtons.YesNo, MessageBoxType.Question, MessageBoxDefaultButton.Yes);
                        
                        if (confirm != DialogResult.Yes) {
                            return;
                        }
                    }
                    
                    //Application.Instance.Invoke()
                    snippet.Hotkey = e.KeyData;
                    hotkeyButton.Text = snippet.Hotkey.ToShortcutString();
                    
                    inputDialog.Close();
                };
                inputDialog.ShowModal();
            };
            
            var shortcutTextBox = new TextBox { Text = snippet.Shortcut };
            shortcutTextBox.TextChanged += (_, _) => snippet.Shortcut = shortcutTextBox.Text.ReplaceLineEndings(Document.NewLine.ToString());
            
            shortcutTextBox.KeyDown += (_, e) => Console.WriteLine($"Key {e.Key} | {e.Modifiers}");
            shortcutTextBox.TextInput += (_, e) => Console.WriteLine($"Text Input '{e.Text}'");
            
            var textArea = new TextArea {Text = snippet.Insert, Font = FontManager.EditorFontRegular, Width = 500 };
            textArea.TextChanged += (_, _) => snippet.Insert = textArea.Text.ReplaceLineEndings(Document.NewLine.ToString());
            
            int idx = i;
            var upButton = new Button { Text = "\u2bc5", Enabled = i != 0 };
            upButton.Click += (_, _) => { 
                (snippets[idx], snippets[idx - 1]) = (snippets[idx - 1], snippets[idx]); 
                GenerateListEntries(items);
            };
            
            var downButton = new Button { Text = "\u2bc6", Enabled = i != snippets.Count - 1 };
            downButton.Click += (_, _) => {
                (snippets[idx], snippets[idx + 1]) = (snippets[idx + 1], snippets[idx]);
                GenerateListEntries(items);
            };
            
            var deleteButton = new Button { Text = "Delete" };
            deleteButton.Click += (_, _) => {
                if (MessageBox.Show("Are you sure you want to delete this snippet?", MessageBoxButtons.YesNo, MessageBoxType.Question, MessageBoxDefaultButton.Yes) != DialogResult.Yes) {
                    return;
                }
                
                snippets.RemoveAt(idx);
                GenerateListEntries(items);
            };
            
            var layout = new DynamicLayout {DefaultSpacing = new Size(15, 5), Padding = new Padding(0, 0, 0, 10)};
            {
                layout.BeginHorizontal();
                layout.BeginVertical();
                
                layout.BeginHorizontal();
                layout.AddCentered(new Label {Text = "Enabled"});
                layout.Add(enabledCheckBox);
                layout.EndBeginHorizontal();
                layout.AddCentered(new Label {Text = "Hotkey"});
                layout.Add(hotkeyButton, yscale: true);
                layout.EndBeginHorizontal();
                layout.AddCentered(new Label {Text = "Shortcut"});
                layout.Add(shortcutTextBox);
                layout.EndHorizontal();
                
                layout.EndVertical();
                
                layout.Add(textArea);
                
                layout.BeginVertical();
                layout.Add(upButton);
                layout.Add(downButton);
                layout.Add(deleteButton);
                layout.EndVertical();
                
                layout.EndHorizontal();
            }
            
            items.Add(layout);
        }
    }
    
    public static void Show() {
        var dialog = new SnippetDialog();
        if (!dialog.ShowModal())
            return;
        
        Settings.Instance.Snippets = dialog.snippets;
        Settings.OnChanged();
        Settings.Save();
    }
}
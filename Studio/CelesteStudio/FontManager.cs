using Eto;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Eto.Drawing;
using SkiaSharp;
using System;
using System.Diagnostics;

namespace CelesteStudio;

public static class FontManager {
    public const string FontFamilyBuiltin = "<builtin>";
#if MACOS
    public const string FontFamilyBuiltinDisplayName = "Monaco (builtin)";
#else
    public const string FontFamilyBuiltinDisplayName = "JetBrains Mono (builtin)";
#endif

    private static Font? editorFontRegular, editorFontBold, editorFontItalic, editorFontBoldItalic, statusFont, popupFont;
    private static SKFont? skEditorFontRegular, skEditorFontBold, skEditorFontItalic, skEditorFontBoldItalic;

    public static Font EditorFontRegular    => editorFontRegular    ??= CreateEditor(FontStyle.None);
    public static Font EditorFontBold       => editorFontBold       ??= CreateEditor(FontStyle.Bold);
    public static Font EditorFontItalic     => editorFontItalic     ??= CreateEditor(FontStyle.Italic);
    public static Font EditorFontBoldItalic => editorFontBoldItalic ??= CreateEditor(FontStyle.Bold | FontStyle.Italic);
    public static Font StatusFont           => statusFont           ??= CreateStatus();
    public static Font PopupFont            => popupFont            ??= CreatePopup();

    public static SKFont SKEditorFontRegular    => skEditorFontRegular    ??= CreateSKFont(Settings.Instance.FontFamily, Settings.Instance.EditorFontSize * Settings.Instance.FontZoom, FontStyle.None);
    public static SKFont SKEditorFontBold       => skEditorFontBold       ??= CreateSKFont(Settings.Instance.FontFamily, Settings.Instance.EditorFontSize * Settings.Instance.FontZoom, FontStyle.Bold);
    public static SKFont SKEditorFontItalic     => skEditorFontItalic     ??= CreateSKFont(Settings.Instance.FontFamily, Settings.Instance.EditorFontSize * Settings.Instance.FontZoom, FontStyle.Italic);
    public static SKFont SKEditorFontBoldItalic => skEditorFontBoldItalic ??= CreateSKFont(Settings.Instance.FontFamily, Settings.Instance.EditorFontSize * Settings.Instance.FontZoom, FontStyle.Bold | FontStyle.Italic);

    private static FontFamily? builtinFontFamily;
    public static Font CreateFont(string fontFamily, float size, FontStyle style = FontStyle.None) {
        if (Platform.Instance.IsMac && fontFamily == FontFamilyBuiltin) {
            // The built-in font is broken on macOS for some reason, so fallback to a system font
            fontFamily = "Monaco";
        }

        if (fontFamily == FontFamilyBuiltin) {
            var asm = Assembly.GetExecutingAssembly();
            builtinFontFamily ??= FontFamily.FromStreams(asm.GetManifestResourceNames()
                .Where(name => name.StartsWith("JetBrainsMono/"))
                .Select(name => asm.GetManifestResourceStream(name)));

            return new Font(builtinFontFamily, size, style);
        } else {
            return new Font(fontFamily, size, style);
        }
    }

    public static SKFont CreateSKFont(string fontFamily, float size, FontStyle style) {
        // TODO: Don't hardcode this
        const float dpi = 96.0f / 72.0f;

        if (Platform.Instance.IsMac && fontFamily == FontFamilyBuiltin) {
            // The built-in font is broken on macOS for some reason, so fallback to a system font
            fontFamily = "Monaco";
        }

        if (fontFamily == FontFamilyBuiltin) {
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(style switch {
                FontStyle.None => "JetBrainsMono/JetBrainsMono-Regular",
                FontStyle.Bold => "JetBrainsMono/JetBrainsMono-Bold",
                FontStyle.Italic => "JetBrainsMono/JetBrainsMono-Italic",
                FontStyle.Bold | FontStyle.Italic => "JetBrainsMono/JetBrainsMono-BoldItalic",
                _ => throw new UnreachableException(),
            });
            var typeface = SKTypeface.FromStream(stream);

            return new SKFont(typeface, size * dpi) { LinearMetrics = true };
        } else {
            var typeface = style switch {
                FontStyle.None => SKTypeface.FromFamilyName(fontFamily, SKFontStyleWeight.Light, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright),
                FontStyle.Bold => SKTypeface.FromFamilyName(fontFamily, SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright),
                FontStyle.Italic => SKTypeface.FromFamilyName(fontFamily, SKFontStyleWeight.Light, SKFontStyleWidth.Normal, SKFontStyleSlant.Italic),
                FontStyle.Bold | FontStyle.Italic => SKTypeface.FromFamilyName(fontFamily, SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Italic),
                _ => throw new UnreachableException(),
            };

            return new SKFont(typeface, size * dpi) { LinearMetrics = true };
        }
    }

    private static readonly Dictionary<Font, float> charWidthCache = new();
    public static float CharWidth(this Font font) {
        if (charWidthCache.TryGetValue(font, out float width)) {
            return width;
        }

        width = font.MeasureString("X").Width;
        charWidthCache.Add(font, width);
        return width;
    }
    public static float LineHeight(this Font font) {
        if (Eto.Platform.Instance.IsWpf) {
            // WPF reports the line height a bit to small for some reason?
            return font.LineHeight + 5.0f;
        }

        return font.LineHeight;
    }
    public static float MeasureWidth(this Font font, string text, bool measureReal = false) {
        if (measureReal) {
            return string.IsNullOrEmpty(text) ? 0.0f : font.MeasureString(text).Width;
        }

        return font.CharWidth() * text.Length;
    }

    private static readonly Dictionary<SKFont, float> widthCache = [];
    public static float CharWidth(this SKFont font) {
        if (widthCache.TryGetValue(font, out float width)) {
            return width;
        }

        font.MeasureText([font.GetGlyph('X')]);
        //widthCache[font] = width = font.Metrics.AverageCharacterWidth * font.ScaleX;
        widthCache[font] = width = font.MeasureText([font.GetGlyph('X')]);
        return width;
    }
    public static float MeasureWidth(this SKFont font, string text) {
        return font.CharWidth() * text.Length;
    }
    // Apply +/- 1.0f for better visuals
    public static float LineHeight(this SKFont font) {
        return font.Spacing + 0.6f;
    }
    public static float Offset(this SKFont font) {
        return -font.Metrics.Ascent + 0.7f;
    }

    public static void OnFontChanged() {
        // Clear cached fonts
        editorFontRegular?.Dispose();
        editorFontBold?.Dispose();
        editorFontItalic?.Dispose();
        editorFontBoldItalic?.Dispose();
        statusFont?.Dispose();
        popupFont?.Dispose();
        charWidthCache.Clear();

        editorFontRegular = editorFontBold = editorFontItalic = editorFontBoldItalic = statusFont = popupFont = null;

        skEditorFontRegular?.Dispose();

        skEditorFontRegular = null;
    }

    private static Font CreateEditor(FontStyle style) => CreateFont(Settings.Instance.FontFamily, Settings.Instance.EditorFontSize * Settings.Instance.FontZoom, style);
    private static Font CreateStatus() => CreateFont(Settings.Instance.FontFamily, Settings.Instance.StatusFontSize);
    private static Font CreatePopup() => CreateFont(Settings.Instance.FontFamily, Settings.Instance.PopupFontSize);
}

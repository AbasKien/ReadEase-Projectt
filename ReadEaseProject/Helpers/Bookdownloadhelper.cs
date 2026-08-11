using System.Web;

namespace ReadEase.Helpers;

/// <summary>
/// Builds a beautifully formatted, printable HTML file for book downloads.
/// Open in any browser → File → Print → Save as PDF.
/// </summary>
public static class BookDownloadHelper
{
    // ── CSS is stored as a plain const to avoid interpolation brace conflicts ──
    private const string Css = @"
        @import url('https://fonts.googleapis.com/css2?family=Lora:ital,wght@0,400;0,600;1,400&family=Nunito:wght@400;700;800;900&display=swap');

        *,*::before,*::after{box-sizing:border-box;margin:0;padding:0}
        html{font-size:18px;-webkit-print-color-adjust:exact;print-color-adjust:exact;-webkit-text-size-adjust:100%}
        body{
            font-family:'Lora',Georgia,serif;
            background:#fafaf7;
            color:#1a1309;
            max-width:720px;
            margin:0 auto;
            padding:3rem 2rem 6rem;
            line-height:1.9;
            -webkit-font-smoothing:antialiased;
            -moz-osx-font-smoothing:grayscale;
            overflow-x:hidden;
            word-wrap:break-word;
            overflow-wrap:break-word;
        }

        /* Cover */
        .re-cover{
            text-align:center;
            padding:4rem 2.5rem 3rem;
            background:linear-gradient(160deg,#1a1a2e 0%,#16213e 55%,#0f3460 100%);
            border-radius:20px;
            margin-bottom:3.5rem;
            position:relative;
            overflow:hidden;
        }
        .re-cover::before{
            content:'';
            position:absolute;inset:0;
            background:radial-gradient(ellipse at 70% 30%,rgba(242,101,34,.18) 0%,transparent 65%);
        }
        .re-cover-label{
            font-family:'Nunito',sans-serif;
            font-size:.68rem;font-weight:900;
            color:rgba(255,255,255,.4);
            letter-spacing:.16em;text-transform:uppercase;
            margin-bottom:1.25rem;
            position:relative;z-index:1;
        }
        .re-cover-title{
            font-family:'Lora',Georgia,serif;
            font-size:clamp(1.9rem,4vw,3rem);
            font-weight:600;color:#fff;
            line-height:1.15;
            margin-bottom:.85rem;
            position:relative;z-index:1;
            text-shadow:0 2px 20px rgba(0,0,0,.45);
        }
        .re-cover-author{
            font-family:'Nunito',sans-serif;
            font-size:1rem;font-weight:700;
            color:rgba(255,255,255,.55);
            margin-bottom:1.5rem;
            position:relative;z-index:1;
        }
        .re-cover-meta{
            display:inline-flex;align-items:center;gap:.65rem;
            position:relative;z-index:1;
            flex-wrap:wrap;justify-content:center;
        }
        .re-cover-tag{
            font-family:'Nunito',sans-serif;
            font-size:.72rem;font-weight:800;
            padding:.28rem .85rem;border-radius:99px;
            background:rgba(255,255,255,.1);
            color:rgba(255,255,255,.7);
            border:1px solid rgba(255,255,255,.15);
        }
        .re-cover-tag.accent{
            background:rgba(242,101,34,.25);
            color:#ffb085;
            border-color:rgba(242,101,34,.35);
        }
        .re-cover-ornament{
            font-size:.55rem;color:rgba(255,165,60,.6);
            letter-spacing:.35em;
            margin-top:1.75rem;
            position:relative;z-index:1;
        }

        /* Description */
        .re-desc{
            font-family:'Nunito',sans-serif;
            font-size:.92rem;line-height:1.75;
            color:#6b7280;font-style:italic;
            padding:1.25rem 1.5rem;
            border-left:3px solid #f26522;
            background:#fff9f6;
            border-radius:0 12px 12px 0;
            margin-bottom:3rem;
        }

        /* TOC */
        .re-toc{
            background:#fff;
            border:1px solid #ede8e0;
            border-radius:16px;
            padding:1.75rem 2rem;
            margin-bottom:3rem;
            box-shadow:0 2px 12px rgba(0,0,0,.05);
        }
        .re-toc-heading{
            font-family:'Nunito',sans-serif;
            font-size:.68rem;font-weight:900;
            color:#9994a8;text-transform:uppercase;letter-spacing:.1em;
            margin-bottom:1.1rem;
        }
        .re-toc-item{
            display:flex;align-items:baseline;gap:.5rem;
            padding:.55rem 0;
            border-bottom:1px solid #f5f3ef;
            font-family:'Nunito',sans-serif;
            font-size:.88rem;font-weight:600;
            color:#1a1a2e;text-decoration:none;
            -webkit-tap-highlight-color:rgba(242,101,34,.15);
            min-height:44px;
        }
        .re-toc-item:active{background:rgba(242,101,34,.06);border-radius:8px;}
        .re-toc-item:last-child{border-bottom:none;}
        .re-toc-num{font-size:.72rem;font-weight:900;color:#f26522;min-width:36px;flex-shrink:0;}
        .re-toc-dots{flex:1;border-bottom:1.5px dotted #e2ddd8;margin:0 .25rem .28rem;}

        /* Chapter */
        .re-chapter{
            margin-bottom:4.5rem;
            padding-bottom:3rem;
            border-bottom:1px solid #ede8e0;
        }
        .re-chapter:last-child{border-bottom:none;margin-bottom:2rem;}
        .re-chapter-eyebrow{
            font-family:'Nunito',sans-serif;
            font-size:.68rem;font-weight:900;
            color:#f26522;
            letter-spacing:.12em;text-transform:uppercase;
            display:flex;align-items:center;gap:.5rem;
            margin-bottom:.8rem;
        }
        .re-chapter-eyebrow::before{
            content:'';display:block;
            width:24px;height:2px;
            background:#f26522;border-radius:2px;opacity:.6;
        }
        .re-chapter-title{
            font-family:'Lora',Georgia,serif;
            font-size:clamp(1.45rem,3vw,2.1rem);
            font-weight:600;color:#1a140d;
            line-height:1.2;
            margin-bottom:1.75rem;
        }
        .re-divider{
            display:flex;align-items:center;gap:.6rem;
            margin-bottom:1.75rem;opacity:.38;
        }
        .re-divider::before,.re-divider::after{content:'';flex:1;height:1px;background:#b0a898;}
        .re-divider-icon{font-size:.5rem;color:#f26522;letter-spacing:.3em;}

        /* Body text */
        .re-body{font-size:1rem;line-height:2;color:#2b2315;}
        .re-body p{margin-bottom:1.4em;text-indent:1.8em;}
        .re-body p:first-child{text-indent:0;}

        /* Footer */
        .re-footer{
            text-align:center;
            margin-top:4rem;padding-top:2rem;
            border-top:1px solid #ede8e0;
            font-family:'Nunito',sans-serif;
            font-size:.72rem;font-weight:600;
            color:#c0bbb2;
        }

        /* ── Mobile-optimized reading ── */
        @media screen and (max-width:600px){
            html{font-size:16px}
            body{padding:1.5rem 1rem 5rem}
            .re-cover{padding:2.5rem 1.5rem 2rem;border-radius:14px;margin-bottom:2rem}
            .re-cover-title{font-size:1.6rem}
            .re-cover-author{font-size:.88rem}
            .re-desc{padding:1rem 1.1rem;border-radius:0 10px 10px 0;margin-bottom:2rem}
            .re-toc{padding:1.25rem 1.1rem;border-radius:12px;margin-bottom:2rem}
            .re-toc-item{padding:.5rem 0;font-size:.82rem;min-height:40px}
            .re-chapter{margin-bottom:3rem;padding-bottom:2rem}
            .re-chapter-title{font-size:1.35rem;margin-bottom:1.25rem}
            .re-body{line-height:1.85}
            .re-body p{text-indent:1.5em}
        }

        /* ── Dark mode (follows system preference) ── */
        @media (prefers-color-scheme:dark){
            body{background:#0f1922;color:rgba(255,255,255,.88)}
            .re-cover{background:linear-gradient(160deg,#0b1219 0%,#0f1922 55%,#141e28 100%)}
            .re-desc{background:rgba(255,255,255,.04);border-left-color:#f26522;color:rgba(255,255,255,.55)}
            .re-toc{background:rgba(255,255,255,.04);border-color:rgba(255,255,255,.08);box-shadow:none}
            .re-toc-item{color:rgba(255,255,255,.85);border-bottom-color:rgba(255,255,255,.06)}
            .re-toc-dots{border-bottom-color:rgba(255,255,255,.12)}
            .re-chapter{border-bottom-color:rgba(255,255,255,.06)}
            .re-chapter-title{color:#fff}
            .re-body{color:rgba(255,255,255,.82)}
            .re-divider::before,.re-divider::after{background:rgba(255,255,255,.15)}
            .re-footer{border-top-color:rgba(255,255,255,.06);color:rgba(255,255,255,.35)}
        }

        /* Print */
        @media print{
            body{background:#fff;padding:1.5cm 2cm;}
            .re-cover{-webkit-print-color-adjust:exact;print-color-adjust:exact;}
            .re-chapter{page-break-inside:avoid;}
            .re-chapter-title{page-break-after:avoid;}
            .re-toc{page-break-after:always;}
        }
    ";

    public static string BuildBookHtml(
        string title,
        string author,
        string category,
        string? description,
        List<(int Number, string Title, string Content)>? chapters,
        string? legacyContent,
        bool singleChapter = false)
    {
        var sb = new System.Text.StringBuilder();

        var pageTitle = singleChapter && chapters?.Count == 1
            ? $"{EscHtml(title)} \u2014 Chapter {chapters[0].Number}"
            : EscHtml(title);

        // ── HEAD ─────────────────────────────────────────────────────────
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"utf-8\"/>");
        sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"/>");
        sb.AppendLine($"  <title>{pageTitle}</title>");
        sb.AppendLine("  <style>");
        sb.AppendLine(Css);
        sb.AppendLine("  </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        // ── COVER ─────────────────────────────────────────────────────────
        var chapterLabel = singleChapter && chapters?.Count == 1
            ? $"Chapter {chapters[0].Number}"
            : chapters?.Count > 0
                ? $"{chapters.Count} Chapter{(chapters.Count != 1 ? "s" : "")}"
                : "Full Book";

        sb.AppendLine("<div class=\"re-cover\">");
        sb.AppendLine("  <div class=\"re-cover-label\">ReadEase &middot; Digital Library</div>");
        sb.AppendLine($"  <div class=\"re-cover-title\">{EscHtml(title)}</div>");
        sb.AppendLine($"  <div class=\"re-cover-author\">by {EscHtml(author)}</div>");
        sb.AppendLine("  <div class=\"re-cover-meta\">");
        sb.AppendLine($"    <span class=\"re-cover-tag accent\">&#128214; {EscHtml(category)}</span>");
        sb.AppendLine($"    <span class=\"re-cover-tag\">{EscHtml(chapterLabel)}</span>");
        sb.AppendLine("  </div>");
        sb.AppendLine("  <div class=\"re-cover-ornament\">&#10022; &nbsp;&nbsp; &#10022; &nbsp;&nbsp; &#10022;</div>");
        sb.AppendLine("</div>");

        // ── DESCRIPTION ───────────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(description))
        {
            sb.AppendLine($"<div class=\"re-desc\">{EscHtml(description)}</div>");
        }

        // ── TABLE OF CONTENTS ─────────────────────────────────────────────
        if (chapters is { Count: > 1 } && !singleChapter)
        {
            sb.AppendLine("<div class=\"re-toc\">");
            sb.AppendLine("  <div class=\"re-toc-heading\">Table of Contents</div>");
            foreach (var ch in chapters)
            {
                sb.AppendLine($"  <a class=\"re-toc-item\" href=\"#ch-{ch.Number}\">");
                sb.AppendLine($"    <span class=\"re-toc-num\">Ch.{ch.Number}</span>");
                sb.AppendLine($"    {EscHtml(ch.Title)}");
                sb.AppendLine("    <span class=\"re-toc-dots\"></span>");
                sb.AppendLine("  </a>");
            }
            sb.AppendLine("</div>");
        }

        // ── CHAPTERS ──────────────────────────────────────────────────────
        if (chapters is { Count: > 0 })
        {
            foreach (var ch in chapters)
            {
                sb.AppendLine($"<div class=\"re-chapter\" id=\"ch-{ch.Number}\">");
                sb.AppendLine($"  <div class=\"re-chapter-eyebrow\">Chapter {ch.Number}</div>");
                sb.AppendLine($"  <h2 class=\"re-chapter-title\">{EscHtml(ch.Title)}</h2>");
                sb.AppendLine("  <div class=\"re-divider\"><span class=\"re-divider-icon\">&#10022; &nbsp; &#10022; &nbsp; &#10022;</span></div>");
                sb.AppendLine($"  <div class=\"re-body\">{FormatContent(ch.Content)}</div>");
                sb.AppendLine("</div>");
            }
        }
        else if (!string.IsNullOrWhiteSpace(legacyContent))
        {
            sb.AppendLine("<div class=\"re-chapter\">");
            sb.AppendLine("  <div class=\"re-divider\"><span class=\"re-divider-icon\">&#10022; &nbsp; &#10022; &nbsp; &#10022;</span></div>");
            sb.AppendLine($"  <div class=\"re-body\">{FormatContent(legacyContent)}</div>");
            sb.AppendLine("</div>");
        }

        // ── FOOTER ────────────────────────────────────────────────────────
        sb.AppendLine("<div class=\"re-footer\">");
        sb.AppendLine($"  Downloaded from <strong>ReadEase</strong> &middot; {EscHtml(title)} by {EscHtml(author)}<br/>");
        sb.AppendLine("  For personal reading only &middot; Press Ctrl+P to save as PDF");
        sb.AppendLine("</div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static string EscHtml(string? s)
        => HttpUtility.HtmlEncode(s ?? "");

    private static string FormatContent(string? content)
    {
        if (string.IsNullOrWhiteSpace(content)) return "";

        var lines = content
            .Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);

        var paras = new List<string>();
        var current = new System.Text.StringBuilder();

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                if (current.Length > 0)
                {
                    paras.Add(current.ToString().Trim());
                    current.Clear();
                }
            }
            else
            {
                if (current.Length > 0) current.Append(' ');
                current.Append(trimmed);
            }
        }

        if (current.Length > 0)
            paras.Add(current.ToString().Trim());

        if (paras.Count == 0)
            paras.Add(content.Trim());

        return string.Join("", paras.Select(p =>
            $"<p>{HttpUtility.HtmlEncode(p)}</p>"));
    }

    public static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var clean = string.Concat(name.Where(c => !invalid.Contains(c))).Replace(" ", "_");
        return clean.Length > 60 ? clean[..60] : clean;
    }
}
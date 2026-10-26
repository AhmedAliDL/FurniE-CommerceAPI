using App.Application.Common.Interfaces.Services;
using App.Application.Common.Models;
using System.Net;
using System.Text;

namespace App.Infrastructure.Services
{
    public class EmailTemplateBuilder : IEmailTemplateBuilder
    {
        public string Build(EmailContent content)
        {
            var body = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(content.GreetingName))
                body.Append($"<p style=\"margin:0 0 16px;\">Hi {Encode(content.GreetingName)},</p>");

            if (!string.IsNullOrWhiteSpace(content.Intro))
                body.Append($"<p style=\"margin:0 0 16px; line-height:1.6;\">{Encode(content.Intro)}</p>");

            if (content.Details.Count > 0)
            {
                body.Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"margin:0 0 20px; border:1px solid #e7e5e4; border-radius:8px;\">");
                foreach (var (label, value) in content.Details)
                {
                    body.Append("<tr>" +
                        $"<td style=\"padding:10px 16px; background-color:#fafaf9; color:#78716c; font-size:13px; width:38%; border-bottom:1px solid #f5f5f4;\">{Encode(label)}</td>" +
                        $"<td style=\"padding:10px 16px; color:#1c1917; font-size:14px; font-weight:bold; border-bottom:1px solid #f5f5f4;\">{Encode(value)}</td>" +
                        "</tr>");
                }
                body.Append("</table>");
            }

            if (!string.IsNullOrWhiteSpace(content.Highlight))
                body.Append($"<div style=\"margin:0 0 20px; padding:18px; background-color:#fffbeb; border:1px dashed #f59e0b; border-radius:8px; text-align:center; font-size:26px; font-weight:bold; letter-spacing:6px; color:#92400e;\">{Encode(content.Highlight)}</div>");

            if (!string.IsNullOrWhiteSpace(content.Note))
                body.Append($"<div style=\"margin:0 0 20px; padding:14px 16px; background-color:#f8fafc; border-left:4px solid #d97706; border-radius:4px; color:#44403c; line-height:1.6;\">{EncodeWithBreaks(content.Note)}</div>");

            if (!string.IsNullOrWhiteSpace(content.ActionUrl) && !string.IsNullOrWhiteSpace(content.ActionText))
                body.Append("<table role=\"presentation\" align=\"center\" cellpadding=\"0\" cellspacing=\"0\" style=\"margin:4px auto 22px;\"><tr><td style=\"border-radius:8px; background-color:#d97706;\">" +
                    $"<a href=\"{Encode(content.ActionUrl)}\" style=\"display:inline-block; padding:13px 36px; font-family:Arial,sans-serif; font-size:15px; font-weight:bold; color:#ffffff; text-decoration:none;\">{Encode(content.ActionText)}</a>" +
                    "</td></tr></table>");

            if (!string.IsNullOrWhiteSpace(content.Outro))
                body.Append($"<p style=\"margin:0 0 16px; line-height:1.6;\">{Encode(content.Outro)}</p>");

            body.Append("<p style=\"margin:26px 0 0; line-height:1.6;\">Best regards,<br/><strong>The Furni Team</strong></p>");

            var preheader = content.Intro.Length <= 120 ? content.Intro : content.Intro[..120];

            return Template
                .Replace("{{TITLE}}", Encode(content.Title), StringComparison.Ordinal)
                .Replace("{{PREHEADER}}", Encode(preheader), StringComparison.Ordinal)
                .Replace("{{BODY}}", body.ToString(), StringComparison.Ordinal)
                .Replace("{{YEAR}}", DateTime.UtcNow.Year.ToString(), StringComparison.Ordinal);
        }

        private static string Encode(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

        private static string EncodeWithBreaks(string value) =>
            Encode(value).Replace("\r\n", "<br/>", StringComparison.Ordinal).Replace("\n", "<br/>", StringComparison.Ordinal);

        private const string Template = """
            <!DOCTYPE html>
            <html lang="en" xmlns="http://www.w3.org/1999/xhtml">
            <head>
                <meta charset="utf-8" />
                <meta name="viewport" content="width=device-width, initial-scale=1.0" />
                <meta name="x-apple-disable-message-reformatting" />
                <title>{{TITLE}}</title>
            </head>
            <body style="margin:0; padding:0; word-spacing:normal; background-color:#f5f5f4;">
                <div style="display:none; font-size:1px; line-height:1px; max-height:0; max-width:0; opacity:0; overflow:hidden; mso-hide:all;">{{PREHEADER}}</div>
                <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="background-color:#f5f5f4;">
                    <tr>
                        <td align="center" style="padding:36px 12px;">
                            <table role="presentation" width="600" cellpadding="0" cellspacing="0" border="0" style="width:600px; max-width:100%;">
                                <tr>
                                    <td style="background-color:#1c1917; border-radius:14px 14px 0 0; padding:26px 36px;">
                                        <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0">
                                            <tr>
                                                <td align="left">
                                                    <span style="font-family:Georgia,'Times New Roman',serif; font-size:26px; letter-spacing:6px; color:#fafaf9;">Furni</span>
                                                </td>
                                                <td align="right">
                                                    <span style="font-family:Arial,sans-serif; font-size:11px; letter-spacing:2px; text-transform:uppercase; color:#a8a29e;">Furniture &amp; Home</span>
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="height:4px; line-height:4px; font-size:4px; background-color:#d97706;">&nbsp;</td>
                                </tr>
                                <tr>
                                    <td style="background-color:#ffffff; padding:38px 36px 32px;">
                                        <h1 style="margin:0 0 20px; font-family:Georgia,'Times New Roman',serif; font-size:24px; line-height:1.3; color:#1c1917;">{{TITLE}}</h1>
                                        <div style="font-family:Arial,sans-serif; font-size:15px; color:#44403c;">
                                            {{BODY}}
                                        </div>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="background-color:#1c1917; border-radius:0 0 14px 14px; padding:22px 36px; text-align:center;">
                                        <p style="margin:0 0 6px; font-family:Arial,sans-serif; font-size:12px; color:#a8a29e;">&copy; {{YEAR}} Furni. All rights reserved.</p>
                                        <p style="margin:0; font-family:Arial,sans-serif; font-size:11px; color:#78716c;">You are receiving this email because of recent activity on your Furni account.</p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                </table>
            </body>
            </html>
            """;
    }
}

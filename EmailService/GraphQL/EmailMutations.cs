using EmailService.GraphQL.Types;
using EmailService.Services;
using HotChocolate.Authorization;

namespace EmailService.GraphQL;

public class EmailMutations
{
    [Authorize(Policy = "ApiKey")]
    public async Task<EmailResult> SendEmailAsync(
        SendEmailInput input,
        [Service] IEmailService emailService)
    {
        return await emailService.SendEmailAsync(input);
    }

    [Authorize(Policy = "ApiKey")]
    public GenerateHtmlResult GenerateHtml(GenerateHtmlInput input)
    {
        var html = GenerateHtmlTemplate(input);

        return new GenerateHtmlResult
        {
            Success = true,
            Html = html,
            Message = "HTML generated successfully"
        };
    }

    private string GenerateHtmlTemplate(GenerateHtmlInput input)
    {
        return $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{input.Title}</title>
    <style>
        body {{
            margin: 0;
            padding: 0;
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
            background-color: #f4f4f4;
        }}
        .email-container {{
            max-width: 600px;
            margin: 0 auto;
            background-color: #ffffff;
        }}
        .header {{
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            padding: 40px 30px;
            text-align: center;
        }}
        .header h1 {{
            margin: 0;
            color: #ffffff;
            font-size: 28px;
            font-weight: 600;
        }}
        .content {{
            padding: 40px 30px;
        }}
        .content h2 {{
            color: #333333;
            font-size: 24px;
            margin-top: 0;
            margin-bottom: 20px;
        }}
        .content p {{
            color: #666666;
            font-size: 16px;
            line-height: 1.6;
            margin: 0 0 15px 0;
        }}
        .button {{
            display: inline-block;
            padding: 14px 30px;
            margin: 20px 0;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: #ffffff !important;
            text-decoration: none;
            border-radius: 5px;
            font-weight: 600;
            font-size: 16px;
        }}
        .button:hover {{
            opacity: 0.9;
        }}
        .footer {{
            background-color: #f8f9fa;
            padding: 30px;
            text-align: center;
            border-top: 1px solid #e9ecef;
        }}
        .footer p {{
            color: #999999;
            font-size: 14px;
            margin: 5px 0;
        }}
        .divider {{
            height: 1px;
            background-color: #e9ecef;
            margin: 30px 0;
        }}
        @media only screen and (max-width: 600px) {{
            .email-container {{
                width: 100% !important;
            }}
            .header, .content, .footer {{
                padding: 20px !important;
            }}
            .header h1 {{
                font-size: 24px !important;
            }}
            .content h2 {{
                font-size: 20px !important;
            }}
        }}
    </style>
</head>
<body>
    <div class=""email-container"">
        <div class=""header"">
            <h1>{input.Title}</h1>
        </div>
        <div class=""content"">
            <h2>{input.Heading}</h2>
            <p>{input.Message}</p>
            {(string.IsNullOrEmpty(input.ButtonText) || string.IsNullOrEmpty(input.ButtonUrl) ? "" :
            $@"<div style=""text-align: center;"">
                <a href=""{input.ButtonUrl}"" class=""button"">{input.ButtonText}</a>
            </div>")}
            {(string.IsNullOrEmpty(input.AdditionalInfo) ? "" :
            $@"<div class=""divider""></div>
            <p style=""color: #999999; font-size: 14px;"">{input.AdditionalInfo}</p>")}
        </div>
        <div class=""footer"">
            <p>{input.FooterText ?? "Thank you for using our service"}</p>
            <p style=""font-size: 12px; color: #bbbbbb;"">© {DateTime.UtcNow.Year} All rights reserved</p>
        </div>
    </div>
</body>
</html>";
    }
}

using System;

namespace Uniflow.Services
{
    public static class EmailTemplates
    {
        private static string GetBaseTemplate(string title, string content, string buttonText, string buttonLink)
        {
            var year = DateTime.Now.Year;
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
    <title>{title}</title>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #F4F3F1; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: #FFFFFF; padding: 40px; border-radius: 8px; box-shadow: 0 4px 6px rgba(0,0,0,0.1); margin-top: 40px; margin-bottom: 40px; }}
        .header {{ text-align: center; margin-bottom: 30px; }}
        .header h1 {{ color: #3171C6; margin: 0; font-size: 28px; font-weight: 700; }}
        .content {{ color: #4B5563; font-size: 16px; line-height: 1.6; margin-bottom: 30px; }}
        .button-container {{ text-align: center; margin: 30px 0; }}
        .button {{ display: inline-block; padding: 12px 30px; background-color: #3171C6; color: #FFFFFF !important; text-decoration: none; border-radius: 6px; font-weight: 600; font-size: 16px; transition: background-color 0.3s; }}
        .button:hover {{ background-color: #2a5fa8; }}
        .footer {{ text-align: center; color: #4B5563; font-size: 13px; border-top: 1px solid #E5E7EB; padding-top: 20px; margin-top: 40px; }}
        a {{ color: #3171C6; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Uniflow</h1>
        </div>
        <div class=""content"">
            <h2 style=""color: #2D2D2D; margin-top: 0;"">{title}</h2>
            {content}
        </div>
        <div class=""button-container"">
            <a href=""{buttonLink}"" class=""button"">{buttonText}</a>
        </div>
        <div class=""content"" style=""font-size: 14px; color: #4B5563;"">
            <p>Dacă butonul nu funcționează, copiază și lipește următorul link în browser:</p>
            <p><a href=""{buttonLink}"" style=""word-break: break-all;"">{buttonLink}</a></p>
        </div>
        <div class=""footer"">
            <p>&copy; {year} Uniflow Team. Toate drepturile rezervate.</p>
            <p>Acest email a fost trimis automat. Te rugăm să nu răspunzi la acest mesaj.</p>
        </div>
    </div>
</body>
</html>";
        }

        public static string GetConfirmAccountEmail(string link)
        {
            var content = @"
                <p>Salut,</p>
                <p>Îți mulțumim că te-ai înregistrat pe <strong>Uniflow</strong>! Pentru a începe să utilizezi contul, te rugăm să confirmi adresa de email.</p>
                <p>Doar un singur pas te mai desparte de accesul la platforma noastră.</p>";
            
            return GetBaseTemplate("Confirmare Cont", content, "Confirmă Contul", link);
        }

        public static string GetResetPasswordEmail(string link)
        {
            var content = @"
                <p>Salut,</p>
                <p>Am primit o cerere de resetare a parolei pentru contul tău Uniflow.</p>
                <p>Dacă nu ai solicitat acest lucru, poți ignora acest email în siguranță. Parola ta va rămâne neschimbată.</p>
                <p>Pentru a alege o parolă nouă, apasă pe butonul de mai jos:</p>";

            return GetBaseTemplate("Resetare Parolă", content, "Resetează Parola", link);
        }
    }
}

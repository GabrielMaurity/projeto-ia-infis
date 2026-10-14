using System.Globalization;
using System.Net;
using Worker.Suporte.Mensagens;

namespace Worker.Suporte.Email;

/// <summary>
/// E-mail pronto para envio.
/// </summary>
public record EmailSuporte(string Assunto, string Html, string Texto);

/// <summary>
/// Monta o e-mail de novo atendimento para a equipe de suporte, com layout HTML padrão
/// (tabelas e CSS inline, compatível com a maioria dos clientes de e-mail) e versão em texto.
/// </summary>
public static class ModeloEmailSuporte
{
    public static EmailSuporte Montar(MensagemAtendimento mensagem, DateTime dataHoraUtc)
    {
        // Quebras de linha no assunto permitiriam injetar cabeçalhos.
        var usuario = mensagem.NomeUsuario.ReplaceLineEndings(" ").Trim();
        var assunto = $"Novo atendimento solicitado por: {usuario}";
        var data = dataHoraUtc.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture) + " UTC";

        var usuarioHtml = WebUtility.HtmlEncode(usuario);
        var descricaoHtml = WebUtility.HtmlEncode(mensagem.Descricao.Trim()).ReplaceLineEndings("<br>");

        var html = $$"""
            <!DOCTYPE html>
            <html lang="pt-BR">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <title>{{WebUtility.HtmlEncode(assunto)}}</title>
            </head>
            <body style="margin:0;padding:0;background-color:#f3f4f6;font-family:Segoe UI,Arial,Helvetica,sans-serif;color:#1f2937;">
              <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background-color:#f3f4f6;padding:24px 0;">
                <tr>
                  <td align="center">
                    <table role="presentation" width="600" cellpadding="0" cellspacing="0" style="max-width:600px;width:100%;background-color:#ffffff;border-radius:8px;overflow:hidden;border:1px solid #e5e7eb;">
                      <tr>
                        <td style="background-color:#1e3a8a;padding:20px 28px;color:#ffffff;">
                          <div style="font-size:12px;letter-spacing:1px;text-transform:uppercase;opacity:0.85;">INFIS · Suporte</div>
                          <div style="font-size:20px;font-weight:600;margin-top:4px;">Novo atendimento solicitado</div>
                        </td>
                      </tr>
                      <tr>
                        <td style="padding:24px 28px 8px 28px;font-size:15px;line-height:1.5;">
                          O assistente tributário e contábil encaminhou um atendimento para a equipe de suporte humano.
                        </td>
                      </tr>
                      <tr>
                        <td style="padding:8px 28px;">
                          <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="font-size:14px;border-collapse:collapse;">
                            <tr>
                              <td style="padding:8px 12px;background-color:#f9fafb;border:1px solid #e5e7eb;width:140px;font-weight:600;">Usuário</td>
                              <td style="padding:8px 12px;border:1px solid #e5e7eb;">{{usuarioHtml}}</td>
                            </tr>
                            <tr>
                              <td style="padding:8px 12px;background-color:#f9fafb;border:1px solid #e5e7eb;font-weight:600;">Protocolo</td>
                              <td style="padding:8px 12px;border:1px solid #e5e7eb;">#{{mensagem.Id}}</td>
                            </tr>
                            <tr>
                              <td style="padding:8px 12px;background-color:#f9fafb;border:1px solid #e5e7eb;font-weight:600;">Recebido em</td>
                              <td style="padding:8px 12px;border:1px solid #e5e7eb;">{{data}}</td>
                            </tr>
                          </table>
                        </td>
                      </tr>
                      <tr>
                        <td style="padding:16px 28px 8px 28px;font-size:16px;font-weight:600;">Resumo do atendimento</td>
                      </tr>
                      <tr>
                        <td style="padding:0 28px 24px 28px;">
                          <div style="background-color:#f9fafb;border-left:4px solid #1e3a8a;padding:12px 16px;font-size:14px;line-height:1.6;">{{descricaoHtml}}</div>
                        </td>
                      </tr>
                      <tr>
                        <td style="padding:16px 28px;background-color:#f9fafb;border-top:1px solid #e5e7eb;font-size:12px;color:#6b7280;">
                          Mensagem automática enviada pelo Worker.Suporte. Responda ao usuário pelo canal de atendimento.
                        </td>
                      </tr>
                    </table>
                  </td>
                </tr>
              </table>
            </body>
            </html>
            """;

        var texto = $"""
            Novo atendimento solicitado

            Usuário: {usuario}
            Protocolo: #{mensagem.Id}
            Recebido em: {data}

            Resumo do atendimento:
            {mensagem.Descricao.Trim()}
            """;

        return new EmailSuporte(assunto, html, texto);
    }
}

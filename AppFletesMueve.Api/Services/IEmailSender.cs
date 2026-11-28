namespace AppFletesMueve.Api.Services
{
    public interface IEmailSender
    {
        Task<bool> EnviarCodigoRecuperacionAsync(string destinatario, string codigo);
    }
}

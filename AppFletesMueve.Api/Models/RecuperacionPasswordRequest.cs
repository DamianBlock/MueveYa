namespace AppFletesMueve.Api.Models
{
    public class RecuperacionPasswordRequest
    {
        public string Email { get; set; } = string.Empty;
    }

    public class RestablecerPasswordRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
        public string PasswordNueva { get; set; } = string.Empty;
    }
}

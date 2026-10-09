using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppFletesMueve.Api.Models
{
    public class ChatMessage
    {
        [Key]
        public int ChatMessageId { get; set; }

        public int SolicitudFleteId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Sender { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        public DateTime TimestampUtc { get; set; }

        // Read receipt
        public bool IsRead { get; set; }
        public DateTime? ReadTimestampUtc { get; set; }
        public string? ReadBy { get; set; }

        // Navigation omitted to keep model simple
    }
}

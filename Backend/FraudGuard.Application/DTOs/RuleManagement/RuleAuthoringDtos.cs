using System.Collections.Generic;
using FraudGuard.Application.DTOs;

namespace FraudGuard.Application.DTOs.RuleManagement
{
    /// <summary>
    /// Bir ifadeyi kaydetmeden önce derleyip doğrulamak için kullanılır.
    /// </summary>
    public class ValidateExpressionRequest : RequestDTO
    {
        public string Expression { get; set; } = string.Empty;
    }

    public class ValidateExpressionResponse
    {
        public bool IsValid { get; set; }

        /// <summary>Geçersizse derleyicinin döndürdüğü hata.</summary>
        public string? Error { get; set; }
    }

    /// <summary>
    /// Yeni dinamik kural tanımı. İfade doğrulanmadan kural kaydedilmez.
    /// </summary>
    public class CreateFraudRuleRequest : RequestDTO
    {
        /// <summary>Benzersiz kural kodu. Örn: "S13".</summary>
        public string RuleCode { get; set; } = string.Empty;

        public string RuleName { get; set; } = string.Empty;

        public string? Description { get; set; }

        /// <summary>
        /// Tek parametresi <c>input</c> olan boolean ifade.
        /// Örn: <c>input.AyniKartIslemAdedi &gt;= 3</c>
        /// </summary>
        public string Expression { get; set; } = string.Empty;

        /// <summary>Tetiklendiğinde eklenecek ceza puanı.</summary>
        public int Score { get; set; }

        /// <summary>"Card" veya "Merchant".</summary>
        public string Target { get; set; } = "Card";

        /// <summary>"Velocity", "Amount", "Time", "Identity" veya "Location".</summary>
        public string Category { get; set; } = "Velocity";

        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Kesin/yaptırım kuralı mı. true ise puanı güven indiriminden muaf tutulur.
        /// Yalnızca deterministik kurallar için işaretlenmelidir.
        /// </summary>
        public bool IsCritical { get; set; }
    }

    public class CreateFraudRuleResponse
    {
        public int RuleId { get; set; }
        public string RuleCode { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Kuralı silmeden devre dışı bırakmak (veya geri açmak) için kullanılır.
    /// </summary>
    public class SetRuleStatusRequest : RequestDTO
    {
        public bool IsActive { get; set; }
    }

    public class RuleMutationResponse
    {
        public int RuleId { get; set; }
        public string RuleCode { get; set; } = string.Empty;

        /// <summary>İşlem sonrası kuralın aktiflik durumu. Silinen kuralda false.</summary>
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// İfadelerde kullanılabilecek bir alanın tanımı.
    /// </summary>
    public class RuleFieldDto
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Alanın çalışma anında gerçekten doldurulup doldurulmadığı.
        /// false ise bu alanı kullanan kural hiç tetiklenmez.
        /// </summary>
        public bool IsPopulated { get; set; }

        public string? Note { get; set; }
    }
}

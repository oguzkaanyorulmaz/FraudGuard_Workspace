using System;

namespace FraudGuard.Application.DTOs.Merchant
{
    /// <summary>
    /// İşyeri seçim listesinin tek satırı.
    /// </summary>
    public class GetMerchantsResponse
    {
        public string MerchantId { get; set; } = string.Empty;
        public string MerchantName { get; set; } = string.Empty;
        public string MccCode { get; set; } = string.Empty;
        public string MerchantCategory { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public DateTime PosAssignmentDate { get; set; }

        /// <summary>
        /// POS tahsisinden bu yana geçen gün. İstemcinin tarih aritmetiği yapmasını gereksiz kılar
        /// ve kural motorundaki <c>IsyeriYasiGun</c> ile aynı bilgiyi gösterir.
        /// </summary>
        public int PosAgeDays { get; set; }
    }
}

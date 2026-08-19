using System.Collections.Generic;

namespace FraudGuard.Domain.Entities
{
    public class ECreditCard
    {
        public int CardId { get; set; }
        public int CustomerId { get; set; }
        public string CardNumber { get; set; }
        public string ExpiryDate { get; set; }
        public string CVV { get; set; }
        public decimal CardLimit { get; set; }
        public decimal AvailableLimit { get; set; }
        public bool IsBlocked { get; set; } = false;
        
        public int? BlockReasonId { get; set; } 

        public virtual ECustomer Customer { get; set; }
        public virtual EBlockReason? BlockReason { get; set; }
        

        /// <summary>
        /// Kartı bloke eder. Bloke bayrağı ile gerekçe birlikte hareket eder; ikisini ayrı ayrı
        /// set etmek gerekçesiz bloke gibi tutarsız bir duruma yol açabilirdi.
        /// </summary>
        /// <param name="reasonId">
        /// EBlockReason kaydının kimliği. Gerekçenin bilinmediği durumlarda null geçilebilir;
        /// korunan kural gerekçenin varlığı değil, bayrakla birlikte hareket etmesidir.
        /// </param>
        public void Block(int? reasonId)
        {
            IsBlocked = true;
            BlockReasonId = reasonId;
        }

        /// <summary>Blokeyi kaldırır ve gerekçeyi temizler.</summary>
        public void Unblock()
        {
            IsBlocked = false;
            BlockReasonId = null;
        }

        public virtual ICollection<ECreditCardTransaction> Transactions { get; set; }
    }
}
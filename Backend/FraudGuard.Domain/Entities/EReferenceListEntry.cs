namespace FraudGuard.Domain.Entities
{
    /// <summary>
    /// Kural motorunun okuduğu ad-değer listesi. Ülke/şema durdurma, yasaklı MCC gibi
    /// operasyonel listeler koda gömülmek yerine burada tutulur; böylece bir ülkeyi
    /// durdurmak için yeniden derleme gerekmez.
    /// </summary>
    public class EReferenceListEntry
    {
        public int EntryId { get; set; }

        /// <summary>Liste türü. Bkz. <see cref="Common.Constants.ReferenceListTypes"/>.</summary>
        public string ListType { get; set; } = string.Empty;

        /// <summary>Liste değeri. Ülke kodu, şema adı veya MCC.</summary>
        public string Value { get; set; } = string.Empty;

        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
    }
}

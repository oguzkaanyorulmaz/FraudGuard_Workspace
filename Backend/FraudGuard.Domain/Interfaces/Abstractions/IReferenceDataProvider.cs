using System.Threading.Tasks;
using FraudGuard.Domain.DomainObjects.FraudEvaluation;

namespace FraudGuard.Domain.Interfaces.Abstractions
{
    /// <summary>
    /// Kural değerlendirmesinde kullanılan referans verisini (BIN tablosu, operasyonel listeler)
    /// arama için hazır yapılarda sunar.
    /// <para>
    /// Repository yerine ayrı bir soyutlama olmasının sebebi ömür farkıdır: referans verisi
    /// nadiren değişir ama her işlemde okunur. Repository'den her seferinde çekmek, gerçek bir
    /// BIN tablosunda (yüz binlerce satır) her işleme bir veritabanı turu ve sözlük kurulumu
    /// maliyeti bindirirdi.
    /// </para>
    /// </summary>
    public interface IReferenceDataProvider
    {
        Task<ReferenceDataContext> GetAsync();
    }
}

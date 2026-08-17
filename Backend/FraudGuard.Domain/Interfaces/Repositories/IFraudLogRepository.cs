using FraudGuard.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FraudGuard.Domain.Interfaces.Repositories
{
    public interface IFraudLogRepository
    {
        Task AddAsync(EFraudLog log);
        Task<List<EFraudLog>> GetUnresolvedLogsAsync();
        Task<EFraudLog> GetByIdAsync(int logId);
        Task DeleteAsync(int logId);
        Task UpdateAsync(EFraudLog fraudLog);
        Task<EFraudLog> GetLogWithDetailsAsync(int logId);
        Task<List<EFraudLog>> GetResolvedLogsAsync();

        /// <summary>
        /// Bir karta ait, verilen tarihten sonra açılmış fraud alarmı sayısı.
        /// Güven skoru hesabında "son 90 günde temiz geçmiş" faktörü için kullanılır.
        /// </summary>
        Task<int> CountRecentAlarmsForCardAsync(int cardId, bool isCreditCard, DateTime since);
    }
}
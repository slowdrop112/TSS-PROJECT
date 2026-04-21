using Microsoft.EntityFrameworkCore;
using Uniflow.Data;
using Uniflow.Models;

namespace Uniflow.Services
{
    /// <summary>
    /// Serviciu pentru gestionarea voucherelor (reward-uri pentru studenți)
    /// </summary>
    public class VoucherService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<VoucherService> _logger;

        public VoucherService(ApplicationDbContext context, ILogger<VoucherService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Generează un cod unic pentru voucher (format: UNI-XXXX-XXXX)
        /// </summary>
        public string GenerateUniqueCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            string code;
            bool isUnique;

            do
            {
                var part1 = new string(Enumerable.Repeat(chars, 4)
                    .Select(s => s[random.Next(s.Length)]).ToArray());
                var part2 = new string(Enumerable.Repeat(chars, 4)
                    .Select(s => s[random.Next(s.Length)]).ToArray());
                
                code = $"UNI-{part1}-{part2}";
                
                // Verifică unicitatea codului în baza de date
                isUnique = !_context.UserVouchers.Any(v => v.Code == code);
            } while (!isUnique);

            return code;
        }

        /// <summary>
        /// Acordă un voucher unui utilizator
        /// </summary>
        public async Task<UserVoucher?> AwardVoucherAsync(string userId, int voucherId)
        {
            try
            {
                var voucher = await _context.Vouchers.FindAsync(voucherId);
                if (voucher == null || !voucher.IsActive)
                {
                    _logger.LogWarning($"Voucher {voucherId} not found or inactive.");
                    return null;
                }

                // Generează cod unic
                var code = GenerateUniqueCode();

                // Creează UserVoucher
                var userVoucher = new UserVoucher
                {
                    UserId = userId,
                    VoucherId = voucherId,
                    Code = code,
                    AwardedDate = DateTime.Now,
                    ExpiryDate = DateTime.Now.AddDays(voucher.ValidityDays),
                    IsRedeemed = false
                };

                _context.UserVouchers.Add(userVoucher);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Awarded voucher '{voucher.Title}' (Code: {code}) to user {userId}.");
                
                // Load navigation properties
                await _context.Entry(userVoucher).Reference(v => v.Voucher).LoadAsync();
                
                return userVoucher;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error awarding voucher {voucherId} to user {userId}.");
                return null;
            }
        }

        /// <summary>
        /// Obține toate voucherele unui utilizator (incluzând expirate și folosite)
        /// </summary>
        public async Task<List<UserVoucher>> GetUserVouchersAsync(string userId)
        {
            return await _context.UserVouchers
                .Include(uv => uv.Voucher)
                .Where(uv => uv.UserId == userId)
                .OrderByDescending(uv => uv.AwardedDate)
                .ToListAsync();
        }

        /// <summary>
        /// Obține doar voucherele active (neexpirate și nefolosite)
        /// </summary>
        public async Task<List<UserVoucher>> GetActiveVouchersAsync(string userId)
        {
            var now = DateTime.Now;
            return await _context.UserVouchers
                .Include(uv => uv.Voucher)
                .Where(uv => uv.UserId == userId && 
                            !uv.IsRedeemed && 
                            uv.ExpiryDate > now)
                .OrderBy(uv => uv.ExpiryDate)
                .ToListAsync();
        }

        /// <summary>
        /// Marchează un voucher ca fiind folosit
        /// </summary>
        public async Task<bool> MarkAsRedeemedAsync(int userVoucherId)
        {
            try
            {
                var userVoucher = await _context.UserVouchers.FindAsync(userVoucherId);
                if (userVoucher == null)
                {
                    _logger.LogWarning($"UserVoucher {userVoucherId} not found.");
                    return false;
                }

                userVoucher.IsRedeemed = true;
                userVoucher.RedeemedDate = DateTime.Now;
                
                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"Marked voucher {userVoucherId} as redeemed.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error marking voucher {userVoucherId} as redeemed.");
                return false;
            }
        }

        /// <summary>
        /// Obține voucher-ul disponibil pentru un nivel specific
        /// </summary>
        public async Task<Voucher?> GetAvailableVoucherForLevelAsync(int level)
        {
            return await _context.Vouchers
                .Where(v => v.IsActive && v.RequiredLevel == level)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Verifică dacă utilizatorul a primit deja voucher pentru un nivel
        /// </summary>
        public async Task<bool> HasReceivedVoucherForLevelAsync(string userId, int level)
        {
            var voucher = await GetAvailableVoucherForLevelAsync(level);
            if (voucher == null) return true; // Nu există voucher pentru acest nivel

            return await _context.UserVouchers
                .AnyAsync(uv => uv.UserId == userId && uv.VoucherId == voucher.Id);
        }
    }
}

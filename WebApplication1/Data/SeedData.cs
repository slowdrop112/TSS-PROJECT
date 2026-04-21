using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Uniflow.Data; 

namespace Uniflow
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>(); // Get logger

            // 1. Crearea Rolurilor (Admin, Profesor, Student)
            string[] roleNames = { "Admin", "Profesor", "Student" };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            await EnsureUserAsync(userManager, context, logger, "admin@uniflow.com", "Admin123!", "Admin", "Admin", "User");

            // 2. Seed Vouchers
            await SeedVouchersAsync(context, logger);
        }

        private static async Task SeedVouchersAsync(ApplicationDbContext context, ILogger logger)
        {
            if (!await context.Vouchers.AnyAsync())
            {
                var vouchers = new[]
                {
                    new Uniflow.Models.Voucher
                    {
                        Title = "Reducere 10% la Starbucks",
                        Description = "Primești 10% reducere la orice comandă de la Starbucks. Valabil la toate locațiile din oraș.",
                        PartnerName = "Starbucks",
                        DiscountType = "Percentage",
                        DiscountValue = "10%",
                        RequiredLevel = 5,
                        ValidityDays = 30,
                        IconUrl = "☕",
                        IsActive = true
                    },
                    new Uniflow.Models.Voucher
                    {
                        Title = "Voucher 15 lei la Librăria Humanitas",
                        Description = "Reducere de 15 lei la orice achiziție de minimum 50 lei de la Librăria Humanitas.",
                        PartnerName = "Librăria Humanitas",
                        DiscountType = "FixedAmount",
                        DiscountValue = "15 lei",
                        RequiredLevel = 10,
                        ValidityDays = 45,
                        IconUrl = "📚",
                        IsActive = true
                    },
                    new Uniflow.Models.Voucher
                    {
                        Title = "Reducere 20% la Cinema City",
                        Description = "Primești 20% reducere la biletele de cinema. Valabil pentru orice film, în orice zi a săptămânii.",
                        PartnerName = "Cinema City",
                        DiscountType = "Percentage",
                        DiscountValue = "20%",
                        RequiredLevel = 15,
                        ValidityDays = 60,
                        IconUrl = "🎬",
                        IsActive = true
                    },
                    new Uniflow.Models.Voucher
                    {
                        Title = "Voucher 25 lei la Fitness Club",
                        Description = "Reducere de 25 lei la abonamentul lunar la orice sală de fitness partenera.",
                        PartnerName = "Fitness Club Network",
                        DiscountType = "FixedAmount",
                        DiscountValue = "25 lei",
                        RequiredLevel = 20,
                        ValidityDays = 30,
                        IconUrl = "💪",
                        IsActive = true
                    },
                    new Uniflow.Models.Voucher
                    {
                        Title = "Reducere 30% la Restaurant Il Calcio",
                        Description = "Primești 30% reducere la masa ta la restaurantul italian Il Calcio. Valabil pentru orice comandă.",
                        PartnerName = "Il Calcio Restaurant",
                        DiscountType = "Percentage",
                        DiscountValue = "30%",
                        RequiredLevel = 25,
                        ValidityDays = 45,
                        IconUrl = "🍝",
                        IsActive = true
                    },
                    new Uniflow.Models.Voucher
                    {
                        Title = "MacBook Air - Reducere 50 lei",
                        Description = "Reducere specială de 50 lei la achiziționarea unui MacBook Air de la Apple Store.",
                        PartnerName = "Apple Store",
                        DiscountType = "FixedAmount",
                        DiscountValue = "50 lei",
                        RequiredLevel = 30,
                        ValidityDays = 90,
                        IconUrl = "💻",
                        IsActive = true
                    }
                };

                context.Vouchers.AddRange(vouchers);
                await context.SaveChangesAsync();
                logger.LogInformation($"Seeded {vouchers.Length} vouchers successfully.");
            }
        }

        private static async Task EnsureUserAsync(
            UserManager<IdentityUser> userManager, 
            ApplicationDbContext context,
            ILogger logger,
            string email, 
            string password, 
            string role,
            string firstName,
            string lastName,
            int xp = 0)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new IdentityUser 
                { 
                    UserName = email, 
                    Email = email, 
                    EmailConfirmed = true 
                };
                
                var result = await userManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    logger.LogInformation($"Created user {email} successfully.");

                    if (!await userManager.IsInRoleAsync(user, role))
                    {
                        var roleResult = await userManager.AddToRoleAsync(user, role);
                        if (!roleResult.Succeeded)
                        {
                            logger.LogError($"Failed to add user {email} to role {role}: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
                        }
                    }
                    
                    // Creare UserProfile
                    var profile = await context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
                    if (profile == null)
                    {
                        context.UserProfiles.Add(new Uniflow.Models.UserProfile
                        {
                            UserId = user.Id,
                            FirstName = firstName,
                            LastName = lastName,
                            XP = xp
                        });
                        await context.SaveChangesAsync();
                        logger.LogInformation($"Created profile for {email}.");
                    }
                }
                else
                {
                    logger.LogError($"Failed to create user {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
            else
            {
                logger.LogInformation($"User {email} already exists.");
                // Ensure profile exists even if user exists
                var profile = await context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
                if (profile == null)
                {
                    context.UserProfiles.Add(new Uniflow.Models.UserProfile
                    {
                        UserId = user.Id,
                        FirstName = firstName,
                        LastName = lastName,
                        XP = xp
                    });
                    await context.SaveChangesAsync();
                    logger.LogInformation($"Created missing profile for existing user {email}.");
                }
            }
        }
    }
}

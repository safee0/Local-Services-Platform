using LocalServicesPlatform.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LocalServicesPlatform.Services
{
    public class PlatformService
    {
        private readonly LocalServicesDbContext _context;

        public PlatformService(LocalServicesDbContext context)
        {
            _context = context;
        }

        // --- AUTHENTICATION ---

        // Check login credentials against the database
        public async Task<User?> VerifyUserAsync(string email, string password)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email && u.PasswordHash == password);
        }

        // Register User and auto-sync Provider table if necessary
        public async Task<string> RegisterUserAsync(User user)
        {
            try
            {
                // 1. Check if email already exists
                var existingUser = await _context.Users
                    .AnyAsync(u => u.Email.ToLower() == user.Email.ToLower());

                if (existingUser)
                {
                    return "EmailAlreadyExists";
                }

                // 2. Setup new user
                user.CreatedAt = DateTime.Now;
                user.IsActive = true;
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // 3. Handle Provider role sync
                if (user.Role == "Provider")
                {
                    var providerEntry = new Provider
                    {
                        UserId = user.UserId,
                        AvailabilityStatus = true,
                        CreatedAt = DateTime.Now
                    };
                    _context.Providers.Add(providerEntry);
                    await _context.SaveChangesAsync();
                }

                return "Success";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return "DatabaseError";
            }
        }

        // --- USER MANAGEMENT ---

        // Fetch all registered users across the platform
        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _context.Users.ToListAsync();
        }

        // --- SERVICE MANAGEMENT ---

        // Fetch all categories for the dropdown selectors
        public async Task<List<ServiceCategory>> GetCategoriesAsync()
        {
            return await _context.ServiceCategories.ToListAsync();
        }

        // Fetch all services with full details (Category + Provider + User Name)
        public async Task<List<Service>> GetAllServicesAsync()
        {
            var services = await _context.Services
                .Include(s => s.Provider)
                    .ThenInclude(p => p!.User)
                .Include(s => s.Category)
                .ToListAsync();

            return services ?? new List<Service>();
        }

        // Fetch a single service for details page
        public async Task<Service?> GetServiceByIdAsync(int id)
        {
            return await _context.Services
                .Include(s => s.Provider)
                    .ThenInclude(p => p!.User)
                .Include(s => s.Category)
                .FirstOrDefaultAsync(s => s.ServiceId == id);
        }

        // Add a completely new service posted by a provider
        public async Task<bool> AddServiceAsync(Service service)
        {
            try
            {
                _context.Services.Add(service);
                int entriesUpdated = await _context.SaveChangesAsync();
                return entriesUpdated > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXECUTION FAILURE IN ADDSERVICE: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"INNER EXCEPTION: {ex.InnerException.Message}");
                }
                return false;
            }
        }

        // Helper: Find a Provider's PK using their Session User ID
        public async Task<int?> GetProviderIdByUserIdAsync(int userId)
        {
            var provider = await _context.Providers
                .FirstOrDefaultAsync(p => p.UserId == userId);

            return provider?.ProviderId;
        }

        // --- BOOKING OPERATIONS ---

        // Instantly save an application booking directly into the dbo.Bookings data grid layer
        public async Task<bool> CreateBookingAsync(Booking booking)
        {
            try
            {
                _context.Bookings.Add(booking);
                int savedEntries = await _context.SaveChangesAsync();
                return savedEntries > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXECUTION FAILURE IN CREATEBOOKING: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"INNER DB EXCEPTION: {ex.InnerException.Message}");
                }
                return false;
            }
        }

        // Fetch customized bookings array matching a specific user identification token
        public async Task<List<Booking>> GetBookingsByCustomerIdAsync(int customerId)
        {
            return await _context.Bookings
                .Include(b => b.Service)
                .Where(b => b.CustomerId == customerId)
                .ToListAsync();
        }

        // FIXED METHOD: Safely links through the related Service table without referencing a missing ProviderId field
        public async Task<List<Booking>> GetBookingsByProviderAsync(int userId)
        {
            try
            {
                var provider = await _context.Providers.FirstOrDefaultAsync(p => p.UserId == userId);
                if (provider == null) return new List<Booking>();

                return await _context.Bookings
                    .Include(b => b.Customer)
                    .Include(b => b.Service)
                    .Where(b => b.Service != null && b.Service.ProviderId == provider.ProviderId)
                    .OrderByDescending(b => b.BookingDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error gathering provider bookings: {ex.Message}");
                return new List<Booking>();
            }
        }

        // --- ACCOUNT DELETION WORKFLOWS ---

        // Completely delete a Customer account with a cascading fallback structure
        public async Task<bool> DeleteCustomerAccountAsync(int userId)
        {
            try
            {
                // 1. Fetch the user profile first to verify existence
                var user = await _context.Users.FindAsync(userId);
                if (user == null) return false;

                // 2. Clear out any connected Bookings and their dependent data
                var customerBookings = await _context.Bookings
                    .Where(b => b.CustomerId == userId)
                    .ToListAsync();

                if (customerBookings.Any())
                {
                    var bookingIds = customerBookings.Select(b => b.BookingId).ToList();

                    // Clean up payments related to these bookings
                    var payments = await _context.Payments.Where(p => bookingIds.Contains(p.BookingId)).ToListAsync();
                    _context.Payments.RemoveRange(payments);

                    // Clean up reviews related to these bookings
                    var reviews = await _context.Reviews.Where(r => bookingIds.Contains(r.BookingId)).ToListAsync();
                    _context.Reviews.RemoveRange(reviews);

                    _context.Bookings.RemoveRange(customerBookings);
                }

                // 3. Foolproof Favorites wipe
                try
                {
                    var favorites = await _context.Favorites.ToListAsync();
                    var userFavorites = favorites.Where(f =>
                        f.GetType().GetProperty("CustomerId")?.GetValue(f)?.ToString() == userId.ToString() ||
                        f.GetType().GetProperty("UserId")?.GetValue(f)?.ToString() == userId.ToString()
                    ).ToList();

                    if (userFavorites.Any())
                    {
                        _context.Favorites.RemoveRange(userFavorites);
                    }
                }
                catch (Exception favEx)
                {
                    Console.WriteLine($"Handled internal favorites exception: {favEx.Message}");
                }

                // 4. Final step: Purge the core user record
                _context.Users.Remove(user);

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CRITICAL ERROR deleting customer account: {ex.Message}");
                return false;
            }
        }

        // Completely delete a Provider account, their posted services, and related jobs
        public async Task<bool> DeleteProviderAccountAsync(int userId)
        {
            try
            {
                // 1. Get the Provider ID
                var provider = await _context.Providers.FirstOrDefaultAsync(p => p.UserId == userId);
                if (provider != null)
                {
                    // 2. Find all services posted by this provider
                    var providerServices = await _context.Services.Where(s => s.ProviderId == provider.ProviderId).ToListAsync();
                    if (providerServices.Any())
                    {
                        var serviceIds = providerServices.Select(s => s.ServiceId).ToList();

                        // Delete related bookings, images, and favorites tied to these services
                        var bookings = await _context.Bookings.Where(b => serviceIds.Contains(b.ServiceId)).ToListAsync();
                        if (bookings.Any())
                        {
                            var bookingIds = bookings.Select(b => b.BookingId).ToList();
                            var payments = await _context.Payments.Where(p => bookingIds.Contains(p.BookingId)).ToListAsync();
                            _context.Payments.RemoveRange(payments);
                            var reviews = await _context.Reviews.Where(r => bookingIds.Contains(r.BookingId)).ToListAsync();
                            _context.Reviews.RemoveRange(reviews);
                            _context.Bookings.RemoveRange(bookings);
                        }

                        var images = await _context.ServiceImages.Where(i => serviceIds.Contains(i.ServiceId)).ToListAsync();
                        _context.ServiceImages.RemoveRange(images);

                        var favorites = await _context.Favorites.Where(f => serviceIds.Contains(f.ServiceId)).ToListAsync();
                        _context.Favorites.RemoveRange(favorites);

                        _context.Services.RemoveRange(providerServices);
                    }

                    _context.Providers.Remove(provider);
                }

                // 3. Delete core user record
                var user = await _context.Users.FindAsync(userId);
                if (user != null) _context.Users.Remove(user);

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting provider account: {ex.Message}");
                return false;
            }
        }
    }
}
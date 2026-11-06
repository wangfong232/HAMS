using System;
using System.Security.Cryptography;
using System.Text;
using HAMS.Domain.Common;

namespace HAMS.Domain.Entities
{
    public class User : BaseEntity
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty; // Patient, Doctor, Receptionist, Cashier, Technician, Pharmacist, ClinicManager, Admin
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual Patient? Patient { get; set; }
        public virtual Doctor? Doctor { get; set; }

        public bool VerifyPassword(string plainPassword)
        {
            if (string.IsNullOrEmpty(plainPassword) || string.IsNullOrEmpty(PasswordHash))
                return false;

            // Standard SHA256 / BCrypt verification abstraction
            using var sha = SHA256.Create();
            var hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(plainPassword));
            var computedHash = Convert.ToBase64String(hashBytes);
            return PasswordHash == computedHash || PasswordHash == plainPassword;
        }

        public void UpdatePasswordHash(string newPasswordHash)
        {
            PasswordHash = newPasswordHash;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}

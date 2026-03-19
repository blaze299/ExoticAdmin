using System;

namespace ExoticAdmin
{
    public class Vehicle
    {
        public int Id { get; set; }
        public string Category { get; set; }
        public string Brand { get; set; }
        public string Model { get; set; }
        public string ExteriorColor { get; set; }
        public string Interior { get; set; }
        public int Year { get; set; }
        public int Weight { get; set; }
        public int Doors { get; set; }
        public string WheelStyle { get; set; }
        public string Powertrain { get; set; }
        public string Transmission { get; set; }
        public int Hp { get; set; }
        public int Torque { get; set; }
        public string Acceleration { get; set; }
        public int TopSpeed { get; set; }
        public string Extras { get; set; }
        public string Drive { get; set; }
        public decimal PricePerDay { get; set; }
        public string Description { get; set; }
        public int TimesRented { get; set; }
        public int Status { get; set; }

        public string StatusName => Status == 1 ? "Elérhető" : Status == 2 ? "Kiadva" : "Szervizben";
        public string StatusColor => Status == 1 ? "#4CAF50" : Status == 2 ? "#FFC107" : "#F44336";
        public string DisplayName => $"{Brand} {Model} ({Year})";
    }

    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string LicenseNumber { get; set; }
        public int Clearance { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public DateTime? LicenseExpiryDate { get; set; }
        public bool IsVerified { get; set; }

        public string RoleName => Clearance == 3 ? "Admin" : Clearance == 2 ? "Sofőr" : "Felhasználó";

        // --- ÚJ UI SEGÉDMEZŐK A HITELESÍTÉSHEZ ---
        public string VerifiedText => IsVerified ? "✔ Hitelesítve" : "✖ Nincs hitelesítve";
        public string VerifiedColor => IsVerified ? "#4CAF50" : "#F44336"; // Zöld vagy Piros
    }
}

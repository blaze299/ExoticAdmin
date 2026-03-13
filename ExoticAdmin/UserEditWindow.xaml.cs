using MySql.Data.MySqlClient;
using BCrypt.Net;
using System;
using System.Linq;
using System.Windows;

namespace ExoticAdmin
{
    public partial class UserEditWindow : Window
    {
        private User _user;
        private string connectionString = "Server=127.0.0.1;Database=exotic_rentals;Uid=root;Pwd=;";

        public UserEditWindow(User user)
        {
            InitializeComponent();
            _user = user;

            // BETÖLTÉS EGY LÉTEZŐ FELHASZNÁLÓNÁL
            if (_user.Id != 0)
            {
                txtUsername.Text = _user.Username;
                txtFullName.Text = _user.FullName;
                txtEmail.Text = _user.Email;
                txtPhone.Text = _user.PhoneNumber;
                txtClearance.Text = _user.Clearance.ToString();
                txtLicense.Text = _user.LicenseNumber;

                if (_user.DateOfBirth.HasValue) txtDob.Text = _user.DateOfBirth.Value.ToString("yyyy-MM-dd");
                if (_user.LicenseExpiryDate.HasValue) txtLicenseExpiry.Text = _user.LicenseExpiryDate.Value.ToString("yyyy-MM-dd");

                chkIsVerified.IsChecked = _user.IsVerified;

                // --- ÚJ RÉSZ: Feketelista betöltése ---
                chkIsBanned.IsChecked = _user.IsBanned;
                txtAdminNotes.Text = _user.AdminNotes;
            }
            else
            {
                txtClearance.Text = "1";
            }
        }

        private void BtnGeneratePassword_Click(object sender, RoutedEventArgs e)
        {
            if (_user.Id == 0)
            {
                MessageBox.Show("Előbb mentsd el az új felhasználót, és utána generálj neki új jelszót!", "Figyelmeztetés", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$*";
                Random random = new Random();
                string newPassword = new string(Enumerable.Repeat(chars, 10).Select(s => s[random.Next(s.Length)]).ToArray());

                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(newPassword);

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    new MySqlCommand($"UPDATE users SET password = '{hashedPassword}' WHERE id = {_user.Id}", conn).ExecuteNonQuery();
                }

                MessageBox.Show($"Az új jelszó sikeresen beállítva az adatbázisban!\n\nJelszó: {newPassword}\n\nKérlek, másold ki és juttasd el a felhasználónak!", "Sikeres jelszó generálás", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Hiba a jelszó generálása során: " + ex.Message, "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query;

                    // MENTÉS - ÚJ FELHASZNÁLÓ (Kibővítve a banned és notes mezőkkel)
                    if (_user.Id == 0)
                    {
                        query = @"INSERT INTO users 
                                (username, full_name, email, phoneNumber, clearance, license_number, date_of_birth, license_expiry_date, is_verified, password, is_banned, admin_notes) 
                                VALUES 
                                (@user, @name, @email, @phone, @clearance, @license, @dob, @expiry, @verified, '$2a$12$K7O8DqM2f8L2/VzQ4W5E.OeB8jG9h1i2k3l4m5n6o7p8q9r0s1t2u', @banned, @notes)";
                    }
                    // MENTÉS - MEGLÉVŐ FELHASZNÁLÓ FRISSÍTÉSE (Kibővítve a banned és notes mezőkkel)
                    else
                    {
                        query = @"UPDATE users 
                                SET username=@user, full_name=@name, email=@email, phoneNumber=@phone, clearance=@clearance, 
                                license_number=@license, date_of_birth=@dob, license_expiry_date=@expiry, is_verified=@verified,
                                is_banned=@banned, admin_notes=@notes 
                                WHERE id=@id";
                    }

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@user", txtUsername.Text);
                        cmd.Parameters.AddWithValue("@name", txtFullName.Text);
                        cmd.Parameters.AddWithValue("@email", txtEmail.Text);
                        cmd.Parameters.AddWithValue("@phone", txtPhone.Text);
                        cmd.Parameters.AddWithValue("@license", txtLicense.Text);

                        cmd.Parameters.AddWithValue("@clearance", int.TryParse(txtClearance.Text, out int c) ? c : 1);
                        cmd.Parameters.AddWithValue("@verified", chkIsVerified.IsChecked == true ? 1 : 0);

                        // --- ÚJ RÉSZ: Feketelista paraméterek ---
                        cmd.Parameters.AddWithValue("@banned", chkIsBanned.IsChecked == true ? 1 : 0);
                        cmd.Parameters.AddWithValue("@notes", txtAdminNotes.Text);

                        if (DateTime.TryParse(txtDob.Text, out DateTime dob)) cmd.Parameters.AddWithValue("@dob", dob);
                        else cmd.Parameters.AddWithValue("@dob", DBNull.Value);

                        if (DateTime.TryParse(txtLicenseExpiry.Text, out DateTime expiry)) cmd.Parameters.AddWithValue("@expiry", expiry);
                        else cmd.Parameters.AddWithValue("@expiry", DBNull.Value);

                        if (_user.Id != 0) cmd.Parameters.AddWithValue("@id", _user.Id);

                        cmd.ExecuteNonQuery();
                    }
                }

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Hiba a mentés során (ellenőrizd, hogy a felhasználónév/email nem foglalt-e): \n" + ex.Message, "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
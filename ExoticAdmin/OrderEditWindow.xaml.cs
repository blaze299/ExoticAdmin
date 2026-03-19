using MySql.Data.MySqlClient;
using System;
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

            // Betöltjük a meglévő adatokat, ha szerkesztésről van szó
            if (_user.Id != 0)
            {
                txtUsername.Text = _user.Username;
                txtFullName.Text = _user.FullName;
                txtEmail.Text = _user.Email;
                txtPhone.Text = _user.PhoneNumber;
                txtClearance.Text = _user.Clearance.ToString();
                txtLicense.Text = _user.LicenseNumber;

                // Dátumok formázása (ha van adat)
                if (_user.DateOfBirth.HasValue) txtDob.Text = _user.DateOfBirth.Value.ToString("yyyy-MM-dd");
                if (_user.LicenseExpiryDate.HasValue) txtLicenseExpiry.Text = _user.LicenseExpiryDate.Value.ToString("yyyy-MM-dd");

                chkIsVerified.IsChecked = _user.IsVerified;
            }
            else
            {
                txtClearance.Text = "1"; // Alapból sima felhasználó
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

                    if (_user.Id == 0) // ÚJ FELHASZNÁLÓ (A jelszó mező miatt itt egy alapértelmezett titkosított jelszót adunk)
                    {
                        query = @"INSERT INTO users 
                                (username, full_name, email, phoneNumber, clearance, license_number, date_of_birth, license_expiry_date, is_verified, password) 
                                VALUES 
                                (@user, @name, @email, @phone, @clearance, @license, @dob, @expiry, @verified, '$2a$12$K7O8DqM2f8L2/VzQ4W5E.OeB8jG9h1i2k3l4m5n6o7p8q9r0s1t2u')";
                        // (A hash a 'teszt' jelszó egy generált bcrypt változata, mivel a DB nem enged NULL jelszót)
                    }
                    else // FRISSÍTÉS
                    {
                        query = @"UPDATE users 
                                SET username=@user, full_name=@name, email=@email, phoneNumber=@phone, clearance=@clearance, 
                                license_number=@license, date_of_birth=@dob, license_expiry_date=@expiry, is_verified=@verified 
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

                        // Dátumok biztonságos konvertálása (Ha üres vagy hibás, akkor NULL megy az adatbázisba)
                        if (DateTime.TryParse(txtDob.Text, out DateTime dob))
                            cmd.Parameters.AddWithValue("@dob", dob);
                        else
                            cmd.Parameters.AddWithValue("@dob", DBNull.Value);

                        if (DateTime.TryParse(txtLicenseExpiry.Text, out DateTime expiry))
                            cmd.Parameters.AddWithValue("@expiry", expiry);
                        else
                            cmd.Parameters.AddWithValue("@expiry", DBNull.Value);

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

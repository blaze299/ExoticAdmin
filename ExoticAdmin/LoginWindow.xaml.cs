using MySql.Data.MySqlClient;
using BCrypt.Net;
using System;
using System.Windows;
using System.Windows.Input;

namespace ExoticAdmin
{
    public partial class LoginWindow : Window
    {
        private string connectionString = "Server=127.0.0.1;Database=exotic_rentals;Uid=root;Pwd=;";

        public LoginWindow()
        {
            InitializeComponent();

            // Ablak mozgatása bal egérgombbal
            this.MouseLeftButtonDown += (s, e) => this.DragMove();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string email = txtEmail.Text.Trim();
            string password = txtPassword.Password;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Kérlek add meg az emailed és a jelszavad!", "Hiányzó adatok", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    // JAVÍTVA: Lekérjük a username és full_name oszlopokat is!
                    string query = "SELECT username, full_name, password, clearance FROM users WHERE email = @email LIMIT 1";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@email", email);

                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string dbPasswordHash = reader.GetString("password");
                                int clearance = reader.GetInt32("clearance");

                                // ÚJ: Név kiolvasása
                                string username = reader.GetString("username");
                                string fullName = reader.IsDBNull(reader.GetOrdinal("full_name")) ? "" : reader.GetString("full_name");

                                // Ha van megadva teljes név (Full Name), akkor azt használjuk, különben a felhasználónevet
                                string displayName = string.IsNullOrWhiteSpace(fullName) ? username : fullName;

                                // 1. Jelszó ellenőrzése
                                bool isPasswordValid = BCrypt.Net.BCrypt.Verify(password, dbPasswordHash);

                                if (!isPasswordValid)
                                {
                                    MessageBox.Show("Hibás jelszó vagy email cím!", "Bejelentkezési hiba", MessageBoxButton.OK, MessageBoxImage.Error);
                                    return;
                                }

                                // 2. Jogosultság ellenőrzése
                                if (clearance != 3)
                                {
                                    MessageBox.Show("Nem megfelelő a jogosultságod.\nEz a felület kizárólag adminisztrátoroknak elérhető.", "Jogosultsági hiba", MessageBoxButton.OK, MessageBoxImage.Error);
                                    return;
                                }

                                // 3. Sikeres belépés, átadjuk a nevet a MainWindow-nak!
                                MainWindow mainWindow = new MainWindow(displayName);
                                mainWindow.Show();
                                this.Close();
                            }
                            else
                            {
                                MessageBox.Show("Nem található felhasználó ezzel az email címmel.", "Bejelentkezési hiba", MessageBoxButton.OK, MessageBoxImage.Warning);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Hiba az adatbázis kapcsolatban: " + ex.Message, "Rendszerhiba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
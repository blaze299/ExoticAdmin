using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ExoticAdmin
{
    public partial class MainWindow : Window
    {
        private string connectionString = "Server=127.0.0.1;Database=exotic_rentals;Uid=root;Pwd=;";

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += (s, e) => LoadVehicles();
        }

        // --- MENÜ NAVIGÁCIÓ ---
        private void BtnMenuVehicles_Click(object sender, RoutedEventArgs e)
        {
            SwitchMenuAppearance(btnMenuVehicles);
            pnlVehicles.Visibility = Visibility.Visible;
            pnlUsers.Visibility = Visibility.Hidden;
            pnlDrivers.Visibility = Visibility.Hidden;
            LoadVehicles();
        }

        private void BtnMenuUsers_Click(object sender, RoutedEventArgs e)
        {
            SwitchMenuAppearance(btnMenuUsers);
            pnlVehicles.Visibility = Visibility.Hidden;
            pnlUsers.Visibility = Visibility.Visible;
            pnlDrivers.Visibility = Visibility.Hidden;
            LoadUsers("Users");
        }

        private void BtnMenuDrivers_Click(object sender, RoutedEventArgs e)
        {
            SwitchMenuAppearance(btnMenuDrivers);
            pnlVehicles.Visibility = Visibility.Hidden;
            pnlUsers.Visibility = Visibility.Hidden;
            pnlDrivers.Visibility = Visibility.Visible;
            LoadUsers("Drivers");
        }

        private void SwitchMenuAppearance(Button activeButton)
        {
            btnMenuVehicles.Background = Brushes.Transparent;
            btnMenuVehicles.Foreground = (Brush)FindResource("AccentGold");

            btnMenuUsers.Background = Brushes.Transparent;
            btnMenuUsers.Foreground = (Brush)FindResource("AccentGold");

            btnMenuDrivers.Background = Brushes.Transparent;
            btnMenuDrivers.Foreground = (Brush)FindResource("AccentGold");

            activeButton.Background = (Brush)FindResource("AccentGold");
            activeButton.Foreground = (Brush)FindResource("PrimaryBlack");
        }

        // ==========================================================
        //                       AUTÓK KEZELÉSE
        // ==========================================================
        private void LoadVehicles()
        {
            if (cmbCategoryFilter == null || txtSearchVehicle == null || icVehicles == null) return;

            string categoryFilter = ((ComboBoxItem)cmbCategoryFilter.SelectedItem)?.Content?.ToString() ?? "Összes";
            string searchQuery = txtSearchVehicle.Text.Trim();

            List<Vehicle> vehicles = new List<Vehicle>();
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT * FROM vehicles WHERE 1=1";

                    if (categoryFilter != "Összes") query += " AND category = @cat";
                    if (!string.IsNullOrEmpty(searchQuery)) query += " AND (brand LIKE @search OR model LIKE @search)";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        if (categoryFilter != "Összes") cmd.Parameters.AddWithValue("@cat", categoryFilter);
                        if (!string.IsNullOrEmpty(searchQuery)) cmd.Parameters.AddWithValue("@search", "%" + searchQuery + "%");

                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                vehicles.Add(new Vehicle
                                {
                                    Id = reader.GetInt32("id"),
                                    Category = reader.IsDBNull(reader.GetOrdinal("category")) ? "" : reader.GetString("category"),
                                    Brand = reader.IsDBNull(reader.GetOrdinal("brand")) ? "" : reader.GetString("brand"),
                                    Model = reader.IsDBNull(reader.GetOrdinal("model")) ? "" : reader.GetString("model"),
                                    Year = reader.IsDBNull(reader.GetOrdinal("year")) ? 0 : reader.GetInt32("year"),
                                    PricePerDay = reader.IsDBNull(reader.GetOrdinal("price_per_day")) ? 0 : reader.GetDecimal("price_per_day"),
                                    TimesRented = reader.IsDBNull(reader.GetOrdinal("times_rented")) ? 0 : reader.GetInt32("times_rented"),
                                    Status = reader.IsDBNull(reader.GetOrdinal("status")) ? 1 : reader.GetInt32("status")
                                });
                            }
                        }
                    }
                }
                icVehicles.ItemsSource = vehicles;
            }
            catch (Exception ex) { MessageBox.Show("Hiba az autók betöltésekor: " + ex.Message, "Hiba"); }
        }

        private void FilterVehicles_Changed(object sender, RoutedEventArgs e) { LoadVehicles(); }

        private void BtnAddVehicle_Click(object sender, RoutedEventArgs e)
        {
            VehicleEditWindow editWin = new VehicleEditWindow(new Vehicle { Id = 0 });
            if (editWin.ShowDialog() == true) LoadVehicles();
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is Vehicle selectedVehicle)
            {
                VehicleEditWindow editWin = new VehicleEditWindow(selectedVehicle);
                if (editWin.ShowDialog() == true) LoadVehicles();
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is Vehicle selectedVehicle)
            {
                if (MessageBox.Show($"Biztosan törölni szeretnéd?\n\n{selectedVehicle.Brand} {selectedVehicle.Model}", "Törlés", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        conn.Open();
                        new MySqlCommand("DELETE FROM vehicles WHERE id = " + selectedVehicle.Id, conn).ExecuteNonQuery();
                    }
                    LoadVehicles();
                }
            }
        }

        // ==========================================================
        //                 FELHASZNÁLÓK & SOFŐRÖK KEZELÉSE
        // ==========================================================
        private void LoadUsers(string targetPanel)
        {
            if (cmbUserFilter == null || txtSearchUser == null || icUsers == null || icDrivers == null) return;

            List<User> users = new List<User>();
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT * FROM users WHERE 1=1";

                    if (targetPanel == "Drivers")
                    {
                        query += " AND clearance = 2";
                    }
                    else
                    {
                        string roleFilter = ((ComboBoxItem)cmbUserFilter.SelectedItem)?.Content?.ToString() ?? "Összes";
                        string searchQuery = txtSearchUser.Text.Trim();

                        if (roleFilter == "Admin") query += " AND clearance = 3";
                        else if (roleFilter == "Sofőr") query += " AND clearance = 2";
                        else if (roleFilter == "User") query += " AND clearance = 1";

                        if (!string.IsNullOrEmpty(searchQuery))
                        {
                            query += " AND (username LIKE @search OR email LIKE @search OR full_name LIKE @search)";
                        }
                    }

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        if (targetPanel == "Users" && !string.IsNullOrEmpty(txtSearchUser.Text.Trim()))
                            cmd.Parameters.AddWithValue("@search", "%" + txtSearchUser.Text.Trim() + "%");

                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                users.Add(new User
                                {
                                    Id = reader.GetInt32("id"),
                                    Username = reader.GetString("username"),
                                    FullName = reader.IsDBNull(reader.GetOrdinal("full_name")) ? "" : reader.GetString("full_name"),
                                    Email = reader.GetString("email"),
                                    PhoneNumber = reader.IsDBNull(reader.GetOrdinal("phoneNumber")) ? "" : reader.GetString("phoneNumber"),
                                    LicenseNumber = reader.IsDBNull(reader.GetOrdinal("license_number")) ? "" : reader.GetString("license_number"),
                                    Clearance = reader.GetInt32("clearance"),
                                    IsVerified = !reader.IsDBNull(reader.GetOrdinal("is_verified")) && reader.GetBoolean("is_verified")
                                });
                            }
                        }
                    }
                }

                if (targetPanel == "Drivers") icDrivers.ItemsSource = users;
                else icUsers.ItemsSource = users;
            }
            catch (Exception ex) { MessageBox.Show("Hiba a felhasználók betöltésekor: " + ex.Message); }
        }

        private void FilterUsers_Changed(object sender, RoutedEventArgs e) { LoadUsers("Users"); }

        private void BtnEditUser_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is User selectedUser)
            {
                UserEditWindow editWin = new UserEditWindow(selectedUser);
                if (editWin.ShowDialog() == true) LoadUsers(pnlDrivers.Visibility == Visibility.Visible ? "Drivers" : "Users");
            }
        }

        private void BtnDeleteUser_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is User selectedUser)
            {
                if (selectedUser.Username == "admin" || selectedUser.Clearance == 3) { MessageBox.Show("Admint nem lehet törölni!"); return; }
                if (MessageBox.Show($"Biztosan törlöd?\n\n{selectedUser.Username}", "Törlés", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        conn.Open();
                        new MySqlCommand("DELETE FROM users WHERE id = " + selectedUser.Id, conn).ExecuteNonQuery();
                    }
                    LoadUsers(pnlDrivers.Visibility == Visibility.Visible ? "Drivers" : "Users");
                }
            }
        }
    }
}

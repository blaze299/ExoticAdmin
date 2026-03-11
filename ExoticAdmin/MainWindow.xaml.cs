using MySql.Data.MySqlClient;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ExoticAdmin
{
    public partial class MainWindow : Window
    {
        private string connectionString = "Server=127.0.0.1;Database=exotic_rentals;Uid=root;Pwd=;";

        public MainWindow(string loggedInName)
        {
            InitializeComponent();

            // Beállítjuk a dinamikus üdvözlő szöveget
            txtWelcome.Text = $"Üdvözlünk, {loggedInName}!";

            this.Loaded += (s, e) =>
            {
                LoadDashboardStats();
                LoadVehicles();
            };
        }

        // ==========================================================
        //                       ANIMÁCIÓK ÉS TOAST
        // ==========================================================
        private void AnimatePanel(Grid panel)
        {
            Storyboard fadeIn = (Storyboard)FindResource("FadeInAnimation");
            fadeIn.Begin(panel);
        }

        private async void ShowToast(string message)
        {
            txtToastMessage.Text = message;
            DoubleAnimation fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3));
            ToastNotification.BeginAnimation(OpacityProperty, fadeIn);
            await Task.Delay(3000);
            DoubleAnimation fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.3));
            ToastNotification.BeginAnimation(OpacityProperty, fadeOut);
        }

        // ==========================================================
        //                       MENÜ NAVIGÁCIÓ
        // ==========================================================
        private void SwitchMenuAppearance(Button activeButton)
        {
            btnMenuHome.Background = Brushes.Transparent; btnMenuHome.Foreground = (Brush)FindResource("AccentGold");
            btnMenuOrders.Background = Brushes.Transparent; btnMenuOrders.Foreground = (Brush)FindResource("AccentGold");
            btnMenuVehicles.Background = Brushes.Transparent; btnMenuVehicles.Foreground = (Brush)FindResource("AccentGold");
            btnMenuUsers.Background = Brushes.Transparent; btnMenuUsers.Foreground = (Brush)FindResource("AccentGold");
            btnMenuDrivers.Background = Brushes.Transparent; btnMenuDrivers.Foreground = (Brush)FindResource("AccentGold");

            activeButton.Background = (Brush)FindResource("AccentGold");
            activeButton.Foreground = (Brush)FindResource("PrimaryBlack");
        }

        private void HideAllPanels()
        {
            pnlHome.Visibility = Visibility.Hidden;
            pnlOrders.Visibility = Visibility.Hidden;
            pnlVehicles.Visibility = Visibility.Hidden;
            pnlUsers.Visibility = Visibility.Hidden;
            pnlDrivers.Visibility = Visibility.Hidden;
        }

        private void BtnMenuHome_Click(object sender, RoutedEventArgs e) { SwitchMenuAppearance(btnMenuHome); HideAllPanels(); pnlHome.Visibility = Visibility.Visible; AnimatePanel(pnlHome); LoadDashboardStats(); }
        private void BtnMenuOrders_Click(object sender, RoutedEventArgs e) { SwitchMenuAppearance(btnMenuOrders); HideAllPanels(); pnlOrders.Visibility = Visibility.Visible; AnimatePanel(pnlOrders); LoadOrders(); }
        private void BtnMenuVehicles_Click(object sender, RoutedEventArgs e) { SwitchMenuAppearance(btnMenuVehicles); HideAllPanels(); pnlVehicles.Visibility = Visibility.Visible; AnimatePanel(pnlVehicles); LoadVehicles(); }
        private void BtnMenuUsers_Click(object sender, RoutedEventArgs e) { SwitchMenuAppearance(btnMenuUsers); HideAllPanels(); pnlUsers.Visibility = Visibility.Visible; AnimatePanel(pnlUsers); LoadUsers("Users"); }
        private void BtnMenuDrivers_Click(object sender, RoutedEventArgs e) { SwitchMenuAppearance(btnMenuDrivers); HideAllPanels(); pnlDrivers.Visibility = Visibility.Visible; AnimatePanel(pnlDrivers); LoadUsers("Drivers"); }

        // ==========================================================
        //                       KEZDŐLAP & GRAFIKON
        // ==========================================================
        private void LoadDashboardStats()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    using (MySqlCommand cmd = new MySqlCommand("SELECT COUNT(*) FROM vehicles", conn))
                        txtStatVehicles.Text = cmd.ExecuteScalar()?.ToString() ?? "0";

                    using (MySqlCommand cmd = new MySqlCommand("SELECT COUNT(*) FROM users WHERE clearance = 1", conn))
                        txtStatUsers.Text = cmd.ExecuteScalar()?.ToString() ?? "0";

                    using (MySqlCommand cmd = new MySqlCommand("SELECT COUNT(*) FROM users WHERE clearance = 2", conn))
                        txtStatDrivers.Text = cmd.ExecuteScalar()?.ToString() ?? "0";

                    using (MySqlCommand cmd = new MySqlCommand("SELECT SUM(total_price) FROM orders WHERE status = 3", conn))
                    {
                        var rev = cmd.ExecuteScalar();
                        txtStatRevenue.Text = rev != DBNull.Value ? $"{Convert.ToDecimal(rev):N0} €" : "0 €";
                    }

                    // ÚJ: Karbantartási Értesítések
                    List<string> alerts = new List<string>();
                    string alertQuery = "SELECT brand, model, mot_expiry FROM vehicles WHERE mot_expiry IS NOT NULL AND mot_expiry <= DATE_ADD(CURDATE(), INTERVAL 30 DAY) ORDER BY mot_expiry ASC";
                    using (MySqlCommand cmd = new MySqlCommand(alertQuery, conn))
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            DateTime expiry = reader.GetDateTime("mot_expiry");
                            int daysLeft = (expiry - DateTime.Today).Days;
                            string carName = $"{reader.GetString("brand")} {reader.GetString("model")}";

                            if (daysLeft < 0) alerts.Add($"A(z) {carName} műszaki vizsgája {-daysLeft} napja LEJÁRT!");
                            else if (daysLeft == 0) alerts.Add($"A(z) {carName} műszaki vizsgája MA lejár!");
                            else alerts.Add($"A(z) {carName} műszaki vizsgája {daysLeft} nap múlva lejár ({expiry:yyyy.MM.dd})!");
                        }
                    }
                    if (icAlerts != null)
                    {
                        icAlerts.ItemsSource = alerts;
                        icAlerts.Visibility = alerts.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
                    }

                    List<ChartItem> chartData = new List<ChartItem>();
                    string chartQuery = @"SELECT u.username, o.total_price FROM orders o JOIN users u ON o.user_id = u.id ORDER BY o.created_at DESC LIMIT 5";
                    using (MySqlCommand cmd = new MySqlCommand(chartQuery, conn))
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        double maxVal = 1;
                        while (reader.Read())
                        {
                            double val = Convert.ToDouble(reader.GetDecimal("total_price"));
                            if (val > maxVal) maxVal = val;
                            chartData.Add(new ChartItem { Label = reader.GetString("username"), Value = val });
                        }
                        foreach (var item in chartData) item.Width = (item.Value / maxVal) * 300;
                        if (icChart != null) icChart.ItemsSource = chartData;
                    }

                    // Top 3 Autó
                    List<Vehicle> topCars = new List<Vehicle>();
                    string topQuery = @"SELECT v.brand, v.model, v.times_rented, vi.image_url FROM vehicles v LEFT JOIN vehicle_images vi ON v.id = vi.vehicle_id AND vi.is_primary = 1 ORDER BY v.times_rented DESC LIMIT 3";
                    using (MySqlCommand cmd = new MySqlCommand(topQuery, conn))
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            topCars.Add(new Vehicle
                            {
                                Brand = reader.GetString("brand"),
                                Model = reader.GetString("model"),
                                TimesRented = reader.GetInt32("times_rented"),
                                ImageUrl = reader.IsDBNull(reader.GetOrdinal("image_url")) ? "" : reader.GetString("image_url")
                            });
                        }
                        if (icTopCars != null) icTopCars.ItemsSource = topCars;
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine("Hiba a statisztikák betöltésekor: " + ex.Message); }
        }

        // ==========================================================
        //                       BÉRLÉSEK (ORDERS)
        // ==========================================================
        private void LoadOrders()
        {
            if (cmbOrderStatusFilter == null || txtSearchOrder == null || icOrders == null) return;

            string statusFilter = ((ComboBoxItem)cmbOrderStatusFilter.SelectedItem)?.Content?.ToString() ?? "Összes";
            string searchQuery = txtSearchOrder.Text.Trim();

            List<Order> orders = new List<Order>();
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"SELECT o.id, o.start_date, o.end_date, o.total_price, o.status, 
                                            u.full_name, u.username, v.brand, v.model 
                                     FROM orders o
                                     JOIN users u ON o.user_id = u.id
                                     JOIN vehicles v ON o.vehicle_id = v.id
                                     WHERE 1=1";

                    if (statusFilter == "Függőben") query += " AND o.status = 1";
                    else if (statusFilter == "Aktív") query += " AND o.status = 2";
                    else if (statusFilter == "Befejezve") query += " AND o.status = 3";

                    if (!string.IsNullOrEmpty(searchQuery))
                        query += " AND (u.full_name LIKE @search OR u.username LIKE @search OR v.brand LIKE @search OR v.model LIKE @search)";

                    query += " ORDER BY o.created_at DESC";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        if (!string.IsNullOrEmpty(searchQuery)) cmd.Parameters.AddWithValue("@search", "%" + searchQuery + "%");

                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string uName = reader.IsDBNull(reader.GetOrdinal("full_name")) || string.IsNullOrWhiteSpace(reader.GetString("full_name"))
                                                ? reader.GetString("username") : reader.GetString("full_name");

                                orders.Add(new Order
                                {
                                    Id = reader.GetInt32("id"),
                                    CustomerName = uName,
                                    VehicleName = $"{reader.GetString("brand")} {reader.GetString("model")}",
                                    StartDate = reader.GetDateTime("start_date"),
                                    EndDate = reader.GetDateTime("end_date"),
                                    TotalPrice = reader.GetDecimal("total_price"),
                                    Status = reader.GetInt32("status")
                                });
                            }
                        }
                    }
                }
                icOrders.ItemsSource = orders;
            }
            catch (Exception ex) { MessageBox.Show("Hiba a bérlések betöltésekor: " + ex.Message); }
        }

        private void FilterOrders_Changed(object sender, RoutedEventArgs e) { LoadOrders(); }

        private void BtnCompleteOrder_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is Order selectedOrder)
            {
                if (selectedOrder.Status == 3) { ShowToast("Már be van fejezve!"); return; }

                if (MessageBox.Show("Biztosan befejezettnek jelölöd a bérlést?", "Megerősítés", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        conn.Open();
                        new MySqlCommand($"UPDATE orders SET status = 3 WHERE id = {selectedOrder.Id}", conn).ExecuteNonQuery();

                        string vName = selectedOrder.VehicleName.Split(' ')[0];
                        new MySqlCommand($"UPDATE vehicles SET times_rented = times_rented + 1 WHERE brand = '{vName}'", conn).ExecuteNonQuery();
                    }
                    LoadOrders();
                    ShowToast("✔️ Bérlés befejezve!");
                }
            }
        }

        private void BtnExportOrders_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var orders = icOrders.ItemsSource as List<Order>;
                if (orders == null || orders.Count == 0) return;

                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "CSV Fájl (*.csv)|*.csv";
                saveFileDialog.FileName = "berlesek_export.csv";

                if (saveFileDialog.ShowDialog() == true)
                {
                    StringBuilder csv = new StringBuilder();
                    csv.AppendLine("ID;Ügyfél;Autó;Kezdés;Befejezés;Végösszeg;Státusz");

                    foreach (var o in orders)
                        csv.AppendLine($"{o.Id};{o.CustomerName};{o.VehicleName};{o.StartDate:yyyy.MM.dd};{o.EndDate:yyyy.MM.dd};{o.TotalPrice};{o.StatusName}");

                    File.WriteAllText(saveFileDialog.FileName, csv.ToString(), Encoding.UTF8);
                    ShowToast("📄 Bérlések sikeresen kimentve!");
                }
            }
            catch (Exception ex) { MessageBox.Show("Hiba az exportáláskor: " + ex.Message); }
        }

        // ==========================================================
        //                       AUTÓK KEZELÉSE
        // ==========================================================
        private void LoadVehicles()
        {
            if (cmbCategoryFilter == null || txtSearchVehicle == null || icVehicles == null) return;

            List<int> validStatuses = new List<int>();
            if (chkStatusAvailable?.IsChecked == true) validStatuses.Add(1);
            if (chkStatusRented?.IsChecked == true) validStatuses.Add(2);
            if (chkStatusMaintenance?.IsChecked == true) validStatuses.Add(3);

            if (validStatuses.Count == 0) { icVehicles.ItemsSource = null; return; }

            string categoryFilter = ((ComboBoxItem)cmbCategoryFilter.SelectedItem)?.Content?.ToString() ?? "Összes";
            string searchQuery = txtSearchVehicle.Text.Trim();
            string sortFilter = ((ComboBoxItem)cmbSortVehicles?.SelectedItem)?.Content?.ToString() ?? "Alapértelmezett";

            List<Vehicle> vehicles = new List<Vehicle>();
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"SELECT v.*, vi.image_url FROM vehicles v LEFT JOIN vehicle_images vi ON v.id = vi.vehicle_id AND vi.is_primary = 1 WHERE 1=1";
                    string statusIn = string.Join(",", validStatuses);
                    query += $" AND v.status IN ({statusIn})";

                    if (categoryFilter != "Összes") query += " AND v.category = @cat";
                    if (!string.IsNullOrEmpty(searchQuery)) query += " AND (v.brand LIKE @search OR v.model LIKE @search)";

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
                                    Status = reader.IsDBNull(reader.GetOrdinal("status")) ? 1 : reader.GetInt32("status"),
                                    ExteriorColor = reader.IsDBNull(reader.GetOrdinal("exterior_color")) ? "" : reader.GetString("exterior_color"),
                                    Interior = reader.IsDBNull(reader.GetOrdinal("interior")) ? "" : reader.GetString("interior"),
                                    Weight = reader.IsDBNull(reader.GetOrdinal("weight")) ? 0 : reader.GetInt32("weight"),
                                    Doors = reader.IsDBNull(reader.GetOrdinal("doors")) ? 0 : reader.GetInt32("doors"),
                                    WheelStyle = reader.IsDBNull(reader.GetOrdinal("wheel_style")) ? "" : reader.GetString("wheel_style"),
                                    Powertrain = reader.IsDBNull(reader.GetOrdinal("powertrain")) ? "" : reader.GetString("powertrain"),
                                    Transmission = reader.IsDBNull(reader.GetOrdinal("transmission")) ? "" : reader.GetString("transmission"),
                                    Hp = reader.IsDBNull(reader.GetOrdinal("hp")) ? 0 : reader.GetInt32("hp"),
                                    Torque = reader.IsDBNull(reader.GetOrdinal("torque")) ? 0 : reader.GetInt32("torque"),
                                    Acceleration = reader.IsDBNull(reader.GetOrdinal("acceleration")) ? "" : reader.GetString("acceleration"),
                                    TopSpeed = reader.IsDBNull(reader.GetOrdinal("top_speed")) ? 0 : reader.GetInt32("top_speed"),
                                    Extras = reader.IsDBNull(reader.GetOrdinal("extras")) ? "" : reader.GetString("extras"),
                                    Drive = reader.IsDBNull(reader.GetOrdinal("drive")) ? "" : reader.GetString("drive"),
                                    Description = reader.IsDBNull(reader.GetOrdinal("description")) ? "" : reader.GetString("description"),
                                    ImageUrl = reader.IsDBNull(reader.GetOrdinal("image_url")) ? "" : reader.GetString("image_url"),

                                    MotExpiry = reader.IsDBNull(reader.GetOrdinal("mot_expiry")) ? (DateTime?)null : reader.GetDateTime("mot_expiry"),
                                    CurrentMileage = reader.IsDBNull(reader.GetOrdinal("current_mileage")) ? 0 : reader.GetInt32("current_mileage"),
                                    MaintenanceNotes = reader.IsDBNull(reader.GetOrdinal("maintenance_notes")) ? "" : reader.GetString("maintenance_notes")
                                });
                            }
                        }
                    }
                }
                if (sortFilter == "Ár szerint (Növekvő)") vehicles = vehicles.OrderBy(v => v.PricePerDay).ToList();
                else if (sortFilter == "Ár szerint (Csökkenő)") vehicles = vehicles.OrderByDescending(v => v.PricePerDay).ToList();
                icVehicles.ItemsSource = vehicles;
            }
            catch (Exception ex) { MessageBox.Show("Hiba az autók betöltésekor: " + ex.Message, "Hiba"); }
        }

        private void FilterVehicles_Changed(object sender, RoutedEventArgs e) { LoadVehicles(); }

        private void BtnAddVehicle_Click(object sender, RoutedEventArgs e)
        {
            VehicleEditWindow editWin = new VehicleEditWindow(new Vehicle { Id = 0 });
            if (editWin.ShowDialog() == true) { LoadVehicles(); ShowToast("✔️ Új autó sikeresen felvéve!"); }
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is Vehicle selectedVehicle)
            {
                VehicleEditWindow editWin = new VehicleEditWindow(selectedVehicle);
                if (editWin.ShowDialog() == true) { LoadVehicles(); ShowToast("✔️ Autó sikeresen módosítva!"); }
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is Vehicle selectedVehicle)
            {
                if (MessageBox.Show($"Biztosan törölni szeretnéd az alábbi autót?\n\n{selectedVehicle.Brand} {selectedVehicle.Model}", "Törlés", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (MySqlConnection conn = new MySqlConnection(connectionString))
                        {
                            conn.Open();
                            new MySqlCommand("DELETE FROM vehicles WHERE id = " + selectedVehicle.Id, conn).ExecuteNonQuery();
                        }
                        LoadVehicles();
                        ShowToast("🗑️ Autó törölve a rendszerből.");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Hiba a törlés során: " + ex.Message, "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void BtnExportVehicles_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var vehicles = icVehicles.ItemsSource as List<Vehicle>;
                if (vehicles == null || vehicles.Count == 0) return;

                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "CSV Fájl (*.csv)|*.csv";
                saveFileDialog.FileName = "autok_export.csv";

                if (saveFileDialog.ShowDialog() == true)
                {
                    StringBuilder csv = new StringBuilder();
                    csv.AppendLine("ID;Márka;Modell;Évjárat;Kategória;Napi Ár;Státusz");
                    foreach (var v in vehicles) csv.AppendLine($"{v.Id};{v.Brand};{v.Model};{v.Year};{v.Category};{v.PricePerDay};{v.StatusName}");
                    File.WriteAllText(saveFileDialog.FileName, csv.ToString(), Encoding.UTF8);
                    ShowToast("📄 Járműlista sikeresen kimentve!");
                }
            }
            catch (Exception ex) { MessageBox.Show("Hiba az exportáláskor: " + ex.Message); }
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
                        if (txtSearchDriver != null && !string.IsNullOrEmpty(txtSearchDriver.Text.Trim()))
                            query += " AND (username LIKE @search OR email LIKE @search OR full_name LIKE @search)";
                    }
                    else
                    {
                        bool showVerified = chkVerified?.IsChecked == true;
                        bool showUnverified = chkUnverified?.IsChecked == true;
                        if (!showVerified && !showUnverified) { icUsers.ItemsSource = null; return; }

                        query += " AND clearance != 2";
                        if (showVerified && !showUnverified) query += " AND is_verified = 1";
                        else if (!showVerified && showUnverified) query += " AND (is_verified = 0 OR is_verified IS NULL)";

                        string roleFilter = ((ComboBoxItem)cmbUserFilter.SelectedItem)?.Content?.ToString() ?? "Összes";
                        string searchQuery = txtSearchUser.Text.Trim();

                        if (roleFilter == "Admin") query += " AND clearance = 3";
                        else if (roleFilter == "User") query += " AND clearance = 1";

                        if (!string.IsNullOrEmpty(searchQuery))
                            query += " AND (username LIKE @search OR email LIKE @search OR full_name LIKE @search)";
                    }

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        if (targetPanel == "Users" && !string.IsNullOrEmpty(txtSearchUser.Text.Trim())) cmd.Parameters.AddWithValue("@search", "%" + txtSearchUser.Text.Trim() + "%");
                        else if (targetPanel == "Drivers" && txtSearchDriver != null && !string.IsNullOrEmpty(txtSearchDriver.Text.Trim())) cmd.Parameters.AddWithValue("@search", "%" + txtSearchDriver.Text.Trim() + "%");

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
                                    IsVerified = !reader.IsDBNull(reader.GetOrdinal("is_verified")) && reader.GetBoolean("is_verified"),
                                    IsBanned = !reader.IsDBNull(reader.GetOrdinal("is_banned")) && reader.GetBoolean("is_banned"),
                                    AdminNotes = reader.IsDBNull(reader.GetOrdinal("admin_notes")) ? "" : reader.GetString("admin_notes")
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
        private void FilterDrivers_Changed(object sender, RoutedEventArgs e) { LoadUsers("Drivers"); }

        private void BtnEditUser_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is User selectedUser)
            {
                UserEditWindow editWin = new UserEditWindow(selectedUser);
                if (editWin.ShowDialog() == true)
                {
                    LoadUsers(pnlDrivers.Visibility == Visibility.Visible ? "Drivers" : "Users");
                    ShowToast("✔️ Profil mentve.");
                }
            }
        }

        private void BtnDeleteUser_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is User selectedUser)
            {
                if (selectedUser.Username == "admin" || selectedUser.Clearance == 3) { ShowToast("❌ Admint nem lehet törölni!"); return; }

                string displayName = string.IsNullOrWhiteSpace(selectedUser.FullName) ? selectedUser.Username : selectedUser.FullName;

                string promptMessage = selectedUser.Clearance == 2
                    ? $"Biztosan törölni szeretnéd az alábbi sofőrt?\n\n{displayName}"
                    : $"Biztosan törölni szeretnéd az alábbi felhasználót?\n\n{displayName}";

                string toastMessage = selectedUser.Clearance == 2 ? "🗑️ Sofőr törölve." : "🗑️ Felhasználó törölve.";

                if (MessageBox.Show(promptMessage, "Törlés", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        conn.Open();
                        new MySqlCommand("DELETE FROM users WHERE id = " + selectedUser.Id, conn).ExecuteNonQuery();
                    }
                    LoadUsers(pnlDrivers.Visibility == Visibility.Visible ? "Drivers" : "Users");
                    ShowToast(toastMessage);
                }
            }
        }
    }
}
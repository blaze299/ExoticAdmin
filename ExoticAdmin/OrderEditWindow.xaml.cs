using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Windows;

namespace ExoticAdmin
{
    public partial class OrderEditWindow : Window
    {
        private Order _order;
        private string connectionString = "Server=127.0.0.1;Database=exotic_rentals;Uid=root;Pwd=;";

        public class ComboItem
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public OrderEditWindow(Order order)
        {
            InitializeComponent();
            _order = order;
            LoadComboBoxes();

            if (_order.Id != 0)
            {
                cmbCustomer.SelectedValue = _order.UserId;
                cmbVehicle.SelectedValue = _order.VehicleId;
                txtStart.Text = _order.StartDate.ToString("yyyy-MM-dd");
                txtEnd.Text = _order.EndDate.ToString("yyyy-MM-dd");
                txtPrice.Text = _order.TotalPrice.ToString();
            }
            else
            {
                txtStart.Text = DateTime.Today.ToString("yyyy-MM-dd");
                txtEnd.Text = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd");
                txtPrice.Text = "0";
            }
        }

        private void LoadComboBoxes()
        {
            List<ComboItem> customers = new List<ComboItem>();
            List<ComboItem> vehicles = new List<ComboItem>();

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT id, username, full_name FROM users", conn))
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string fname = reader.IsDBNull(2) ? "" : reader.GetString(2);
                            string uname = reader.GetString(1);
                            customers.Add(new ComboItem { Id = reader.GetInt32(0), Name = string.IsNullOrEmpty(fname) ? uname : fname });
                        }
                    }

                    using (MySqlCommand cmd = new MySqlCommand("SELECT id, brand, model FROM vehicles", conn))
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            vehicles.Add(new ComboItem { Id = reader.GetInt32(0), Name = $"{reader.GetString(1)} {reader.GetString(2)}" });
                        }
                    }
                }
                cmbCustomer.ItemsSource = customers;
                cmbVehicle.ItemsSource = vehicles;
            }
            catch (Exception ex) { MessageBox.Show("Hiba az adatok betöltésekor: " + ex.Message); }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (cmbCustomer.SelectedValue == null || cmbVehicle.SelectedValue == null)
            {
                MessageBox.Show("Kérlek válassz ügyfelet és autót is!", "Hiányzó adat", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query;

                    if (_order.Id == 0) // ÚJ BÉRLÉS FELVÉTELE
                    {
                        query = @"INSERT INTO orders (user_id, vehicle_id, start_date, end_date, total_price, status) 
                                  VALUES (@uid, @vid, @start, @end, @price, @status)";
                    }
                    else // MEGLÉVŐ FRISSÍTÉSE
                    {
                        query = @"UPDATE orders SET user_id=@uid, vehicle_id=@vid, start_date=@start, end_date=@end, total_price=@price WHERE id=@id";
                    }

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@uid", cmbCustomer.SelectedValue);
                        cmd.Parameters.AddWithValue("@vid", cmbVehicle.SelectedValue);
                        cmd.Parameters.AddWithValue("@start", DateTime.Parse(txtStart.Text));
                        cmd.Parameters.AddWithValue("@end", DateTime.Parse(txtEnd.Text));
                        cmd.Parameters.AddWithValue("@price", decimal.Parse(txtPrice.Text));
                        cmd.Parameters.AddWithValue("@status", _order.Id == 0 ? 1 : _order.Status); // Új alapértelmezetten Függőben

                        if (_order.Id != 0) cmd.Parameters.AddWithValue("@id", _order.Id);

                        cmd.ExecuteNonQuery();
                    }
                }
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Hiba a mentés során (ellenőrizd a dátum formátumát): \n" + ex.Message, "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
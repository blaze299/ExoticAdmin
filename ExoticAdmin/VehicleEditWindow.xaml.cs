using MySql.Data.MySqlClient;
using System;
using System.Windows;

namespace ExoticAdmin
{
    public partial class VehicleEditWindow : Window
    {
        private Vehicle _vehicle;
        private string connectionString = "Server=127.0.0.1;Database=exotic_rentals;Uid=root;Pwd=;";

        public VehicleEditWindow(Vehicle vehicle)
        {
            InitializeComponent();
            _vehicle = vehicle;

            // BETÖLTÉS EGY LÉTEZŐ AUTÓNÁL
            if (_vehicle.Id != 0)
            {
                txtBrand.Text = _vehicle.Brand;
                txtModel.Text = _vehicle.Model;
                txtCategory.Text = _vehicle.Category;
                txtYear.Text = _vehicle.Year.ToString();
                txtPrice.Text = _vehicle.PricePerDay.ToString();
                txtColor.Text = _vehicle.ExteriorColor;
                txtInterior.Text = _vehicle.Interior;
                txtDoors.Text = _vehicle.Doors.ToString();
                txtWeight.Text = _vehicle.Weight.ToString();
                txtWheel.Text = _vehicle.WheelStyle;

                txtPowertrain.Text = _vehicle.Powertrain;
                txtTransmission.Text = _vehicle.Transmission;
                txtDrive.Text = _vehicle.Drive;
                txtHp.Text = _vehicle.Hp.ToString();
                txtTorque.Text = _vehicle.Torque.ToString();
                txtAcceleration.Text = _vehicle.Acceleration;
                txtTopSpeed.Text = _vehicle.TopSpeed.ToString();
                txtStatus.Text = _vehicle.Status.ToString();

                txtExtras.Text = _vehicle.Extras;
                txtDescription.Text = _vehicle.Description;
                txtImageUrl.Text = _vehicle.ImageUrl;

                // --- ÚJ RÉSZ: Szerviz adatok betöltése ---
                txtMileage.Text = _vehicle.CurrentMileage.ToString();
                if (_vehicle.MotExpiry.HasValue) txtMotExpiry.Text = _vehicle.MotExpiry.Value.ToString("yyyy-MM-dd");
                txtMaintenance.Text = _vehicle.MaintenanceNotes;
            }
            else
            {
                txtStatus.Text = "1";
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

                    // MENTÉS - ÚJ AUTÓ (Kibővítve a szerviz mezőkkel)
                    if (_vehicle.Id == 0)
                    {
                        query = @"INSERT INTO vehicles 
                                (category, brand, model, exterior_color, interior, year, weight, doors, wheel_style, 
                                powertrain, transmission, hp, torque, acceleration, top_speed, extras, drive, price_per_day, description, status, current_mileage, mot_expiry, maintenance_notes) 
                                VALUES 
                                (@category, @brand, @model, @color, @interior, @year, @weight, @doors, @wheel, 
                                @powertrain, @transmission, @hp, @torque, @accel, @speed, @extras, @drive, @price, @desc, @status, @mileage, @mot, @mnotes)";
                    }
                    // MENTÉS - FRISSÍTÉS (Kibővítve a szerviz mezőkkel)
                    else
                    {
                        query = @"UPDATE vehicles 
                                SET category=@category, brand=@brand, model=@model, exterior_color=@color, interior=@interior, 
                                year=@year, weight=@weight, doors=@doors, wheel_style=@wheel, powertrain=@powertrain, 
                                transmission=@transmission, hp=@hp, torque=@torque, acceleration=@accel, top_speed=@speed, 
                                extras=@extras, drive=@drive, price_per_day=@price, description=@desc, status=@status,
                                current_mileage=@mileage, mot_expiry=@mot, maintenance_notes=@mnotes 
                                WHERE id=@id";
                    }

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@brand", txtBrand.Text);
                        cmd.Parameters.AddWithValue("@model", txtModel.Text);
                        cmd.Parameters.AddWithValue("@category", txtCategory.Text);
                        cmd.Parameters.AddWithValue("@color", txtColor.Text);
                        cmd.Parameters.AddWithValue("@interior", txtInterior.Text);
                        cmd.Parameters.AddWithValue("@wheel", txtWheel.Text);
                        cmd.Parameters.AddWithValue("@powertrain", txtPowertrain.Text);
                        cmd.Parameters.AddWithValue("@transmission", txtTransmission.Text);
                        cmd.Parameters.AddWithValue("@drive", txtDrive.Text);
                        cmd.Parameters.AddWithValue("@accel", txtAcceleration.Text);
                        cmd.Parameters.AddWithValue("@extras", txtExtras.Text);
                        cmd.Parameters.AddWithValue("@desc", txtDescription.Text);

                        cmd.Parameters.AddWithValue("@year", int.TryParse(txtYear.Text, out int y) ? y : 0);
                        cmd.Parameters.AddWithValue("@price", decimal.TryParse(txtPrice.Text, out decimal p) ? p : 0);
                        cmd.Parameters.AddWithValue("@weight", int.TryParse(txtWeight.Text, out int w) ? w : 0);
                        cmd.Parameters.AddWithValue("@doors", int.TryParse(txtDoors.Text, out int d) ? d : 0);
                        cmd.Parameters.AddWithValue("@hp", int.TryParse(txtHp.Text, out int h) ? h : 0);
                        cmd.Parameters.AddWithValue("@torque", int.TryParse(txtTorque.Text, out int t) ? t : 0);
                        cmd.Parameters.AddWithValue("@speed", int.TryParse(txtTopSpeed.Text, out int s) ? s : 0);
                        cmd.Parameters.AddWithValue("@status", int.TryParse(txtStatus.Text, out int stat) ? stat : 1);

                        // --- ÚJ RÉSZ: Szerviz paraméterek ---
                        cmd.Parameters.AddWithValue("@mileage", int.TryParse(txtMileage.Text, out int m) ? m : 0);
                        if (DateTime.TryParse(txtMotExpiry.Text, out DateTime mot)) cmd.Parameters.AddWithValue("@mot", mot);
                        else cmd.Parameters.AddWithValue("@mot", DBNull.Value);
                        cmd.Parameters.AddWithValue("@mnotes", txtMaintenance.Text);

                        if (_vehicle.Id != 0) cmd.Parameters.AddWithValue("@id", _vehicle.Id);

                        cmd.ExecuteNonQuery();

                        // KÉP MENTÉSE (Ezt meghagytam, mert korábban megírtuk)
                        long currentVehicleId = _vehicle.Id == 0 ? cmd.LastInsertedId : _vehicle.Id;

                        string checkImgQuery = "SELECT COUNT(*) FROM vehicle_images WHERE vehicle_id=@vid AND is_primary=1";
                        using (MySqlCommand checkCmd = new MySqlCommand(checkImgQuery, conn))
                        {
                            checkCmd.Parameters.AddWithValue("@vid", currentVehicleId);
                            long imgCount = Convert.ToInt64(checkCmd.ExecuteScalar());

                            string imgSql = imgCount > 0
                                ? "UPDATE vehicle_images SET image_url=@img WHERE vehicle_id=@vid AND is_primary=1"
                                : "INSERT INTO vehicle_images (vehicle_id, image_url, is_primary) VALUES (@vid, @img, 1)";

                            using (MySqlCommand imgCmd = new MySqlCommand(imgSql, conn))
                            {
                                imgCmd.Parameters.AddWithValue("@vid", currentVehicleId);
                                imgCmd.Parameters.AddWithValue("@img", txtImageUrl.Text.Trim());
                                imgCmd.ExecuteNonQuery();
                            }
                        }
                    }
                }
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Hiba a mentés során: " + ex.Message, "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
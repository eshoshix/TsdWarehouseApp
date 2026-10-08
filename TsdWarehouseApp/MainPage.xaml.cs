using Microsoft.Data.SqlClient;
using Microsoft.Maui.Controls.Internals;
using System.Data;


namespace TsdWarehouseApp
{
    public partial class MainPage : ContentPage
    {
        DataBase database = new DataBase();

        public MainPage()
        {
           
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            UpdateTsdStatus();
          
            CheckDatabaseConnectionAsync();
        }

        private async void OnSettingsClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new SettingsPage());
        }

       
        private async Task<bool> CheckDatabaseConnectionAsync()
        {
            try
            {
                using var connection = database.getConnection();
                await connection.OpenAsync();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"--- ОШИБКА БАЗЫ ДАННЫХ: {ex.Message} ---");
                return false;
            }
        }

        private async Task<(int Id, string FullName)?> GetEmployeeByLoginAsync(string login)
        {
            if (string.IsNullOrEmpty(login)) return null;

            string searchLogin = login.Trim().ToUpperInvariant();
            using var connection = database.getConnection();

            await connection.OpenAsync();

            string query = "SELECT id, FullName FROM Employees WHERE UPPER(login) = @login";

            using var command = new SqlCommand(query, connection);
            command.Parameters.Add("@login", SqlDbType.NVarChar, 50).Value = searchLogin;

            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                int idIndex = reader.GetOrdinal("id");
                int nameIndex = reader.GetOrdinal("FullName");

                if (!reader.IsDBNull(nameIndex))
                {
                    int id = reader.GetInt32(idIndex);
                    string fullName = reader.GetString(nameIndex);
                    return (id, fullName);
                }
            }

            return null;
        }
        private async Task<bool> GetDoubleEmployeeAsync(int id)
        {
            int searchid = id;

            using var connection = database.getConnection();
            await connection.OpenAsync();

           
            string query = @"
                    SELECT employee_id
                    FROM ShiftLogs
                    WHERE employee_id = '1'
                    AND (
                    (CreatedAt >= DATEADD(HOUR, 9, CAST(CAST(GETDATE() AS DATE) AS DATETIME))
                    AND CreatedAt < DATEADD(HOUR, 20, CAST(CAST(GETDATE() AS DATE) AS DATETIME)))

                    OR

                   (CreatedAt >= DATEADD(HOUR, 21, CAST(CAST(GETDATE() AS DATE) AS DATETIME))
                   AND CreatedAt < DATEADD(HOUR, 8, CAST(DATEADD(DAY, 1, CAST(GETDATE() AS DATE)) AS DATETIME)))
              );";

            using var command = new SqlCommand(query, connection);
            command.Parameters.Add("@id", SqlDbType.NVarChar, 50).Value = searchid;

            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                
                return true;
            }

            return false;
        }

        private void UpdateTsdStatus()
        {
            
            string tsdNumber = DeviceCache.GetTsdNumber();
            if (string.IsNullOrEmpty(tsdNumber))
            {
                LblTsdNumber.Text = "Не задан (требуется настройка)";
                LblTsdNumber.TextColor = Colors.Red;
            }
            else
            {
                LblTsdNumber.Text = tsdNumber;
                LblTsdNumber.TextColor = Colors.DarkBlue;
            }

        
            string defects = DeviceCache.GetTsdDefects();
            if (string.IsNullOrEmpty(defects))
            {
                LblTsdDefects.Text = "Нет дефектов";
                LblTsdDefects.TextColor = Colors.Green;
            }
            else
            {
                LblTsdDefects.Text = defects;
                LblTsdDefects.TextColor = Colors.Red;
            }
        }
        private CancellationTokenSource? _searchCts;

        private async void OnLoginTextChanged(object sender, TextChangedEventArgs e)
        {
            string login = (sender as Entry)?.Text?.Trim() ?? string.Empty;


            _searchCts?.Cancel();
            _searchCts = new CancellationTokenSource();
            var token = _searchCts.Token;

            if (string.IsNullOrEmpty(login))
            {
                LblFullName.Text = "Не указано";
                LblFullName.TextColor = Colors.Gray;
                return;
            }

            try
            {
                await Task.Delay(400, token);
            }
            catch (TaskCanceledException)
            {
                return;
            }

            if (token.IsCancellationRequested) return;

            LblFullName.Text = "Поиск...";
            LblFullName.TextColor = Colors.Gray;

            try
            {
                var result = await GetEmployeeByLoginAsync(login);



                if (result != null)
                {
                    string fullName = result.Value.FullName;
                    LblFullName.Text = fullName;
                    LblFullName.TextColor = Colors.Green;

                    TxtVestNumber.Focus();
                }
                else
                {
                    LblFullName.Text = "Не найдено";
                    LblFullName.TextColor = Colors.Red;
                }
            }
            catch (Exception ex)
            {
                LblFullName.Text = "Ошибка загрузки";
                LblFullName.TextColor = Colors.Red;
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }


            

        }

        private async void OnSubmitClicked(object sender, EventArgs e)
        {
            string currentTsd = DeviceCache.GetTsdNumber();
            if (string.IsNullOrEmpty(currentTsd))
            {
                await DisplayAlert("Ошибка", "Сначала укажите номер ТСД в настройках!", "OK");
                return;
            }

            string login = TxtLogin.Text?.Trim();
            if (string.IsNullOrWhiteSpace(login))
            {
                await DisplayAlert("Ошибка", "Сперва отсканируйте логин!", "OK");
                TxtLogin.Focus();
                return;
            }

            string fullName = LblFullName.Text;
            if (string.IsNullOrWhiteSpace(fullName) || fullName == "Не найдено")
            {
                
                var result = await DisplayAlert("Внимание",
                    $"Сотрудник с логином {login} не найден в базе. Продолжить сохранение?", "Да", "Нет");
                if (!result) return;
               
                fullName = login;
            }

            string vestNumber = TxtVestNumber.Text;
            if (string.IsNullOrWhiteSpace(vestNumber))
            {
                await DisplayAlert("Ошибка", "Сперва введите номер жилетки!", "OK");
                TxtVestNumber.Focus();
                return;
            }

            bool saved = await SaveRecordAsync(currentTsd, vestNumber);
            if (saved)
            {
                await DisplayAlert("Успех", "Данные успешно сохранены!", "OK");
                ClearForm();
            }
        }

        private void ClearForm()
        {
            TxtLogin.Text = string.Empty;
            LblFullName.Text = "Не указано";
            LblFullName.TextColor = Colors.Gray;
            TxtVestNumber.Text = string.Empty;
            TxtLogin.Focus(); 
        }

        private async void OnLoginCompleted(object sender, EventArgs e)
        {
            var entry = sender as Entry;
            string rawLogin = entry?.Text?.Trim();

            if (string.IsNullOrEmpty(rawLogin))
            {
                LblFullName.Text = "Не указано";
                LblFullName.TextColor = Colors.Gray;
                return;
            }

            entry.IsEnabled = false;
            LblFullName.Text = "Поиск...";
            LblFullName.TextColor = Colors.Gray;

            try
            {
                var result = await GetEmployeeByLoginAsync(rawLogin);

                if (result != null)
                {
                    string fullName = result.Value.FullName;
                    LblFullName.Text = fullName;
                    LblFullName.TextColor = Colors.Black;

                    
                    TxtVestNumber.Focus();
                }
                else
                {
                  
                    LblFullName.Text = "Не найдено";
                    LblFullName.TextColor = Colors.Red;
                    System.Diagnostics.Debug.WriteLine($"Сотрудник с логином {rawLogin} не найден.");
                }
            }
            catch (Exception ex)
            {
               
                await DisplayAlert("Ошибка БД", "Не удалось получить данные сотрудника. Проверьте связь.", "OK");
                LblFullName.Text = "Ошибка загрузки";
                LblFullName.TextColor = Colors.Red;
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
            finally
            {
                entry.IsEnabled = true; 
            }
        }



        private async Task<bool> SaveRecordAsync(string tsdNumber, string vestNumber)
        {
            TimeZoneInfo nskTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Novosibirsk");
            DateTime now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, nskTimeZone);
            
            int hour = now.Hour;

            string shiftType;
            DateTime shiftDate;

            // Дневная смена (официально 09:00 - 21:00, с запасом для прихода с 02:00 до 14:00):
            if (hour >= 8 && hour < 18)
            {
                shiftType = "День";
                shiftDate = now.Date;
            }
            else
            {
                // Ночная смена (официально 21:00 - 09:00, с запасом с 14:00 до 02:00):
                shiftType = "Ночь";

                // Если сотрудник пришел ночью после полуночи (с 00:00 до 02:00), 
                // то технически это ночь, которая началась ВЧЕРА вечером (после 20:00)
                // Либо если сейчас от 14:00 до 23:59 — это тоже ночная смена сегодняшнего дня.

                if (hour >= 18)
                {
                    shiftDate = now.Date;
                }
                else
                {
                    // Если время с 00:00 до 02:00 ночи — это вчерашняя ночная смена
                    shiftDate = now.Date.AddDays(-1);
                }
            }



            try
            {
                using var connection = database.getConnection();
                await connection.OpenAsync();

                var res = await GetEmployeeByLoginAsync(TxtLogin.Text);
                
                int id = res.Value.Id;

                if (await GetDoubleEmployeeAsync(id))
                {
                    await DisplayAlert("Ошибка!", "Сотрудник с таким логином уже записан на текущую смену", "OK");
                    return false;
                }

                string query = @"INSERT INTO ShiftLogs (employee_ID, TsdNumber, VestNumber, TsdDefects, CreatedAt, shiftType) 
                        VALUES (@employee_ID, @TsdNumber, @VestNumber, @TsdDefects, GETDATE(), @shiftType )";

                using var command = new SqlCommand(query, connection);

                command.Parameters.AddWithValue("@employee_ID", id);
                command.Parameters.AddWithValue("@TsdNumber", tsdNumber);
                command.Parameters.AddWithValue("@VestNumber", vestNumber);
                command.Parameters.AddWithValue("@shiftType", shiftType);

                string tsdDefects = DeviceCache.GetTsdDefects();
                command.Parameters.AddWithValue("@TsdDefects", string.IsNullOrEmpty(tsdDefects) ? DBNull.Value : tsdDefects);

                await command.ExecuteNonQueryAsync();
                return true;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка сохранения", ex.Message, "OK");
                return false;
            }
        }
    }
}
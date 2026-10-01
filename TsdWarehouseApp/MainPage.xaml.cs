using Microsoft.Data.SqlClient;
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
            // Проверку соединения можно делать реже, не каждый раз при появлении страницы
            // await CheckDatabaseConnectionAsync(); 
            CheckDatabaseConnectionAsync();
        }

        private async void OnSettingsClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new SettingsPage());
        }

        // Проверка соединения (лучше вызывать при старте приложения, а не на каждой странице)
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

        private async Task<string?> GetFullNameByLoginAsync(string login)
        {
            if (string.IsNullOrEmpty(login)) return null;


            string searchLogin = login.Trim().ToUpperInvariant();

            using var connection = database.getConnection();


            await connection.OpenAsync();

            string query = "SELECT FullName FROM Employees WHERE UPPER(login) = @login";

            using var command = new SqlCommand(query, connection);
            command.Parameters.Add("@login", SqlDbType.NVarChar, 50).Value = searchLogin;

            using var rdr = await command.ExecuteReaderAsync();

            if (await rdr.ReadAsync())
            {
                int ordinal = rdr.GetOrdinal("FullName");
                if (!rdr.IsDBNull(ordinal))
                    return rdr.GetString(ordinal);
            }

            return null;
        }

        private void UpdateTsdStatus()
        {
            // Загружаем и устанавливаем номер ТСД
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

            // Загружаем и устанавливаем дефекты ТСД
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

            // Отменяем предыдущий поиск, если пользователь печатает быстро
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
                string fullName = await GetFullNameByLoginAsync(login);

                if (token.IsCancellationRequested) return;

                if (!string.IsNullOrEmpty(fullName))
                {
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

            // Если ФИО не подтянулось, но логин есть — можно сохранить только логин, 
            // либо требовать ФИО. Сейчас логика требует ФИО.
            string fullName = LblFullName.Text;
            if (string.IsNullOrWhiteSpace(fullName) || fullName == "Не найдено")
            {
                // Мягкое предупреждение вместо жесткого блока, если бизнес-логика позволяет
                var result = await DisplayAlert("Внимание",
                    $"Сотрудник с логином {login} не найден в базе. Продолжить сохранение?", "Да", "Нет");
                if (!result) return;
                // Если продолжили, можно сохранить сам логин вместо ФИО, если так принято
                fullName = login;
            }

            string vestNumber = TxtVestNumber.Text;
            if (string.IsNullOrWhiteSpace(vestNumber))
            {
                await DisplayAlert("Ошибка", "Сперва введите номер жилетки!", "OK");
                TxtVestNumber.Focus();
                return;
            }

            bool saved = await SaveRecordAsync(currentTsd, vestNumber, fullName);
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
            TxtLogin.Focus(); // Возвращаем фокус на логин для следующего скана
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

            // Блокируем повторный ввод и показываем статус
            entry.IsEnabled = false;
            LblFullName.Text = "Поиск...";
            LblFullName.TextColor = Colors.Gray;

            try
            {
                string fullName = await GetFullNameByLoginAsync(rawLogin);

                if (!string.IsNullOrEmpty(fullName))
                {
                    LblFullName.Text = fullName;
                    LblFullName.TextColor = Colors.Black;

                    // Автоматически ставим фокус на жилетку, чтобы оператор сразу сканировал дальше
                    TxtVestNumber.Focus();
                }
                else
                {
                    // НЕ используем DisplayAlert здесь! Это ломает ритм работы на ТСД.
                    LblFullName.Text = "Не найдено";
                    LblFullName.TextColor = Colors.Red;
                    System.Diagnostics.Debug.WriteLine($"Сотрудник с логином {rawLogin} не найден.");
                }
            }
            catch (Exception ex)
            {
                // Критическая ошибка БД — вот тут можно показать алерт
                await DisplayAlert("Ошибка БД", "Не удалось получить данные сотрудника. Проверьте связь.", "OK");
                LblFullName.Text = "Ошибка загрузки";
                LblFullName.TextColor = Colors.Red;
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
            finally
            {
                entry.IsEnabled = true; // Возвращаем возможность ввода
            }
        }

        private async Task<bool> SaveRecordAsync(string tsdNumber, string vestNumber, string fullName)
        {
            try
            {
                using var connection = database.getConnection();
                await connection.OpenAsync();

                string query = @"INSERT INTO ShiftLogs (FullName, TsdNumber, VestNumber, TsdDefects, CreatedAt) 
                        VALUES (@FullName, @TsdNumber, @VestNumber, @TsdDefects, GETDATE())";

                using var command = new SqlCommand(query, connection);

                command.Parameters.AddWithValue("@FullName", fullName ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@TsdNumber", tsdNumber);
                command.Parameters.AddWithValue("@VestNumber", vestNumber);

                // Подтягиваем дефекты из отдельного кэш-файла
                string tsdDefects = DeviceCache.GetTsdDefects();
                command.Parameters.AddWithValue("@TsdDefects", string.IsNullOrEmpty(tsdDefects) ? (object)DBNull.Value : tsdDefects);

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

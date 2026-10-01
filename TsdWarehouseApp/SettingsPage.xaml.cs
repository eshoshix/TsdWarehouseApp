namespace TsdWarehouseApp
{
    public partial class SettingsPage : ContentPage
    {
        public SettingsPage()
        {
            // Сначала инициализируем элементы из XAML!
            InitializeComponent();

            // ПРАВИЛЬНО: Подгружаем сохранённые данные в поля ввода при открытии страницы
            TxtTsdInput.Text = DeviceCache.GetTsdNumber();
            TxtDefectsInput.Text = DeviceCache.GetTsdDefects();
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            // 1. Валидация номера ТСД
            string rawInput = TxtTsdInput.Text;
            string digitsOnly = System.Text.RegularExpressions.Regex.Replace(rawInput, @"[^0-9]", ""); // исправил регулярку на [^0-9], чтобы нули тоже не вырезались случайно

            if (string.IsNullOrEmpty(digitsOnly))
            {
                await DisplayAlert("Ошибка", "Номер ТСД не может быть пустым!", "OK");
                return;
            }

            string defects = TxtDefectsInput.Text?.Trim() ?? string.Empty;

            // 3. Сохраняем данные в кэш
            DeviceCache.SaveTsdNumber(digitsOnly);
            DeviceCache.SaveTsdDefects(defects); // ПРАВИЛЬНО: передаем переменную с дефектами

            await Navigation.PopAsync();
        }
    }
}
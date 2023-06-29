using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace TimePicker
{
    public partial class TimePickerControl : UserControl
    {
        private readonly string[] monthList = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

        private Mode mode = Mode.Day;
        private Dictionary<string, List<RadioButton>> dict = new Dictionary<string, List<RadioButton>>();
        private List<Tuple<DateTime, DateTime>> enabledTimeRange = new List<Tuple<DateTime, DateTime>>();
        private List<Tuple<DateTime, DateTime>> disableTimeRange = new List<Tuple<DateTime, DateTime>>();
        private RadioButton selectedRadioButton;

        private int currentYear;
        private int currentMonth;

        private int startYear = 1980;
        private int endYear = 2100;

        private string preDateString;
        private string preTimeString;

        private bool _weekendEnabled = true;
        public bool WeekendEnabled
        {
            get => _weekendEnabled;
            set
            {
                _weekendEnabled = value;
                UpdateAllRadioButtonEnable();
            }
        }

        #region constructor

        public TimePickerControl()
        {
            InitializeComponent();

            DateTime now = DateTime.Now;
            DateTime dateTime = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0);

            SetDateTime(dateTime);
        }

        #endregion

        #region public method

        public void SetMonth(int year, int month, bool force = false)
        {
            if (!force && year == currentYear && month == currentMonth) return;
            if (year > endYear || year < startYear) return;

            gridDay.Children.Clear();

            btnCurrentMonth.Content = $"{monthList[month - 1]} {year}";
            currentYear = year;
            currentMonth = month;

            if (!dict.ContainsKey($"{currentYear}-{currentMonth}"))
            {
                CreateMonthButtons(currentYear, currentMonth);
            }

            foreach (var button in dict[$"{currentYear}-{currentMonth}"])
            {
                gridDay.Children.Add(button);
            }

            CubicEase cubicEase = new CubicEase();
            cubicEase.EasingMode = EasingMode.EaseOut;

            DoubleAnimation doubleAnimation = new DoubleAnimation();
            doubleAnimation.EasingFunction = cubicEase;
            doubleAnimation.From = 0;
            doubleAnimation.To = 1;
            doubleAnimation.Duration = new Duration(TimeSpan.FromMilliseconds(800));

            gridDay.BeginAnimation(OpacityProperty, doubleAnimation);
        }

        public void AddMonth(int months)
        {
            int year = currentYear;
            int month = currentMonth + months;

            if (month > 12)
            {
                year += month / 12;
                month %= 12;
            }
            else if (month <= 0)
            {
                int minusYears = -month / 12 + 1;
                year -= minusYears;
                month += 12 * minusYears;
            }

            SetMonth(year, month);
        }

        public void AddDay(int days)
        {
            DateTime dateTime = (DateTime)selectedRadioButton.Tag;
            dateTime = dateTime.AddDays(days);
            SetDay(dateTime.Year, dateTime.Month, dateTime.Day);
        }

        public void SetDay(int year, int month, int day)
        {
            if (year > endYear || year < startYear) return;

            if (DateTime.TryParse($"{year}-{month}-{day}", out DateTime dateTime))
            {
                string key = $"{year}-{month}";

                if (!dict.ContainsKey(key))
                {
                    CreateMonthButtons(year, month);
                }

                RadioButton? rb = dict[key].FirstOrDefault(i => (DateTime)i.Tag == dateTime);
                if (rb?.IsEnabled == true)
                {
                    SetMonth(year, month);
                    rb.IsChecked = true;
                }
            }
            else
            {
                throw new ArgumentException($"{year}-{month}-{day} can not parse to DateTime");
            }
        }

        public void SetTime(int hour, int minute, int second)
        {
            if (DateTime.TryParse($"{hour}:{minute}:{second}", out DateTime time))
            {
                tbCurrentTime.Text = time.ToString("HH:mm");
            }
            else
            {
                throw new ArgumentException();
            }
        }

        public void SetDateTime(DateTime dateTime)
        {
            SetDay(dateTime.Year, dateTime.Month, dateTime.Day);
            SetTime(dateTime.Hour, dateTime.Minute, dateTime.Second);
        }

        public bool TryGetTime(out DateTime time)
        {
            if (selectedRadioButton != null)
            {
                DateTime dateTime = (DateTime)selectedRadioButton.Tag;
                if (DateTime.TryParse($"{dateTime.Date:yyyy-MM-dd} {tbCurrentTime.Text}", out var result))
                {
                    time = result;
                    return true;
                }
            }
            time = DateTime.Now;
            return false;
        }

        public void SetEnabledTimeRange(DateTime startTime, DateTime endTime)
        {
            if (startTime > endTime)
            {
                throw new ArgumentException("endTime must be larger than startTime");
            }

            enabledTimeRange.Add(new Tuple<DateTime, DateTime>(startTime, endTime));
            UpdateAllRadioButtonEnable();
        }

        public void SetDisableTimeRange(DateTime startTime, DateTime endTime)
        {
            if (startTime > endTime)
            {
                throw new ArgumentException("endTime must be larger than startTime");
            }

            disableTimeRange.Add(new Tuple<DateTime, DateTime>(startTime, endTime));
            UpdateAllRadioButtonEnable();
        }

        #endregion

        #region private method

        private void UpdateAllRadioButtonEnable()
        {
            foreach (List<RadioButton> rbs in dict.Values)
            {
                foreach (RadioButton rb in rbs)
                {
                    UpdateRadioButtonEnable(rb);
                }
            }
        }

        private void UpdateRadioButtonEnable(RadioButton rb)
        {
            rb.IsEnabled = false;
            DateTime dateTime = (DateTime)rb.Tag;

            if (enabledTimeRange.Count > 0)
            {
                foreach (var item in enabledTimeRange)
                {
                    if (dateTime >= item.Item1 && dateTime <= item.Item2)
                    {
                        if (dateTime.DayOfWeek == DayOfWeek.Saturday || dateTime.DayOfWeek == DayOfWeek.Sunday)
                            rb.IsEnabled = WeekendEnabled;
                        else
                            rb.IsEnabled = true;
                        break;
                    }
                }
            }
            else
            {
                if (dateTime.DayOfWeek == DayOfWeek.Saturday || dateTime.DayOfWeek == DayOfWeek.Sunday)
                    rb.IsEnabled = WeekendEnabled;
                else
                    rb.IsEnabled = true;
            }

            if (disableTimeRange.Count > 0)
            {
                foreach (var item in disableTimeRange)
                {
                    if (dateTime >= item.Item1 && dateTime <= item.Item2)
                    {
                        rb.IsEnabled = false;
                        break;
                    }
                }
            }
        }

        private void CreateMonthButtons(int year, int month)
        {
            if (year > endYear || year < startYear) return;

            List<RadioButton> rbs = new List<RadioButton>();
            DateTime startTime = new DateTime(year, month, 1);
            DateTime endTime = startTime.AddMonths(1);
            int row = 0;
            int col = (int)startTime.DayOfWeek;

            for (; startTime < endTime; startTime = startTime.AddDays(1))
            {
                RadioButton rb = CreateButton(startTime.Day, row, col % 7);

                rb.Checked += Rb_Checked;
                rb.Tag = startTime;

                if (startTime.Date == DateTime.Now.Date)
                {
                    rb.Style = (Style)FindResource("CalendarTodayButton");
                }

                rbs.Add(rb);
                col++;
                if (startTime.DayOfWeek == DayOfWeek.Saturday)
                {
                    row++;
                }
            }

            dict[$"{year}-{month}"] = rbs;
            UpdateAllRadioButtonEnable();
        }

        private RadioButton CreateButton(object contennt, int row, int col)
        {
            RadioButton rb = new RadioButton();
            rb.Content = contennt;
            rb.Style = (Style)FindResource("CalendarButton");
            rb.Width = 34;
            rb.Height = 34;
            rb.Margin = new Thickness(0, 2, 0, 2);

            Grid.SetRow(rb, row);
            Grid.SetColumn(rb, col);

            return rb;
        }

        private void Rb_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb != selectedRadioButton)
            {
                DateTime currentDate = (DateTime)rb.Tag;

                tbCurrentDate.Text = currentDate.ToString("yyyy-MM-dd");
                tbCurrentDate.CaretIndex = tbCurrentDate.Text.Length;

                if (selectedRadioButton != null)
                {
                    selectedRadioButton.IsChecked = false;
                }
                selectedRadioButton = rb;
            }
        }

        private void btnPreMonth_Click(object sender, RoutedEventArgs e)
        {
            switch (mode)
            {
                case Mode.Day:
                    AddMonth(-1);
                    break;
                case Mode.Month:
                    currentYear--;
                    btnCurrentMonth.Content = currentYear;
                    break;
            }
        }

        private void btnNextMonth_Click(object sender, RoutedEventArgs e)
        {
            switch (mode)
            {
                case Mode.Day:
                    AddMonth(1);
                    break;
                case Mode.Month:
                    currentYear++;
                    btnCurrentMonth.Content = currentYear;
                    break;
            }
        }

        private void tbCurrentDate_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9 -]+");
            e.Handled = regex.IsMatch(e.Text) || tbCurrentDate.Text.Length >= 10;
        }

        private void tbCurrentDate_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                string text = textBox.Text;
                if (!CheackDateStringValid(text))
                {
                    int preCaretIndex = textBox.CaretIndex;
                    textBox.Text = preDateString;
                    textBox.CaretIndex = preCaretIndex;
                    return;
                }

                Regex regex = new Regex("^[0-9]{4}$");

                if (regex.IsMatch(text) && preDateString.Length < text.Length)
                {
                    textBox.Text += "-";
                    textBox.CaretIndex = textBox.Text.Length;
                }

                regex = new Regex("^[0-9]{4}-[0-9]{2}$");
                if (regex.IsMatch(text) && preDateString.Length < text.Length)
                {
                    textBox.Text += "-";
                    textBox.CaretIndex = textBox.Text.Length;
                }

                regex = new Regex("^[0-9]{4}-[0-9]{2}-[0-9]{2}$");
                if (regex.IsMatch(text) &&
                    DateTime.TryParse(text, out DateTime dateTime))
                {
                    mode = Mode.Day;
                    borderDayTitle.Visibility = Visibility.Visible;
                    gridDay.Visibility = Visibility.Visible;
                    gridMonth.Visibility = Visibility.Collapsed;
                    SetDay(dateTime.Year, dateTime.Month, dateTime.Day);
                }

                preDateString = text;
            }
        }

        private bool CheackDateStringValid(string date)
        {
            if (date.Contains("--")) return false;

            Regex regex = new Regex("^[0-9]{0,4}$");
            if (regex.IsMatch(date)) return true;

            regex = new Regex("^[0-9]{0,4}-[0-9]{0,2}$");
            if (regex.IsMatch(date)) return true;

            regex = new Regex("^[0-9]{0,4}-[0-9]{0,2}-[0-9]{0,2}$");
            if (regex.IsMatch(date)) return true;

            return false;
        }

        private void tbCurrentDate_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up)
            {
                AddDay(1);
            }
            if (e.Key == Key.Down)
            {
                AddDay(-1);
            }
        }

        private void tbCurrentTime_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9 :]+");
            e.Handled = regex.IsMatch(e.Text) || tbCurrentTime.Text.Length >= 8;
        }

        private void tbCurrentTime_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                string text = textBox.Text;
                if (!CheackTimeStringValid(text))
                {
                    int preCaretIndex = textBox.CaretIndex;
                    textBox.Text = preTimeString;
                    textBox.CaretIndex = preCaretIndex;
                    return;
                }

                Regex regex = new Regex("^[0-9]{2}$");

                if (regex.IsMatch(text) && preTimeString.Length < text.Length)
                {
                    textBox.Text += ":";
                    textBox.CaretIndex = textBox.Text.Length;
                }

                preTimeString = text;
            }
        }

        private bool CheackTimeStringValid(string date)
        {
            if (date.Contains("::")) return false;

            Regex regex = new Regex("^[0-9]{0,2}$");
            if (regex.IsMatch(date)) return true;

            regex = new Regex("^[0-9]{0,2}:[0-9]{0,2}$");
            if (regex.IsMatch(date)) return true;

            regex = new Regex("^[0-9]{0,2}:[0-9]{0,2}:[0-9]{0,2}$");
            if (regex.IsMatch(date)) return true;

            return false;
        }

        #endregion

        private void btnCurrentMonth_Click(object sender, RoutedEventArgs e)
        {
            mode = Mode.Month;
            borderDayTitle.Visibility = Visibility.Collapsed;
            gridDay.Visibility = Visibility.Collapsed;
            gridMonth.Visibility = Visibility.Visible;
            btnCurrentMonth.Content = currentYear;
        }

        private void btnMonth_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string month)
            {
                mode = Mode.Day;
                borderDayTitle.Visibility = Visibility.Visible;
                gridDay.Visibility = Visibility.Visible;
                gridMonth.Visibility = Visibility.Collapsed;
                SetMonth(currentYear, int.Parse(month), true);
            }
        }

        private enum Mode
        {
            Day,
            Month,
            Year
        }
    }
}

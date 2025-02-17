using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using LiveCharts;
using LiveCharts.Wpf;

namespace Covid
{
    public partial class MainWindow : Window
    {
        private Random random = new Random(); // Генератор случайных чисел
        private List<Person> people = new List<Person>(); // Список людей в симуляции
        private Timer simulationTimer; // Таймер для обновления симуляции
        private int day = 0; // Счётчик дней в симуляции

        // Данные для графиков
        public ChartValues<int> SusceptibleValues { get; set; } = new ChartValues<int>();
        public ChartValues<int> InfectedValues { get; set; } = new ChartValues<int>();
        public ChartValues<int> RemovedValues { get; set; } = new ChartValues<int>();
        public ChartValues<int> DeadValues { get; set; } = new ChartValues<int>();
        public ChartValues<int> InfectedButNoValues { get; set; } = new ChartValues<int>();


        // Параметры симуляции
        public int InfectionProbability { get; set; } = 50; // Вероятность заражения (в процентах)
        public int InfectionDuration { get; set; } = 14; // Длительность болезни (в днях)
        public int ASInfection { get; set; } = 5; // Вероятность бессимптомного заражения (в процентах)
        public int QuarantineDuration { get; set; } = 30; // Длительность карантина (в днях)
        public int SocialDistancing { get; set; } = 75; // Уровень социального дистанцирования (в процентах)

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
        }
        // Обработчики событий для изменения параметров 
        private void CPBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            InfectionProbability = int.Parse(((ComboBoxItem)CPBox.SelectedItem).Tag.ToString());
        }
        private void DTBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            InfectionDuration = int.Parse(((ComboBoxItem)DTBox.SelectedItem).Tag.ToString());
        }
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ASInfection = int.Parse(((ComboBoxItem)ASBox.SelectedItem).Tag.ToString());
        }
        private void IPBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            QuarantineDuration = int.Parse(((ComboBoxItem)IPBox.SelectedItem).Tag.ToString());
        }
        private void SDBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SocialDistancing = int.Parse(((ComboBoxItem)SDBox.SelectedItem).Tag.ToString());
        }
        private void StartSimulation_Click(object sender, RoutedEventArgs e)
        {
            InitializeSimulation(); // Инициализация симуляции
            simulationTimer = new Timer(700); // Запуск таймера с интервалом 700 мс
            simulationTimer.Elapsed += (s, args) => Dispatcher.Invoke(UpdateSimulation);
            simulationTimer.Start();
        }
        // Инициализация людей в городе
        private void InitializeSimulation()
        {
            CityCanvas.Children.Clear();
            QuarantineCanvas.Children.Clear();
            people.Clear();
            SusceptibleValues.Clear();
            InfectedValues.Clear();
            RemovedValues.Clear();
            DeadValues.Clear();
            day = 0;

            int population = int.Parse(PopulationBox.Text);
            if(population >= 500 &&  population <= 4000)
            {
                for (int i = 0; i < population; i++)
                {
                    var person = new Person
                    {
                        X = random.Next((int)CityCanvas.ActualWidth),
                        Y = random.Next((int)CityCanvas.ActualHeight),
                        State = i == 0 ? State.Infected : State.Susceptible,  // Первый человек сразу заражен
                        InfectedDays = 0  // Начальный счётчик дней заражения
                    };
                    people.Add(person);
                    DrawPerson(person);
                }
            }
       
        }
        // Проверка, находится ли человек слишком близко к другому
        private bool IsTooClose(Person person)
        {
            double safeDistance = 2 * QuarantineDuration;
            foreach (var other in people)
            {
                if (other != person)
                {
                    double distance = Math.Sqrt(Math.Pow(person.X - other.X, 2) + Math.Pow(person.Y - other.Y, 2));
                    if (distance < safeDistance)
                    {
                        return true; // Слишком близко к другому человеку
                    }
                }
            }
            return false;
        }
        // Обновление состояния симуляции
        private void UpdateSimulation()
        {
            day++;

            // Перемещаем людей по канвасу
            foreach (var person in people)
            {
                if (person.State == State.Susceptible || person.State == State.Infected || person.State == State.Recovered || person.State == State.InfectedButNo)
                {
                    bool ignoresIsolation = random.Next(100) < (100 - SocialDistancing);

                    if(ignoresIsolation || !IsTooClose(person))
                    {
                        person.X += random.Next(-5, 6);
                        person.Y += random.Next(-5, 6);
                    }                
                }
            }

            // Заражение людей
            for (int i = 0; i < people.Count; i++)
            {
                if (people[i].State == State.Infected || people[i].State == State.InfectedButNo)
                {
                    foreach (var other in people)
                    {
                        if (other.State == State.Susceptible &&
                            Math.Abs(people[i].X - other.X) < 15 &&  // Радиус заражения 15
                            Math.Abs(people[i].Y - other.Y) < 15)  // Проверка расстояния
                        {
                            // Применение вероятности заражения
                            int infectionChance = InfectionProbability; 
                            int infectionNo = ASInfection;


                            if (random.Next(0, 101) <= infectionChance)
                            {
                                other.State = State.Infected; // Заражаем
                                other.InfectedDays = 0;  // Счётчик дней заражения обнуляется

                                if(random.Next(0,101) <= ASInfection) //Добавляем невыявленные случаи
                                {
                                    other.State = State.InfectedButNo;
                                }
                            }
                        }
                    }
                }
            }


            // Обработка состояния инфицированных
            foreach (var person in people)
            {
                if (person.State == State.Infected || person.State == State.InfectedButNo)
                {
                    // Увеличиваем количество дней заражения
                    person.InfectedDays++;

                    if (person.InfectedDays >= InfectionDuration)
                    {
                        // После истечения времени заражения проверяем вероятность выздоровления или смерти
                        if (random.NextDouble() < 0.12) // Вероятность смерти
                        {
                            person.State = State.Dead;
                        }
                        else // Вероятность выздоровления
                        {
                            person.State = State.Recovered;
                        }
                    }
                }
            }

            // Подсчёт людей в разных состояниях
            int s = people.Count(p => p.State == State.Susceptible);
            int q = people.Count(p => p.State == State.Infected);
            int r = people.Count(p => p.State == State.Recovered);
            int d = people.Count(p => p.State == State.Dead);
            int z = people.Count(p => p.State == State.InfectedButNo);

            s += z;

            SusceptibleValues.Add(s);
            InfectedValues.Add(q);
            RemovedValues.Add(r);
            DeadValues.Add(d);

            if(q > 50)
            {
                foreach(var person in people.Where(p => p.State == State.Infected && !p.InQuarantine))
                {
                    person.InQuarantine = true;
                    person.DaysInQuarantine = 0;
                    person.X = random.Next((int)QuarantineCanvas.ActualWidth);
                    person.Y = random.Next((int)QuarantineCanvas.ActualHeight);
                }
            }
            foreach (var person in people.Where(p => p.InQuarantine))
            {
                person.DaysInQuarantine++;

                if (person.DaysInQuarantine >= QuarantineDuration) // Если дни карантина прошли
                {
                    if (random.NextDouble() < 0.12) // 12% вероятность смерти
                    {
                        person.State = State.Dead;
                    }
                    else
                    {
                        person.State = State.Recovered;
                        person.InQuarantine = false;
                        person.X = random.Next((int)CityCanvas.ActualWidth);
                        person.Y = random.Next((int)CityCanvas.ActualHeight);
                    }
                }
            }
            Redraw();
        }

        private void Redraw()
        {
            CityCanvas.Children.Clear();
            QuarantineCanvas.Children.Clear();
            foreach (var person in people)
            {
                DrawPerson(person);
            }
        }

        private void DrawPerson(Person person)
        {
            Ellipse ellipse = new Ellipse
            {
                Width = 5,
                Height = 5,
                Fill = person.State == State.Susceptible ? Brushes.Blue :
                       person.State == State.Infected ? Brushes.Red :
                       person.State == State.InfectedButNo ? Brushes.Blue :
                       person.State == State.Recovered ? Brushes.Green :
                       Brushes.Gray
            };

            if(person.InQuarantine)
            {
                Canvas.SetLeft(ellipse, person.X);
                Canvas.SetTop(ellipse, person.Y);
                QuarantineCanvas.Children.Add(ellipse);
            }
            else
            {
                Canvas.SetLeft(ellipse, person.X);
                Canvas.SetTop(ellipse, person.Y);
                CityCanvas.Children.Add(ellipse);
            }
          
        }
    }

    public class Person
    {
        public double X { get; set; }
        public double Y { get; set; }
        public State State { get; set; }
        public int InfectedDays { get; set; } 
        public int DaysInQuarantine {  get; set; } = 0;
        public bool InQuarantine { get; set;} = false;
    }

    public enum State
    {
        Susceptible,
        Infected,
        Recovered,
        Dead,
        InfectedButNo
    }
}

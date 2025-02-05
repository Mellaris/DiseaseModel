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
        private Random random = new Random();
        private List<Person> people = new List<Person>();
        private Timer simulationTimer;
        private int day = 0;

        public ChartValues<int> SusceptibleValues { get; set; } = new ChartValues<int>();
        public ChartValues<int> InfectedValues { get; set; } = new ChartValues<int>();
        public ChartValues<int> RemovedValues { get; set; } = new ChartValues<int>();
        public ChartValues<int> DeadValues { get; set; } = new ChartValues<int>();

        public double InfectionProbability { get; set; } = 0.5;  // Начальная вероятность заражения (50%)
        public int InfectionDuration { get; set; } = 14;  // Длительность болезни в днях (например, 14 дней)

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void StartSimulation_Click(object sender, RoutedEventArgs e)
        {
            InitializeSimulation();
            simulationTimer = new Timer(500);  // Обновление раз в 5 секунд для более медленного времени
            simulationTimer.Elapsed += (s, args) => Dispatcher.Invoke(UpdateSimulation);
            simulationTimer.Start();
        }

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

        private void UpdateSimulation()
        {
            day++;

            // Перемещаем людей по канвасу
            foreach (var person in people)
            {
                if (person.State == State.Susceptible || person.State == State.Infected)
                {
                    person.X += random.Next(-5, 6);
                    person.Y += random.Next(-5, 6);
                }
            }

            // Заражение людей
            for (int i = 0; i < people.Count; i++)
            {
                if (people[i].State == State.Infected)
                {
                    foreach (var other in people)
                    {
                        if (other.State == State.Susceptible &&
                            Math.Abs(people[i].X - other.X) < 15 &&  // Радиус заражения 15
                            Math.Abs(people[i].Y - other.Y) < 15)  // Проверка расстояния
                        {
                            // Применение вероятности заражения
                            double infectionChance = InfectionProbability; // Можно добавить логику на основе других факторов

                            if (random.NextDouble() < infectionChance)
                            {
                                other.State = State.Infected; // Заражаем
                                other.InfectedDays = 0;  // Счётчик дней заражения обнуляется
                            }
                        }
                    }
                }
            }


            // Обработка состояния инфицированных
            foreach (var person in people)
            {
                if (person.State == State.Infected)
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

            SusceptibleValues.Add(s);
            InfectedValues.Add(q);
            RemovedValues.Add(r);
            DeadValues.Add(d);

            Redraw();
        }

        private void Redraw()
        {
            CityCanvas.Children.Clear();
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
                       person.State == State.Recovered ? Brushes.Green :
                       Brushes.Gray
            };

            Canvas.SetLeft(ellipse, person.X);
            Canvas.SetTop(ellipse, person.Y);
            CityCanvas.Children.Add(ellipse);
        }
    }

    public class Person
    {
        public double X { get; set; }
        public double Y { get; set; }
        public State State { get; set; }
        public int InfectedDays { get; set; }  // Количество дней, в течение которых человек болеет
    }

    public enum State
    {
        Susceptible,
        Infected,
        Recovered,
        Dead
    }
}














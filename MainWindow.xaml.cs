using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

namespace Covid
{
    public enum PersonState
    {
        Susceptible,
        Infectious,
        Removed
    }

    public class Person
    {
        public double X { get; set; }
        public double Y { get; set; }
        public int DX { get; set; }
        public int DY { get; set; }
        public PersonState State { get; set; }
        public int DaysInfected { get; set; }
    }

    public partial class MainWindow : Window
    {
        private const int CitySize = 600; // Размер города в пикселях
        private List<Person> population = new List<Person>();
        private Random random = new Random();
        private DispatcherTimer timer = new DispatcherTimer();

        // Настройки
        private int populationCount = 500;
        private double infectionProbability = 0.1; // 10%
        private int infectionRadius = 15;
        private int diseaseDuration = 14; // 14 дней

        public MainWindow()
        {
            InitializeComponent();
            timer.Interval = TimeSpan.FromMilliseconds(50); // Обновление каждые 50 мс
            timer.Tick += Timer_Tick;
        }

        private void StartSimulation_Click(object sender, RoutedEventArgs e)
        {
            // Получаем данные из интерфейса
            int count;
            if (int.TryParse(PopulationInput.Text, out count) && count >= 12000 && count <= 100000)
            {
                populationCount = count;
            }
            else
            {
                MessageBox.Show("Введите корректное число жителей (от 12000 до 100000).");
                return;
            }

            if (InfectionProbabilityInput.SelectedIndex == 0)
                infectionProbability = 0.1;
            else if (InfectionProbabilityInput.SelectedIndex == 1)
                infectionProbability = 0.2;
            else if (InfectionProbabilityInput.SelectedIndex == 2)
                infectionProbability = 0.3;

            int radius;
            if (int.TryParse(InfectionRadiusInput.Text, out radius) && radius > 0)
            {
                infectionRadius = radius;
            }
            else
            {
                MessageBox.Show("Введите корректный радиус заражения.");
                return;
            }

            if (DiseaseDurationInput.SelectedIndex == 0)
                diseaseDuration = 14;
            else if (DiseaseDurationInput.SelectedIndex == 1)
                diseaseDuration = 21;
            else if (DiseaseDurationInput.SelectedIndex == 2)
                diseaseDuration = 38;

            InitializeCity(populationCount);
        }

        private void InitializeCity(int populationCount)
        {
            population.Clear();
            CityCanvas.Children.Clear();

            for (int i = 0; i < populationCount; i++)
            {
                population.Add(new Person
                {
                    X = random.Next(0, CitySize),
                    Y = random.Next(0, CitySize),
                    DX = random.Next(-2, 3),
                    DY = random.Next(-2, 3),
                    State = i == 0 ? PersonState.Infectious : PersonState.Susceptible, // Один заражённый
                    DaysInfected = 0
                });
            }

            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            UpdateCity();
            UpdateCityCanvas();
        }

        private void UpdateCity()
        {
            foreach (var person in population.ToList())
            {
                // Движение
                person.X += person.DX;
                person.Y += person.DY;

                if (person.X < 0 || person.X > CitySize)
                {
                    person.DX = -person.DX;
                    person.X = Math.Max(0, Math.Min(person.X, CitySize));
                }
                if (person.Y < 0 || person.Y > CitySize)
                {
                    person.DY = -person.DY;
                    person.Y = Math.Max(0, Math.Min(person.Y, CitySize));
                }

                // Логика заражения
                if (person.State == PersonState.Infectious)
                {
                    person.DaysInfected++;
                    if (person.DaysInfected >= diseaseDuration)
                    {
                        person.State = PersonState.Removed;
                    }
                }

                if (person.State == PersonState.Susceptible)
                {
                    foreach (var other in population)
                    {
                        if (other.State == PersonState.Infectious)
                        {
                            double distance = Math.Sqrt(Math.Pow(person.X - other.X, 2) + Math.Pow(person.Y - other.Y, 2));
                            if (distance < infectionRadius && random.NextDouble() < infectionProbability)
                            {
                                person.State = PersonState.Infectious;
                                break;
                            }
                        }
                    }
                }
            }
        }

        private void UpdateCityCanvas()
        {
            CityCanvas.Children.Clear();

            foreach (var person in population)
            {
                Ellipse ellipse = new Ellipse
                {
                    Width = 5,
                    Height = 5,
                    Fill = person.State == PersonState.Susceptible ? Brushes.Blue :
                           person.State == PersonState.Infectious ? Brushes.Red : Brushes.Green
                };

                Canvas.SetLeft(ellipse, person.X);
                Canvas.SetTop(ellipse, person.Y);
                CityCanvas.Children.Add(ellipse);
            }
        }
    }
}

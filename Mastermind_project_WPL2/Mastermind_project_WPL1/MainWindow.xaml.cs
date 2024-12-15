using System.Drawing;
using System.Text;
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

namespace Mastermind_project_WPL1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private int attempts = 1;
        private string targetColorCode;
        private bool debugMode = false;
        private DispatcherTimer timer;
        private int remainingTime = 10;

        private string[] highscores = new string[15];
        private int highscoreCount = 0;

        private List<string> playerNames;
        private int currentPlayerIndex = 0;

        public MainWindow()
        {
            InitializeComponent();

            // Start het spel
            playerNames = startGame();

            // Initialiseer de timer
            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timer.Tick += Timer_Tick;

            targetColorCode = generateRandomColorCode();
            debugTextBox.Text = targetColorCode;
            updateWindowTitle();

            // Vul de comboBoxen met kleuren
            string[] colors = { "Rood", "Geel", "Oranje", "Wit", "Groen", "Blauw" };
            comboBox1.ItemsSource = colors;
            comboBox2.ItemsSource = colors;
            comboBox3.ItemsSource = colors;
            comboBox4.ItemsSource = colors;

            // Event handlers voor kleurenselectie
            comboBox1.SelectionChanged += (s, e) => updateLabel(label1, comboBox1.SelectedItem.ToString());
            comboBox2.SelectionChanged += (s, e) => updateLabel(label2, comboBox2.SelectedItem.ToString());
            comboBox3.SelectionChanged += (s, e) => updateLabel(label3, comboBox3.SelectedItem.ToString());
            comboBox4.SelectionChanged += (s, e) => updateLabel(label4, comboBox4.SelectedItem.ToString());

            // Sneltoets voor debug-modus
            this.KeyDown += MainWindow_KeyDown;

            this.Closing += MainWindow_Closing;


            // Start de timer bij de eerste codegeneratie
            startCountdown();
        }

        // Sneltoets-event voor debug-modus
        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F12 && Keyboard.Modifiers == ModifierKeys.Control)
            {
                toggleDebugMode();
            }
        }

        /// <summary>
        /// Schakelt de debug-modus in of uit.
        /// In debug-modus wordt de gegenereerde kleurencode zichtbaar in de debugTextBox.
        /// Activering gebeurt via de sneltoets CTRL + F12.
        /// </summary>
        private void toggleDebugMode()
        {
            debugMode = !debugMode;
            debugTextBox.Visibility = debugMode ? Visibility.Visible : Visibility.Collapsed;
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Vraag bevestiging
            MessageBoxResult result = MessageBox.Show(
                "Weet je zeker dat je het spel wilt beëindigen?",
                "Applicatie afsluiten",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );

            // Controleer de keuze van de gebruiker
            if (result == MessageBoxResult.No)
            {
                // Annuleer het sluitproces
                e.Cancel = true;
            }
        }


        // Methode om random kleurencode te genereren
        private string generateRandomColorCode()
        {
            // Beschikbare kleuren
            string[] colors = { "Rood", "Geel", "Oranje", "Wit", "Groen", "Blauw" };
            Random random = new Random();
            string[] randomColors = new string[4];

            // Genereer vier willekeurige kleuren
            for (int i = 0; i < 4; i++)
            {
                randomColors[i] = colors[random.Next(colors.Length)];
            }

            // Combineer de kleuren met een scheidingsteken
            return string.Join(" - ", randomColors);
        }

        // Methode om de achtergrondkleur te tonen
        private void updateLabel(Label label, string color)
        {
            label.Content = color;
            label.Background = color switch
            {
                "Rood" => Brushes.Red,
                "Geel" => Brushes.Yellow,
                "Oranje" => Brushes.Orange,
                "Wit" => Brushes.White,
                "Groen" => Brushes.Green,
                "Blauw" => Brushes.Blue,
                _ => Brushes.Transparent
            };
        }

        private void checkButton_Click(object sender, RoutedEventArgs e)
        {
            // Haal de geselecteerde kleuren op uit de comboBoxen
            string[] selectedColors = {
                comboBox1.SelectedItem?.ToString(),
                comboBox2.SelectedItem?.ToString(),
                comboBox3.SelectedItem?.ToString(),
                comboBox4.SelectedItem?.ToString()
            };

            // Haal de gegenereerde kleurencode op
            string[] targetColors = targetColorCode.Split(" - ");

            string currentPlayer = playerNames[currentPlayerIndex];
            string nextPlayer = playerNames[((currentPlayerIndex + 1) % playerNames.Count)];

            // Controleer of de geselecteerde kleuren overeenkomen met de gegenereerde code
            if (selectedColors.SequenceEqual(targetColors))
            {
                MessageBox.Show($"Gefeliciteerd! Je hebt de code gekraakt in {attempts} pogingen. \nNu is speler {nextPlayer} aan de beurt.", currentPlayer);
                currentPlayerIndex = (currentPlayerIndex + 1) % playerNames.Count;
                resetGame();
                targetColorCode = generateRandomColorCode();
                debugTextBox.Text = targetColorCode;
                return;
            }

            calculateScore(selectedColors, targetColors);

            // Maak een array van booleans om bij te houden welke kleuren al als correct gemarkeerd zijn
            bool[] correctPositions = new bool[4];
            Array.Fill(correctPositions, false);

            // Feedback berekenen
            int correctPosition = 0;
            int correctColor = 0;
            bool[] matched = new bool[4];

            for (int i = 0; i < 4; i++)
            {
                if (selectedColors[i] == targetColors[i])
                {
                    correctPosition++;
                    matched[i] = true;
                }
            }

            for (int i = 0; i < 4; i++)
            {
                if (!matched[i] && targetColors.Contains(selectedColors[i]) && selectedColors[i] != targetColors[i])
                {
                    correctColor++;
                }
            }

            // Voeg poging toe aan de lijst
            addAttemptToList(selectedColors, correctPosition, correctColor);

            // Controleer de geselecteerde kleuren en pas de randkleur aan
            updateBorder(label1, selectedColors[0], targetColors, 0, ref correctPositions);
            updateBorder(label2, selectedColors[1], targetColors, 1, ref correctPositions);
            updateBorder(label3, selectedColors[2], targetColors, 2, ref correctPositions);
            updateBorder(label4, selectedColors[3], targetColors, 3, ref correctPositions);

            // Verhoog het aantal pogingen en update de titel
            attempts++;
            updateWindowTitle();

            // Start de timer opnieuw bij een poging
            startCountdown();
        }

        private void addAttemptToList(string[] selectedColors, int correctPosition, int correctColor)
        {
            // Voeg poging en feedback toe aan de ListBox
            string attempt = $"Poging {attempts}: {string.Join(", ", selectedColors)} | Rood: {correctPosition} | Wit: {correctColor}";
            attemptsListBox.Items.Add(attempt);
        }

        // Methode om de randen de juiste kleur te geven op basis van de ingevulde kleur
        private void updateBorder(Label label, string selectedColor, string[] targetColors, int index, ref bool[] correctPositions)
        {
            if (selectedColor == null)
            {
                label.BorderBrush = Brushes.Transparent;
                label.BorderThickness = new Thickness(0);
                return;
            }

            // Controleer of de kleur op de juiste plaats staat
            if (targetColors[index] == selectedColor)
            {
                label.BorderBrush = Brushes.Red;
                label.BorderThickness = new Thickness(4);
                label.ToolTip = "Juiste kleur, juiste positie";
                correctPositions[index] = true;
            }
            else if (Array.Exists(targetColors, color => color == selectedColor))
            {
                // Controleer of de kleur ergens anders voorkomt en nog niet als correct is gemarkeerd
                bool alreadyMarked = false;

                for (int i = 0; i < targetColors.Length; i++)
                {
                    if (targetColors[i] == selectedColor && !correctPositions[i])
                    {
                        alreadyMarked = true;
                        break;
                    }
                }

                if (alreadyMarked)
                {
                    label.BorderBrush = Brushes.Yellow;
                    label.BorderThickness = new Thickness(4);
                    label.ToolTip = "Juiste kleur, foute positie";
                }
                else
                {
                    label.BorderBrush = Brushes.Transparent;
                    label.BorderThickness = new Thickness(0);
                    label.ToolTip = "Foute kleur";
                }
            }
            else
            {
                label.BorderBrush = Brushes.Transparent;
                label.BorderThickness = new Thickness(0);
                label.ToolTip = "Foute kleur";
            }
        }

        private int calculateScore(string[] selectedColors, string[] targetColors)
        {
            int totalScore = 0;

            // Boolean array om bij te houden welke kleuren al correct of incorrect zijn gemarkeerd
            bool[] matched = new bool[4];
            Array.Fill(matched, false);

            // Strafpunten berekenen
            for (int i = 0; i < 4; i++)
            {
                if (selectedColors[i] == targetColors[i])
                {
                    // 0 strafpunten voor een correcte kleur op de juiste plaats
                    matched[i] = true;
                }
                else if (targetColors.Contains(selectedColors[i]) && !matched[i])
                {
                    // 1 strafpunt voor een correcte kleur op de verkeerde plaats
                    totalScore += 1;
                }
                else
                {
                    // 2 strafpunten voor een kleur die niet voorkomt in de code
                    totalScore += 2;
                }
            }

            // Score weergeven in het label
            scoreLabel.Content = $"Score: {totalScore} strafpunten";
            return totalScore;
        }


        // Methode om de window title te updaten
        private void updateWindowTitle()
        {
            string currentPlayer = playerNames[currentPlayerIndex];
            string nextPlayer = playerNames[((currentPlayerIndex + 1) % playerNames.Count)];

            if (attempts > 10)
            {
                MessageBox.Show($"Je hebt verloren! De code was: {targetColorCode}. \nNu is het {nextPlayer} zijn beurt.", currentPlayer);
                currentPlayerIndex = (currentPlayerIndex + 1) % playerNames.Count;
                resetGame();
                generateRandomColorCode();
                return;
            }
            else
            {
                this.Title = $"Poging {attempts} - Tijd: {remainingTime} seconden - Actieve Speler: {playerNames[currentPlayerIndex]}";
            }
        }

        // Timer Tick-event
        private void Timer_Tick(object sender, EventArgs e)
        {
            remainingTime--;

            if (remainingTime <= 0)
            {
                stopCountdown();
            }

            // Controleer of het spel al gewonnen is
            if (this.IsActive == false)
            {
                timer.Stop();
                return;
            }

            updateWindowTitle();
        }

        /// <summary>
        /// Start de countdown-timer van 10 seconden.
        /// Wordt aangeroepen telkens wanneer een nieuwe poging begint of wanneer een nieuwe code wordt gegenereerd.
        /// Reset de resterende tijd en activeert de timer om het Tick-event te verwerken.
        /// </summary>
        private void startCountdown()
        {
            remainingTime = 10;
            timer.Start();
        }

        /// <summary>
        /// Stopt de countdown-timer wanneer de tijd op is.
        /// Wordt automatisch aangeroepen door het Tick-event van de timer.
        /// Informeert de speler dat de beurt verloren is en verhoogt het aantal pogingen.
        /// </summary>
        private void stopCountdown()
        {
            string currentPlayer = playerNames[currentPlayerIndex];
            string nextPlayer = playerNames[((currentPlayerIndex + 1) % playerNames.Count)];

            if (attempts > 10)
            {
                MessageBox.Show($"Je hebt verloren! De code was: {targetColorCode}. \nNu is het {nextPlayer} zijn beurt.", currentPlayer);
                currentPlayerIndex = (currentPlayerIndex + 1) % playerNames.Count;
                resetGame();
                generateRandomColorCode();
                return;
            }
            else
            {
                timer.Stop();
                MessageBox.Show("Tijd voorbij! Je hebt je beurt verloren.", "Beurt Verloren", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            attempts++;

            startCountdown();

            updateWindowTitle();
        }

        private void quitGameMenuItem_Click(object sender, RoutedEventArgs e) => this.Close();


        private void resetGame()
        {
            attempts = 1;
            remainingTime = 10;

            targetColorCode = generateRandomColorCode();
            debugTextBox.Text = debugMode ? targetColorCode : "";

            comboBox1.SelectedIndex = -1;
            comboBox2.SelectedIndex = -1;
            comboBox3.SelectedIndex = -1;
            comboBox4.SelectedIndex = -1;

            resetLabelsAndBorders();

            attemptsListBox.Items.Clear();

            scoreLabel.Content = "Score: 0 strafpunten";

            startCountdown();

            updateWindowTitle();
        }

        private void resetLabelsAndBorders()
        {
            Label[] labels = { label1, label2, label3, label4 };

            foreach (Label label in labels)
            {
                label.Content = "";
                label.Background = Brushes.Transparent;
                label.BorderBrush = Brushes.Transparent;
                label.BorderThickness = new Thickness(0);
            }
        }

        private List<string> startGame()
        {
            List<string> playerNames = new List<string>();


            bool addAnotherPlayer = true;

            while (addAnotherPlayer)
            {
                // Vraag de naam van de speler
                string playerName = "";

                while (string.IsNullOrWhiteSpace(playerName))
                {
                    playerName = Microsoft.VisualBasic.Interaction.InputBox(
                        "Geef de naam van de speler in:",
                        "Spelersnaam",
                        ""); // Default value is leeg

                    if (string.IsNullOrWhiteSpace(playerName))
                    {
                        MessageBox.Show("De naam van de speler mag niet leeg zijn. Probeer opnieuw.",
                                        "Ongeldige invoer",
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Warning);
                    }

                }

                // Voeg de naam toe aan de lijst
                playerNames.Add(playerName);

                // Vraag of de gebruiker nog een speler wil toevoegen
                MessageBoxResult result = MessageBox.Show("Wil je nog een speler toevoegen?",
                                                          "Nog een speler?",
                                                          MessageBoxButton.YesNo,
                                                          MessageBoxImage.Question);

                addAnotherPlayer = result == MessageBoxResult.Yes;
            }

            return playerNames;
        }


        private void addHighscore(string playerName, int attempts, int score)
        {
            string newHighscore = $"{playerName} - {attempts} pogingen - {score}/100";

            if (highscoreCount < 15)
            {
                highscores[highscoreCount] = newHighscore;
                highscoreCount++;
            }
            else
            {
                // Vervang de slechtste score (laatste) met de nieuwe score als dat nodig is
                highscores[14] = newHighscore;
            }

            // Sorteer de array op basis van pogingen en score (beste eerst)
            Array.Sort(highscores, 0, highscoreCount, Comparer<string>.Create((x, y) =>
            {
                int attemptsX = int.Parse(x.Split('-')[1].Trim().Split(' ')[0]);
                int attemptsY = int.Parse(y.Split('-')[1].Trim().Split(' ')[0]);

                int scoreX = int.Parse(x.Split('-')[2].Trim().Split('/')[0]);
                int scoreY = int.Parse(y.Split('-')[2].Trim().Split('/')[0]);

                // Eerst sorteren op pogingen, daarna op score
                if (attemptsX != attemptsY)
                    return attemptsX.CompareTo(attemptsY);
                return scoreY.CompareTo(scoreX);
            }));
        }

        private void showHighscores()
        {
            StringBuilder sb = new StringBuilder("Highscores:\n\n");

            for (int i = 0; i < highscoreCount; i++)
            {
                sb.AppendLine(highscores[i]);
            }

            MessageBox.Show(sb.ToString(), "Highscores", MessageBoxButton.OK, MessageBoxImage.Information);
        }

    private void highscoresMenuItem_Click(object sender, RoutedEventArgs e) => showHighscores();

    }
}
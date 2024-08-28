using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Policy;
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

namespace ScryDraft
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static HttpClient client = new();

        private Card[] cardSlots = new Card[5];

        private Image[] artSlots = new Image[5];

        private BitmapImage[] loadArt = new BitmapImage[5];

        private BitmapImage cardBack;

        public string deckList = "";

        public string qbase = "";

        private string rarity = "common";

        private bool rareLocked = true;

        private string qmod = "";

        private int tokens = 15;

        private int deckSize = 0;

        private ObservableCollection<ImageSource> items = new ObservableCollection<ImageSource>();


        //private static string cardBackSource = "D:\OtherCode\MagicDraft\ScryDraft\ScryDraft\CardBack.jpg";

        public MainWindow()
        {
            InitializeComponent();

            client.BaseAddress = new Uri("http://api.scryfall.com/cards/random");
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            artSlots[0] = artSlot0;
            artSlots[1] = artSlot1;
            artSlots[2] = artSlot2;
            artSlots[3] = artSlot3;
            artSlots[4] = artSlot4;

            this.DataContext = items;

            //create a local bitmap for the card back
            cardBack = new BitmapImage();
            cardBack.BeginInit();
            cardBack.UriSource = new Uri(@"CardBack.png", UriKind.RelativeOrAbsolute);
            cardBack.EndInit();

            OnPack();
        }

        public async void OnPack(object sender, RoutedEventArgs e)
        {
            OnPack();
        }

        public async void OnPack()
        {
            DisableButtons();

            //replace art with placeholder cardback while retrieving new art
            for (int i = 0; i < cardSlots.Length; i++)
            {
                artSlots[i].Source = cardBack;
            }

            //choose rarity before so each card is of same rarity
            int chance = new Random().Next(1, 100);
            if (chance <= 40) { rarity = "common"; }
            else if (chance <= 75) { rarity = "uncommon"; }
            else if (chance <= 95) { rarity = "rare"; }
            else { rarity = "mythic"; }

            //pull a batch of cards
            for (int i = 0; i < cardSlots.Length; i++)
            {
                await PullCard(i);
            }

            //reset any modifiers from the special choice buttons
            rareLocked = true;
            qmod = "";

            //wait a little bit for all the images to load right
            //also helpss comply with scryfall's pull speed requests
            await Task.Delay(500);

            //replace placeholder card back with the loaded art all at once
            for (int i = 0; i < cardSlots.Length; i++)
            {
                artSlots[i].Source = loadArt[i];
            }

            EnableButtons();

            return;
        }

        public async Task<string> PullCard(int slot)
        {
            string q;
            if (qbase == "")
            {
                q = "?q=is%3Acommander";
            }
            else
            {
                q = qbase;
                // 
                if (rareLocked) 
                {
                    q += "%20rarity%3A" + rarity;
                }

                q += qmod;
            }
            Card card =  await GetCardAsync("http://api.scryfall.com/cards/random" + q);

            LoadCardArt(card, slot);

            cardSlots[slot] = card;

            return card.Name;
        }

        static async Task<Card> GetCardAsync(string path)
        {
            Card card = null;
            HttpResponseMessage response = await client.GetAsync(path);
            if (response.IsSuccessStatusCode)
            {
                card = await response.Content.ReadAsAsync<Card>();
            }
            if (card != null)
            {
                return card;
            }
            else return null;
            
        }

        private void LoadCardArt(Card card, int slot)
        {
            if (card.Image_Uris != null)
            {
                JToken token = card.Image_Uris.First.Next.Next.Next;

                if (token is JProperty property2)
                {
                    string uri = property2.Value.ToString();

                    // Create source.
                    BitmapImage bi = new BitmapImage();
                    // BitmapImage.UriSource must be in a BeginInit/EndInit block.
                    bi.BeginInit();
                    bi.UriSource = new Uri(uri, UriKind.RelativeOrAbsolute);
                    bi.EndInit();
                    // Set the image source.
                    loadArt[slot] = bi;
                }
            }
            else
            {
                //TODO: figure out 2-face cards
                loadArt[slot] = cardBack;
            }
            return;
        }

        public void Choose0(object sender, RoutedEventArgs e)
        {
            CardChosen(0, cardSlots[0]);
            OnPack();
        }

        public void Choose1(object sender, RoutedEventArgs e)
        {
            CardChosen(1, cardSlots[1]);
            OnPack();
        }

        public void Choose2(object sender, RoutedEventArgs e)
        {
            CardChosen(2, cardSlots[2]);
            OnPack();
        }

        public void Choose3(object sender, RoutedEventArgs e)
        {
            CardChosen(3, cardSlots[3]);
            OnPack();
        }

        public void Choose4(object sender, RoutedEventArgs e)
        {
            CardChosen(4, cardSlots[4]);
            OnPack();
        }

        //param slot: the slot the card was chosen from
        public void CardChosen(int slot, Card card)
        {
            //normal operation, just adds to deck
            deckList += $"x1 {card.Name}\n";
            deckBox.Text = deckList;
            deckSize++;
            sizeChecker.Text = $"Cards Picked: {deckSize}\n\n Tokens Left: {tokens}";

            items.Add(loadArt[slot]);

            //if the commander isn't chosen yet, set the qbase to match colorID of commander
            if (qbase == "")
            {
                qbase = "?q=id%3C%3D";
                if (card.Color_Identity == null || card.Color_Identity.Length == 0)
                {
                    qbase += "colorless";
                }
                else
                {
                    string colorCombo = "";
                    foreach (string color in card.Color_Identity)
                    {
                        colorCombo += color;
                    }
                    qbase += colorCombo;
                }

                //include some other nice things, like omitting basic lands
                qbase += "%20%2Dt%3Abasic";
                qbase += "%20%2Dt%3Asticker";
                qbase += "%20%2Dt%3Aattraction";
                qbase += "%20legal%3Acommander";
                //removing 2-faced cards for now
                qbase += "%20%2Dis%3Atransform";
                qbase += "%20%2Dis%3Adfc";
                qbase += "%20%2Dis%3Amdfc";
                qbase += "%20%2Dis%3Ameld";
            }
        }

        public void ModRamp(object sender, RoutedEventArgs e)
        {
            qmod = "%20otag%3Aramp";
            rareLocked = false;
            DisableModButtons();
            modRamp.Background = Brushes.Red;
            tokens -= 2;
        }

        public void ModDraw(object sender, RoutedEventArgs e)
        {
            qmod = "%20o%3Adraw%20%28o%3Acards%20or%20o%3Awhenever%29";
            rareLocked = false;
            DisableModButtons();
            modDraw.Background = Brushes.Red;
            tokens -= 2;
        }

        public void ModWipe(object sender, RoutedEventArgs e)
        {
            qmod = "%20otag%3Aboardwipe";
            rareLocked = false;
            DisableModButtons();
            modWipe.Background = Brushes.Red;
            tokens -= 3;
        }

        public void ModHigh(object sender, RoutedEventArgs e)
        {
            qmod = "%20mv%3E%3D5";
            rareLocked = true;
            DisableModButtons();
            modWipe.Background = Brushes.Red;
            tokens -= 1;
        }

        public void ModLow(object sender, RoutedEventArgs e)
        {
            qmod = "%20mv%3C%3D4";
            rareLocked = true;
            DisableModButtons();
            modWipe.Background = Brushes.Red;
            tokens -= 1;
        }

        public void ModRemoval(object sender, RoutedEventArgs e)
        {
            qmod = "%20otag%3Aremoval";
            rareLocked = false;
            DisableModButtons();
            modWipe.Background = Brushes.Red;
            tokens -= 2;
        }

        public void ModLand(object sender, RoutedEventArgs e)
        {
            qmod = "%20t%3Aland"; ;
            rareLocked = false;
            DisableModButtons();
            modWipe.Background = Brushes.Red;
            tokens -= 1;
        }

        private void DisableButtons()
        {
            btn0.IsEnabled = false;
            btn1.IsEnabled = false;
            btn2.IsEnabled = false;
            btn3.IsEnabled = false;
            btn4.IsEnabled = false;
            modDraw.IsEnabled = false;
            modRamp.IsEnabled = false;
            modWipe.IsEnabled = false;
            modHigh.IsEnabled = false;
            modLow.IsEnabled = false;
            modRemoval.IsEnabled = false;
            modLand.IsEnabled = false;
        }

        private void DisableModButtons()
        {
            modDraw.IsEnabled = false;
            modRamp.IsEnabled = false;
            modWipe.IsEnabled = false;
            modHigh.IsEnabled = false;
            modLow.IsEnabled = false;
            modRemoval.IsEnabled = false;
            modLand.IsEnabled = false;

            modDraw.Background = Brushes.DimGray;
            modRamp.Background = Brushes.DimGray;
            modWipe.Background = Brushes.DimGray;
            modHigh.Background = Brushes.DimGray;
            modLow.Background = Brushes.DimGray;
            modRemoval.Background = Brushes.DimGray;
            modLand.Background = Brushes.DimGray;
        }

        private void EnableButtons()
        {
            btn0.IsEnabled = true;
            btn1.IsEnabled = true;
            btn2.IsEnabled = true;
            btn3.IsEnabled = true;
            btn4.IsEnabled = true;
            modDraw.IsEnabled = tokens >= 2;
            modRamp.IsEnabled = tokens >= 2;
            modWipe.IsEnabled = tokens >= 3;
            modHigh.IsEnabled = tokens >= 1;
            modLow.IsEnabled = tokens >= 1;
            modRemoval.IsEnabled = tokens >= 2;
            modLand.IsEnabled = tokens >= 1;

            modDraw.Background = Brushes.BlanchedAlmond;
            modRamp.Background = Brushes.BlanchedAlmond;
            modWipe.Background = Brushes.BlanchedAlmond;
            modHigh.Background = Brushes.BlanchedAlmond;
            modLow.Background = Brushes.BlanchedAlmond;
            modRemoval.Background = Brushes.BlanchedAlmond;
            modLand.Background = Brushes.BlanchedAlmond;
        }

        public class Card
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string[]? Color_Identity { get; set; }
            public JObject Image_Uris { get; set; }
        }

    }
}

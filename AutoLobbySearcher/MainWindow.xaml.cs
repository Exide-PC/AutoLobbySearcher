using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
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

namespace AutoLobbySearcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ObservableCollection<TableEntry> tableEntries = new ObservableCollection<TableEntry>();
        Lobby sourceLobby = null;

        public MainWindow()
        {
            InitializeComponent();

            Lobby l = new Lobby("steam://joinlobby/730/109775245033117187/76561198201922808");
        }

        static Task<HtmlDocument> DownloadHtmlAsync(Uri url)
        {
            return Task<HtmlDocument>.Factory.StartNew(() =>
            {
                HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create(url);
                myHttpWebRequest.ProtocolVersion = HttpVersion.Version10;
                myHttpWebRequest.Method = "GET";
                HttpWebResponse response = (HttpWebResponse)myHttpWebRequest.GetResponse();

                if (response.ContentType.Contains("html"))
                {
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        string html = reader.ReadToEnd();
                        HtmlDocument doc = new HtmlDocument();
                        doc.LoadHtml(html);
                        return doc;
                    }
                }
                else
                    throw new InvalidDataException($"Невозможно получить HTML код из контента {response.ContentType}");
            });
        }

        static Task UpdateTableEntry(TableEntry entry)
        {
            return Task.Factory.StartNew(() =>
            {
                HtmlDocument doc = DownloadHtmlAsync(entry.Url).Result;

                // Получим узел с игрой
                HtmlNode gameNode = doc.DocumentNode.Descendants("div")
                    .FirstOrDefault(d => d.Attributes["class"]?.Value == "profile_in_game_name");

                if (gameNode != null)
                    entry.Game = gameNode.InnerText.Trim();

                // Получим узел с лобби
                HtmlNode lobbyNode = doc.DocumentNode.Descendants("div")
                    .FirstOrDefault(d => d.Attributes["class"]?.Value == "profile_in_game_joingame");

                if (lobbyNode != null)
                {
                    string lobbyLink = lobbyNode.ChildNodes.FindFirst("a").Attributes["href"].Value;
                    Lobby lobby = new Lobby(lobbyLink);
                    entry.Lobby = lobby;
                }
            });
        }
        
        private async void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            // steam://joinlobby/730/109775245033117187/76561198201922808
            if (!Uri.TryCreate(urlBox.Text, UriKind.Absolute, out var url)) return;
            
            if (url.Host == "steamcommunity.com")
            {
                TableEntry tmpEntry = new TableEntry()
                {
                    Url = url
                };

                scanButton.IsEnabled = false;
                await UpdateTableEntry(tmpEntry);
                scanButton.IsEnabled = true;

                if (tmpEntry.Lobby != null && tmpEntry.Lobby.IsValid)
                {
                    InitRandomLobbyTool(tmpEntry.Lobby);
                }
            }
            else if (url.Host == "joinlobby")
            {
                Lobby lobby = new Lobby(url.AbsoluteUri);
                InitRandomLobbyTool(lobby);
            }
        }

        void InitRandomLobbyTool(Lobby sourceLobby)
        {
            this.sourceLobby = sourceLobby;
            urlBox.Text = sourceLobby.Url;
            this.sourceIdLabel.Text = sourceLobby.LobbyId.ToString();
            differenceLabel.Text = 0.ToString();
            relativeIdBox.Text = 1.ToString();
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(urlBox.Text.Trim());
        }

        private void JoinNextButton_Click(object sender, RoutedEventArgs e)
        {
            // Попробуем получить относительную разницу между лобби
            if (!int.TryParse(relativeIdBox.Text, out var dif)) return;

            long prevSourceDif = long.Parse(differenceLabel.Text);
            long newId = this.sourceLobby.LobbyId + prevSourceDif + dif;
            //this.differenceLabel.Text = prevSourceDif.ToString();

            // Скопируем исходное лобби
            Lobby newLobby = new Lobby(this.sourceLobby.Url);
            // И отредактируем id
            newLobby.SetLobbyId(newId);

            // Определим разницу между текущим и исходным
            long sourceDif = newLobby.LobbyId - sourceLobby.LobbyId;
            this.differenceLabel.Text = sourceDif.ToString();
            this.urlBox.Text = newLobby.Url;
            this.relativeIdBox.Text = 1.ToString();

            System.Diagnostics.Process.Start(newLobby.Url);
        }
    }
}

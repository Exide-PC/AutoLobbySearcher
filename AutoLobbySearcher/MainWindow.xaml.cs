using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml.Serialization;

namespace AutoLobbySearcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ObservableCollection<TableEntry> tableEntries = new ObservableCollection<TableEntry>();
        static readonly Regex numRegex = new Regex(@"^-?\d*$"); // регулярное выражение для проверки числового ввода
        Lobby sourceLobby = null;
        string cfgPath = "AutoLobbySearcher.xml";
        CancellationTokenSource tokenSource;
        Task monitorTask = null;

        public MainWindow()
        {
            InitializeComponent();

            dataGrid.ItemsSource = tableEntries;
        }

        #region UI
        /// <summary>
        /// Метод, вызываемый при загрузке окна. Здесь мы инициализируем программу из конфига
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(cfgPath)) return;

            XmlSerializer ser = new XmlSerializer(typeof(IniSettings));

            using (FileStream fs = File.OpenRead(cfgPath))
            {
                IniSettings iniSet = (IniSettings)ser.Deserialize(fs);

                foreach (TableEntry entry in iniSet.TableEntries)
                    this.tableEntries.Add(entry);
            }
        }

        /// <summary>
        /// Метод, вызываемый при закрытии окна. Здесь мы сохраняем данные
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (File.Exists(cfgPath)) File.Delete(cfgPath);

            XmlSerializer ser = new XmlSerializer(typeof(IniSettings));
            using (FileStream fs = File.OpenWrite(this.cfgPath))
            {
                IniSettings iniSet = new IniSettings()
                {
                    TableEntries = this.tableEntries.Where(entry => entry.IsPlayer).ToArray()
                };
                ser.Serialize(fs, iniSet);
            }
        }

        private async void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            // steam://joinlobby/730/109775245033117187/76561198201922808
            if (!TryCreateEntry(urlBox.Text, out var createdEntry))
            {
                SetStatus("Incorrect url. Paste profile or direct lobby link");
                return;
            }

            if (createdEntry.IsPlayer)
            {
                scanButton.IsEnabled = false;
                await UpdateTableEntryAsync(createdEntry);
                scanButton.IsEnabled = true;

                if (createdEntry.Lobby != null && createdEntry.Lobby.IsValid)
                    InitRandomLobbyTool(createdEntry.Lobby);
            }
            else
            {
                InitRandomLobbyTool(createdEntry.Lobby);
            }
        }

        private void JoinNextButton_Click(object sender, RoutedEventArgs e)
        {
            // Попробуем получить относительную разницу между лобби
            if (sourceLobby == null || !int.TryParse(relativeIdBox.Text, out var dif))
            {
                SetStatus("Incorrect difference value");
                return;
            }

            // Получим прошлую разницу между ID и прибавим к ней новую
            long prevSourceDif = long.Parse(differenceLabel.Text);
            long newId = this.sourceLobby.LobbyId + prevSourceDif + dif;

            // Скопируем исходное лобби
            Lobby newLobby = new Lobby(this.sourceLobby.Url);
            // И отредактируем id
            newLobby.SetLobbyId(newId);

            // Определим разницу между новым и исходным лобби
            long sourceDif = newLobby.LobbyId - sourceLobby.LobbyId;
            this.differenceLabel.Text = sourceDif.ToString();
            this.urlBox.Text = newLobby.Url;
            this.relativeIdBox.Text = 1.ToString();

            JoinLobby(newLobby);
        }

        private void AddToListButton_Click(object sender, RoutedEventArgs e)
        {
            string lobbyLink = urlBox.Text;
            Lobby lobby = null;

            // Попробуем из ссылки вверху создать экземпляр лобби. Если не получается, то gg.
            // TODO: Lobby.TryCreate
            try
            {
                lobby = new Lobby(lobbyLink);
            }
            catch
            {
                SetStatus("Incorrect lobby link");
                return;
            }

            // Если получилось, то добавляем в таблицу
            TableEntry newEntry = new TableEntry()
            {
                Name = $"Lobby №{this.tableEntries.Where(entry => !entry.IsPlayer).Count() + 1}",
                Lobby = lobby,
                Game = string.Empty,
                IsPublic = false
            };

            this.tableEntries.Add(newEntry);
        }
        
        private void LowerAddButton_Click(object sender, RoutedEventArgs e)
        {
            // Попробуем создать элемент таблицы по записанному в боксе URL
            if (!TryCreateEntry(lowerUrlBox.Text, out var createdEntry))
            {
                SetStatus("Incorrect url. Paste profile or direct lobby link");
                return;
            }
            // Если получилось, то обновляем коллекцию
            this.tableEntries.Add(createdEntry);
        }

        private async void AutoRefreshButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Primitives.ToggleButton toggleButton =
                (System.Windows.Controls.Primitives.ToggleButton)sender;

            // Инвертируем значение переключателя
            // TODO: Реализовать через стиль кнопки
            toggleButton.IsChecked = !toggleButton.IsChecked;


            //Запустим или завершим задачу мониторинга в зависимости от состояния кнопки
            if ((bool)toggleButton.IsChecked)
            {
                this.tokenSource = new CancellationTokenSource();
                this.monitorTask = MonitorAsync(tokenSource.Token);
            }
            else
            {
                this.tokenSource.Cancel();

                // Пока задача не завершится - отключаем кнопку
                autoRefreshButton.IsEnabled = false;
                await this.monitorTask;
                autoRefreshButton.IsEnabled = true;

                this.tokenSource = null;
            }
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(urlBox.Text.Trim());
        }

        private void NumericTextbox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !numRegex.IsMatch(e.Text);
        }

        private void dataGrid_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Delete) return;
            // Удалим элемент коллекции, если нажат Del
            this.tableEntries.RemoveAt(dataGrid.SelectedIndex);
        }

        private void JoinLobbyButton_Click(object sender, RoutedEventArgs e)
        {
            if (dataGrid.SelectedIndex == -1)
            {
                SetStatus("Table doesnt have row selected");
                return;
            }

            TableEntry targetEntry = this.tableEntries[dataGrid.SelectedIndex];

            if (targetEntry.Lobby == null || !targetEntry.Lobby.IsValid)
            {
                SetStatus("Table entry has invalid lobby");
                return;
            }

            JoinLobby(targetEntry.Lobby);
        }
        #endregion

        #region Framework
        /// <summary>
        /// Метод для получения Html-кода по переданному URL
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
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
                    throw new InvalidDataException($"Impossible to get HTML from next content type: {response.ContentType}");
            });
        }

        /// <summary>
        /// Метод для создания элемента таблицы по переданному URL
        /// </summary>
        /// <param name="url"></param>
        /// <param name="resultEntry">Элемент представляющий собой либо конечное лобби, либо стим-профиль</param>
        /// <returns>Флаг успеха операции</returns>
        bool TryCreateEntry(string url, out TableEntry resultEntry)
        {
            resultEntry = null;
            if (!Uri.TryCreate(url, UriKind.Absolute, out var createdUrl)) return false;

            if (createdUrl.Host == "steamcommunity.com" &&
                (createdUrl.AbsolutePath.StartsWith("/profiles/") || createdUrl.AbsolutePath.StartsWith("/id/")))
            {
                resultEntry = new TableEntry()
                {
                    Url = createdUrl,
                    Name = createdUrl.AbsolutePath
                };
                return true;
            }
            else if (createdUrl.Host == "joinlobby")
            {
                Lobby lobby = new Lobby(createdUrl.AbsoluteUri);

                resultEntry = new TableEntry()
                {
                    Name = $"Lobby №{this.tableEntries.Where(entry => !entry.IsPlayer).Count() + 1}",
                    Lobby = lobby
                };
                return true;
            }
            return false;
        }

        /// <summary>
        /// Асинхронный метод для обновления информации о стим-профиле. Таблица обновляется автоматически по причине реализации интерфейса INPC
        /// </summary>
        /// <param name="entry">Профиль, информацию о котором необходимо обновить</param>
        /// <returns></returns>
        Task UpdateTableEntryAsync(TableEntry entry)
        {
            return Task.Factory.StartNew(() =>
            {
                HtmlDocument doc = DownloadHtmlAsync(entry.Url).Result;

                // Получим узел с игрой
                HtmlNode gameNode = doc.DocumentNode.Descendants("div")
                    .FirstOrDefault(d => d.Attributes["class"]?.Value == "profile_in_game_name");

                if (gameNode != null)
                    entry.Game = gameNode.InnerText.Trim();
                else
                    entry.Game = "-";

                // Получим узел с лобби
                HtmlNode lobbyNode = doc.DocumentNode.Descendants("div")
                    .FirstOrDefault(d => d.Attributes["class"]?.Value == "profile_in_game_joingame");

                if (lobbyNode != null)
                {
                    string lobbyLink = lobbyNode.ChildNodes.FindFirst("a").Attributes["href"].Value;
                    Lobby newLobby = new Lobby(lobbyLink);

                    if (newLobby.IsValid)
                    {
                        // Проверим найдено ли новое лобби
                        // Новое, если до этого его не было вообще, либо оно было не валидно, либо если URL разные
                        if (entry.Lobby == null || !entry.Lobby.IsValid || entry.Lobby.Url != newLobby.Url)
                        {
                            // Сделаем звуковое уведомление, что найдено новое лобби
                            System.Media.SystemSounds.Beep.Play();
                            // Войдем в лобби, если необходимо
                            // Не забываем, что комбобокс является UI-элементом
                            this.Dispatcher.Invoke(() =>
                            {
                                switch (joinOptionCombobox.SelectedIndex)
                                {
                                    case 1:
                                        {
                                            JoinLobby(newLobby);
                                            joinOptionCombobox.SelectedIndex = 0;
                                            break;
                                        }
                                    case 2:
                                        {
                                            JoinLobby(newLobby);
                                            break;
                                        }
                                }
                            });
                        }

                        entry.Lobby = newLobby;
                        entry.IsPublic = true;
                    }
                    else
                    {
                        entry.IsPublic = false;
                    }
                }
                else
                    entry.IsPublic = false;

                // Увеличим счетчик обновлений
                this.Dispatcher.Invoke(() => updatesLabel.Text = (int.Parse(updatesLabel.Text) + 1).ToString());
            });
        }

        void InitRandomLobbyTool(Lobby sourceLobby)
        {
            this.sourceLobby = sourceLobby;
            urlBox.Text = sourceLobby.Url;
            this.sourceIdLabel.Text = sourceLobby.LobbyId.ToString();
            differenceLabel.Text = 0.ToString();
            relativeIdBox.Text = 1.ToString();
        }

        Task MonitorAsync(CancellationToken token)
        {
            return Task.Factory.StartNew(() =>
            {
                while (!token.IsCancellationRequested)
                {
                    IEnumerable<Task> tasks = this.tableEntries
                        .Where(e => e.IsPlayer)
                        .Select(e => UpdateTableEntryAsync(e));

                    Task.WaitAll(tasks.ToArray());

                    int delay = 1;
                    this.Dispatcher.Invoke(() => int.TryParse(delayBox.Text, out delay));

                    Thread.Sleep(delay * 1000);
                }
            });
        }

        void JoinLobby(Lobby lobby)
        {
            if (!lobby.IsValid) throw new Exception("Impossible to join invalid lobby");

            System.Diagnostics.Process.Start(lobby.Url);
        }

        void SetStatus(string status)
        {
            this.status.Dispatcher.Invoke(() => this.status.Text = status);
        }
        #endregion

        private void About_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow aw = new AboutWindow();
            aw.ShowDialog();
        }

        private void Grid_MouseEnter(object sender, MouseEventArgs e)
        {
            SetStatus(string.Empty);
        }
    }
}

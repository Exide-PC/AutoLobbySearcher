using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoLobbySearcher
{
    public class Lobby: BindableBase
    {
        string _url;
        int _gameId = -1;
        long _lobbyId = -1;
        long _magicNum = -1;

        public bool IsValid => this._gameId != -1 && _lobbyId != -1;

        public string Url
        {
            get => this._url;
            set => SetProperty(ref _url, value);
        }

        public int GameId
        {
            get => this._gameId;
            set => SetProperty(ref _gameId, value);
        }

        public long LobbyId
        {
            get => this._lobbyId;
            set => SetProperty(ref _lobbyId, value);
        }

        public long MagicNum
        {
            get => this._magicNum;
            set => SetProperty(ref _magicNum, value);
        }
        
        public Lobby(string url)
        {
            UpdateProperties(url);
        }

        void UpdateProperties(string url)
        {
            this.Url = url;

            string[] urlParts = url.Split('/');
            if (urlParts.Length != 6) return;

            this.GameId = int.Parse(urlParts[3]);
            this.LobbyId = long.Parse(urlParts[4]);
            this.MagicNum = long.Parse(urlParts[5]);
        }
        
        public void SetLobbyId(long newId)
        {
            string prevUrl = this._url;
            string newUrl = prevUrl.Replace($"/{_lobbyId}/", $"/{newId}/");

            UpdateProperties(newUrl);
        }
    }
}

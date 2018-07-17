/*
MIT License

Copyright (c) 2018 Exide-PC

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

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
            if (!url.StartsWith("steam://"))
                throw new Exception("Incorrect lobby url");

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

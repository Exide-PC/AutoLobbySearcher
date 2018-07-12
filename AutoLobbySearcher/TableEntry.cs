using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoLobbySearcher
{
    public class TableEntry: BindableBase
    {
        Uri _url;
        string _name = null;
        string _game = null;
        Lobby _lobby = null;
        bool _isPublic = false;

        public Uri Url
        {
            get => this._url;
            set => SetProperty(ref _url, value);
        }

        public string Name
        {
            get => this._name;
            set => SetProperty(ref _name, value);
        }

        public string Game
        {
            get => this._game;
            set => SetProperty(ref _game, value);
        }

        public Lobby Lobby
        {
            get => this._lobby;
            set => SetProperty(ref _lobby, value);
        }
        
        public bool IsPublic
        {
            get => this._isPublic;
            set => SetProperty(ref _isPublic, value);
        }
    }
}

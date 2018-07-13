using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace AutoLobbySearcher
{
    public class TableEntry: BindableBase, IXmlSerializable
    {
        Uri _url;
        string _name = null;
        string _game = null;
        bool _isPublic = false;
        Lobby _lobby = null;

        public bool IsPlayer => this._url != null;

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

        [XmlIgnore]
        public string Game
        {
            get => this._game;
            set => SetProperty(ref _game, value);
        }

        [XmlIgnore]
        public Lobby Lobby
        {
            get => this._lobby;
            set
            {
                SetProperty(ref _lobby, value);
                // Если переданное лобби валидное, то вызовем событие для обновления таблицы
                if (value.IsValid) this.RaisePropertyChanged(nameof(LobbyId));
            }
        }

        [XmlIgnore]
        public string LobbyId
        {
            get => this._lobby?.LobbyId.ToString() ?? string.Empty;
        }

        [XmlIgnore]
        public bool IsPublic
        {
            get => this._isPublic;
            set => SetProperty(ref _isPublic, value);
        }

        public XmlSchema GetSchema() => null;

        public void ReadXml(XmlReader reader)
        {
            reader.MoveToAttribute(nameof(Name));
            this.Name = reader.Value;
            reader.MoveToAttribute(nameof(Url));
            this.Url = new Uri(reader.Value, UriKind.Absolute);

            reader.Read();
        }

        public void WriteXml(XmlWriter writer)
        {
            if (this.Url == null) throw new Exception("Сериализуемый элемент не имеет ссылки на профиль");

            writer.WriteAttributeString(nameof(Name), this.Name);
            writer.WriteAttributeString(nameof(Url), this.Url.AbsoluteUri);
        }
    }
}

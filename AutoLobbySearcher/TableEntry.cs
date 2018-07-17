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

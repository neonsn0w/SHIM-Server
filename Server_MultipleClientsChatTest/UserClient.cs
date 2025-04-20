using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server_MultipleClientsChatTest
{
    internal class UserClient
    {
        private Socket socket;
        private string nickname;
        private string publicKey;

        // constructor
        public UserClient(Socket socket, string nickname, string publicKey)
        {
            this.socket = socket;
            this.nickname = nickname;
            this.publicKey = publicKey;
        }

        // constructor
        public UserClient(Socket socket)
        {
            this.socket = socket;
            this.nickname = "";
            this.publicKey = "";
        }

        // getters and setters
        public Socket Socket
        {
            get { return socket; }
            set { socket = value; }
        }
        public string Nickname
        {
            get { return nickname; }
            set { nickname = value; }
        }
        public string PublicKey
        {
            get { return publicKey; }
            set { publicKey = value; }
        }

    }
}

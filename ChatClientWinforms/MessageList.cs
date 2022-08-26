using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Security.Cryptography;

namespace ChatClientWinforms
{
    public class MessageList
    {
        public string name;
        public List<ListViewItem> messages;
        public bool newMessage;
        public byte[] pubKeyExponent;
        public byte[] pubKeyModulus;
        public bool keyObtained;
        public RSAParameters RSAParams;

        public MessageList(string Name)
        {
            name = Name;
            messages = new List<ListViewItem>();
            newMessage = false;

            pubKeyExponent = new byte[] { };
            pubKeyModulus = new byte[] { };
            keyObtained = false;
        }

        public MessageList(string Name, byte[] PubKeyModulus, byte[] PubKeyExponent)
        {
            name = Name;
            messages = new List<ListViewItem>();
            newMessage = false;

            pubKeyExponent = PubKeyExponent;
            pubKeyModulus = PubKeyModulus;
            RSAParams.Exponent = pubKeyExponent;
            RSAParams.Modulus = pubKeyModulus;
            keyObtained = true;
        }

        public void UpdateRSAParamaters()
        {
            RSAParams.Exponent = pubKeyExponent;
            RSAParams.Modulus = pubKeyModulus;
        }

    }
}

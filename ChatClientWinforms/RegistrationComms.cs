using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Security.Cryptography;

namespace ChatClientWinforms
{
    public class RegistrationComms
    {
        private TcpClient client;
        private StreamReader reader;
        private StreamWriter writer;
        private RSAParameters RSAParams; 

        public Security secureComms;

        public RegistrationComms(Security SecureComms)
        {
            secureComms = SecureComms;
        }


        //Not async as it's just 1 instance of trying to connect to server, write 1 message to stream, read 1 message from stream, and close again.
        //No threading either for the same reason.
        public string ConnectToServer(string Email, string Password, string NickName)
        {
            int port = 8081;
            IPAddress ipAddess = IPAddress.Parse("127.0.0.1");

            try
            {
                client = new TcpClient(ipAddess.ToString(), port);
                reader = new StreamReader(client.GetStream());
                writer = new StreamWriter(client.GetStream());

                //First wait for server's public key to be sent, then process key;
                string serverKey = reader.ReadLine();

                //[0] = key Modulus. [1] = key Exponent;
                string[] tempArray = serverKey.Split(' ');

                byte[] keyModulus = Array.ConvertAll(tempArray[0].Split(','), Byte.Parse);
                byte[] keyExponent = Array.ConvertAll(tempArray[1].Split(','), Byte.Parse);
                RSAParams.Modulus = keyModulus;
                RSAParams.Exponent = keyExponent;

                //Send the registration info to the server, returning a string value indicating success or failure;
                string temp = Email + "," + Password + "," + NickName;
                temp = secureComms.RSAEncrypt(temp, RSAParams);
                string s = RegisterAccount(temp);
                return s;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e);
                return "No server connection.";
            }

        }

        private string RegisterAccount(string AccountInfo)
        {
            try
            {
                writer.WriteLine(AccountInfo);
                writer.Flush();

                string s = reader.ReadLine();
                return s;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e);
                return "No stream connection.";

            }
        }



    }
}

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

namespace ChatClientWinforms
{
    public class RegistrationComms
    {
        private TcpClient client;
        private StreamReader reader;
        private StreamWriter writer;


        public RegistrationComms()
        {

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

                string s = RegisterAccount(Email, Password, NickName);
                return s;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e);
                return "No server connection.";
            }

        }

        private string RegisterAccount(string Email, string Password, string NickName)
        {
            try
            {
                writer.WriteLine("{0},{1},{2}", Email, Password, NickName);
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

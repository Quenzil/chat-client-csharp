using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChatClientWinforms
{
    public class MessageList
    {
        public string name;
        public List<ListViewItem> messages;
        public bool newMessage;

        public MessageList(string Name)
        {
            name = Name;
            messages = new List<ListViewItem>();
            newMessage = false;
        }


    }
}

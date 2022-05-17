using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatClientWinforms
{
    public class NewConversationEventArgs : EventArgs
    {
        private readonly string contact;

        public NewConversationEventArgs(string Contact)
        {
            this.contact = Contact;
        }

        public string Contact
        {
            get { return contact; }
        }
    }
}

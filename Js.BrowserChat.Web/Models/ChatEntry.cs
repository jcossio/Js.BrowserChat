using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Js.BrowserChat.Web.Models
{
    public class ChatEntry
    {
        public DateTime DatePosted { get; set; }
        public string WhoPosted { get; set; }
        public string Text { get; set; }
    }
}

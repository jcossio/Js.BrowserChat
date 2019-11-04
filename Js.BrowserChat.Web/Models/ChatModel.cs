using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Js.BrowserChat.Web.Models
{
    public class ChatModel
    {
        public IEnumerable<ChatEntry> ChatEntries { get; set; }
        public string ChatText { get; set; }
    }
}

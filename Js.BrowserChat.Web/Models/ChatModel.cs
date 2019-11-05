using Js.BrowserChat.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Js.BrowserChat.Web.Models
{
    public class ChatModel
    {
        public List<ChatEntry> ChatEntries { get; set; }
        [Required]
        public string ChatText { get; set; }
    }
}

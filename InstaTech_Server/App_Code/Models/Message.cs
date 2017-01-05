using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace InstaTech.App_Code.Models
{
    /// <summary>
    /// Summary description for Message
    /// </summary>
    public class Message
    {
        public Message()
        {
            DTSent = DateTime.Now;
        }
        public DateTime DTSent { get; set; }
        public string FromUserID { get; set; }
        public string Content { get; set; }
    } 
}
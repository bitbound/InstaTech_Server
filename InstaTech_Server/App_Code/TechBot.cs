using InstaTech.App_Code.Socket_Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Timers;
using System.Web;
using System.Web.Helpers;

namespace InstaTech.App_Code
{
    /// <summary>
    /// Summary description for TechBot
    /// </summary>
    public class TechBot
    {
        private static TechBot Current { get; set; }
        private static List<Socket_Main> Sockets { get; set; } = new List<Socket_Main>();
        public static void Notify(Socket_Main Socket)
        {
            if (Current == null)
            {
                Current = new TechBot();
            }
            Sockets.Add(Socket);
        }
        private static System.Timers.Timer TechTimer { get; set; }
        private static void TechTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            while (Sockets.Count > 0)
            {
                var thisSocket = Sockets[0];
                Sockets.RemoveAt(0);
                thisSocket.SupportCase.DTReceived = DateTime.Now;
                thisSocket.Partner = new Socket_Main();
                var JsonData = new
                {
                    Type = "TakeCase",
                    Status = "ok",
                    TechID = "TechBot",
                    TechFirstName = "Tech",
                    TechLastName = "Bot"
                };
                thisSocket.Send(Json.Encode(JsonData));
                foreach (Socket_Main socket in Socket_Main.Customers.Where(cu => cu.Partner == null))
                {
                    socket.SendWaitUpdate();
                }
                var request = Json.Encode(new
                {
                    Type = "CaseUpdate",
                    Status = "Remove",
                    Case = thisSocket.SupportCase
                });
                Socket_Main.Techs.ForEach((Socket_Main sc) => {
                    sc.Send(request);
                });
                var message1 = new
                {
                    Type = "ChatMessage",
                    Message = Convert.ToBase64String(Encoding.UTF8.GetBytes(@"Greetings!  I'm the Tech Bot.  Since no other techs are logged in, I'll be handling your case today.")),
                };
                for (int i = 0; i < 5; i++)
                {
                    thisSocket.Send(Json.Encode(new { Type = "Typing" }));
                    Thread.Sleep(500);
                }
                thisSocket.Send(Json.Encode(message1));
                Thread.Sleep(3000);
                var message2 = new
                {
                    Type = "ChatMessage",
                    Message = Convert.ToBase64String(Encoding.UTF8.GetBytes(@"So it looks like you're having a problem with " + thisSocket.SupportCase.SupportCategory + "?  No problem!  I can help with that."))
                };
                for (int i = 0; i < 5; i++)
                {
                    thisSocket.Send(Json.Encode(new { Type = "Typing" }));
                    Thread.Sleep(500);
                }
                thisSocket.Send(Json.Encode(message2));
                Thread.Sleep(3000);
                var message3 = new
                {
                    Type = "ChatMessage",
                    Message = Convert.ToBase64String(Encoding.UTF8.GetBytes(@"In fact, I'll have that fixed in exactly 10 seconds after sending this message.")),
                };
                for (int i = 0; i < 5; i++)
                {
                    thisSocket.Send(Json.Encode(new { Type = "Typing" }));
                    Thread.Sleep(500);
                }
                thisSocket.Send(Json.Encode(message3));
                Thread.Sleep(10000);
                thisSocket.SupportCase.DTClosed = DateTime.Now;
                thisSocket.SupportCase.Save();
                thisSocket.Send(Json.Encode(new { Type = "SessionEnded", Details = "Thank you for contacting us!" }));
                thisSocket.Close();
                TechTimer.Stop();
                TechTimer = null;
                Current = null;
            }
        }
        public TechBot()
        {
            TechTimer = new System.Timers.Timer(10000);
            TechTimer.Elapsed += TechTimer_Elapsed;
            TechTimer.Start();
        }
    } 
}
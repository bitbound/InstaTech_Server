using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using InstaTech.App_Code;
using System.Web.Helpers;

namespace InstaTech.App_Code.Models
{
    /// <summary>
    /// Summary description for Case
    /// </summary>
    public class Case
    {
        public Case()
        {
            var dirs = Directory.GetDirectories(Utilities.App_Data + "Cases\\")?.ToList();
            if (dirs != null)
            {
                dirs.Sort();
                CaseID = int.Parse(new DirectoryInfo(dirs.Last()).Name.Split('-')[0]);
            }
            while (Directory.GetFiles(CaseDir).Length == 999)
            {
                CaseID += 1000;
            }
            while (File.Exists(CaseDir + CaseID + ".json"))
            {
                CaseID += 1;
            }
            DTCreated = DateTime.Now;
        }
        public int CaseID { get; set; } = 1;
        public DateTime? DTCreated { get; set; }
        public DateTime? DTReceived { get; set; }
        public DateTime? DTClosed { get; set; }
        public DateTime? DTAbandoned { get; set; }
        public string CustomerFirstName { get; set; }
        public string CustomerLastName { get; set; }
        public string CustomerUserID { get; set; }
        public string CustomerComputerName { get; set; }
        public string CustomerPhone { get; set; }
        public string CustomerEmail { get; set; }
        public string SupportCategory { get; set; }
        public string SupportType { get; set; }
        public string SupportQueue
        {
            get
            {
                return Config.Support_Categories.Find(tp => tp.Item1 == SupportCategory && tp.Item2 == SupportType).Item3;
            }
        }
        public string Details { get; set; }
        public string TechUserID { get; set; }
        public List<Message> Messages { get; set; } = new List<Message>();
        private string CaseDir
        {
            get
            {
                var floored = Math.Floor((decimal)(CaseID / 1000));
                if (!Directory.Exists(Utilities.App_Data + "Cases\\" + String.Format("{0}-{1}", floored + 1, floored + 1000)))
                {
                    Directory.CreateDirectory(Utilities.App_Data + "Cases\\" + String.Format("{0}-{1}", floored + 1, floored + 1000));
                }
                return Utilities.App_Data + "Cases\\" + String.Format("{0}-{1}", floored + 1, floored + 1000) + "\\";
            }
        }
        public void Save()
        {
            File.WriteAllText(CaseDir + CaseID + ".json", Json.Encode(this));
        }
    }
}
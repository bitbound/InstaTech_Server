using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Helpers;

/// <summary>
/// Summary description for TechAccount
/// </summary>
namespace InstaTech.App_Code.Models
{
    public class Tech_Account
    {
        public Tech_Account()
        {
        }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UserID { get; set; }
        public string HashedPassword { get; set; }
        public string AuthenticationToken { get; set; }
        public string TempPassword { get; set; }
        public int BadLoginAttempts { get; set; } = 0;
        public DateTime LastBadLogin { get; set; }
        public string Email { get; set; }
        public Access_Levels AccessLevel { get; set; } = Access_Levels.Standard;

        public enum Access_Levels
        {
            Standard,
            Admin
        }
        public List<int> Cases { get; set; } = new List<int>();
        public void Save()
        {
            if (!Directory.Exists(Utilities.App_Data + "Tech_Accounts"))
            {
                Directory.CreateDirectory(Utilities.App_Data + "Tech_Accounts");
            }
            File.WriteAllText(Utilities.App_Data + "Tech_Accounts\\" + UserID + ".json", Json.Encode(this));
        }
    } 
}
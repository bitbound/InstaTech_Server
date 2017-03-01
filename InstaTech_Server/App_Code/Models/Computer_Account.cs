using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

/// <summary>
/// Holds information about a computer running the service.
/// </summary>
public class Computer_Account
{
    public Computer_Account()
    {
    }
    public string ComputerGroup { get; set; }
    public string LastLoggedOnUser { get; set; }
}
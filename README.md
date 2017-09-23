# InstaTech Server
An ASP.NET server for remote control, live support chat, and workstation management.

Client Repo: https://github.com/Jay-Rad/InstaTech_Client  
Website: https://instatech.invis.me

### Building
You can use the Setup.ps1 script to install InstaTech.  You must run the script on the Windows computer that will be hosting InstaTech.  The script will ask you for a company name and hostname, then recompile all apps with those variables.

**Requirements**  
- Windows 10 Pro, Windows 10 Enterprise, or Windows Server 2012+
- .NET 4.5.2 Developer Pack (included in Visual Studio 2017, but can be installed separately)
- Source code for both the server and the client downloaded.
    - The server solution file should be in a folder named InstaTech_Server, and the client solution in the folder InstaTech_Client.
    - The two folders should be in the same directory.
- An IIS site already created

To customize your server manually, change the properties in /InstaTech_Server/App_Code/Config.cs.  For the clients, check the comments within the client repo in files /InstaTech_Client/MainWindow.xaml.cs, /InstaTech_Service/Socket.cs, and /InstaTech_CP/app/main.js.

# InstaTech Server
An ASP.NET server for remote control, live support chat, and workstation management.

Client Repo: https://github.com/Jay-Rad/InstaTech_Client  
Website: https://instatech.azurewebsites.net

### Building
There's an automated package builder available at https://instatech.azurewebsites.net/Downloads.  It will recompile the client applications so they target your hostname, rebuild the ASP.NET site with your company name on it, and put them in an installer.

Alternatively, you can change the properties in /InstaTech_Server/App_Code/Config.cs.  For the clients, check the comments within the client repo in files /InstaTech_Client/MainWindow.xaml.cs, /InstaTech_Service/Socket.cs, and /InstaTech_CP/app/main.js.

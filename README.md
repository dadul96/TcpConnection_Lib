# TcpConnection_Lib
This C# library contains a class for handling the TCP-connection. It provides methods for connecting to a TCP-server or creating your own. In addition, send and receive methods are implemented.

### Installation
Either copy the class directly to your code or use the **.dll** provided in [Releases](https://github.com/dadul96/TcpConnection_Lib/releases).

### Requirements
The .NET Framework Version of your project should be **4.7.2 or higher**, since this library is build for the .NET Standard 2.0.

### How to use
Following methods can be called:
* **bool Connect(string IP, int port)** - returns true, if the client could connect to the server
* **bool Disconnect()** - returns true, if all connections could be successfully closed
* **bool Listen(int port)** - returns true, if the listener could be successfully started
* **bool Send(string sendString)** - returns true, if the string could be successfully sent
* **string GetReceivedString()** - returns the received string or an empty string, if nothing got received
* **void Dispose()** - runs Disconnect() and disposes everything

Following properties can be read:
* **RemoteEndpointAddress** - address of the client that connected to the server
* **TcpIsConnected** - is true, if a TCP client is connected

An example program can be found in my [TCP_Server_Client_Tester](https://github.com/dadul96/TCP_Server_Client_Tester)-repository.

### Built With
* [Visual Studio 2019](https://visualstudio.microsoft.com/) - IDE used for programming
* [.NET Standard](https://docs.microsoft.com/en-us/dotnet/standard/net-standard) - .NET Standard 2.0 used

### Author
**Daniel Duller** - [dadul96](https://github.com/dadul96)

### License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details

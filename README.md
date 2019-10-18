# TcpConnection_Lib (outdated README)
This C# library contains a class for handling the TCP-connection. It provides methods for connecting to a TCP-server or creating your own. Also, send and receive methods are implemented.

### Installation
Either copy the class directly to your code or use the **.dll** provided in [Releases](https://github.com/dadul96/TcpConnection_Lib/releases).

### Requirements
The .NET Framework Version of your project should be **4.7.2 or higher** since this library was build for the .NET Standard 2.0.

### How to use
Following methods can be called:
* **bool TryConnect(string ipAdress, int port)** - Returns true, if the client could connect to the server.
* **bool TryListen(int port)** - Returns true, if a client could successfully connect to the listener. (This method is blocking)
* **bool TryListen(int port, out string RemoteEndpointAddress )** - Returns true, if a client could successfully connect to the listener (server). In addition, the string-argument ```RemoteEndpointAddress``` is passed by reference. (This method is blocking)
* **void Disconnect()** - Stops reading data, closes the client/listener, clears the receive buffer and sets the ```TcpConnected```-flag to false.
* **void Dispose()** - Runs ```Disconnect()```.
* **bool TrySend(string sendString)** - Returns true, if the ```sendString``` could be successfully sent.
* **bool TryReadingData()** - Returns true, if the "readingThread" could be successfully started.
* **void StopReadingData()** - Stops the "readingThread".
* **string GetReceivedString()** - Returns the received string or ```null```, if no string was received yet.

Following properties can be read:
* **TcpIsConnected** - Is true, if a working TCP connection exists.

An example program can be found in my [TCP_Server_Client_Tester](https://github.com/dadul96/TCP_Server_Client_Tester)-repository.

| **TcpConnection_Lib-versions**                                             	|     	| **TCP_Server_Client_Tester-versions**                                             	|
|---------------------------------------------------------------------------	|-----	|----------------------------------------------------------------------------------	|
| [1.0.0](https://github.com/dadul96/TcpConnection_Lib/releases/tag/v1.0.0) 	| -> 	| [1.0.0](https://github.com/dadul96/TCP_Server_Client_Tester/releases/tag/v1.0.0) 	|
| [2.0.0](https://github.com/dadul96/TcpConnection_Lib/releases/tag/v2.0.0) 	| -> 	| [2.0.0](https://github.com/dadul96/TCP_Server_Client_Tester/releases/tag/v2.0.0) 	|

### Built With
* [Visual Studio 2019](https://visualstudio.microsoft.com/) - IDE used for programming
* [.NET Standard](https://docs.microsoft.com/en-us/dotnet/standard/net-standard) - .NET Standard 2.0 used

### Author
**Daniel Duller** - [dadul96](https://github.com/dadul96)

### License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details

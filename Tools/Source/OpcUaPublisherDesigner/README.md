# IoT Edge Sample Tool - OpcPublisher DesignerApp
This application is a sample tool to help generating [OpcPublisher](https://github.com/Azure/iot-edge-opc-publisher) published nodes configuration file.

This DesignerApp provides GUI to connect to Opcua servers, select nodes for subscription, and then export subscribed nodes into a json file to be consumed by OpcPublisher.

DesignerApp is an UWP app developed using nuget package from [OPC Foundation UA .NETStandardLibrary](https://github.com/OPCFoundation/UA-.NETStandard) and utilize sample codes from [Client](https://github.com/OPCFoundation/UA-.NETStandard/tree/master/SampleApplications/Samples/Controls), [ClientControl](https://github.com/OPCFoundation/UA-.NETStandard/tree/master/SampleApplications/Samples/ClientControls) (with minor modification).

#### License

* OPC Foundations's OPC UA reference stack used by this app has licensing terms defined here:
  * [UA .NET StandardLibrary Stack License](http://opcfoundation.github.io/UA-.NETStandard/)
* Following codes in this project are modified from Opc Foundation OPCUA .NETStardardLibrary samples, hence inherits this license: [Opc Foundation MIT License 1.00](https://opcfoundation.org/license/mit/1.00/)
    * "OpcuaUtil\Controls" original source: https://github.com/OPCFoundation/UA-.NETStandard/tree/master/SampleApplications/Samples/Controls
    * "OpcuaUtil\ClientControls" original source: https://github.com/OPCFoundation/UA-.NETStandard/tree/master/SampleApplications/Samples/ClientControls
* Source code in this project are published under this license: [License.txt](License.txt)

## System Requirement

#### To build and test this app:
* Visual Studio 2017 with latest udpate and Universal Windows App Development Tools option selected.
* Windows SDK 15063.
* Windows 10 Desktop version 1703 or later.
* Enable "Developer Mode".


#### To install and run this app:
* Windows 10 Desktop version 1703 or later.
* Enable Developer Mode.

## Build and Test on Local Machine

1. Launch Visual Studio 2017. In Solution Explorer, open the solution of this app.

2. Select target platform that match to your local machine, for example, x64.

3. Select "Build" to build the solution.

4. Press "F5" to start run and debug this app, or "Ctrl+F5" to run this app without debugging.

## Create App Package for Test and Side-load on Other Machines

#### To generate app package:
1. Launch Visual Studio 2017. In Solution Explorer, open the solution of this app.

2. Right-click the project and choose "Store > Create App Packages...".

3. In "Create App Packages" wizard dialog:

  * When asked if to upload the app package to store, choose "No"
  * At "Select and configure package" dialog, you must select the configuration mapping that matche to your target machines; choose "Never" for "Generate app bundle"; unselect "Include full PDB symbol files" to keep app package size compact.
  
4. Once app packages are generated, browse to the containing folder to get app packages.

#### To side load app package on target machine:

1. Make sure "Developer Mode" is enabled on target machine.
2. Copy app package (folder) to target machine, and browse to app package folder.
3. Find "Add-AppDevPackage.ps1" and right-click on this file and choose "Run with Powershell" to start install.
4. When installation complete, press "Windows" key and find "Sample.OpcUaPublisherDesignerApp" in app list.

##### [NOTE]
When building and generating appx package using above steps, the appx package is only for development/test purpose, and cannot be used for distribution.

## To Run This App

1. Make sure your target Opcua servers are up and running, and the networking between this app and servers is OK.
2. Launch DesignerApp. Click on left-top corner hamberbur icon to show function menu. Click on "Open Profile" to show profile management panel, and create a profile with a desired name, and press "Save".
4. In DesignerApp main window, to add new server session, click "+" to show server connection window:
    * In server connection window, enter session name, and paste target server url at connection text box, and press "Connect" to connect to server.
    * You will be prompted to ask if to accept an untrusted connection to the server, press "Yes", and later will be prompted to ask if you would like to save this server certificate, press "Yes".
    * When DesignerApp tries to connect to server, the target Opcua server might also need to get your consent to accept untrusted client to connect, choose "Yes" to establish session between client and server. Sometimes this dialog window will not be shown as top-most window, hence you must find this dialog yourself to response, or Opcua server will wait and client connection will then gets timeout error or authentication error.
    * Once connection is established, you may browse data from server at "Opc.Ua Source Tree" block, and select the node you would like to subscribe, and press "+" to pick this node into subscription list. A dialog will be popup to ask for a friendly name for this data.
    * Once all required data nodes are picked into subscription list, press "Save" and get back to main window.
5. You may repeat step 4 to add as many server session as you need. All nodes subscribed from these server sessions can then be exported in one configuration file.
6. In DesignerApp main window, Press "|<-" icon to export all selected nodes to your target json file location.
7. Copy the exported node selection json file to target IoT Edge device storage, which will be later accessed by OpcPublisher module, for example: ``c:\data\gateway.temp\publishednodes.json``
8. If you want to export json file directly to target device, for example, an IoT Edge device, you must first enable the network share to target device, and when select export target location, for example "``\\<target_device_ip\c$\data\gateway.temp``", and to save file directly on target machine storage.
    * To enable network share on your host machine to connect to an IoT Edge device with IoT Core platform:
    
        ```
        > net use "\\<target_device_ip>\c$" /user:administrator
        ```
    
    * To disable the network share on your host machine:
    
        ```
        > net use "\\<target_device_ip>\c$" /delete
        ```


##### [NOTE]

When running this app to connect to Opcua Server on the same machine, you must execute the following command to allow UWP app to access to localhost ports.

    
    > CheckNetIsolation.exe LoopbackExempt –a –n=Sample.OpcUaPublisherDesignerApp_040afsa2n4qpw

If you have made changes in UWP Package.appxmanifest to modify publisher name or app name, then the package family name (Sample.OpcUaPublisherDesignerApp_040afsa2n4qpw) will be changed, you must use your actual UWP package family name in the above command.



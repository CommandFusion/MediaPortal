# MediaPortal Control

## Contents
* [Overview](#Overview)
* [API Documentation][API]



## <a name="Overview" />Overview
This project allows for control of MediaPortal software on Windows&trade; PC's via CommandFusion iViewer.

The project is split into 3 parts:

1. [MediaPortal Plugin](#plugin)
1. [iViewer JavaScript file](#js)
1. [CF iViewer Project](#iviewer)

### <a name="plugin" />MediaPortal Plugin
We have developed a plugin for MediaPortal that allows full two-way control via a TCP Socket.  
It also includes a web server to serve up art work for movie artwork, etc.  
The plugin has a configuration interface to allow easy configuration.

### <a name="js" />iViewer JavaScript File
We have developed a JavaScript file for iViewer that handles all the communication with the [MediaPortal plugin](#plugin)

### <a name="iviewer" />CF iViewer Project
To go along with the [JavaScript file](#js), we have created a sample iViewer project.  
This project can be used as is, or used for reference on how to embed MediaPortal control into your own project.

[API]: http://github.com/CommandFusion/MediaPortal/wiki
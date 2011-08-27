Imports System.IO
Imports System.Net
Imports System.Threading
Imports System.Windows.Forms
Imports System.Xml
Imports System.Text.RegularExpressions

Imports MediaPortal.GUI.Library
Imports MediaPortal.Dialogs
Imports MediaPortal.Configuration
Imports MediaPortal.InputDevices
Imports MediaPortal.Player
Imports MediaPortal.Playlists
Imports WindowPlugins.GUITVSeries

Public Class MPCommandFusion
    Implements ISetupForm
    Implements IPlugin

    Private isMovingPicturesPresent As Boolean = Nothing
    Private isTVSeriesPresent As Boolean = Nothing
    Private isFanartHandlerPresent As Boolean = Nothing
    Private remoteHandler As InputHandler
    Private WithEvents tcp As New TCPComms
    Public EOM As String = Chr(245) & Chr(245)
    Private _playlistPlayer As MediaPortal.Playlists.PlayListPlayer

    Const DEFAULT_PORT As Integer = 8020
    Const SUPPORTED_MOVING_PICTURES_MINVERSION As String = "1.0.6.1116"
    Const SUPPORTED_TV_SERIES_MINVERSION As String = "2.6.3.1242"
    Const SUPPORTED_FANART_HANDLER_MINVERSION As String = "2.2.1.19191"
    Private port As Integer = DEFAULT_PORT

    Public WithEvents webServ As Webserver

#Region "HELPER FUNCTIONS"

    Public Shared Function IsPluginLoaded(ByVal dll As String, ByVal minVersion As String, Optional ByVal type As String = "windows") As Boolean

        Dim filename As String = String.Format("{0}\{1}\{2}", Config.Dir.Plugins, type, dll)

        If File.Exists(filename) Then
            Dim fileVersionInfo As FileVersionInfo = fileVersionInfo.GetVersionInfo(filename)
            Dim maj As Integer = Split(minVersion, ".")(0)
            Dim min As Integer = Split(minVersion, ".")(1)
            Dim bld As Integer = Split(minVersion, ".")(2)
            Dim rev As Integer = Split(minVersion, ".")(3)
            Log.Info(String.Format("plugin: MPCommandFusion - Plugin found : {0} ({1}.{2}.{3}.{4})", dll, fileVersionInfo.FileMajorPart, fileVersionInfo.FileMinorPart, fileVersionInfo.FileBuildPart, fileVersionInfo.FilePrivatePart))
            If PluginManager.IsPlugInEnabled(dll) Then
                If (fileVersionInfo.FileMajorPart > maj) Then Return True
                If (fileVersionInfo.FileMajorPart < maj) Then Return False
                If (fileVersionInfo.FileMinorPart > min) Then Return True
                If (fileVersionInfo.FileMinorPart < min) Then Return False
                If (fileVersionInfo.FileBuildPart > bld) Then Return True
                If (fileVersionInfo.FileBuildPart < bld) Then Return False
                If (fileVersionInfo.FilePrivatePart >= rev) Then Return True
            End If
        End If

        Return False
    End Function

    Public Function GenerateReadable(ByVal bytes As Byte()) As String
        Dim tmpMsg As String = System.Text.Encoding.ASCII.GetString(bytes)
        Dim i As Integer = 0
        Dim readable As String = ""
        For Each aByte As Byte In bytes
            If Int32.Parse(aByte) >= 33 And Int32.Parse(aByte) < 127 Then
                readable &= tmpMsg(i)
            Else
                readable &= "\x" & Conversion.Hex(aByte).PadLeft(2, "0")
            End If
            'hexonly &= Conversion.Hex(aByte).PadLeft(2, "0")
            i += 1
        Next
        Return readable
    End Function

    Public Function ToAscii(ByVal Data As String) As String
        Return System.Text.Encoding.ASCII.GetString(System.Text.Encoding.Unicode.GetBytes(Data))
    End Function

    Public Function BytesToAscii(ByVal Data As Byte()) As String
        Return System.Text.Encoding.ASCII.GetString(Data)
    End Function

    Private Sub SetClipboardText(ByVal text As String)
        'My.Computer.Clipboard.SetText(text)
    End Sub
#End Region

#Region "PLUGIN IMPLEMENTATION"

    Public Function Author() As String Implements MediaPortal.GUI.Library.ISetupForm.Author
        Return "CommandFusion"
    End Function

    Public Function CanEnable() As Boolean Implements MediaPortal.GUI.Library.ISetupForm.CanEnable
        Return True
    End Function

    Public Function DefaultEnabled() As Boolean Implements MediaPortal.GUI.Library.ISetupForm.DefaultEnabled
        Return True
    End Function

    Public Function Description() As String Implements MediaPortal.GUI.Library.ISetupForm.Description
        Return "Provides a TCP Server for CommandFusion software to connect and control MediaPortal."
    End Function

    Public Function GetHome(ByRef strButtonText As String, ByRef strButtonImage As String, ByRef strButtonImageFocus As String, ByRef strPictureImage As String) As Boolean Implements MediaPortal.GUI.Library.ISetupForm.GetHome
        strButtonText = String.Empty
        strButtonImage = String.Empty
        strButtonImageFocus = String.Empty
        strPictureImage = String.Empty
        Return False
    End Function

    Public Function GetWindowId() As Integer Implements MediaPortal.GUI.Library.ISetupForm.GetWindowId
        Return -1
    End Function

    Public Function HasSetup() As Boolean Implements MediaPortal.GUI.Library.ISetupForm.HasSetup
        Return True
    End Function

    Public Function PluginName() As String Implements MediaPortal.GUI.Library.ISetupForm.PluginName
        Return "MPCommandFusion"
    End Function

    Public Sub ShowPlugin() Implements MediaPortal.GUI.Library.ISetupForm.ShowPlugin
        Using setupForm As Form = New Setup
            setupForm.ShowDialog()
        End Using
    End Sub
#End Region

    Public Sub Start() Implements MediaPortal.GUI.Library.IPlugin.Start
        remoteHandler = New InputHandler("CommandFusion")
        AddHandler g_Player.PlayBackChanged, AddressOf PlaybackChanged
        AddHandler g_Player.PlayBackStarted, AddressOf PlaybackStarted
        DoStart()
    End Sub

    Public Sub [Stop]() Implements MediaPortal.GUI.Library.IPlugin.Stop
        DoStop()
    End Sub

    Private Sub DoStart()

        Using xmlReader As MediaPortal.Profile.Settings = New MediaPortal.Profile.Settings(Config.GetFile(Config.Dir.Config, "MediaPortal.xml"))
            port = xmlReader.GetValueAsInt("MPCommandFusion", "TCPPort", DEFAULT_PORT)
        End Using

        If isMovingPicturesPresent = Nothing Then
            isMovingPicturesPresent = IsPluginLoaded("MovingPictures.dll", SUPPORTED_MOVING_PICTURES_MINVERSION)
            Log.Info("plugin: MPCommandFusion - MovingPictures detected, min version: {0} supported: {1}", SUPPORTED_MOVING_PICTURES_MINVERSION, isMovingPicturesPresent)
        End If

        If isTVSeriesPresent = Nothing Then
            isTVSeriesPresent = IsPluginLoaded("MP-TVSeries.dll", SUPPORTED_TV_SERIES_MINVERSION)
            Log.Info("plugin: MPCommandFusion - TVSeries detected,  min version: {0} supported: {1}", SUPPORTED_TV_SERIES_MINVERSION, isTVSeriesPresent.ToString)
        End If

        If isFanartHandlerPresent = Nothing Then
            isFanartHandlerPresent = IsPluginLoaded("FanartHandler.dll", SUPPORTED_FANART_HANDLER_MINVERSION, "process")
            Log.Info("plugin: MPCommandFusion - FanartHandler detected, min version: {0} supported: {1}", SUPPORTED_FANART_HANDLER_MINVERSION, isFanartHandlerPresent.ToString)
        End If

        tcp.Port = port
        tcp.Init()
        Log.Info("plugin: MPCommandFusion - started TCP listening on port {0}", port.ToString)

        webServ = New Webserver(port + 1)
        webServ.StartServer()
        Log.Info("plugin: MPCommandFusion - started Web Server listening on port {0}", webServ.Port.ToString)
    End Sub

    Private Sub DoStop()
        Try
            tcp.Close()
            Log.Info("plugin: MPCommandFusion - stopped TCP listening")
        Catch ex As Exception
        End Try
        Try
            webServ.StopServer()
            Log.Info("plugin: MPCommandFusion - stopped web server")
        Catch ex As Exception
        End Try
    End Sub

    Public Overridable Sub DataReceived(ByVal connID As Long, ByVal remoteIP As String, ByVal data As Byte()) Handles tcp.DataReceived
        Dim msg As String = GenerateReadable(data)
        Dim bytes As String = System.Text.Encoding.GetEncoding(1252).GetString(data)

        'Dim msgs As String() = System.Text.RegularExpressions.Regex.Split(msg, "(" & System.Text.RegularExpressions.Regex.Escape(Me.EOM) & ")")
        Dim msgs As String() = bytes.Split(New String() {Me.EOM}, StringSplitOptions.RemoveEmptyEntries)
        Dim regexp As New Regex("\xF3(\w+)\xF4(.*)\xF5\xF5", RegexOptions.Compiled)
        For Each aMsg As String In msgs
            aMsg &= Chr(245) & Chr(245)
            ' Ensure data is in correct format
            If Not regexp.IsMatch(aMsg) Then
                ' incoming data in incorrect format
                Log.Info("plugin: MPCommandFusion - Received Incorrectly Formatted Message [" & remoteIP & "]: " & aMsg)
                Continue For
            End If

            Dim matchGroups As GroupCollection = regexp.Match(aMsg).Groups
            ' What did the client request?
            Select Case matchGroups(1).Value
                Case "TNAV" ' Navigation button request sent
                    ' Get the action
                    Dim btn As RemoteButton
                    Select Case matchGroups(2).Value.ToLower
                        Case "stop"
                            g_Player.Stop()
                            btn = RemoteButton.Stop
                        Case "record"
                            btn = RemoteButton.Record
                        Case "playpause"
                            _playlistPlayer = New MediaPortal.Playlists.PlayListPlayer
                            _playlistPlayer = MediaPortal.Playlists.PlayListPlayer.SingletonPlayer
                            _playlistPlayer.RepeatPlaylist = False
                            _playlistPlayer.GetPlaylist(MediaPortal.Playlists.PlayListType.PLAYLIST_VIDEO)
                            If MediaPortal.Player.g_Player.Playing Or MediaPortal.Player.g_Player.Paused Then
                                btn = RemoteButton.Pause
                            ElseIf _playlistPlayer.GetPlaylist(MediaPortal.Playlists.PlayListType.PLAYLIST_VIDEO).Count <> 0 Then
                                btn = RemoteButton.Play
                            End If
                        Case "pause"
                            btn = RemoteButton.Pause
                        Case "play"
                            btn = RemoteButton.Play
                        Case "rewind"
                            btn = RemoteButton.Rewind
                        Case "forward"
                            btn = RemoteButton.Forward
                        Case "replay"
                            btn = RemoteButton.Replay
                        Case "skip"
                            btn = RemoteButton.Skip
                        Case "back"
                            btn = RemoteButton.Back
                        Case "info"
                            btn = RemoteButton.Info
                        Case "up"
                            btn = RemoteButton.Up
                        Case "down"
                            btn = RemoteButton.Down
                        Case "left"
                            btn = RemoteButton.Left
                        Case "right"
                            btn = RemoteButton.Right
                        Case "ok"
                            btn = RemoteButton.Ok
                        Case "volup"
                            btn = RemoteButton.VolumeUp
                        Case "voldown"
                            btn = RemoteButton.VolumeDown
                        Case "volmute"
                            btn = RemoteButton.Mute
                        Case "chup"
                            btn = RemoteButton.ChannelUp
                        Case "chdown"
                            btn = RemoteButton.ChannelDown
                        Case "dvdmenu"
                            btn = RemoteButton.DVDMenu
                        Case "0"
                            btn = RemoteButton.NumPad0
                        Case "1"
                            btn = RemoteButton.NumPad1
                        Case "2"
                            btn = RemoteButton.NumPad2
                        Case "3"
                            btn = RemoteButton.NumPad3
                        Case "4"
                            btn = RemoteButton.NumPad4
                        Case "5"
                            btn = RemoteButton.NumPad5
                        Case "6"
                            btn = RemoteButton.NumPad6
                        Case "7"
                            btn = RemoteButton.NumPad7
                        Case "8"
                            btn = RemoteButton.NumPad8
                        Case "9"
                            btn = RemoteButton.NumPad9
                        Case "*"
                            btn = RemoteButton.Star
                        Case "#"
                            btn = RemoteButton.Hash
                        Case "clear"
                            btn = RemoteButton.Clear
                        Case "enter"
                            btn = RemoteButton.Enter
                        Case "teletext"
                            btn = RemoteButton.Teletext
                        Case "red"
                            btn = RemoteButton.Red
                        Case "blue"
                            btn = RemoteButton.Blue
                        Case "yellow"
                            btn = RemoteButton.Yellow
                        Case "green"
                            btn = RemoteButton.Green
                        Case "home"
                            btn = RemoteButton.Home
                        Case "basichome"
                            btn = RemoteButton.BasicHome
                        Case "plugins"
                            btn = RemoteButton.Plugins
                        Case "pictures"
                            btn = RemoteButton.MyPictures
                        Case "music"
                            btn = RemoteButton.MyMusic
                        Case "nowplaying"
                            btn = RemoteButton.NowPlaying
                        Case "radio"
                            btn = RemoteButton.MyRadio
                        Case "tv"
                            btn = RemoteButton.MyTV
                        Case "tvguide"
                            btn = RemoteButton.Guide
                        Case "tvrecs"
                            btn = RemoteButton.RecordedTV
                        Case "videos"
                            btn = RemoteButton.MyVideos
                        Case "tvseries"
                            btn = RemoteButton.TVSeries
                        Case "weather"
                            btn = RemoteButton.Weather
                        Case "movingpictures"
                            btn = RemoteButton.MovingPictures
                        Case "dvd"
                            btn = RemoteButton.PlayDVD
                        Case "playlists"
                            btn = RemoteButton.MyPlaylists
                        Case "f2"
                            btn = RemoteButton.F2
                        Case Else
                            btn = RemoteButton.Ok
                    End Select

                    remoteHandler.MapAction(btn)
                    System.Threading.Thread.Sleep(100)
                    Log.Info("plugin: MPCommandFusion - Pressed button {0}", btn.ToString)

                Case "TGETLIST" ' Status request sent
                    Dim params() As String = matchGroups(2).Value.Split("|")
                    Dim response As String = ""
                    Select Case params(0)
                        Case "allmovies"
                            ' check for limit params
                            If params.Length = 2 Then
                                If Not IsNumeric(params(1)) Then
                                    Log.Info("plugin: MPCommandFusion - Non-numeric parameters received in call '" & params(1) & "'")
                                Else
                                    ' Get list of artists, in blocks of params(1)
                                    response = Movies.GetAllMovies(params(1))
                                End If
                            Else
                                ' Get list of artists, one at a time
                                response = Movies.GetAllMovies()
                            End If
                        Case "alltvseries"
                            ' check for limit params
                            If params.Count = 2 Then
                                If Not IsNumeric(params(1)) Then
                                    Log.Info("plugin: MPCommandFusion - Non-numeric parameters received in call '{0}'", params(0))
                                Else
                                    response = TVSeries.GetAllSeries(params(1))
                                End If
                            Else
                                response = TVSeries.GetAllSeries()
                            End If
                        Case "alltvseasons"
                            If params.Count = 2 Then
                                If IsNumeric(params(1)) Then
                                    response = TVSeries.GetAllSeasons(params(1))
                                Else
                                    Log.Info("plugin: MPCommandFusion - Non-numeric series ID received {0}", params(1))
                                End If
                            Else
                                Log.Info("plugin: MPCommandFusion - Missing parameters in call '{0}'", params(0))
                            End If
                        Case "alltvepisodes"
                            If params.Count = 3 Then
                                If IsNumeric(params(1)) And IsNumeric(params(2)) Then
                                    response = TVSeries.GetAllEpisodes(params(1), params(2))
                                Else
                                    Log.Info("plugin: MPCommandFusion - Non-numeric series ID or Episode index received {0}, {1}", params(1), params(2))
                                End If
                            Else
                                Log.Info("plugin: MPCommandFusion - Missing parameters in call '{0}'", params(0))
                            End If
                    End Select
                    If response <> "" Then
                        tcp.SendMsg(response, connID)
                    End If
                Case "TGETMOVIE" ' Request the movie info for a specific movie ID
                    Dim response As String = Movies.GetVideoInfo(matchGroups(2).Value)
                    If response <> "" Then
                        tcp.SendMsg(response, connID)
                    End If
                Case "TSEARCHMOVIES" ' Search for movies
                    Dim params() As String = matchGroups(2).Value.ToLower.Split("|")
                    Dim response As String
                    If params.Count >= 2 Then
                        response = Movies.Search(params(0), params(1))
                    Else
                        response = Movies.Search(params(0))
                    End If

                    If response <> "" Then
                        tcp.SendMsg(response, connID)
                    End If
                Case "TPLAY" ' Request to play a media item
                    Dim params() As String = matchGroups(2).Value.ToLower.Split("|")
                    Dim response As String = ""
                    Select Case params(0)
                        Case "tvepisode"
                            If params.Count = 4 Then
                                Dim theEp As DBEpisode = TVSeries.GetEpisode(params(1), params(2), params(3))
                                If theEp IsNot Nothing Then
                                    If g_Player.Playing Then g_Player.Stop()

                                    _playlistPlayer = New MediaPortal.Playlists.PlayListPlayer
                                    _playlistPlayer = MediaPortal.Playlists.PlayListPlayer.SingletonPlayer
                                    _playlistPlayer.RepeatPlaylist = False
                                    _playlistPlayer.GetPlaylist(MediaPortal.Playlists.PlayListType.PLAYLIST_VIDEO).Clear()

                                    Dim item As New MediaPortal.Playlists.PlayListItem
                                    item.FileName = theEp.Item(DBEpisode.cFilename).ToString
                                    item.Type = MediaPortal.Playlists.PlayListItem.PlayListItemType.Video
                                    _playlistPlayer.GetPlaylist(MediaPortal.Playlists.PlayListType.PLAYLIST_VIDEO).Add(item)

                                    If _playlistPlayer.GetPlaylist(MediaPortal.Playlists.PlayListType.PLAYLIST_VIDEO).Count > 0 Then
                                        _playlistPlayer.Reset()
                                        _playlistPlayer.CurrentPlaylistType = MediaPortal.Playlists.PlayListType.PLAYLIST_VIDEO
                                        _playlistPlayer.Play(0)
                                    End If
                                Else
                                    Log.Info("plugin: MPCommandFusion - TV Episode could not be found {0}", params(1) & "s" & params(2) & "x" & params(3))
                                End If
                            End If
                        Case "movie"
                            If params.Count = 2 Then
                                Movies.PlayMovie(params(1))
                            Else
                                Log.Info("plugin: MPCommandFusion - Missing ID from movie play command")
                            End If
                    End Select
                Case "TVOL"
                    VolumeHandler.Instance.Volume = Math.Floor(CInt(matchGroups(2).Value) * VolumeHandler.Instance.Maximum / 100.0)
                    Dim response As String = "\xF3RVOL\xF4" & CInt(Math.Floor(100.0 * VolumeHandler.Instance.Volume / VolumeHandler.Instance.Maximum)) & "\xF5\xF5"
                    If response <> "" Then
                        tcp.SendMsg(response)
                    End If
                Case "TGETVOL"
                    Dim response As String = "\xF3RVOL\xF4" & CInt(Math.Floor(100.0 * VolumeHandler.Instance.Volume / VolumeHandler.Instance.Maximum)) & "\xF5\xF5"
                    If response <> "" Then
                        tcp.SendMsg(response)
                    End If
            End Select
        Next
        Log.Info("plugin: MPCommandFusion - Received [" & remoteIP & "]: " & msg)
    End Sub

#Region "MP Events"
    Public Sub PlaybackChanged(ByVal type As MediaPortal.Player.g_Player.MediaType, ByVal stoptime As Integer, ByVal filename As String)
        'MsgBox(MediaPortal.Player.g_Player.Playing)
        'MsgBox("PlaybackItemChanged: " & type.ToString)
        If type = g_Player.MediaType.Video Then
            'MsgBox(g_Player.currentDescription)
        End If
    End Sub



    Public Sub PlaybackStarted(ByVal type As MediaPortal.Player.g_Player.MediaType, ByVal filename As String)
        'MsgBox("PlaybackStarted: " & type.ToString)
        Dim response As String
        If type = g_Player.MediaType.Video Then
            Dim sqlCondition As New SQLCondition
            sqlCondition.Add(New DBEpisode(), DBEpisode.cFilename, g_Player.Player.CurrentFile.ToString, SQLConditionType.Equal)

            Dim episodeList As List(Of DBEpisode) = DBEpisode.Get(sqlCondition)
            If (episodeList.Count > 0) Then
                Dim series As DBSeries = DBSeries.Get(episodeList(0).onlineEpisode.Item(DBOnlineEpisode.cSeriesID).ToString)
                response = "\xF3RNOWPLAYING\xF4tvepisode|" & episodeList(0).onlineEpisode.Item(DBOnlineEpisode.cSeriesID).ToString & _
                    "|" & episodeList(0).onlineEpisode.Item(DBOnlineEpisode.cSeasonIndex).ToString & _
                    "|" & episodeList(0).onlineEpisode.Item(DBOnlineEpisode.cEpisodeIndex).ToString & _
                    "|" & episodeList(0).onlineEpisode.Item(DBOnlineEpisode.cEpisodeName).ToString & _
                    "|" & episodeList(0).onlineEpisode.Item(DBOnlineEpisode.cEpisodeSummary).ToString & _
                    "|" & episodeList(0).onlineEpisode.Item(DBOnlineEpisode.cFirstAired).ToString & _
                    "|" & episodeList(0).onlineEpisode.Item(DBOnlineEpisode.cRating).ToString & _
                    "|" & series.Item("Pretty_Name").ToString & "\xF5\xF5"
                tcp.SendMsg(response)
            End If
        End If
    End Sub

#End Region

#Region "Remote Control"

    Private Enum RemoteButton
        'Same as MCE buttons
        None = 0
        Power1 = 165
        Power2 = 12
        PowerTV = 101
        Record = 23
        Pause = 24
        [Stop] = 25
        Rewind = 21
        Play = 22
        Forward = 20
        Replay = 27
        Skip = 26
        Back = 35
        Up = 30
        Info = 15
        Left = 32
        Ok = 34
        Right = 33
        Down = 31
        VolumeUp = 16
        VolumeDown = 17
        Start = 13
        ChannelUp = 18
        ChannelDown = 19
        Mute = 14
        RecordedTV = 72
        Guide = 38
        LiveTV = 37
        DVDMenu = 36
        NumPad1 = 1
        NumPad2 = 2
        NumPad3 = 3
        NumPad4 = 4
        NumPad5 = 5
        NumPad6 = 6
        NumPad7 = 7
        NumPad8 = 8
        NumPad9 = 9
        NumPad0 = 0
        Oem8 = 29
        OemGate = 28
        Clear = 10
        Enter = 11
        Teletext = 90
        Red = 91
        Green = 92
        Yellow = 93
        Blue = 94

        ' MCE keyboard specific
        MyTV = 70
        MyMusic = 71
        MyPictures = 73
        MyVideos = 74
        MyRadio = 80
        Messenger = 105

        ' Special OEM buttons
        AspectRatio = 39 ' FIC Spectra
        Print = 78 ' Hewlett Packard MCE Edition

        'MPClientController specific mappings
        Home = 800
        BasicHome = 801
        Weather = 802
        Plugins = 803
        Star = 804
        Hash = 805
        TVSeries = 806
        MovingPictures = 807
        NowPlaying = 808
        PlayDVD = 809
        MyPlaylists = 810

        'Try some keyboard keys
        F2 = 820

    End Enum

    Private Shared Function SendKeystring(ByVal request As String) As String

        Dim keystring As Integer = 0
        Dim modifiers As Integer = 0
        Dim modifier As String = String.Empty

        If InStr(request, "+") > 0 Then
            modifier = Split(request, "+")(0)
            request = Split(request, "+")(1)
        End If

        Select Case modifier.ToLower
            Case "ctrl"
                modifier = 2
            Case "shift"
                modifier = 1
            Case "alt"
                modifier = 0
            Case Else
                modifier = 0
        End Select

        Select Case request.ToLower
            Case "0"
                keystring = Keys.D0
            Case "1"
                keystring = Keys.D1
            Case "2"
                keystring = Keys.D2
            Case "3"
                keystring = Keys.D3
            Case "4"
                keystring = Keys.D4
            Case "5"
                keystring = Keys.D5
            Case "6"
                keystring = Keys.D6
            Case "7"
                keystring = Keys.D7
            Case "8"
                keystring = Keys.D8
            Case "9"
                keystring = Keys.D9

            Case "a"
                keystring = Keys.A
            Case "b"
                keystring = Keys.B
            Case "c"
                keystring = Keys.C
            Case "d"
                keystring = Keys.D
            Case "e"
                keystring = Keys.E
            Case "f"
                keystring = Keys.F
            Case "g"
                keystring = Keys.G
            Case "h"
                keystring = Keys.H
            Case "i"
                keystring = Keys.I
            Case "j"
                keystring = Keys.J
            Case "k"
                keystring = Keys.K
            Case "l"
                keystring = Keys.L
            Case "m"
                keystring = Keys.M
            Case "n"
                keystring = Keys.N
            Case "o"
                keystring = Keys.O
            Case "p"
                keystring = Keys.P
            Case "q"
                keystring = Keys.Q
            Case "r"
                keystring = Keys.R
            Case "s"
                keystring = Keys.S
            Case "t"
                keystring = Keys.T
            Case "u"
                keystring = Keys.U
            Case "v"
                keystring = Keys.V
            Case "w"
                keystring = Keys.W
            Case "x"
                keystring = Keys.X
            Case "y"
                keystring = Keys.Y
            Case "z"
                keystring = Keys.Z

            Case "f1"
                keystring = Keys.F1
            Case "f2"
                keystring = Keys.F2
            Case "f3"
                keystring = Keys.F3
            Case "f4"
                keystring = Keys.F4
            Case "f5"
                keystring = Keys.F5
            Case "f6"
                keystring = Keys.F6
            Case "f7"
                keystring = Keys.F7
            Case "f8"
                keystring = Keys.F8
            Case "f9"
                keystring = Keys.F9
            Case "f10"
                keystring = Keys.F10
            Case "f11"
                keystring = Keys.F11
            Case "f12"
                keystring = Keys.F12

            Case "pageup"
                keystring = Keys.PageUp
            Case "pagedown"
                keystring = Keys.PageDown
            Case "tab"
                keystring = Keys.Tab
            Case "esc", "escape"
                keystring = Keys.Escape
            Case "home"
                keystring = Keys.Home
            Case "end"
                keystring = Keys.End
            Case "del", "delete"
                keystring = Keys.Delete
            Case "enter", "return", "rtn"
                keystring = Keys.Enter
            Case " ", "space"
                keystring = Keys.Space

            Case Else
                keystring = 0

        End Select

        Dim key As MediaPortal.GUI.Library.Key = New MediaPortal.GUI.Library.Key(keystring + (modifiers * 32), 0)
        Dim action As MediaPortal.GUI.Library.Action = New Action(key, MediaPortal.GUI.Library.Action.ActionType.ACTION_KEY_PRESSED, 0, 0)

        GUIWindowManager.OnAction(action)

        'SendKeys.Send(String.Format("{0}{1}", modifier, keystring))
        System.Threading.Thread.Sleep(100)
        Log.Debug("plugin: MPCommandFusion - Sent keystring - modifier {0}, keychar {1}", modifier, keystring)
        Log.Debug("plugin: MPCommandFusion - {0}", action.ToString)

        Return Nothing
    End Function

#End Region

End Class

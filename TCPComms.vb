Imports System.Net.Sockets.Socket
Imports System.Text
Imports System.Net
Imports System.Net.Sockets
Imports System.Runtime.InteropServices
Imports System.Threading

Imports MediaPortal.GUI.Library

Public Class TCPComms

    Private m_Port As Integer = 8024
    Private m_IPAddress As String = "255.255.255.255"
    Private m_Receiving As Boolean = False
    Private m_PauseSending As Boolean = False

    Private tcpLsn As TcpListener
    Private tcpStream As NetworkStream
    Private tcpSend As Thread
    Private tcpReceiveParse As Thread
    Private tcpAccept As Thread

    Public Event Listening(ByVal Status As Boolean)
    Public Event DataReceivedRaw(ByVal IPAddress As String, ByVal Port As Integer, ByVal Data As String)
    Public Event DataReceived(ByVal connID As Long, ByVal IPAddress As String, ByVal MsgBytes As Byte())
    Public Event DataSent(ByVal IPAddress As String, ByVal Data As String)
    Public Event [Error](ByVal ErrorMsg As String)
    Public Event ListeningBroadcast(ByVal Status As Boolean, ByVal Port As Integer)
    Public Event ClientConnected()
    Public Event ClientDisconnected()

    Private m_SendQueue As New ArrayList
    Private m_ReceiveQueue As New ArrayList

    ' This stores data about each client
    Public dataHolder As New Hashtable
    Private Shared connectId As Long = 0
    Public Structure ClientData
        Public structSocket As Socket
        Public structThread As Thread
        Public structIP As String
        Public connID As Long
    End Structure 'ClientData

    Public ReadOnly Property Receiving() As Boolean
        Get
            Return m_Receiving
        End Get
    End Property

    Public Property PauseSending() As Boolean
        Get
            Return m_PauseSending
        End Get
        Set(ByVal value As Boolean)
            m_PauseSending = value
        End Set
    End Property

    Public Property IPAddress() As String
        Get
            Return m_IPAddress
        End Get
        Set(ByVal value As String)
            m_IPAddress = value
        End Set
    End Property

    Public Property Port() As Integer
        Get
            Return m_Port
        End Get
        Set(ByVal value As Integer)
            m_Port = value
        End Set
    End Property

    Public Sub Close()
        Try
            tcpReceiveParse.Abort()
        Catch ex As Exception
        End Try

        Try
            tcpSend.Abort()
        Catch ex As Exception
        End Try

        Try
            'Abort the "Waiting for clients" thread
            tcpAccept.Abort()
        Catch ex As Exception
        End Try

        Try
            Me.tcpStream.Close()
        Catch ex As Exception
        End Try

        Try
            tcpLsn.Stop()
        Catch ex As Exception
        End Try

        Try
            SyncLock Me
                For Each clntData As ClientData In dataHolder.Values
                    clntData.structSocket.Close()
                    Try
                        clntData.structThread.Abort()
                    Catch ex As Exception
                    End Try
                Next clntData
                dataHolder.Clear()
            End SyncLock
        Catch ex As Exception
        End Try

        RaiseEvent Listening(False)
    End Sub

    Public Function GetClient(ByVal remoteIP As String) As ClientData
        For Each aClient As ClientData In dataHolder
            If aClient.structIP = remoteIP Then
                Return aClient
            End If
        Next
        Return Nothing
    End Function

    Public Sub WaitingForClient()
        Dim CData As ClientData

        RaiseEvent Listening(True)

        dataHolder.Clear()

        While True
            Try

                ' AcceptSocket will block until someone connects 
                CData.structSocket = tcpLsn.AcceptSocket()
                Interlocked.Increment(connectId)
                CData.structIP = CType(CData.structSocket.RemoteEndPoint(), System.Net.IPEndPoint).Address.ToString()
                CData.connID = connectId
                CData.structThread = New Thread(AddressOf ReadSocket)
                CData.structThread.IsBackground = True
                CData.structThread.SetApartmentState(ApartmentState.STA)

                SyncLock Me
                    ' it is used to keep connected Sockets and active thread
                    dataHolder.Add(connectId, CData)
                End SyncLock

                CData.structThread.Start()

                RaiseEvent ClientConnected()
            Catch ex As Exception
                If Not TypeOf ex Is ThreadAbortException Then
                    Log.Info("plugin: MPCommandFusion - " & ex.ToString)
                End If
            End Try
        End While
    End Sub 'WaitingForClient

    Public Sub SendMsg(ByVal msg As String)
        m_SendQueue.Add(msg)
        m_SendQueue.Add(0)
        m_SendQueue.Add(0)
        Log.Info("plugin: MPCommandFusion - Sending {0}", msg)
    End Sub

    Public Sub SendMsg(ByVal msg As String, ByVal connID As Long)
        m_SendQueue.Add(msg)
        m_SendQueue.Add(connID)
        m_SendQueue.Add(port)
    End Sub

    Private Sub Send()
        While True
            If dataHolder.Count = 0 Then
                ' Wait for connection to be established
                Thread.Sleep(1000)
            Else
                If m_SendQueue.Count > 0 And Not m_PauseSending Then
                    Dim TCPmsg As String = m_SendQueue(0)
                    m_SendQueue.RemoveAt(0)
                    Dim connID As Long = m_SendQueue(0)
                    m_SendQueue.RemoveAt(0)
                    Dim thePort As Integer = m_SendQueue(0)
                    m_SendQueue.RemoveAt(0)

                    Try
                        Dim newBytes As New List(Of Byte)

                        For i As Integer = 0 To TCPmsg.Length - 1
                            If TCPmsg(i) = "\" AndAlso TCPmsg(i + 1) = "x" Then
                                Dim hexChars As String = TCPmsg(i + 2) & TCPmsg(i + 3)
                                newBytes.Add(Byte.Parse(hexChars, System.Globalization.NumberStyles.HexNumber))
                                i += 3
                            Else
                                newBytes.Add(Convert.ToByte(TCPmsg(i)))
                            End If
                        Next

                        Dim sendBytes(newBytes.Count - 1) As [Byte]
                        newBytes.CopyTo(sendBytes)

                        For Each aClient As ClientData In dataHolder.Values
                            If connID = 0 Then
                                ' send to all clients
                                Try
                                    aClient.structSocket.Send(sendBytes, sendBytes.Length, SocketFlags.None)
                                    RaiseEvent DataSent(aClient.structIP, TCPmsg)
                                Catch ex As Exception
                                    RaiseEvent [Error]("TCP data could not be sent!")
                                End Try
                            Else
                                ' send to specific client
                                If aClient.connID = connID Then
                                    Try
                                        aClient.structSocket.Send(sendBytes, sendBytes.Length, SocketFlags.None)
                                        RaiseEvent DataSent(aClient.structIP, TCPmsg)
                                    Catch ex As Exception
                                        RaiseEvent [Error]("TCP data could not be sent!")
                                    End Try
                                End If
                            End If
                        Next

                    Catch ex As Exception
                        If Not TypeOf ex Is ThreadAbortException Then
                            Log.Info("plugin: MPCommandFusion - " & ex.ToString)
                        End If
                    End Try
                Else
                    Thread.Sleep(10)
                End If
            End If
        End While
    End Sub

    'Private Sub ParseReply()
    '    While True
    '        Try
    '            If m_ReceiveQueue.Count < 2 Then
    '                Thread.Sleep(1)
    '            Else
    '                ' Get data from queue
    '                Dim remoteIP As String = m_ReceiveQueue(1)
    '                Dim receiveBytes() As Byte = m_ReceiveQueue(0)

    '                ' Remove from queue
    '                m_ReceiveQueue.RemoveAt(0)
    '                m_ReceiveQueue.RemoveAt(0)

    '                If receiveBytes.Length > 0 Then
    '                    RaiseEvent DataReceived(remoteIP, receiveBytes)
    '                End If
    '            End If
    '        Catch ex As Exception
    '            If Not TypeOf ex Is ThreadAbortException Then
    '                Log.Info("plugin: MPCommandFusion - " & ex.ToString)
    '            End If
    '        End Try
    '    End While
    'End Sub

    Public Sub ReadSocket()
        ' realId will be not changed for each thread, but connectId is changed. it can't be used to delete object from Hashtable
        Dim realId As Long = connectId
        Dim receive() As [Byte]
        Dim cd As ClientData = CType(dataHolder(realId), ClientData)
        Dim s As Socket = cd.structSocket
        Dim ret As Integer = 0
        Dim clientIP As String

        clientIP = cd.structIP

        While True
            Try
                If s.Connected Then
                    receive = New [Byte](s.ReceiveBufferSize) {}
                    ' Receive will block until data coming ret is 0 or Exception
                    '
                    '*  happen when Socket connection is broken
                    ret = s.Receive(receive, s.ReceiveBufferSize, SocketFlags.None)
                    If ret > 0 Then
                        Array.Resize(receive, ret)
                        RaiseEvent DataReceived(realId, clientIP, receive)
                        'm_ReceiveQueue.Add(receive)
                        'm_ReceiveQueue.Add(clientIP)
                    Else
                        Exit While
                    End If
                Else
                    Exit While
                End If
            Catch ex As Exception
                If Not TypeOf ex Is ThreadAbortException Then
                    Log.Info("plugin: MPCommandFusion - " & ex.ToString)
                    Exit While
                End If
            End Try
        End While

        'Remove the client data from the data holder
        SyncLock Me
            dataHolder.Remove(connectId)
        End SyncLock

        RaiseEvent ClientDisconnected()

    End Sub 'ReadSocket

    Public Sub Init()
        Try
            tcpLsn = New TcpListener(Net.IPAddress.Any, Port)
            tcpLsn.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1)
            tcpLsn.Start()

            If tcpAccept IsNot Nothing AndAlso tcpAccept.ThreadState = ThreadState.Running Then
                tcpAccept.Abort()
                tcpAccept = Nothing
            End If
            tcpAccept = New Thread(AddressOf WaitingForClient)
            tcpAccept.Start()

            'tcpReceiveParse = New System.Threading.Thread(AddressOf ParseReply)
            'tcpReceiveParse.Start()
            tcpSend = New System.Threading.Thread(AddressOf Send)
            tcpSend.Start()
        Catch ex As Exception
            RaiseEvent Listening(False)
            RaiseEvent [Error]("TCP connection failed!")
            'MsgBox("TCP Client port could not be opened")
        End Try
    End Sub

End Class
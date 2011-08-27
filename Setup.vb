Imports MediaPortal.Configuration

Public Class Setup
    Inherits MediaPortal.UserInterface.Controls.MPConfigForm

    Private Sub SetupForm_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        LoadSettings()
    End Sub

    Private Sub SetupForm_UnLoad(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.FormClosing
        SaveSettings()
    End Sub

    Private Sub LoadSettings()
        Try
            Using xmlReader As MediaPortal.Profile.Settings = New MediaPortal.Profile.Settings(Config.GetFile(Config.Dir.Config, "MediaPortal.xml"))
                numPort.Value = xmlReader.GetValueAsInt("MPCommandFusion", "TCPPort", 8024)
            End Using
        Catch ex As Exception
            MsgBox(ex.ToString)
        End Try

    End Sub

    Private Sub SaveSettings()
        Try
            Using xmlReader As MediaPortal.Profile.Settings = New MediaPortal.Profile.Settings(Config.GetFile(Config.Dir.Config, "MediaPortal.xml"))
                xmlReader.SetValue("MPCommandFusion", "TCPPort", numPort.Value.ToString)
            End Using
        Catch ex As Exception
            MsgBox(ex.ToString)
        End Try
    End Sub

    Private Sub lblHome_LinkClicked(ByVal sender As System.Object, ByVal e As System.Windows.Forms.LinkLabelLinkClickedEventArgs) Handles lblHome.LinkClicked
        Process.Start("http://www.commandfusion.com/wiki/index.php?title=MediaPortal_Plugin")
    End Sub
End Class
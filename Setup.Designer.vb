<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class Setup
    Inherits MediaPortal.UserInterface.Controls.MPConfigForm

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.lblHome = New System.Windows.Forms.LinkLabel
        Me.lblDesc = New System.Windows.Forms.Label
        Me.numPort = New System.Windows.Forms.NumericUpDown
        Me.lblPort = New System.Windows.Forms.Label
        CType(Me.numPort, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'lblHome
        '
        Me.lblHome.AutoSize = True
        Me.lblHome.Location = New System.Drawing.Point(141, 109)
        Me.lblHome.Name = "lblHome"
        Me.lblHome.Size = New System.Drawing.Size(126, 13)
        Me.lblHome.TabIndex = 7
        Me.lblHome.TabStop = True
        Me.lblHome.Text = "MPCommandFusion Help"
        '
        'lblDesc
        '
        Me.lblDesc.Location = New System.Drawing.Point(12, 9)
        Me.lblDesc.Name = "lblDesc"
        Me.lblDesc.Size = New System.Drawing.Size(255, 52)
        Me.lblDesc.TabIndex = 6
        Me.lblDesc.Text = "Here you can adjust the TCP port that the MPCommandFusion plugin listens on." & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "The" & _
            " default is 8020." & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "The artwork web server will use this port + 1"
        '
        'numPort
        '
        Me.numPort.Location = New System.Drawing.Point(107, 73)
        Me.numPort.Maximum = New Decimal(New Integer() {65535, 0, 0, 0})
        Me.numPort.Minimum = New Decimal(New Integer() {1, 0, 0, 0})
        Me.numPort.Name = "numPort"
        Me.numPort.Size = New System.Drawing.Size(61, 20)
        Me.numPort.TabIndex = 5
        Me.numPort.Value = New Decimal(New Integer() {8020, 0, 0, 0})
        '
        'lblPort
        '
        Me.lblPort.AutoSize = True
        Me.lblPort.Location = New System.Drawing.Point(11, 75)
        Me.lblPort.Name = "lblPort"
        Me.lblPort.Size = New System.Drawing.Size(90, 13)
        Me.lblPort.TabIndex = 4
        Me.lblPort.Text = "TCP Socket Port:"
        '
        'Setup
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(279, 131)
        Me.Controls.Add(Me.lblHome)
        Me.Controls.Add(Me.lblDesc)
        Me.Controls.Add(Me.numPort)
        Me.Controls.Add(Me.lblPort)
        Me.Name = "Setup"
        Me.Text = "MPCommandFusion Settings"
        CType(Me.numPort, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents lblHome As System.Windows.Forms.LinkLabel
    Friend WithEvents lblDesc As System.Windows.Forms.Label
    Friend WithEvents numPort As System.Windows.Forms.NumericUpDown
    Friend WithEvents lblPort As System.Windows.Forms.Label
End Class

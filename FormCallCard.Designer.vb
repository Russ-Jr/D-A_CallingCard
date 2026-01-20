<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormCallCard
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(disposing As Boolean)
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
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Me.WebView21 = New Microsoft.Web.WebView2.WinForms.WebView2()
        Me.lblStatus = New System.Windows.Forms.Label()
        Me.btnRegisterNFC = New System.Windows.Forms.Button()
        Me.SuspendLayout()
        '
        'WebView21
        '
        Me.WebView21.CreationProperties = Nothing
        Me.WebView21.DefaultBackgroundColor = System.Drawing.Color.White
        Me.WebView21.Dock = System.Windows.Forms.DockStyle.Fill
        Me.WebView21.Location = New System.Drawing.Point(0, 0)
        Me.WebView21.Name = "WebView21"
        Me.WebView21.Size = New System.Drawing.Size(1200, 800)
        Me.WebView21.TabIndex = 0
        Me.WebView21.ZoomFactor = 1.0R
        '
        'lblStatus
        '
        Me.lblStatus.Anchor = CType(((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lblStatus.BackColor = System.Drawing.Color.White
        Me.lblStatus.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.lblStatus.Location = New System.Drawing.Point(12, 750)
        Me.lblStatus.Name = "lblStatus"
        Me.lblStatus.Size = New System.Drawing.Size(1000, 30)
        Me.lblStatus.TabIndex = 1
        Me.lblStatus.Text = "Ready"
        Me.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'btnRegisterNFC
        '
        Me.btnRegisterNFC.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnRegisterNFC.BackColor = System.Drawing.Color.FromArgb(CType(CType(102, Byte), Integer), CType(CType(126, Byte), Integer), CType(CType(234, Byte), Integer))
        Me.btnRegisterNFC.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnRegisterNFC.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnRegisterNFC.ForeColor = System.Drawing.Color.White
        Me.btnRegisterNFC.Location = New System.Drawing.Point(1020, 750)
        Me.btnRegisterNFC.Name = "btnRegisterNFC"
        Me.btnRegisterNFC.Size = New System.Drawing.Size(168, 30)
        Me.btnRegisterNFC.TabIndex = 2
        Me.btnRegisterNFC.Text = "Register NFC Card"
        Me.btnRegisterNFC.UseVisualStyleBackColor = False
        Me.btnRegisterNFC.Visible = False
        '
        'FormCallCard
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1200, 800)
        Me.Controls.Add(Me.btnRegisterNFC)
        Me.Controls.Add(Me.lblStatus)
        Me.Controls.Add(Me.WebView21)
        Me.Name = "FormCallCard"
        Me.Text = "Calling Card System - Admin Dashboard"
        Me.WindowState = System.Windows.Forms.FormWindowState.Maximized
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents WebView21 As Microsoft.Web.WebView2.WinForms.WebView2
    Friend WithEvents lblStatus As System.Windows.Forms.Label
    Friend WithEvents btnRegisterNFC As System.Windows.Forms.Button
End Class


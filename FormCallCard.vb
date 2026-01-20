Imports System.IO
Imports System.Text
Imports System.Runtime.InteropServices
Imports System.Security.Cryptography
Imports System.Net.Http
Imports System.Web
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq
Imports Microsoft.Web.WebView2.Core

Public Class FormCallCard

    ' =====================================================
    ' NFC Reader Imports (winscard.dll)
    ' =====================================================
    <DllImport("winscard.dll")>
    Private Shared Function SCardEstablishContext(dwScope As Integer, pvReserved1 As IntPtr, pvReserved2 As IntPtr, ByRef phContext As IntPtr) As Integer
    End Function

    <DllImport("winscard.dll")>
    Private Shared Function SCardListReaders(hContext As IntPtr, mszGroups As Byte(), ByVal szReaders As Byte(), ByRef pcchReaders As Integer) As Integer
    End Function

    <DllImport("winscard.dll")>
    Private Shared Function SCardConnect(hContext As IntPtr, szReader As String, dwShareMode As Integer, dwPreferredProtocols As Integer, ByRef phCard As IntPtr, ByRef pdwActiveProtocol As Integer) As Integer
    End Function

    <DllImport("winscard.dll")>
    Private Shared Function SCardTransmit(hCard As IntPtr, pioSendPci As IntPtr, sendBuffer As Byte(), sendBufferLen As Integer, pioRecvPci As IntPtr, recvBuffer As Byte(), ByRef recvBufferLen As Integer) As Integer
    End Function

    <DllImport("winscard.dll")>
    Private Shared Function SCardDisconnect(hCard As IntPtr, dwDisposition As Integer) As Integer
    End Function

    <DllImport("winscard.dll")>
    Private Shared Function SCardReleaseContext(hContext As IntPtr) As Integer
    End Function

    ' NFC Constants
    Private Const SCARD_SCOPE_USER As Integer = 0
    Private Const SCARD_SHARE_SHARED As Integer = 2
    Private Const SCARD_PROTOCOL_T0 As Integer = 1
    Private Const SCARD_PROTOCOL_T1 As Integer = 2
    Private Const SCARD_LEAVE_CARD As Integer = 0

    ' Structure for PCI
    <StructLayout(LayoutKind.Sequential)>
    Private Structure SCARD_IO_REQUEST
        Public dwProtocol As Integer
        Public cbPciLength As Integer
    End Structure

    Private Shared ReadOnly SCARD_PCI_T0 As IntPtr
    Private Shared ReadOnly SCARD_PCI_T1 As IntPtr

    Shared Sub New()
        Dim pciT0 As New SCARD_IO_REQUEST With {
            .dwProtocol = SCARD_PROTOCOL_T0,
            .cbPciLength = Marshal.SizeOf(GetType(SCARD_IO_REQUEST))
        }
        Dim pciT1 As New SCARD_IO_REQUEST With {
            .dwProtocol = SCARD_PROTOCOL_T1,
            .cbPciLength = Marshal.SizeOf(GetType(SCARD_IO_REQUEST))
        }
        SCARD_PCI_T0 = Marshal.AllocHGlobal(Marshal.SizeOf(GetType(SCARD_IO_REQUEST)))
        Marshal.StructureToPtr(pciT0, SCARD_PCI_T0, False)
        SCARD_PCI_T1 = Marshal.AllocHGlobal(Marshal.SizeOf(GetType(SCARD_IO_REQUEST)))
        Marshal.StructureToPtr(pciT1, SCARD_PCI_T1, False)
    End Sub

    ' =====================================================
    ' Configuration
    ' =====================================================
    Private Const WEB_URL As String = "https://tito.ndasphilsinc.com/callingcard/"
    Private Const API_URL As String = "https://tito.ndasphilsinc.com/callingcard/api/"
    Private Const ENCRYPTION_KEY As String = "0123456789abcdef0123456789abcdef" ' 32 bytes
    Private Const ENCRYPTION_IV As String = "abcdef9876543210" ' 16 bytes

    ' =====================================================
    ' Form Controls (Add these to your form designer)
    ' =====================================================
    ' - WebView2 control (Microsoft.Web.WebView2)
    ' - Button: btnRegisterNFC
    ' - Label: lblStatus
    ' - TextBox: txtUserId (hidden, for current user being registered)

    Private currentUserId As Integer = 0
    Private hContext As IntPtr = IntPtr.Zero
    Private hCard As IntPtr = IntPtr.Zero
    Private activeProtocol As Integer = 0

    ' =====================================================
    ' Form Load
    ' =====================================================
    Private Sub FormCallCard_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.Text = "Calling Card System - Admin Dashboard"
        Me.WindowState = FormWindowState.Maximized

        ' Initialize WebView
        InitializeWebView()

        ' Initialize NFC Context
        InitializeNFC()
    End Sub

    ' =====================================================
    ' Initialize WebView
    ' =====================================================
    Private Async Sub InitializeWebView()
        Try
            ' Initialize WebView2
            Await WebView21.EnsureCoreWebView2Async()

            ' Add JavaScript message handler for NFC registration
            AddHandler WebView21.CoreWebView2.WebMessageReceived, AddressOf WebView_MessageReceived

            ' Add navigation completed handler to inject script after page loads
            AddHandler WebView21.CoreWebView2.NavigationCompleted, AddressOf WebView_NavigationCompleted

            ' Navigate to login page
            WebView21.CoreWebView2.Navigate(WEB_URL & "index.php")

            UpdateStatus("WebView initialized. Loading login page...")
        Catch ex As Exception
            MessageBox.Show("Error initializing WebView: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            UpdateStatus("Error: " & ex.Message)
        End Try
    End Sub

    ' =====================================================
    ' WebView Navigation Completed
    ' =====================================================
    Private Sub WebView_NavigationCompleted(sender As Object, e As CoreWebView2NavigationCompletedEventArgs)
        Try
            ' Inject JavaScript to enable WebView2 message passing
            Dim script As String = "
                (function() {
                    // Override registerNFC function to communicate with VB.NET
                    if (typeof registerNFC !== 'undefined') {
                        const originalRegisterNFC = registerNFC;
                        registerNFC = function(userId) {
                            if (window.chrome && window.chrome.webview && window.chrome.webview.postMessage) {
                                window.chrome.webview.postMessage(JSON.stringify({
                                    action: 'registerNFC',
                                    userId: userId
                                }));
                            } else {
                                originalRegisterNFC(userId);
                            }
                        };
                    }
                })();
            "
            WebView21.CoreWebView2.ExecuteScriptAsync(script)
        Catch ex As Exception
            ' Ignore script injection errors
        End Try
    End Sub

    ' =====================================================
    ' WebView Message Handler
    ' =====================================================
    Private Sub WebView_MessageReceived(sender As Object, e As CoreWebView2WebMessageReceivedEventArgs)
        Try
            Dim message As String = e.TryGetWebMessageAsString()
            Dim data As JObject = JObject.Parse(message)

            If data("action").ToString() = "registerNFC" Then
                currentUserId = CInt(data("userId"))
                Me.Invoke(New Action(AddressOf StartNFCRegistration))
            End If
        Catch ex As Exception
            MessageBox.Show("Error processing message: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ' =====================================================
    ' Initialize NFC
    ' =====================================================
    Private Sub InitializeNFC()
        If SCardEstablishContext(SCARD_SCOPE_USER, IntPtr.Zero, IntPtr.Zero, hContext) <> 0 Then
            UpdateStatus("NFC Reader: Failed to establish context")
        Else
            UpdateStatus("NFC Reader: Ready")
        End If
    End Sub

    ' =====================================================
    ' Start NFC Registration
    ' =====================================================
    Private Sub StartNFCRegistration()
        If currentUserId = 0 Then
            MessageBox.Show("No user selected for NFC registration.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return
        End If

        UpdateStatus("Waiting for NFC card... Please tap the card on the reader.")
        btnRegisterNFC.Enabled = False

        ' Start background thread to wait for card
        Dim t As New Threading.Thread(AddressOf WaitForCardAndRegister)
        t.IsBackground = True
        t.Start()
    End Sub

    ' =====================================================
    ' Wait for Card and Register
    ' =====================================================
    Private Sub WaitForCardAndRegister()
        Try
            ' Wait for card tap
            Dim nfcUid As String = ReadNFCUID()
            If String.IsNullOrEmpty(nfcUid) Then
                Me.Invoke(New Action(Sub()
                                         UpdateStatus("Failed to read NFC card.")
                                         btnRegisterNFC.Enabled = True
                                     End Sub))
                Return
            End If

            Me.Invoke(New Action(Sub()
                                     UpdateStatus("NFC UID read: " & nfcUid & vbCrLf & "Registering with server...")
                                 End Sub))

            ' Register NFC with PHP API
            Dim result As String = RegisterNFCWithAPI(currentUserId, nfcUid)
            If String.IsNullOrEmpty(result) Then
                Me.Invoke(New Action(Sub()
                                         UpdateStatus("Failed to register NFC with server.")
                                         btnRegisterNFC.Enabled = True
                                     End Sub))
                Return
            End If

            ' Parse response to get NDEF URL
            Dim response As JObject = JObject.Parse(result)
            If response("success").ToObject(Of Boolean)() Then
                Dim ndefUrl As String = response("data")("ndef_url").ToString()

                ' Write NDEF URL to card
                If WriteNDEFToCard(ndefUrl) Then
                    Me.Invoke(New Action(Sub()
                                             UpdateStatus("NFC card registered successfully!")
                                             MessageBox.Show("NFC card registered successfully!" & vbCrLf & "NDEF URL written to card.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
                                             btnRegisterNFC.Enabled = True
                                             currentUserId = 0

                                             ' Reload WebView to refresh user list
                                             WebView21.CoreWebView2.Reload()
                                         End Sub))
                Else
                    Me.Invoke(New Action(Sub()
                                             UpdateStatus("Failed to write NDEF URL to card.")
                                             btnRegisterNFC.Enabled = True
                                         End Sub))
                End If
            Else
                Me.Invoke(New Action(Sub()
                                         UpdateStatus("Server error: " & response("message").ToString())
                                         btnRegisterNFC.Enabled = True
                                     End Sub))
            End If

        Catch ex As Exception
            Me.Invoke(New Action(Sub()
                                     UpdateStatus("Error: " & ex.Message)
                                     btnRegisterNFC.Enabled = True
                                 End Sub))
        End Try
    End Sub

    ' =====================================================
    ' Read NFC UID
    ' =====================================================
    Private Function ReadNFCUID() As String
        Try
            ' Connect to card
            If Not ConnectToCard() Then
                Return ""
            End If

            ' Read UID from pages 0-3 (NTAG213)
            Dim uid As New List(Of Byte)
            Dim pioSendPci As IntPtr = If(activeProtocol = SCARD_PROTOCOL_T0, SCARD_PCI_T0, SCARD_PCI_T1)

            For page As Integer = 0 To 3
                Dim apdu As Byte() = {&HFF, &HB0, 0, CByte(page), 4}
                Dim recvBuffer(255) As Byte
                Dim recvLen As Integer = recvBuffer.Length

                If SCardTransmit(hCard, pioSendPci, apdu, apdu.Length, IntPtr.Zero, recvBuffer, recvLen) = 0 Then
                    uid.AddRange(recvBuffer.Take(4))
                Else
                    SCardDisconnect(hCard, SCARD_LEAVE_CARD)
                    Return ""
                End If
            Next

            SCardDisconnect(hCard, SCARD_LEAVE_CARD)

            ' Convert UID to hex string
            Return BitConverter.ToString(uid.Take(7).ToArray()).Replace("-", "")
        Catch ex As Exception
            Return ""
        End Try
    End Function

    ' =====================================================
    ' Connect to Card
    ' =====================================================
    Private Function ConnectToCard() As Boolean
        Try
            ' List readers
            Dim readers As Byte() = New Byte(255) {}
            Dim readersLen As Integer = readers.Length
            If SCardListReaders(hContext, Nothing, readers, readersLen) <> 0 Then
                Return False
            End If

            Dim readerName As String = Encoding.ASCII.GetString(readers, 0, readersLen).TrimEnd(Chr(0))
            If String.IsNullOrEmpty(readerName) Then
                Return False
            End If

            ' Connect to card
            Dim result As Integer = SCardConnect(hContext, readerName, SCARD_SHARE_SHARED, SCARD_PROTOCOL_T0 Or SCARD_PROTOCOL_T1, hCard, activeProtocol)
            Return result = 0
        Catch ex As Exception
            Return False
        End Try
    End Function

    ' =====================================================
    ' Register NFC with PHP API
    ' =====================================================
    Private Function RegisterNFCWithAPI(userId As Integer, nfcUid As String) As String
        Try
            Using client As New HttpClient()
                client.Timeout = TimeSpan.FromSeconds(30)

                Dim postData As New List(Of KeyValuePair(Of String, String))
                postData.Add(New KeyValuePair(Of String, String)("action", "register_nfc"))
                postData.Add(New KeyValuePair(Of String, String)("user_id", userId.ToString()))
                postData.Add(New KeyValuePair(Of String, String)("nfc_uid", nfcUid))

                Dim content As New FormUrlEncodedContent(postData)
                Dim response As HttpResponseMessage = client.PostAsync(API_URL & "nfc.php", content).Result

                If response.IsSuccessStatusCode Then
                    Return response.Content.ReadAsStringAsync().Result
                Else
                    Return ""
                End If
            End Using
        Catch ex As Exception
            Return ""
        End Try
    End Function

    ' =====================================================
    ' Write NDEF to Card
    ' =====================================================
    Private Function WriteNDEFToCard(url As String) As Boolean
        Try
            ' Connect to card
            If Not ConnectToCard() Then
                Return False
            End If

            ' Encode NDEF URL
            Dim ndefData As Byte() = EncodeNDEFUrl(url)

            ' Write to card starting at page 4
            Dim pioSendPci As IntPtr = If(activeProtocol = SCARD_PROTOCOL_T0, SCARD_PCI_T0, SCARD_PCI_T1)
            Dim page As Integer = 4

            For i As Integer = 0 To ndefData.Length - 1 Step 4
                Dim chunk As Byte() = ndefData.Skip(i).Take(4).ToArray()
                If chunk.Length < 4 Then
                    Dim padded(3) As Byte
                    Array.Copy(chunk, padded, chunk.Length)
                    chunk = padded
                End If

                Dim apdu As Byte() = {&HFF, &HD6, 0, CByte(page), 4, chunk(0), chunk(1), chunk(2), chunk(3)}
                Dim recvBuffer(255) As Byte
                Dim recvLen As Integer = recvBuffer.Length

                If SCardTransmit(hCard, pioSendPci, apdu, apdu.Length, IntPtr.Zero, recvBuffer, recvLen) <> 0 Then
                    SCardDisconnect(hCard, SCARD_LEAVE_CARD)
                    Return False
                End If

                page += 1
            Next

            SCardDisconnect(hCard, SCARD_LEAVE_CARD)
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

    ' =====================================================
    ' Encode NDEF URL
    ' =====================================================
    Private Function EncodeNDEFUrl(url As String) As Byte()
        Dim uriIdentifier As Byte = 0
        Dim formattedUrl As String = url

        ' Determine URI identifier code
        If url.StartsWith("http://www.", StringComparison.OrdinalIgnoreCase) Then
            uriIdentifier = &H1
            formattedUrl = url.Substring(11)
        ElseIf url.StartsWith("https://www.", StringComparison.OrdinalIgnoreCase) Then
            uriIdentifier = &H2
            formattedUrl = url.Substring(12)
        ElseIf url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) Then
            uriIdentifier = &H4
            formattedUrl = url.Substring(8)
        ElseIf url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) Then
            uriIdentifier = &H3
            formattedUrl = url.Substring(7)
        End If

        Dim urlBytes As Byte() = Encoding.UTF8.GetBytes(formattedUrl)
        Dim payloadLength As Integer = urlBytes.Length + 1

        ' Create NDEF Record
        Dim ndefRecord As New List(Of Byte)
        ndefRecord.Add(&HD1)                    ' NDEF Record header
        ndefRecord.Add(&H1)                     ' Type Length
        ndefRecord.Add(CByte(payloadLength))    ' Payload Length
        ndefRecord.Add(&H55)                    ' Type ('U' for URI)
        ndefRecord.Add(uriIdentifier)           ' URI Identifier Code
        ndefRecord.AddRange(urlBytes)           ' URL Data

        ' Create TLV
        Dim tlv As New List(Of Byte)
        tlv.Add(&H3)                            ' NDEF Message TLV Tag
        tlv.Add(CByte(ndefRecord.Count))        ' Length
        tlv.AddRange(ndefRecord)
        tlv.Add(&HFE)                           ' Terminator TLV

        Return tlv.ToArray()
    End Function

    ' =====================================================
    ' Update Status Label
    ' =====================================================
    Private Sub UpdateStatus(message As String)
        If lblStatus.InvokeRequired Then
            lblStatus.Invoke(New Action(Of String)(AddressOf UpdateStatus), message)
        Else
            lblStatus.Text = message
            Application.DoEvents()
        End If
    End Sub

    ' =====================================================
    ' Form Closing
    ' =====================================================
    Private Sub FormCallCard_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        If hContext <> IntPtr.Zero Then
            SCardReleaseContext(hContext)
        End If
    End Sub

End Class


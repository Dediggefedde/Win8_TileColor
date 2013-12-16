Imports System.Security.Permissions
Imports System.Security
Imports Microsoft.Win32

Public Class Form1
    Private Structure linkup
        Public path, name, rpath As String
        Public id As Int16
        Public co As Color
        Public Sub New(ByVal p, ByVal n, ByVal r, ByVal i)
            path = p : name = n : id = i : rpath = r
        End Sub
    End Structure
    Private Function ReadLnk(ByVal filename As String) As String
        Dim shortCut As IWshRuntimeLibrary.IWshShortcut
        shortCut = CType((New IWshRuntimeLibrary.WshShell).CreateShortcut(filename), IWshRuntimeLibrary.IWshShortcut)
        If (My.Computer.FileSystem.FileExists(shortCut.TargetPath)) Then
            Return shortCut.TargetPath
        Else
            If (shortCut.TargetPath.Contains("Program Files (x86)")) Then
                Return shortCut.TargetPath.Replace("Program Files (x86)", "Program Files")
            ElseIf (shortCut.TargetPath.Contains("Program Files")) Then
                Return shortCut.TargetPath.Replace("Program Files", "Program Files (x86)")
            End If
        End If
    End Function
    Dim keylist As New List(Of linkup)
    Function parentfold(ByVal mpath As String, ByVal level As Int16) As String
        Dim pathsec As String() = mpath.Split(IO.Path.DirectorySeparatorChar)
        parentfold = ""
        For i As Int16 = 0 To pathsec.Length - 1 - level
            parentfold &= pathsec(i) & IO.Path.DirectorySeparatorChar
        Next
        Return parentfold
    End Function
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        keylist.Clear()
        DataGridView1.Rows.Clear()
        Dim li(1) As IReadOnlyCollection(Of String)
        li(0) = My.Computer.FileSystem.GetFiles(parentfold(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup), 2), FileIO.SearchOption.SearchAllSubDirectories, "*.lnk")
        li(1) = My.Computer.FileSystem.GetFiles(My.Computer.FileSystem.SpecialDirectories.Programs, FileIO.SearchOption.SearchAllSubDirectories, "*.lnk")
        Dim rex As System.Text.RegularExpressions.Regex = New System.Text.RegularExpressions.Regex("([^\\/]*?)\.lnk")
        Dim rlink, rname As String
        Dim usprung As Int16 = -1
        For y As Int16 = 0 To 1
            For x As Int16 = 0 To li(y).Count - 1
                usprung += 1
                rlink = ReadLnk(li(y)(x))
                rname = rex.Match(li(y)(x)).Groups(1).Value
                If (rlink = "" Or rname.Contains("Uninstall")) Then usprung -= 1 : Continue For
                DataGridView1.Rows.Add(rname, usprung)
                keylist.Add(New linkup(li(y)(x), rname, rlink, usprung))
            Next
        Next

        Dim MeinKey, ukey As RegistryKey
        MeinKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\GameUX\\Games")

        If (MeinKey Is Nothing) Then MeinKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\GameUX\\Games")
        If (MeinKey Is Nothing) Then Exit Sub
        Dim unams() As String = MeinKey.GetSubKeyNames
        For Each s As String In unams
            usprung += 1
            ukey = MeinKey.OpenSubKey(s)
            rname = ukey.GetValue("Title", "")
            DataGridView1.Rows.Add(rname, usprung)
            keylist.Add(New linkup(s, ukey.GetValue("Title", ""), ukey.GetValue("ConfigGDFBinaryPath", ""), usprung))
            ukey.Close()
        Next
        MeinKey.Close()

        DataGridView1.Sort(DataGridView1.Columns(0), System.ComponentModel.ListSortDirection.Ascending)
    End Sub

    Private Sub DataGridView1_CellClick(sender As Object, e As DataGridViewCellEventArgs) Handles DataGridView1.CellClick
        If (e.RowIndex < 0) Then Return
        Label2.Text = keylist(DataGridView1.Item(1, e.RowIndex).Value).rpath


        Dim nname As String = keylist(DataGridView1.Item(1, e.RowIndex).Value).rpath
        If nname.LastIndexOf(".") <= 0 Then
            nname &= ".VisualElementsManifest.xml"
        End If
        nname = nname.Substring(0, nname.LastIndexOf(".")) & ".VisualElementsManifest.xml"
        If Not My.Computer.FileSystem.FileExists(nname) Then Return
        Dim aktconf = My.Computer.FileSystem.ReadAllText(nname)
        Dim aktcol As String = aktconf.Substring(aktconf.IndexOf("BackgroundColor=") + "BackgroundColor=".Length + 1)
        aktcol = aktcol.Substring(0, aktcol.IndexOf(Chr(34)))
        Dim aktshow As String = aktconf.Substring(aktconf.IndexOf("ShowNameOnSquare150x150Logo=") + "ShowNameOnSquare150x150Logo=".Length + 1)
        aktshow = aktshow.Substring(0, aktshow.IndexOf(Chr(34)))
        Dim aktfont As String = aktconf.Substring(aktconf.IndexOf("ForegroundText=") + "ForegroundText=".Length + 1)
        aktfont = aktfont.Substring(0, aktfont.IndexOf(Chr(34)))
        PictureBox1.BackColor = ColorTranslator.FromHtml(aktcol)
        If aktshow = "on" Then CheckBox1.Checked = True Else CheckBox1.Checked = False
        If aktfont = "light" Then RadioButton1.Checked = True : RadioButton2.Checked = False Else RadioButton1.Checked = False : RadioButton2.Checked = True


    End Sub

    Private Sub DataGridView1_CellContentClick(sender As Object, e As DataGridViewCellEventArgs) Handles DataGridView1.CellContentClick
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        ProgressBar1.Visible = True
        ProgressBar1.Maximum = DataGridView1.SelectedRows.Count + 3
        ProgressBar1.Value = 0
        Dim nname As String = ""
        Dim highlight As String = "light"
        Dim display As String = "off"
        If (RadioButton2.Checked) Then highlight = "dark"
        If (CheckBox1.Checked) Then display = "on"
        Dim lognum As Integer = 0
        For Each d As DataGridViewRow In DataGridView1.SelectedRows
            ProgressBar1.Value += 1
            Application.DoEvents()
            nname = keylist(d.Cells(1).Value).rpath
            If nname.LastIndexOf(".") <= 0 Then
                nname &= ".VisualElementsManifest.xml"
            End If
            nname = nname.Substring(0, nname.LastIndexOf(".")) & ".VisualElementsManifest.xml"
            'Dim wp As New FileIOPermission(FileIOPermissionAccess.Write, nname)
            ' If SecurityManager.IsGranted(wp) Then
            Try
                My.Computer.FileSystem.WriteAllText(nname, My.Resources.template.Replace("##3", highlight).Replace("##2", display).Replace("##1", "#" & PictureBox1.BackColor.ToArgb().ToString("X").Substring(2)), False)
                If (keylist(d.Cells(1).Value).path(0) <> "{") Then My.Computer.FileSystem.GetFileInfo(keylist(d.Cells(1).Value).path).LastWriteTime = Now
            Catch ex As Exception
                lognum += 1
                My.Computer.FileSystem.WriteAllText(My.Application.Info.DirectoryPath & "\AccessException.log", Now.ToString & vbCrLf & "Permission denied:" & vbCrLf & nname & vbCrLf, True)
            End Try
            ' End If
        Next
        If (lognum > 0) Then MsgBox("Permission denied to create files in " & lognum & " Cases." & vbCrLf & "See AccessException.log for detailed information!")
        ProgressBar1.Visible = False
    End Sub
    Private Sub PictureBox1_Click(sender As Object, e As EventArgs) Handles PictureBox1.Click
        Dim co As New ColorDialog
        If co.ShowDialog() = DialogResult.Cancel Then Exit Sub
        PictureBox1.BackColor = co.Color
    End Sub

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Label2.Parent = PictureBox1
        Label2.Width = PictureBox1.Width
        '   Label2.Top = (PictureBox1.Height - Label2.Height) / 2
        '  Label2.Left= (PictureBox1.Width - Label2.Width) / 2
        Label2.Dock = DockStyle.Fill
        Label2.TextAlign = ContentAlignment.MiddleCenter
    End Sub

    Private Sub RadioButton2_CheckedChanged(sender As Object, e As EventArgs) Handles RadioButton2.CheckedChanged
        If (RadioButton1.Checked) Then Label2.ForeColor = Color.White Else Label2.ForeColor = Color.Black
    End Sub

    Private Sub RadioButton1_CheckedChanged(sender As Object, e As EventArgs) Handles RadioButton1.CheckedChanged
        If (RadioButton1.Checked) Then Label2.ForeColor = Color.White Else Label2.ForeColor = Color.Black

    End Sub

    Private Sub Label2_Click(sender As Object, e As EventArgs) Handles Label2.Click
        Dim co As New ColorDialog
        If co.ShowDialog() = DialogResult.Cancel Then Exit Sub
        PictureBox1.BackColor = co.Color
    End Sub

    Private Sub Button4_Click(sender As Object, e As EventArgs) Handles Button4.Click
        If (DataGridView1.RowCount = 0) Then Exit Sub
        Dim r As New System.Text.RegularExpressions.Regex(TextBox1.Text, System.Text.RegularExpressions.RegexOptions.IgnoreCase)
        For x As Integer = DataGridView1.CurrentCell.RowIndex + 1 To DataGridView1.RowCount - 1
            If r.IsMatch(DataGridView1.Item(0, x).Value.ToString) Then
                DataGridView1.FirstDisplayedScrollingRowIndex = x
                DataGridView1.CurrentCell = DataGridView1.Item(0, x)
                Exit Sub
            End If
        Next

        DataGridView1.FirstDisplayedScrollingRowIndex = 0
        DataGridView1.CurrentCell = DataGridView1.Item(0, 0)
        MsgBox("Expression not found." & vbCrLf & " Please escape characters like .+?* with an \: \. \+ \? \*")
    End Sub

    Private Sub TextBox1_KeyUp(sender As Object, e As KeyEventArgs) Handles TextBox1.KeyUp
        If e.KeyCode = Keys.Enter Then
            Button4_Click(sender, e)
        End If
    End Sub

    Private Sub TextBox1_TextChanged(sender As Object, e As EventArgs) Handles TextBox1.TextChanged

    End Sub

    Private Sub CheckBox1_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox1.CheckedChanged

    End Sub

    Private Sub Label1_Click(sender As Object, e As EventArgs) Handles Label1.Click
        PictureBox1_Click(PictureBox1, EventArgs.Empty)
    End Sub
End Class

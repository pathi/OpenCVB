﻿Imports System.IO
Imports System.Text.RegularExpressions
Module UI_GeneratorMain
    Sub Main()
        Dim line As String
        Dim ExecDir As New DirectoryInfo(My.Application.Info.DirectoryPath)
        ChDir(ExecDir.FullName)

        Dim directoryInfo As New DirectoryInfo(CurDir() + "/../../vb_classes")
        Dim fileNames As New List(Of String)
        Dim fileEntries As String() = Directory.GetFiles(directoryInfo.FullName)

        Dim pythonAppDir As New IO.DirectoryInfo(directoryInfo.FullName + "/Python/")

        ' we only want to list the python files that are included in the VB_Classes Project.
        Dim projFile As New FileInfo(directoryInfo.FullName + "/VB_Classes.vbproj")
        Dim readProj = New StreamReader(projFile.FullName)
        While readProj.EndOfStream = False
            line = readProj.ReadLine()
            If Trim(line).StartsWith("<Content Include=") Then
                If InStr(line, "Python") Then
                    Dim startName = InStr(line, "Python")
                    line = Mid(line, startName)
                    Dim endName = InStr(line, """")
                    line = Mid(line, 1, endName - 1)
                    line = Mid(line, Len("Python/") + 1)
                    fileNames.Add(directoryInfo.FullName + "\Python\" + line)
                End If
            End If
            If Trim(line).StartsWith("<Compile Include=") Then
                If InStr(line, ".vb""") Then
                    Dim startname = InStr(line, "=") + 2
                    line = Mid(line, startname)
                    Dim endName = InStr(line, """")
                    line = Mid(line, 1, endName - 1)
                    If line.Contains("AlgorithmList.vb") = False And line.Contains("My Project") = False Then fileNames.Add(directoryInfo.FullName + "/" + line)
                End If
                End If
        End While
        readProj.Close()

        Dim className As String = ""
        Dim functionNames As New List(Of String)
        Dim CodeLineCount As Int32
        For Each fileName In fileNames
            If fileName.EndsWith(".py") Then
                Dim fileinfo As New FileInfo(fileName)
                functionNames.Add(fileinfo.Name)
                fileName = fileinfo.FullName
            Else
                Dim nextFile As New System.IO.StreamReader(fileName)
                While nextFile.Peek() <> -1
                    line = Trim(nextFile.ReadLine())
                    If Len(Trim(line)) > 0 Then CodeLineCount += 1
                    If LCase(line).StartsWith("public class") Then
                        Dim split As String() = Regex.Split(line, "\W+")
                        className = split(2) ' public class <classname>
                    End If

                    If LCase(line).StartsWith("public sub new(ocvb as algorithmdata)") Then functionNames.Add(className)
                End While
            End If
        Next

        Dim sortedNames As New SortedList(Of String, Int32)
        For i = 0 To functionNames.Count - 1
            sortedNames.Add(functionNames.ElementAt(i), i)
        Next

        Dim cleanNames As New List(Of String)
        Dim lastName As String = ""
        For i = 0 To sortedNames.Count - 1
            Dim nextName = sortedNames.ElementAt(i).Key
            If nextName <> lastName + ".py" Then cleanNames.Add(nextName)
            lastName = nextName
        Next

        Dim listInfo As New FileInfo(CurDir() + "/../../UI_Generator/AlgorithmList.vb")
        Dim sw As New StreamWriter(listInfo.FullName)
        sw.WriteLine("' this file is automatically generated in a pre-build step.  Do not waste your time modifying manually.")
        sw.WriteLine("Public Class algorithmList")
        sw.WriteLine("Public Function createAlgorithm(name As String, ocvb As AlgorithmData) As Object")
        sw.WriteLine("Select Case name")
        For i = 0 To cleanNames.Count - 1
            Dim nextName = cleanNames.Item(i)
            sw.WriteLine("case """ + nextName + """")
            sw.WriteLine("ocvb.name = """ + nextName + """")
            sw.WriteLine("ocvb.label1 = """ + nextName + """")
            If nextName.EndsWith(".py") Then
                sw.WriteLine("ocvb.PythonFileName = """ + pythonAppDir.FullName + nextName + """")
                sw.WriteLine("return new Python_Run(ocvb)")
            Else
                sw.WriteLine("return new " + nextName + "(ocvb)")
            End If
        Next
        sw.WriteLine("case else")
        sw.WriteLine("return nothing")
        sw.WriteLine("End Select")
        sw.WriteLine("End Function")
        sw.WriteLine("End Class")
        sw.Close()


        Dim textInfo As New FileInfo(directoryInfo.FullName + "/../Data/AlgorithmList.txt")
        sw = New StreamWriter(textInfo.FullName)
        sw.WriteLine("CodeLineCount = " + CStr(CodeLineCount))
        For i = 0 To cleanNames.Count - 1
            sw.WriteLine(cleanNames.Item(i))
        Next
        sw.Close()

        Dim FilesInfo As New FileInfo(directoryInfo.FullName + "/../Data/FileNames.txt")
        sw = New StreamWriter(FilesInfo.FullName)
        For i = 0 To fileNames.Count - 1
            sw.WriteLine(fileNames.Item(i))
        Next
        sw.Close()
    End Sub
End Module
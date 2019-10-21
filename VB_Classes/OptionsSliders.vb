﻿Imports System.ComponentModel

Public Class OptionsSliders
    Private Sub optionsSliders_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.SetDesktopLocation(appLocation.Left + slidersOffset.X, appLocation.Top + appLocation.Height + slidersOffset.Y)
        slidersOffset.X += offsetIncr
        slidersOffset.Y += offsetIncr
        Label1.Text = CStr(TrackBar1.Value)
        Label2.Text = CStr(TrackBar2.Value)
        Label3.Text = CStr(TrackBar3.Value)
        Label4.Text = CStr(TrackBar4.Value)
    End Sub
    Private Sub TrackBar1_ValueChanged(sender As Object, e As EventArgs) Handles TrackBar1.ValueChanged
        Label1.Text = CStr(TrackBar1.Value)
    End Sub
    Private Sub TrackBar2_ValueChanged(sender As Object, e As EventArgs) Handles TrackBar2.ValueChanged
        Label2.Text = CStr(TrackBar2.Value)
    End Sub
    Private Sub TrackBar3_ValueChanged(sender As Object, e As EventArgs) Handles TrackBar3.ValueChanged
        Label3.Text = CStr(TrackBar3.Value)
    End Sub
    Private Sub TrackBar4_ValueChanged(sender As Object, e As EventArgs) Handles TrackBar4.ValueChanged
        Label4.Text = CStr(TrackBar4.Value)
    End Sub
    Public Sub setupTrackBar1(ocvb As AlgorithmData, label As String, min As Integer, max As Integer, value As Integer)
        LabelSlider1.Text = label
        TrackBar1.Minimum = min
        TrackBar1.Maximum = max
        TrackBar1.Value = value
        LabelSlider1.Visible = True
        TrackBar1.Visible = True
        Me.Text = ocvb.name + " Options"
    End Sub
    Public Sub setupTrackBar2(ocvb As AlgorithmData, label As String, min As Integer, max As Integer, value As Integer)
        LabelSlider2.Text = label
        TrackBar2.Minimum = min
        TrackBar2.Maximum = max
        TrackBar2.Value = value
        LabelSlider2.Visible = True
        TrackBar2.Visible = True
        Label2.Visible = True
        Me.Text = ocvb.name + " Options"
    End Sub
    Public Sub setupTrackBar3(ocvb As AlgorithmData, label As String, min As Integer, max As Integer, value As Integer)
        LabelSlider3.Text = label
        TrackBar3.Minimum = min
        TrackBar3.Maximum = max
        TrackBar3.Value = value
        LabelSlider3.Visible = True
        TrackBar3.Visible = True
        Label3.Visible = True
        Me.Text = ocvb.name + " Options"
    End Sub
    Public Sub setupTrackBar4(ocvb As AlgorithmData, label As String, min As Integer, max As Integer, value As Integer)
        LabelSlider4.Text = label
        TrackBar4.Minimum = min
        TrackBar4.Maximum = max
        TrackBar4.Value = value
        LabelSlider4.Visible = True
        TrackBar4.Visible = True
        Label4.Visible = True
        Me.Text = ocvb.name + " Options"
    End Sub
End Class
﻿Imports cv = OpenCvSharp
Public Class Encode_Basics : Implements IDisposable
    Dim sliders As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "Encode Quality Level", 1, 100, 1) ' make it low quality to highlight how different it can be.
        sliders.setupTrackBar2(ocvb, "Encode Output Scaling", 1, 100, 7)
        If ocvb.parms.ShowOptions Then sliders.show()

        ocvb.desc = "Error Level Analysis - to verify a jpg image has not been modified."
        ocvb.label1 = "absDiff with original"
        ocvb.label2 = "Original decompressed"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim buf(ocvb.color.Width * ocvb.color.Height * ocvb.color.ElemSize) As Byte
        Dim encodeParams() As Int32 = {cv.ImwriteFlags.JpegQuality, sliders.TrackBar1.Value}

        cv.Cv2.ImEncode(".jpg", ocvb.color, buf, encodeParams)
        ocvb.result2 = cv.Cv2.ImDecode(buf, 1)

        Dim output As New cv.Mat
        cv.Cv2.Absdiff(ocvb.color, ocvb.result2, output)

        Dim scale = sliders.TrackBar2.Value
        output.ConvertTo(ocvb.result1, cv.MatType.CV_8UC3, scale)
        Dim compressionRatio = buf.Length / (ocvb.color.Rows * ocvb.color.Cols * ocvb.color.ElemSize)
        ocvb.label2 = "Original compressed to len=" + CStr(buf.Length) + " (" + Format(compressionRatio, "0.1%") + ")"
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class



Public Class Encode_Options : Implements IDisposable
    Dim sliders As New OptionsSliders
    Dim radio As New OptionsRadioButtons
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "Encode Quality Level", 1, 100, 1) ' make it low quality to highlight how different it can be.
        sliders.setupTrackBar2(ocvb, "Encode Output Scaling", 1, 100, 85)
        If ocvb.parms.ShowOptions Then sliders.show()

        radio.Setup(ocvb, 6)
        radio.check(0).Text = "JpegChromaQuality"
        radio.check(1).Text = "JpegLumaQuality"
        radio.check(2).Text = "JpegOptimize"
        radio.check(3).Text = "JpegProgressive"
        radio.check(4).Text = "JpegQuality"
        radio.check(5).Text = "WebPQuality"
        radio.check(4).Checked = True
        If ocvb.parms.ShowOptions Then radio.show()

        ocvb.desc = "Encode options that affect quality."
        ocvb.label1 = "absDiff with original image"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim buf(ocvb.color.Width * ocvb.color.Height * ocvb.color.ElemSize) As Byte
        Dim encodeOption As Int32
        For i = 0 To radio.check.Count - 1
            If radio.check(i).Checked Then
                encodeOption = Choose(i + 1, cv.ImwriteFlags.JpegChromaQuality, cv.ImwriteFlags.JpegLumaQuality, cv.ImwriteFlags.JpegOptimize, cv.ImwriteFlags.JpegProgressive,
                                              cv.ImwriteFlags.JpegQuality, cv.ImwriteFlags.WebPQuality)
                Exit For
            End If
        Next

        Dim fileExtension = ".jpg"
        Dim qualityLevel = sliders.TrackBar1.Value
        If encodeOption = cv.ImwriteFlags.JpegProgressive Then
            qualityLevel = 1 ' just on or off
        End If
        If encodeOption = cv.ImwriteFlags.JpegOptimize Then
            qualityLevel = 1 ' just on or off
        End If
        Dim encodeParams() As Int32 = {encodeOption, qualityLevel}

        cv.Cv2.ImEncode(fileExtension, ocvb.color, buf, encodeParams)
        ocvb.result2 = cv.Cv2.ImDecode(buf, 1)

        Dim output As New cv.Mat
        cv.Cv2.Absdiff(ocvb.color, ocvb.result2, output)

        Dim scale = sliders.TrackBar2.Value
        output.ConvertTo(ocvb.result1, cv.MatType.CV_8UC3, scale)
        Dim compressionRatio = buf.Length / (ocvb.color.Rows * ocvb.color.Cols * ocvb.color.ElemSize)
        ocvb.label2 = "Original compressed to len=" + CStr(buf.Length) + " (" + Format(compressionRatio, "0.1%") + ")"
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        radio.Dispose()
    End Sub
End Class
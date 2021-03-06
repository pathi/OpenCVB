﻿Imports cv = OpenCvSharp
' https://github.com/shimat/opencvsharp/blob/master/test/OpenCvSharp.Tests/stitching/StitchingTest.cs
Public Class Stitch_Basics : Implements IDisposable
    Dim sliders As New OptionsSliders
    Public src As New cv.Mat
    Public externalUse As Boolean
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "Number of random images", 10, 50, 10)
        sliders.setupTrackBar2(ocvb, "Rectangle width", ocvb.color.Width / 4, ocvb.color.Width - 1, ocvb.color.Width / 2)
        sliders.setupTrackBar3(ocvb, "Rectangle height", ocvb.color.Height / 4, ocvb.color.Height - 1, ocvb.color.Height / 2)
        If ocvb.parms.ShowOptions Then sliders.Show()
        ocvb.desc = "Stitch together random parts of a color image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim mats As New List(Of cv.Mat)
        Dim autoRand As New Random()
        Dim imageCount = sliders.TrackBar1.Value
        Dim width = sliders.TrackBar2.Value
        Dim height = sliders.TrackBar3.Value
        If externalUse = False Then src = ocvb.color.Clone()
        ocvb.result1 = src.Clone()
        For i = 0 To imageCount - 1
            Dim x1 = CInt(autoRand.NextDouble() * (src.Width - width))
            Dim x2 = CInt(autoRand.NextDouble() * (src.Height - height))
            Dim rect = New cv.Rect(x1, x2, width, height)
            ocvb.result1.Rectangle(rect, cv.Scalar.Red, 2)
            mats.Add(src(rect).Clone())
        Next

        If ocvb.parms.testAllRunning Then
            ' It runs fine but after several runs, it will fail with an external exception.  Only happens on 'Test All' runs.  
            ocvb.putText(New ActiveClass.TrueType("Stitch_Basics only fails when running 'Test All'.", 10, 60, RESULT2))
            ocvb.putText(New ActiveClass.TrueType("Skipping it during a 'Test All' just so all the other tests can be exercised.", 10, 100, RESULT2))
            Exit Sub
        End If

        Dim stitcher = cv.Stitcher.Create(cv.Stitcher.Mode.Scans)
        Dim pano As New cv.Mat

        ' stitcher may fail with an external exception if you make width and height too small.
        Dim status = stitcher.Stitch(mats, pano)

        ocvb.result2.SetTo(0)
        If status = cv.Stitcher.Status.OK Then
            Dim w = pano.Width, h = pano.Height
            If w > ocvb.result1.Width Then w = ocvb.result1.Width
            If h > ocvb.result1.Height Then h = ocvb.result1.Height
            pano.CopyTo(ocvb.result2(New cv.Rect(0, 0, w, h)))
        Else
            If status = cv.Stitcher.Status.ErrorNeedMoreImgs Then
                ocvb.result2.PutText("Need more images", New cv.Point(10, 60), cv.HersheyFonts.HersheySimplex, 0.5, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
            End If
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class

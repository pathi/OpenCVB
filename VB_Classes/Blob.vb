﻿Imports cv = OpenCvSharp
Public Class Blob_Input : Implements IDisposable
    Dim rectangles As Draw_rotatedRectangles
    Dim circles As Draw_Circles
    Dim ellipses As Draw_Ellipses
    Dim poly As Draw_Polygon
    Dim Mats As Mat_4to1
    Public updateFrequency = 30
    Public Sub New(ocvb As AlgorithmData)
        rectangles = New Draw_rotatedRectangles(ocvb)
        circles = New Draw_Circles(ocvb)
        ellipses = New Draw_Ellipses(ocvb)
        poly = New Draw_Polygon(ocvb)

        rectangles.rect.sliders.TrackBar1.Value = 5
        circles.sliders.TrackBar1.Value = 5
        ellipses.sliders.TrackBar1.Value = 5
        poly.sliders.TrackBar1.Value = 5

        rectangles.rect.updateFrequency = 1
        circles.updateFrequency = 1
        ellipses.updateFrequency = 1
        poly.updateFrequency = 1

        poly.radio.check(1).Checked = True ' we want the convex polygon filled.

        Mats = New Mat_4to1(ocvb)
        Mats.externalUse = True

        ocvb.desc = "Test simple Blob Detector."
        ocvb.label2 = ""
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.frameCount Mod updateFrequency = 0 Then
            rectangles.Run(ocvb)
            mats.mat(0) = ocvb.result1.Clone()

            circles.Run(ocvb)
            mats.mat(1) = ocvb.result1.Clone()

            ellipses.Run(ocvb)
            mats.mat(2) = ocvb.result1.Clone()

            poly.Run(ocvb)
            mats.mat(3) = ocvb.result2.Clone()
            Mats.Run(ocvb)
            ocvb.result2.CopyTo(ocvb.result1)
            ocvb.result2.SetTo(0)
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        rectangles.Dispose()
        circles.Dispose()
        ellipses.Dispose()
        poly.Dispose()
        Mats.Dispose()
    End Sub
End Class



Public Class Blob_Detector_CS : Implements IDisposable
    Dim input As Blob_Input
    Dim check As New OptionsCheckbox
    Dim sliders As New OptionsSliders
    Dim blobDetector As New CS_Classes.Blob_Basics
    Public Sub New(ocvb As AlgorithmData)
        input = New Blob_Input(ocvb)
        input.updateFrequency = 1 ' it is pretty fast but sloppy...
        check.Setup(ocvb, 5)
        check.Box(0).Text = "FilterByArea"
        check.Box(1).Text = "FilterByCircularity"
        check.Box(2).Text = "FilterByConvexity"
        check.Box(3).Text = "FilterByInertia"
        check.Box(4).Text = "FilterByColor"
        If ocvb.parms.ShowOptions Then check.show()
        check.Box(4).Checked = True ' filter by color...

        sliders.setupTrackBar1(ocvb, "min Threshold", 0, 255, 100)
        sliders.setupTrackBar2(ocvb, "max Threshold", 0, 255, 255)
        sliders.setupTrackBar3(ocvb, "Threshold Step", 1, 50, 5)
        If ocvb.parms.ShowOptions Then sliders.show()

        ocvb.label1 = "Blob_Detector_CS Input"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim blobParams = New cv.SimpleBlobDetector.Params
        blobParams.FilterByArea = check.Box(0).Checked
        blobParams.FilterByCircularity = check.Box(1).Checked
        blobParams.FilterByConvexity = check.Box(2).Checked
        blobParams.FilterByInertia = check.Box(3).Checked
        blobParams.FilterByColor = check.Box(4).Checked

        blobParams.MaxArea = 100
        blobParams.MinArea = 0.001

        blobParams.MinThreshold = sliders.TrackBar1.Value
        blobParams.MaxThreshold = sliders.TrackBar2.Value
        blobParams.ThresholdStep = sliders.TrackBar3.Value

        blobParams.MinDistBetweenBlobs = 10
        blobParams.MinRepeatability = 1

        input.Run(ocvb)

        ' The create method in SimpleBlobDetector is not available in VB.Net.  Not sure why.  To get around this, just use C# where create method works fine.
        blobDetector.Start(ocvb.result1, ocvb.result2, blobParams)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        input.Dispose()
        check.Dispose()
    End Sub
End Class



Public Class Blob_RenderBlobs : Implements IDisposable
    Dim input As Blob_Input
    Public Sub New(ocvb As AlgorithmData)
        input = New Blob_Input(ocvb)
        input.updateFrequency = 1

        ocvb.desc = "Use connected components to find blobs."
        ocvb.label2 = "Showing only the largest blob in test data"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.frameCount Mod 100 = 0 Then
            input.Run(ocvb)
            Dim gray = ocvb.result1.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            Dim binary = gray.Threshold(0, 255, cv.ThresholdTypes.Otsu Or cv.ThresholdTypes.BinaryInv)
            Dim labelView = ocvb.result1.EmptyClone
            Dim stats As New cv.Mat
            Dim centroids As New cv.Mat
            Dim cc = cv.Cv2.ConnectedComponentsEx(binary)
            Dim labelCount = cv.Cv2.ConnectedComponentsWithStats(binary, labelView, stats, centroids)
            If cc.LabelCount <= 1 Then Exit Sub
            cc.RenderBlobs(labelView)

            Dim maxBlob = cc.GetLargestBlob()
            ocvb.result2.SetTo(0)
            cc.FilterByBlob(ocvb.result1, ocvb.result2, maxBlob)

            For Each blob In cc.Blobs.Skip(1)
                ocvb.result1.Rectangle(blob.Rect, cv.Scalar.Red, 2, cv.LineTypes.AntiAlias)
            Next
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        input.Dispose()
    End Sub
End Class






Public Class Blob_LargestInDepth : Implements IDisposable
    Dim blobs As Blob_DepthClusters
    Public Sub New(ocvb As AlgorithmData)
        blobs = New Blob_DepthClusters(ocvb)

        ocvb.desc = "Display only the largest blob."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        blobs.Run(ocvb)
        Dim blobList = blobs.histBlobs.valleys.sortedBoundaries

        Dim maxSize As Int32
        Dim maxIndex As Int32
        Dim sizes = blobs.histBlobs.valleys.sortedSizes
        For i = 0 To sizes.Count - 1
            If maxSize < sizes(i) Then
                maxSize = sizes(i)
                maxIndex = i
            End If
        Next

        ocvb.result1.SetTo(0)
        Dim startEndDepth = blobs.histBlobs.valleys.sortedBoundaries.ElementAt(maxIndex)
        Dim tmp16 As New cv.Mat, mask As New cv.Mat
        cv.Cv2.InRange(ocvb.depth, startEndDepth.X, startEndDepth.Y, tmp16)
        cv.Cv2.ConvertScaleAbs(tmp16, mask)
        ocvb.color.CopyTo(ocvb.result1, mask)
        ocvb.label1 = "Largest Depth Blob: " + Format(maxSize, "#,000") + " pixels (" + Format(maxSize / ocvb.color.Total, "#0.0%") + ")"
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        blobs.Dispose()
    End Sub
End Class





Public Class Blob_DepthClusters : Implements IDisposable
    Public histBlobs As Histogram_DepthClusters
    Public flood As FloodFill_RelativeRange
    Dim shadow As Depth_Shadow
    Public Sub New(ocvb As AlgorithmData)
        shadow = New Depth_Shadow(ocvb)
        shadow.externalUse = True

        histBlobs = New Histogram_DepthClusters(ocvb)

        flood = New FloodFill_RelativeRange(ocvb)
        flood.fBasics.sliders.TrackBar2.Value = 1 ' pixels are exact.
        flood.fBasics.sliders.TrackBar3.Value = 1 ' pixels are exact.
        flood.fBasics.externalUse = True

        ocvb.desc = "Highlight the distinct histogram blobs found with depth clustering."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        shadow.Run(ocvb)

        histBlobs.Run(ocvb)

        flood.fBasics.srcGray = ocvb.result2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim clusters = ocvb.result2.Clone()
        flood.fBasics.initialMask = shadow.holeMask
        flood.Run(ocvb)
        ocvb.label1 = CStr(histBlobs.valleys.sortedBoundaries.Count) + " Depth Clusters"
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        histBlobs.Dispose()
        flood.Dispose()
        shadow.Dispose()
    End Sub
End Class

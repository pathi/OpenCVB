﻿Imports cv = OpenCvSharp

Public Class Contours_Basics : Implements IDisposable
    Dim rotatedRect As Draw_rotatedRectangles
    Dim radio1 As New OptionsRadioButtons
    Dim radio2 As New OptionsRadioButtons
    Public Sub New(ocvb As AlgorithmData)
        radio1.Setup(ocvb, 5)
        radio1.Text = "Retrieval Mode Options"
        radio1.check(0).Text = "CComp"
        radio1.check(1).Text = "External"
        radio1.check(2).Text = "FloodFill"
        radio1.check(3).Text = "List"
        radio1.check(4).Text = "Tree"
        radio1.check(4).Checked = True
        If ocvb.parms.ShowOptions Then radio1.Show()

        radio2.Setup(ocvb, 4)
        radio2.Text = "ContourApproximation Mode"
        radio2.check(0).Text = "ApproxNone"
        radio2.check(1).Text = "ApproxSimple"
        radio2.check(2).Text = "ApproxTC89KCOS"
        radio2.check(3).Text = "ApproxTC89L1"
        radio2.check(1).Checked = True
        If ocvb.parms.ShowOptions Then radio2.Show()

        rotatedRect = New Draw_rotatedRectangles(ocvb)
        rotatedRect.rect.sliders.TrackBar1.Value = 5
        ocvb.desc = "Demo options on FindContours."
        ocvb.label2 = "FindContours output"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim retrievalMode As cv.RetrievalModes
        Dim ApproximationMode As cv.ContourApproximationModes
        For i = 0 To radio1.check.Count - 1
            If radio1.check(i).Checked Then
                retrievalMode = Choose(i + 1, cv.RetrievalModes.CComp, cv.RetrievalModes.External, cv.RetrievalModes.FloodFill, cv.RetrievalModes.List, cv.RetrievalModes.Tree)
                Exit For
            End If
        Next
        For i = 0 To radio2.check.Count - 1
            If radio2.check(i).Checked Then
                ApproximationMode = Choose(i + 1, cv.ContourApproximationModes.ApproxNone, cv.ContourApproximationModes.ApproxSimple,
                                              cv.ContourApproximationModes.ApproxTC89KCOS, cv.ContourApproximationModes.ApproxTC89L1)
                Exit For
            End If
        Next

        Dim img As New cv.Mat(ocvb.result1.Size(), cv.MatType.CV_8UC1)
        rotatedRect.Run(ocvb)
        img = ocvb.result1.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        img = img.Threshold(254, 255, cv.ThresholdTypes.BinaryInv)

        Dim contours0 As cv.Point()()
        If retrievalMode = cv.RetrievalModes.FloodFill Then
            Dim img32sc1 As New cv.Mat
            img.ConvertTo(img32sc1, cv.MatType.CV_32SC1)
            contours0 = cv.Cv2.FindContoursAsArray(img32sc1, retrievalMode, ApproximationMode)
            img32sc1.ConvertTo(img, cv.MatType.CV_8UC1)
            ocvb.result2 = img.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        Else
            contours0 = cv.Cv2.FindContoursAsArray(img, retrievalMode, ApproximationMode)
            ocvb.result2.SetTo(0)
        End If

        Dim contours()() As cv.Point = Nothing
        ReDim contours(contours0.Length - 1)
        For j = 0 To contours0.Length - 1
            contours(j) = cv.Cv2.ApproxPolyDP(contours0(j), 3, True)
        Next

        cv.Cv2.DrawContours(ocvb.result2, contours, 0, New cv.Scalar(0, 255, 255), 2, cv.LineTypes.AntiAlias)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        rotatedRect.Dispose()
        radio1.Dispose()
        radio2.Dispose()
    End Sub
End Class



Public Class Contours_FindandDraw : Implements IDisposable
    Dim rotatedRect As Draw_rotatedRectangles
    Public Sub New(ocvb As AlgorithmData)
        rotatedRect = New Draw_rotatedRectangles(ocvb)
        rotatedRect.rect.sliders.TrackBar1.Value = 5
        ocvb.desc = "Demo the use of FindContours, ApproxPolyDP, and DrawContours."
        ocvb.label1 = "FindandDraw input"
        ocvb.label2 = "FindandDraw output"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim img As New cv.Mat(ocvb.result1.Size(), cv.MatType.CV_8UC1)
        rotatedRect.Run(ocvb)
        img = ocvb.result1.CvtColor(cv.ColorConversionCodes.bgr2gray)
        img = img.Threshold(254, 255, cv.ThresholdTypes.BinaryInv)

        Dim contours0 = cv.Cv2.FindContoursAsArray(img, cv.RetrievalModes.Tree, cv.ContourApproximationModes.ApproxSimple)
        Dim contours()() As cv.Point = Nothing
        ReDim contours(contours0.Length - 1)
        For j = 0 To contours0.Length - 1
            contours(j) = cv.Cv2.ApproxPolyDP(contours0(j), 3, True)
        Next

        ocvb.result2.SetTo(0)
        cv.Cv2.DrawContours(ocvb.result2, contours, 0, New cv.Scalar(0, 255, 255), 2, cv.LineTypes.AntiAlias)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        rotatedRect.Dispose()
    End Sub
End Class



Public Class Contours_Depth : Implements IDisposable
    Public foreground As Depth_InRangeTrim
    Public Sub New(ocvb As AlgorithmData)
        foreground = New Depth_InRangeTrim(ocvb)
        ocvb.desc = "Find and draw the contour of the depth foreground."
        ocvb.label1 = "DepthContour input"
        ocvb.label2 = "DepthContour output"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        foreground.Run(ocvb)
        Dim img = ocvb.result1.CvtColor(cv.ColorConversionCodes.bgr2gray)
        img = img.Threshold(1, 255, cv.ThresholdTypes.Binary)

        Dim contours0 = cv.Cv2.FindContoursAsArray(img, cv.RetrievalModes.Tree, cv.ContourApproximationModes.ApproxSimple)
        Dim contours()() As cv.Point = Nothing
        ReDim contours(contours0.Length - 1)
        Dim maxIndex As Int32
        Dim maxNodes As Int32
        For j = 0 To contours0.Length - 1
            contours(j) = cv.Cv2.ApproxPolyDP(contours0(j), 3, True)
            If maxNodes < contours(j).Length Then
                maxIndex = j
                maxNodes = contours(j).Length
            End If
        Next

        ocvb.result2.SetTo(New cv.Scalar(0))
        For i = 0 To contours0.Length - 1
            If contours(i).Length > 10 Then cv.Cv2.DrawContours(ocvb.result2, contours, i, New cv.Scalar(0, 255, 255), -1)
        Next
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        foreground.Dispose()
    End Sub
End Class



Public Class Contours_RGB : Implements IDisposable
    Dim foreground As Depth_InRangeTrim
    Public Sub New(ocvb As AlgorithmData)
        foreground = New Depth_InRangeTrim(ocvb)
        ocvb.desc = "Find and draw the contour of the largest foreground RGB contour."
        ocvb.label2 = "Background"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        foreground.Run(ocvb)
        Dim img = ocvb.color.CvtColor(cv.ColorConversionCodes.bgr2gray)
        Dim mask = ocvb.result1.CvtColor(cv.ColorConversionCodes.bgr2gray)
        mask = mask.Threshold(1, 255, cv.ThresholdTypes.BinaryInv)
        img.SetTo(0, mask)

        Dim contours0 = cv.Cv2.FindContoursAsArray(img, cv.RetrievalModes.Tree, cv.ContourApproximationModes.ApproxSimple)
        Dim contours()() As cv.Point = Nothing
        ReDim contours(contours0.Length - 1)
        Dim maxIndex As Int32
        Dim maxNodes As Int32
        For j = 0 To contours0.Length - 1
            contours(j) = cv.Cv2.ApproxPolyDP(contours0(j), 3, True)
            If maxNodes < contours(j).Length Then
                maxIndex = j
                maxNodes = contours(j).Length
            End If
        Next

        If contours.Length = 0 Then Exit Sub

        ocvb.result1.SetTo(New cv.Scalar(0))
        Dim hull() = cv.Cv2.ConvexHull(contours(maxIndex), True)
        Dim listOfPoints = New List(Of List(Of cv.Point))
        Dim points = New List(Of cv.Point)
        For j = 0 To hull.Count - 1
            points.Add(New cv.Point(hull(j).X, hull(j).Y))
        Next
        listOfPoints.Add(points)
        cv.Cv2.DrawContours(ocvb.result1, listOfPoints, 0, New cv.Scalar(255, 0, 0), -1)
        cv.Cv2.DrawContours(ocvb.result1, contours, maxIndex, New cv.Scalar(0, 255, 255), -1)
        ocvb.result2.SetTo(0)
        ocvb.color.CopyTo(ocvb.result2, mask)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        foreground.Dispose()
    End Sub
End Class

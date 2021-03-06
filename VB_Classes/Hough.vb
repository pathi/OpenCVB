﻿Imports cv = OpenCvSharp

Module Hough_Exports
    Public Sub houghShowLines(ByRef dst As cv.Mat, segments() As cv.LineSegmentPolar, desiredCount As Int32)
        For i = 0 To Math.Min(segments.Length, desiredCount) - 1
            Dim rho As Single = segments(i).Rho
            Dim theta As Single = segments(i).Theta

            Dim a As Double = Math.Cos(theta)
            Dim b As Double = Math.Sin(theta)
            Dim x As Double = a * rho
            Dim y As Double = b * rho

            Dim pt1 As cv.Point = New cv.Point(Math.Round(x + 1000 * -b), Math.Round(y + 1000 * a))
            Dim pt2 As cv.Point = New cv.Point(Math.Round(x - 1000 * -b), Math.Round(y - 1000 * a))
            dst.Line(pt1, pt2, cv.Scalar.Red, 2, cv.LineTypes.AntiAlias, 0)
        Next
    End Sub

    Public Sub houghShowLines3D(ByRef dst As cv.Mat, segment As cv.Line3D)
        Dim x As Double = segment.X1 * dst.Cols
        Dim y As Double = segment.Y1 * dst.Rows
        Dim m As Double
        If segment.Vx < 0.001 Then
            m = 0
        Else
            m = segment.Vy / segment.Vx ' vertical slope a no-no.
        End If
        Dim b As Double = y - m * x
        Dim pt1 As cv.Point = New cv.Point(x, y)
        Dim pt2 As cv.Point
        If m = 0 Then pt2 = New cv.Point(x, dst.Rows) Else pt2 = New cv.Point((dst.Rows - b) / m, dst.Rows)
        dst.Line(pt1, pt2, cv.Scalar.Red, 3, cv.LineTypes.AntiAlias, 0)
    End Sub
End Module




' https://docs.opencv.org/3.1.0/d6/d10/tutorial_py_houghlines.html
Public Class Hough_Circles : Implements IDisposable
    Dim circles As Draw_Circles
    Public updateFrequency = 30
    Public Sub New(ocvb As AlgorithmData)
        circles = New Draw_Circles(ocvb)
        circles.sliders.TrackBar1.Value = 3
        ocvb.desc = "Find circles using HoughCircles."
        ocvb.label1 = "Input circles to Hough"
        ocvb.label2 = "Hough Circles found"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        circles.Run(ocvb)
        Static Dim method As Int32 = 3
        Dim gray = New cv.Mat
        cv.Cv2.CvtColor(ocvb.result1, gray, cv.ColorConversionCodes.bgr2gray)
        Dim cFound = cv.Cv2.HoughCircles(gray, method, 1, ocvb.result1.Rows / 4, 100, 10, 1, 200)
        cv.Cv2.CvtColor(gray, ocvb.result2, cv.ColorConversionCodes.gray2bgr)
        Dim foundColor = New cv.Scalar(0, 0, 255)
        For i = 0 To cFound.Length - 1
            cv.Cv2.Circle(ocvb.result2, New cv.Point(CInt(cFound(i).Center.X), CInt(cFound(i).Center.Y)), cFound(i).Radius, foundColor, 5, cv.LineTypes.AntiAlias)
        Next
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        circles.Dispose()
    End Sub
End Class



' https://docs.opencv.org/3.1.0/d6/d10/tutorial_py_houghlines.html
' https://github.com/JiphuTzu/opencvsharp/blob/master/sample/SamplesVB/Samples/HoughLinesSample.vb
Public Class Hough_Lines : Implements IDisposable
    Dim edges As Edges_Canny
    Dim sliders As New OptionsSliders
    Public segments() As cv.LineSegmentPolar
    Public Sub New(ocvb As AlgorithmData)
        edges = New Edges_Canny(ocvb)

        sliders.setupTrackBar1(ocvb, "rho", 1, 100, 1)
        sliders.setupTrackBar2(ocvb, "theta", 1, 1000, 1000 * Math.PI / 180)
        sliders.setupTrackBar3(ocvb, "threshold", 1, 100, 50)
        sliders.setupTrackBar4(ocvb, "Lines to Plot", 1, 1000, 50)
        If ocvb.parms.ShowOptions Then sliders.show()
        ocvb.desc = "Use Houghlines to find lines in the image."
    End Sub

    Public Sub Run(ocvb As AlgorithmData)
        edges.Run(ocvb)
        Dim gray = ocvb.result1.Clone()

        Dim rhoIn = sliders.TrackBar1.Value
        Dim thetaIn = sliders.TrackBar2.Value / 1000
        Dim threshold = sliders.TrackBar3.Value

        segments = cv.Cv2.HoughLines(gray, rhoIn, thetaIn, threshold)
        ocvb.label1 = "Found " + CStr(segments.Length) + " Lines"

        ocvb.color.CopyTo(ocvb.result1)
        ocvb.color.CopyTo(ocvb.result2)
        houghShowLines(ocvb.result1, segments, sliders.TrackBar4.Value)

        Dim probSegments = cv.Cv2.HoughLinesP(gray, rhoIn, thetaIn, threshold)
        For i = 0 To Math.Min(probSegments.Length, sliders.TrackBar4.Value) - 1
            Dim line = probSegments(i)
            ocvb.result2.Line(line.P1, line.P2, cv.Scalar.Red, 3, cv.LineTypes.AntiAlias)
        Next
        ocvb.label2 = "Probablistic lines = " + CStr(probSegments.Length)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        edges.Dispose()
        sliders.Dispose()
    End Sub
End Class





Public Class Hough_Lines_MT : Implements IDisposable
    Dim edges As Edges_Canny
    Public grid As Thread_Grid
    Dim sliders As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "rho", 1, 100, 1)
        sliders.setupTrackBar2(ocvb, "theta", 1, 1000, 1000 * Math.PI / 180)
        sliders.setupTrackBar3(ocvb, "threshold", 1, 100, 3)
        If ocvb.parms.ShowOptions Then sliders.show()

        edges = New Edges_Canny(ocvb)
        edges.externalUse = True

        grid = New Thread_Grid(ocvb)
        grid.sliders.TrackBar1.Value = 16
        grid.sliders.TrackBar2.Value = 16
        grid.externalUse = True ' we don't need any results.
        ocvb.desc = "Multithread Houghlines to find lines in image fragments."
        ocvb.label1 = "Hough_Lines_MT"
        ocvb.label2 = "Hough_Lines_MT"
    End Sub

    Public Sub Run(ocvb As AlgorithmData)
        grid.Run(ocvb)

        edges.src = ocvb.color
        edges.Run(ocvb)

        Dim rhoIn = sliders.TrackBar1.Value
        Dim thetaIn = sliders.TrackBar2.Value / 1000
        Dim threshold = sliders.TrackBar3.Value

        ocvb.color.CopyTo(ocvb.result1)
        Parallel.ForEach(Of cv.Rect)(grid.roiList,
        Sub(roi)
            Dim segments() = cv.Cv2.HoughLines(edges.dst(roi), rhoIn, thetaIn, threshold)
            If segments.Count = 0 Then
                ocvb.result2(roi) = ocvb.depthRGB(roi)
                Exit Sub
            End If
            ocvb.result2(roi).SetTo(0)
            houghShowLines(ocvb.result2(roi), segments, 1)
        End Sub)
        ocvb.result1.SetTo(cv.Scalar.White, grid.gridMask)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        edges.Dispose()
        grid.Dispose()
        sliders.Dispose()
    End Sub
End Class

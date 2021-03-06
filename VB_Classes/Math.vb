﻿Imports cv = OpenCvSharp
Public Class Math_Subtract : Implements IDisposable
    Dim sliders As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "Red", 0, 255, 255)
        sliders.setupTrackBar2(ocvb, "Green", 0, 255, 255)
        sliders.setupTrackBar3(ocvb, "Blue", 0, 255, 255)
        If ocvb.parms.ShowOptions Then sliders.Show()
        ocvb.desc = "Invert the image colors using subtract"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim tmp = New cv.Mat(ocvb.color.Size(), cv.MatType.CV_8UC3)
        tmp.SetTo(New cv.Scalar(sliders.TrackBar3.Value, sliders.TrackBar2.Value, sliders.TrackBar1.Value))
        cv.Cv2.Subtract(tmp, ocvb.color, ocvb.result1)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class






Public Class Math_Median_CDF : Implements IDisposable
    Dim sliders As New OptionsSliders
    Public src As cv.Mat
    Public medianVal As Double
    Public hist As New cv.Mat()
    Public rangeMin As Integer = 0
    Public rangeMax As Integer = 255
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "Histogram Bins", 4, 1000, 100)
        If ocvb.parms.ShowOptions Then sliders.show()
        ocvb.desc = "Compute the src image median"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim bins As Int32 = sliders.TrackBar1.Value
        Dim dimensions() = New Integer() {bins}
        Dim ranges() = New cv.Rangef() {New cv.Rangef(rangeMin, rangeMax)}

        If src Is Nothing Then
            ocvb.result1 = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Else
            ocvb.result1 = src.Clone()
        End If
        cv.Cv2.CalcHist(New cv.Mat() {ocvb.result1}, New Integer() {0}, New cv.Mat(), hist, 1, dimensions, ranges)

        Dim cdf As New cv.Mat
        hist.CopyTo(cdf)
        Dim cdfIndexer = cdf.GetGenericIndexer(Of Single)
        For i = 1 To bins - 1
            cdfIndexer(i) = cdfIndexer(i - 1) + cdfIndexer(i)
        Next
        cdf /= ocvb.result1.Total

        cdfIndexer = cdf.GetGenericIndexer(Of Single) ' need to reset the indexer because the mat will move with the divide above.
        For i = 0 To bins - 1
            If cdfIndexer(i) > 0.5 Then
                medianVal = i * (rangeMax - rangeMin) / bins
                Exit For
            End If
        Next

        If src Is Nothing Then
            Dim mask = New cv.Mat
            mask = ocvb.result1.GreaterThan(medianVal)
            ocvb.result1.SetTo(0)
            ocvb.color.CopyTo(ocvb.result1, mask)
            ocvb.label1 = "Grayscale pixels > " + Format(medianVal, "#0.0")

            cv.Cv2.BitwiseNot(mask, mask)
            ocvb.result2.SetTo(0)
            ocvb.color.CopyTo(ocvb.result2, mask) ' show the other half.
            ocvb.label2 = "Grayscale pixels < " + Format(medianVal, "#0.0")
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class





Public Class Math_DepthMeanStdev : Implements IDisposable
    Dim minMax As Depth_Stable
    Public Sub New(ocvb As AlgorithmData)
        minMax = New Depth_Stable(ocvb)
        ocvb.desc = "This algorithm shows that just using the max depth at each pixel does not improve depth!  Mean and stdev don't change."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        minMax.Run(ocvb)
        If minMax.stableDepth IsNot Nothing Then
            Dim mean As Single = 0, stdev As Single = 0
            cv.Cv2.MeanStdDev(minMax.stableDepth, mean, stdev, ocvb.result1)
            ocvb.label1 = "stablized depth mean=" + Format(mean, "#0.0") + " stdev=" + Format(stdev, "#0.0")
            cv.Cv2.MeanStdDev(ocvb.depth, mean, stdev, ocvb.result1)
            ocvb.label2 = "raw depth mean=" + Format(mean, "#0.0") + " stdev=" + Format(stdev, "#0.0")
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        minMax.Dispose()
    End Sub
End Class





Public Class Math_RGBCorrelation : Implements IDisposable
    Dim flow As Font_FlowText
    Dim corr As MatchTemplate_Correlation
    Public Sub New(ocvb As AlgorithmData)
        flow = New Font_FlowText(ocvb)
        flow.externalUse = True
        flow.result1or2 = RESULT2

        corr = New MatchTemplate_Correlation(ocvb)
        corr.externalUse = True
        corr.reportFreq = 1

        ocvb.desc = "Compute the correlation coefficient of Red-Green and Red-Blue and Green-Blue"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim split = ocvb.color.Split()
        corr.sample1 = split(0)
        corr.sample2 = split(1)
        corr.Run(ocvb)
        Dim blueGreenCorrelation = "Blue-Green " + ocvb.label1

        corr.sample1 = split(2)
        corr.sample2 = split(1)
        corr.Run(ocvb)
        Dim redGreenCorrelation = "Red-Green " + ocvb.label1

        corr.sample1 = split(2)
        corr.sample2 = split(0)
        corr.Run(ocvb)
        Dim redBlueCorrelation = "Red-Blue " + ocvb.label1

        flow.msgs.Add(blueGreenCorrelation + " " + redGreenCorrelation + " " + redBlueCorrelation)
        flow.Run(ocvb)
        ocvb.label1 = ""
        ocvb.label2 = "Log of " + corr.matchText
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        corr.Dispose()
        flow.Dispose()
    End Sub
End Class
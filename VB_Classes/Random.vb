﻿Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices

Public Class Random_Points : Implements IDisposable
    Public sliders As New OptionsSliders
    Public Points() As cv.Point
    Public Points2f() As cv.Point2f
    Public externalUse As Boolean
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "Random Pixel Count", 1, 500, 20)
        If ocvb.parms.ShowOptions Then sliders.show()

        ReDim Points(sliders.TrackBar1.Value - 1)
        ReDim Points2f(sliders.TrackBar1.Value - 1)

        ocvb.desc = "Create a random mask with a specificied number of pixels."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If Points.Length <> sliders.TrackBar1.Value Then
            ReDim Points(sliders.TrackBar1.Value - 1)
            ReDim Points2f(sliders.TrackBar1.Value - 1)
        End If
        If externalUse = False Then ocvb.result1.SetTo(0)
        For i = 0 To Points.Length - 1
            Dim x = ocvb.rng.uniform(0, ocvb.color.Cols)
            Dim y = ocvb.rng.uniform(0, ocvb.color.Rows)
            Points(i) = New cv.Point2f(x, y)
            Points2f(i) = New cv.Point2f(x, y)
            If externalUse = False Then cv.Cv2.Circle(ocvb.result1, Points(i), 3, cv.Scalar.Gray, -1, cv.LineTypes.AntiAlias, 0)
        Next
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class




Public Class Random_Shuffle : Implements IDisposable
    Public Sub New(ocvb As AlgorithmData)
        ocvb.desc = "Use randomShuffle to reorder an image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        ocvb.depthRGB.CopyTo(ocvb.result1)
        Dim myRNG As New cv.RNG
        cv.Cv2.RandShuffle(ocvb.result1, 1.0, myRNG) ' don't remove that myRNG!  It will fail in RandShuffle.
        ocvb.label1 = "Random_shuffle - wave at camera"
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class



Public Class Random_LUTMask : Implements IDisposable
    Dim random As Random_Points
    Dim km As kMeans_Basics
    Public Sub New(ocvb As AlgorithmData)
        km = New kMeans_Basics(ocvb)
        random = New Random_Points(ocvb)
        ocvb.desc = "Use a random Look-Up-Table to modify few colors in a kmeans image.  Note how interpolation impacts results"
        ocvb.label2 = "kmeans run To Get colors"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static lutMat As cv.Mat
        If lutMat Is Nothing Or ocvb.frameCount Mod 10 = 0 Then
            random.Run(ocvb)
            lutMat = cv.Mat.Zeros(New cv.Size(1, 256), cv.MatType.CV_8UC3)
            Dim lutIndex = 0
            km.Run(ocvb) ' sets result1
            ocvb.result1.CopyTo(ocvb.result2)
            For i = 0 To random.Points.Length - 1
                Dim x = random.Points(i).X
                Dim y = random.Points(i).Y
                If x >= ocvb.drawRect.X And x < ocvb.drawRect.X + ocvb.drawRect.Width Then
                    If y >= ocvb.drawRect.Y And y < ocvb.drawRect.Y + ocvb.drawRect.Height Then
                        lutMat.Set(lutIndex, 0, ocvb.result2.At(Of cv.Vec3b)(y, x))
                        lutIndex += 1
                        If lutIndex >= lutMat.Rows Then Exit For
                    End If
                End If
            Next
        End If
        ocvb.result2 = ocvb.color.LUT(lutMat)
        ocvb.label1 = "Using kmeans colors with interpolation"
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        km.Dispose()
        random.Dispose()
    End Sub
End Class



Public Class Random_UniformDist : Implements IDisposable
    Public uDist As cv.Mat
    Public externalUse As Boolean
    Public Sub New(ocvb As AlgorithmData)
        uDist = New cv.Mat(ocvb.color.Size(), cv.MatType.CV_8UC1)
        ocvb.desc = "Create a uniform distribution."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        cv.Cv2.Randu(uDist, 0, 255)
        If externalUse = False Then
            ocvb.result1 = uDist.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class



Public Class Random_NormalDist : Implements IDisposable
    Public sliders As New OptionsSliders
    Public nDistImage As cv.Mat
    Public externalUse As Boolean
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "Random_NormalDist Blue Mean", 0, 255, 25)
        sliders.setupTrackBar2(ocvb, "Random_NormalDist Green Mean", 0, 255, 127)
        sliders.setupTrackBar3(ocvb, "Random_NormalDist Red Mean", 0, 255, 180)
        sliders.setupTrackBar4(ocvb, "Random_NormalDist Stdev", 0, 255, 50)
        If ocvb.parms.ShowOptions Then sliders.show()
        ocvb.desc = "Create a normal distribution."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        cv.Cv2.Randn(ocvb.result1, New cv.Scalar(sliders.TrackBar1.Value, sliders.TrackBar2.Value, sliders.TrackBar3.Value), cv.Scalar.All(sliders.TrackBar4.Value))
        If externalUse Then nDistImage = ocvb.result1.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class



Public Class Random_CheckUniformDist : Implements IDisposable
    Dim histogram As Histogram_KalmanSmoothed
    Dim rUniform As Random_UniformDist
    Public Sub New(ocvb As AlgorithmData)
        histogram = New Histogram_KalmanSmoothed(ocvb)
        histogram.externalUse = True
        histogram.sliders.TrackBar1.Value = 255
        histogram.gray = New cv.Mat

        rUniform = New Random_UniformDist(ocvb)
        rUniform.externalUse = True

        ocvb.desc = "Display the histogram for a uniform distribution."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        rUniform.Run(ocvb)
        ocvb.result1 = rUniform.uDist.CvtColor(cv.ColorConversionCodes.gray2bgr)
        rUniform.uDist.CopyTo(histogram.gray)
        histogram.Run(ocvb)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        rUniform.Dispose()
        histogram.Dispose()
    End Sub
End Class



Public Class Random_CheckNormalDist : Implements IDisposable
    Dim histogram As Histogram_KalmanSmoothed
    Dim normalDist As Random_NormalDist
    Public Sub New(ocvb As AlgorithmData)
        histogram = New Histogram_KalmanSmoothed(ocvb)
        histogram.externalUse = True
        histogram.sliders.TrackBar1.Value = 255
        histogram.gray = New cv.Mat
        histogram.plotHist.minRange = 1
        normalDist = New Random_NormalDist(ocvb)
        normalDist.externalUse = True
        ocvb.desc = "Display the histogram for a Normal distribution."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        normalDist.Run(ocvb)
        ocvb.result1 = normalDist.nDistImage.CvtColor(cv.ColorConversionCodes.gray2bgr)
        normalDist.nDistImage.CopyTo(histogram.gray)
        histogram.Run(ocvb)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        normalDist.Dispose()
        histogram.Dispose()
    End Sub
End Class





Module Random_PatternGenerator_CPP_Module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Random_PatternGenerator_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Random_PatternGenerator_Close(Random_PatternGeneratorPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Random_PatternGenerator_Run(Random_PatternGeneratorPtr As IntPtr, rows As Int32, cols As Int32, channels As Int32) As IntPtr
    End Function
End Module




Public Class Random_PatternGenerator_CPP : Implements IDisposable
    Dim Random_PatternGenerator As IntPtr
    Public Sub New(ocvb As AlgorithmData)
        Random_PatternGenerator = Random_PatternGenerator_Open()
        ocvb.desc = "Generate random patterns for use with 'Random Pattern Calibration'"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim src = ocvb.color
        Dim srcData(src.Total * src.ElemSize) As Byte
        Marshal.Copy(src.Data, srcData, 0, srcData.Length - 1)
        Dim imagePtr = Random_PatternGenerator_Run(Random_PatternGenerator, src.Rows, src.Cols, src.Channels)

        If imagePtr <> 0 Then
            Dim dstData(src.Total - 1) As Byte
            Marshal.Copy(imagePtr, dstData, 0, dstData.Length)
            ocvb.result1 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8UC1, dstData)
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        Random_PatternGenerator_Close(Random_PatternGenerator)
    End Sub
End Class

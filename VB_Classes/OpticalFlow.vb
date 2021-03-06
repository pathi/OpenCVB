﻿Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Module OpticalFlowModule_Exports
    ' https://docs.opencv.org/3.4/db/d7f/tutorial_js_lucas_kanade.html
    Public Function opticalFlow_Dense(oldGray As cv.Mat, gray As cv.Mat, pyrScale As Single, levels As Int32, winSize As Int32, iterations As Int32,
                                polyN As Single, polySigma As Single, OpticalFlowFlags As cv.OpticalFlowFlags) As cv.Mat
        Dim flow As New cv.Mat
        If pyrScale >= 1 Then pyrScale = 0.99
        cv.Cv2.CalcOpticalFlowFarneback(oldGray, gray, flow, pyrScale, levels, winSize, iterations, polyN, polySigma, OpticalFlowFlags)
        Dim flowVec(1) As cv.Mat
        cv.Cv2.Split(flow, flowVec)

        Dim hsv As New cv.Mat
        Dim hsv0 As New cv.Mat
        Dim hsv1 As New cv.Mat(gray.Rows, gray.Cols, cv.MatType.CV_8UC1, 255)
        Dim hsv2 As New cv.Mat

        Dim magnitude As New cv.Mat
        Dim angle As New cv.Mat
        cv.Cv2.CartToPolar(flowVec(0), flowVec(1), magnitude, angle)
        angle.ConvertTo(hsv0, cv.MatType.CV_8UC1, 180 / Math.PI / 2)
        cv.Cv2.Normalize(magnitude, hsv2, 0, 255, cv.NormTypes.MinMax, cv.MatType.CV_8UC1)

        Dim hsvVec() As cv.Mat = {hsv0, hsv1, hsv2}
        cv.Cv2.Merge(hsvVec, hsv)
        Return hsv
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function OpticalFlow_CPP_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub OpticalFlow_CPP_Close(sPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function OpticalFlow_CPP_Run(sPtr As IntPtr, rgbPtr As IntPtr, rows As Int32, cols As Int32) As IntPtr
    End Function
    Public Sub calcOpticalFlowPyrLK_Native(gray1 As cv.Mat, gray2 As cv.Mat, features1 As cv.Mat, features2 As cv.Mat)
        Dim hGray1 As GCHandle
        Dim hGray2 As GCHandle
        Dim hF1 As GCHandle
        Dim hF2 As GCHandle

        Dim grayData1(gray1.Total - 1)
        Dim grayData2(gray2.Total - 1)
        Dim fData1(features1.Total * features1.ElemSize - 1)
        Dim fData2(features2.Total * features2.ElemSize - 1)
        hGray1 = GCHandle.Alloc(grayData1, GCHandleType.Pinned)
        hGray2 = GCHandle.Alloc(grayData2, GCHandleType.Pinned)
        hF1 = GCHandle.Alloc(fData1, GCHandleType.Pinned)
        hF2 = GCHandle.Alloc(fData2, GCHandleType.Pinned)
    End Sub
End Module





Public Class OpticalFlow_DenseOptions : Implements IDisposable
    Public radio As New OptionsRadioButtons
    Public sliders As New OptionsSliders
    Public sliders2 As New OptionsSliders
    Public pyrScale As Single
    Public levels As Int32
    Public winSize As Int32
    Public iterations As Int32
    Public polyN As Single
    Public polySigma As Single
    Public OpticalFlowFlags As cv.OpticalFlowFlags
    Public outputScaling As Int32
    Public Sub New(ocvb As AlgorithmData)
        radio.Setup(ocvb, 5)
        radio.check(0).Text = "FarnebackGaussian"
        radio.check(1).Text = "LkGetMinEigenvals"
        radio.check(2).Text = "None"
        radio.check(3).Text = "PyrAReady"
        radio.check(4).Text = "PyrBReady"
        radio.check(0).Checked = True
        If ocvb.parms.ShowOptions Then radio.show()

        sliders2.setupTrackBar1(ocvb, "Optical Flow PolyN", 1, 15, 5)
        sliders2.setupTrackBar2(ocvb, "Optical Flow Scaling Output", 1, 100, 50)
        If ocvb.parms.ShowOptions Then sliders2.Show()

        sliders.setupTrackBar1(ocvb, "Optical Flow pyrScale", 1, 100, 40)
        sliders.setupTrackBar2(ocvb, "Optical Flow Levels", 1, 10, 1)
        sliders.setupTrackBar3(ocvb, "Optical Flow winSize", 1, 9, 1)
        sliders.setupTrackBar4(ocvb, "Optical Flow Iterations", 1, 10, 1)
        If ocvb.parms.ShowOptions Then sliders.show()

        ocvb.desc = "Use dense optical flow algorithm options"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        pyrScale = sliders.TrackBar1.Value / sliders.TrackBar1.Maximum
        levels = sliders.TrackBar2.Value
        winSize = sliders.TrackBar3.Value
        iterations = sliders.TrackBar4.Value
        If winSize Mod 2 = 0 Then winSize += 1
        polyN = sliders2.TrackBar1.Value
        If polyN Mod 2 = 0 Then polyN += 1
        polySigma = 1.5
        If polyN <= 5 Then polySigma = 1.1

        For i = 0 To radio.check.Count - 1
            If radio.check(i).Checked Then
                OpticalFlowFlags = Choose(i + 1, cv.OpticalFlowFlags.FarnebackGaussian, cv.OpticalFlowFlags.LkGetMinEigenvals, cv.OpticalFlowFlags.None,
                                                     cv.OpticalFlowFlags.PyrAReady, cv.OpticalFlowFlags.PyrBReady, cv.OpticalFlowFlags.UseInitialFlow)
                Exit For
            End If
        Next
        outputScaling = sliders2.TrackBar2.Value
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        sliders2.Dispose()
        radio.Dispose()
    End Sub
End Class




Public Class OpticalFlow_DenseBasics : Implements IDisposable
    Dim flow As OpticalFlow_DenseOptions
    Public Sub New(ocvb As AlgorithmData)
        flow = New OpticalFlow_DenseOptions(ocvb)
        ocvb.desc = "Use dense optical flow algorithm  "
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static oldGray As New cv.Mat
        Dim gray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        If ocvb.frameCount > 0 Then
            flow.Run(ocvb)

            Dim hsv = opticalFlow_Dense(oldGray, gray, flow.pyrScale, flow.levels, flow.winSize, flow.iterations, flow.polyN, flow.polySigma, flow.OpticalFlowFlags)
            ocvb.result1 = hsv.CvtColor(cv.ColorConversionCodes.HSV2RGB)
            ocvb.result1 = ocvb.result1.ConvertScaleAbs(flow.outputScaling)
            ocvb.result2 = ocvb.result1.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        End If
        oldGray = gray.Clone()
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        flow.Dispose()
    End Sub
End Class




Public Class OpticalFlow_DenseBasics_MT : Implements IDisposable
    Public sliders As New OptionsSliders
    Public grid As Thread_Grid
    Dim accum As New cv.Mat
    Dim flow As OpticalFlow_DenseOptions
    Public Sub New(ocvb As AlgorithmData)
        grid = New Thread_Grid(ocvb)
        grid.externalUse = True
        grid.sliders.TrackBar1.Value = 32
        grid.sliders.TrackBar2.Value = 32
        grid.sliders.TrackBar3.Value = 5

        flow = New OpticalFlow_DenseOptions(ocvb)
        flow.sliders.TrackBar1.Value = 75

        sliders.setupTrackBar1(ocvb, "Correlation Threshold", 0, 1000, 1000)
        If ocvb.parms.ShowOptions Then sliders.show()

        ocvb.desc = "MultiThread dense optical flow algorithm  "
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static oldGray As New cv.Mat

        If ocvb.frameCount > 0 Then
            grid.Run(ocvb)
            flow.Run(ocvb)

            Dim CCthreshold = CSng(sliders.TrackBar1.Value / sliders.TrackBar1.Maximum)
            Parallel.For(0, grid.borderList.Count - 1,
            Sub(i)
                Dim broi = grid.borderList(i)
                Dim roi = grid.roiList(i)
                Dim correlation As New cv.Mat
                Dim src = ocvb.color(roi)
                cv.Cv2.MatchTemplate(src, accum(roi), correlation, cv.TemplateMatchModes.CCoeffNormed)
                If CCthreshold > correlation.Get(Of Single)(0, 0) Then
                    src.CopyTo(accum(roi))
                    Dim gray = accum(broi).CvtColor(cv.ColorConversionCodes.BGR2GRAY)
                    Dim hsv = opticalFlow_Dense(oldGray(broi), gray, flow.pyrScale, flow.levels, flow.winSize, flow.iterations, flow.polyN, flow.polySigma, flow.OpticalFlowFlags)
                    Dim tROI = New cv.Rect(roi.X - broi.X, roi.Y - broi.Y, roi.Width, roi.Height)
                    ocvb.result1(roi) = hsv(tROI).CvtColor(cv.ColorConversionCodes.HSV2RGB)
                    ocvb.result1(roi) = ocvb.result1(roi).ConvertScaleAbs(flow.outputScaling)
                Else
                    ocvb.result1(roi).SetTo(0)
                End If
                oldGray(roi) = accum(roi).Clone()
            End Sub)
            ocvb.result2 = accum.Clone()
            ocvb.result2.SetTo(cv.Scalar.All(255), grid.gridMask)
        Else
            oldGray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            accum = ocvb.color.Clone()
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        flow.Dispose()
        grid.Dispose()
    End Sub
End Class





Public Class OpticalFlow_Sparse : Implements IDisposable
    Dim sliders As New OptionsSliders
    Public radio As New OptionsRadioButtons
    Public features As New List(Of cv.Point2f)
    Public externalUse As Boolean

    Dim good As Features_GoodFeatures
    Dim lastFrame As cv.Mat
    Dim sumScale As cv.Mat, sScale As cv.Mat
    Dim errScale As cv.Mat, qScale As cv.Mat, rScale As cv.Mat
    Public Sub New(ocvb As AlgorithmData)
        good = New Features_GoodFeatures(ocvb)
        good.externalUse = True

        sliders.setupTrackBar1(ocvb, "OpticalFlow window", 1, 20, 3)
        sliders.setupTrackBar2(ocvb, "OpticalFlow Max Pixels Distance", 1, 100, 30)
        If ocvb.parms.ShowOptions Then sliders.show()

        radio.Setup(ocvb, 6)
        radio.check(0).Text = "FarnebackGaussian"
        radio.check(1).Text = "LkGetMinEigenvals"
        radio.check(2).Text = "None"
        radio.check(3).Text = "PyrAReady"
        radio.check(4).Text = "PyrBReady"
        radio.check(5).Text = "UseInitialFlow"
        radio.check(5).Enabled = False
        radio.check(0).Checked = True
        If ocvb.parms.ShowOptions Then radio.show()

        ocvb.desc = "Show the optical flow of a sparse matrix."
        ocvb.label1 = ""
        ocvb.label2 = ""
    End Sub
    Private Sub kalmanFilter()
        Dim f1err As New cv.Mat
        cv.Cv2.Add(errScale, qScale, f1err)
        For i = 0 To errScale.Rows - 1
            Dim gainScale = f1err.At(Of Double)(i, 0) / (f1err.At(Of Double)(i, 0) + rScale.At(Of Double)(i, 0))
            sScale.Set(Of Double)(i, 0, sScale.At(Of Double)(i, 0) + gainScale * (sumScale.At(Of Double)(i, 0) - sScale.At(Of Double)(i, 0)))
            errScale.Set(Of Double)(i, 0, (1 - gainScale) * f1err.At(Of Double)(i, 0))
        Next
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        ocvb.result1 = ocvb.color.Clone()
        ocvb.result2 = ocvb.color.Clone()

        Dim OpticalFlowFlag As cv.OpticalFlowFlags
        For i = 0 To radio.check.Count - 1
            If radio.check(i).Checked Then
                OpticalFlowFlag = Choose(i + 1, cv.OpticalFlowFlags.FarnebackGaussian, cv.OpticalFlowFlags.LkGetMinEigenvals, cv.OpticalFlowFlags.None,
                                                     cv.OpticalFlowFlags.PyrAReady, cv.OpticalFlowFlags.PyrBReady, cv.OpticalFlowFlags.UseInitialFlow)
                Exit For
            End If
        Next

        If ocvb.frameCount = 0 Then
            errScale = New cv.Mat(5, 1, cv.MatType.CV_64F, 1)
            qScale = New cv.Mat(5, 1, cv.MatType.CV_64F, 0.004)
            rScale = New cv.Mat(5, 1, cv.MatType.CV_64F, 0.5)
            sumScale = New cv.Mat(5, 1, cv.MatType.CV_64F, 0)
            sScale = New cv.Mat(5, 1, cv.MatType.CV_64F, 0)
        End If

        Dim gray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        good.gray = gray.Clone()
        good.Run(ocvb)
        features = good.goodFeatures
        Dim features1 = New cv.Mat(features.Count, 1, cv.MatType.CV_32FC2, features.ToArray)
        Dim features2 = New cv.Mat
        If lastFrame IsNot Nothing Then
            Dim status As New cv.Mat
            Dim err As New cv.Mat
            Dim winSize As New cv.Size(3, 3)
            cv.Cv2.CalcOpticalFlowPyrLK(gray, lastFrame, features1, features2, status, err, winSize, 3, term, OpticalFlowFlag)
            features = New List(Of cv.Point2f)
            Dim lastFeatures As New List(Of cv.Point2f)
            For i = 0 To status.Rows - 1
                If status.At(Of Byte)(i, 0) Then
                    Dim pt1 = features1.At(Of cv.Point2f)(i, 0)
                    Dim pt2 = features2.At(Of cv.Point2f)(i, 0)
                    Dim length = Math.Sqrt((pt1.X - pt2.X) * (pt1.X - pt2.X) + (pt1.Y - pt2.Y) * (pt1.Y - pt2.Y))
                    If length < 30 Then
                        features.Add(pt1)
                        lastFeatures.Add(pt2)
                        ocvb.result1.Line(pt1, pt2, cv.Scalar.Red, 5, cv.LineTypes.AntiAlias)
                        ocvb.result2.Circle(pt1, 5, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
                        ocvb.result2.Circle(pt2, 3, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
                    End If
                End If
            Next
            ocvb.label1 = "Matched " + CStr(features.Count) + " points "

            If ocvb.frameCount Mod 10 = 0 Then lastFrame = gray.Clone()
        Else
            lastFrame = gray.Clone()
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        good.Dispose()
        radio.Dispose()
        sliders.Dispose()
    End Sub
End Class
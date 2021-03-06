﻿Imports cv = OpenCvSharp
' http://www.mia.uni-saarland.de/Publications/weickert-dagm03.pdf
Public Class Coherence_Basics : Implements IDisposable
    Dim sliders As New OptionsSliders
    Public src As New cv.Mat
    Public externalUse As Boolean
    Public Sub New(ocvb As AlgorithmData)
        ocvb.desc = "Find lines that are artistically coherent in the image - Painterly effect."
        sliders.setupTrackBar1(ocvb, "Coherence Sigma", 1, 15, 9)
        sliders.setupTrackBar2(ocvb, "Coherence Blend", 1, 10, 10)
        sliders.setupTrackBar3(ocvb, "Coherence str_sigma", 1, 15, 15)
        sliders.setupTrackBar4(ocvb, "Coherence eigen kernel", 1, 31, 1)
        If ocvb.parms.ShowOptions Then sliders.Show()
        ocvb.label1 = "Coherence - draw rectangle to apply"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim sigma = sliders.TrackBar1.Value * 2 + 1
        Dim blend = sliders.TrackBar2.Value / 10
        Dim str_sigma = sliders.TrackBar3.Value * 2 + 1
        Dim eigenKernelSize = sliders.TrackBar4.Value * 2 + 1

        Dim side = 512
        If ocvb.color.Height < side Then side = 256
        Dim xoffset = ocvb.color.Width / 2 - side / 2
        Dim yoffset = ocvb.color.Height / 2 - side / 2
        Dim srcRect = New cv.Rect(xoffset, yoffset, side, side)
        If ocvb.drawRect.Width <> 0 Then srcRect = ocvb.drawRect
        If externalUse = False Then src = ocvb.color

        ocvb.result1 = src.Clone()
        src = src(srcRect)

        Dim gray As New cv.Mat
        Dim eigen As New cv.Mat
        Dim split() As cv.Mat
        For i = 0 To 3
            gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            eigen = gray.CornerEigenValsAndVecs(str_sigma, eigenKernelSize)
            split = eigen.Split()
            Dim x = split(2), y = split(3)

            Dim gxx = gray.Sobel(cv.MatType.CV_32F, 2, 0, sigma)
            Dim gxy = gray.Sobel(cv.MatType.CV_32F, 1, 1, sigma)
            Dim gyy = gray.Sobel(cv.MatType.CV_32F, 0, 2, sigma)

            Dim tmpX As New cv.Mat, tmpXY As New cv.Mat, tmpY As New cv.Mat
            cv.Cv2.Multiply(x, x, tmpX)
            cv.Cv2.Multiply(tmpX, gxx, tmpX)
            cv.Cv2.Multiply(x, y, tmpXY)
            cv.Cv2.Multiply(tmpXY, gxy, tmpXY)
            tmpXY.Mul(tmpXY, 2)

            cv.Cv2.Multiply(y, y, tmpY)
            cv.Cv2.Multiply(tmpY, gyy, tmpY)

            Dim gvv As New cv.Mat
            gvv = tmpX + tmpXY + tmpY

            Dim mask = gvv.Threshold(0, 255, cv.ThresholdTypes.BinaryInv)
            mask.ConvertTo(mask, cv.MatType.CV_8U)

            Dim erode = src.Erode(New cv.Mat)
            Dim dilate = src.Dilate(New cv.Mat)

            Dim imgl = erode
            dilate.CopyTo(imgl, mask)
            src = src * (1 - blend) + imgl * blend
        Next
        ocvb.result1(srcRect) = src
        ocvb.result1.Rectangle(srcRect, cv.Scalar.Yellow, 2)
        ocvb.drawRect = srcRect
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class




Public Class Coherence_Depth : Implements IDisposable
    Dim coherent As Coherence_Basics
    Public Sub New(ocvb As AlgorithmData)
        coherent = New Coherence_Basics(ocvb)
        coherent.externalUse = True
        ocvb.desc = "Find coherent lines in the depth image"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        coherent.src = ocvb.depthRGB
        coherent.Run(ocvb)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        coherent.Dispose()
    End Sub
End Class
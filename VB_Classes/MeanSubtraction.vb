﻿Imports cv = OpenCvSharp
'https://www.pyimagesearch.com/2017/11/06/deep-learning-opencvs-blobfromimage-works/
Public Class MeanSubtraction_Basics : Implements IDisposable
    Dim sliders As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "Scaling Factor = mean/scaling factor X100", 1, 500, 100)
        sliders.Show()
        ocvb.desc = "Subtract the mean from the image with a scaling factor"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim mean = cv.Cv2.Mean(ocvb.color)
        cv.Cv2.Subtract(mean, ocvb.color, ocvb.result1)
        Dim scalingFactor = sliders.TrackBar1.Value / 100
        ocvb.result1 *= 1 / scalingFactor
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class
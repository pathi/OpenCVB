﻿Imports cv = OpenCvSharp
Public Class Disparity_Basics : Implements IDisposable
    Dim disp16 As Depth_Colorizer_CPP
    Public Sub New(ocvb As AlgorithmData)
        disp16 = New Depth_Colorizer_CPP(ocvb)
        disp16.externalUse = True

        ocvb.desc = "Show disparity from RealSense camera"
        If ocvb.parms.UsingIntelCamera Then
            ocvb.label1 = "Disparity Image (not depth)"
        Else
            ocvb.label1 = "Kinect Camera Disparity is also the Depth"
        End If
        ocvb.label2 = "Left Infrared Image"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim disparity16u As New cv.Mat
        ocvb.disparity.ConvertTo(disparity16u, cv.MatType.CV_16U)
        disp16.src = disparity16u
        disp16.Run(ocvb)
        ocvb.result1 = disp16.dst
        ocvb.result2 = ocvb.redLeft
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        disp16.Dispose()
    End Sub
End Class

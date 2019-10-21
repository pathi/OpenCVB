﻿Imports cv = OpenCvSharp
Public Class imShow_Basics : Implements IDisposable
    Public Sub New(ocvb As AlgorithmData)
        ocvb.desc = "This is just a reminder that all HighGUI methods are available in OpenCVB"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        cv.Cv2.ImShow("color", ocvb.color)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        cv.Cv2.DestroyAllWindows()
    End Sub
End Class

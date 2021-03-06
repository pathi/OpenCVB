﻿Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Module Depth_Colorizer_CPP_Module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Depth_Colorizer_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Depth_Colorizer_Close(Depth_ColorizerPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Depth_Colorizer_Run(Depth_ColorizerPtr As IntPtr, rgbPtr As IntPtr, rows As Int32, cols As Int32) As IntPtr
    End Function
End Module


Public Class Depth_Colorizer_CPP : Implements IDisposable
    Public dst As New cv.Mat
    Public src As New cv.Mat
    Public externalUse As Boolean
    Dim Depth_Colorizer As IntPtr
    Public Sub New(ocvb As AlgorithmData)
        Depth_Colorizer = Depth_Colorizer_Open()
        ocvb.desc = "Display 16 bit image using C++ instead of VB.Net"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If externalUse = False Then src = ocvb.depth Else dst = New cv.Mat(src.Size(), cv.MatType.CV_8UC3)

        Dim depthData(src.Total * src.ElemSize - 1) As Byte
        Dim handleSrc = GCHandle.Alloc(depthData, GCHandleType.Pinned)
        Marshal.Copy(src.Data, depthData, 0, depthData.Length)
        Dim imagePtr = Depth_Colorizer_Run(Depth_Colorizer, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols)
        handleSrc.Free()

        If imagePtr <> 0 Then
            If dst.Rows = 0 Then dst = New cv.Mat(src.Size(), cv.MatType.CV_8UC3)
            Dim dstData(dst.Total * dst.ElemSize - 1) As Byte
            Marshal.Copy(imagePtr, dstData, 0, dstData.Length)
            If externalUse = False Then
                ocvb.result1 = New cv.Mat(ocvb.result1.Rows, ocvb.result1.Cols, cv.MatType.CV_8UC3, dstData)
            Else
                dst = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8UC3, dstData)
            End If
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        Depth_Colorizer_Close(Depth_Colorizer)
    End Sub
End Class


' this algorithm is only intended to show how the depth can be colorized.  It is very slow.  Use the C++ version of this code nearby.
Public Class Depth_Colorizer : Implements IDisposable
    Public Sub New(ocvb As AlgorithmData)
        ocvb.desc = "Colorize depth manually."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim src = ocvb.depth
        Dim nearColor = New Byte() {0, 255, 255}
        Dim farColor = New Byte() {255, 0, 0}

        Dim histogram(256 * 256 - 1) As Int32
        For y = 0 To src.Rows - 1
            For x = 0 To src.Cols - 1
                Dim pixel = src.Get(Of UInt16)(y, x)
                If pixel Then histogram(pixel) += 1
            Next
        Next
        For i = 1 To histogram.Length - 1
            histogram(i) += histogram(i - 1) + 1
        Next
        For i = 1 To histogram.Length - 1
            histogram(i) = (histogram(i) << 8) / histogram(256 * 256 - 1)
        Next

        Dim stride = src.Width * 3
        Dim rgbdata(stride * src.Height) As Byte
        For y = 0 To src.Rows - 1
            For x = 0 To src.Cols - 1
                Dim pixel = src.Get(Of UInt16)(y, x)
                If pixel Then
                    Dim t = histogram(pixel)
                    rgbdata(x * 3 + 0 + y * stride) = ((256 - t) * nearColor(0) + t * farColor(0)) >> 8
                    rgbdata(x * 3 + 1 + y * stride) = ((256 - t) * nearColor(1) + t * farColor(1)) >> 8
                    rgbdata(x * 3 + 2 + y * stride) = ((256 - t) * nearColor(2) + t * farColor(2)) >> 8
                End If
            Next
        Next
        ocvb.result1 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8UC3, rgbdata)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class





Public Class Depth_ManualTrim : Implements IDisposable
    Public Mask As New cv.Mat
    Public sliders As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "Min Depth", 200, 1000, 200)
        sliders.setupTrackBar2(ocvb, "Max Depth", 200, 10000, 1400)
        If ocvb.parms.ShowOptions Then sliders.show()
        ocvb.desc = "Manually show depth with varying min and max depths."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim minDepth = sliders.TrackBar1.Value
        Dim maxDepth = sliders.TrackBar2.Value
        Mask = ocvb.depth.Threshold(maxDepth, 255, cv.ThresholdTypes.BinaryInv)
        Mask.ConvertTo(Mask, cv.MatType.CV_8U)

        Dim maskMin As New cv.Mat
        maskMin = ocvb.depth.Threshold(minDepth, 255, cv.ThresholdTypes.Binary)
        maskMin.ConvertTo(maskMin, cv.MatType.CV_8U)
        cv.Cv2.BitwiseAnd(Mask, maskMin, Mask)

        ocvb.result1.SetTo(0)
        ocvb.depthRGB.CopyTo(ocvb.result1, Mask)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class


Public Class Depth_Projections : Implements IDisposable
    Dim foreground As Depth_ManualTrim
    Dim grid As Thread_Grid
    Public Sub New(ocvb As AlgorithmData)
        grid = New Thread_Grid(ocvb)
        grid.sliders.TrackBar1.Value = 64
        grid.sliders.TrackBar2.Value = 32


        foreground = New Depth_ManualTrim(ocvb)
        foreground.sliders.TrackBar1.Value = 300  ' fixed distance to keep the images stable.
        foreground.sliders.TrackBar2.Value = 1200 ' fixed distance to keep the images stable.
        ocvb.label1 = "Top View"
        ocvb.label2 = "Side View"
        ocvb.desc = "Project the depth data onto a top view and side view."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        grid.Run(ocvb)
        foreground.Run(ocvb)

        ocvb.result1.SetTo(cv.Scalar.White)
        ocvb.result2.SetTo(cv.Scalar.White)

        Dim h = ocvb.result1.Height
        Dim w = ocvb.result1.Width
        Dim desiredMin = foreground.sliders.TrackBar1.Value
        Dim desiredMax = foreground.sliders.TrackBar2.Value
        Dim range = desiredMax - desiredMin

        Parallel.ForEach(Of cv.Rect)(grid.roiList,
         Sub(roi)
             For y = roi.Y To roi.Y + roi.Height - 1
                 For x = roi.X To roi.X + roi.Width - 1
                     Dim m = foreground.Mask.At(Of Byte)(y, x)
                     If m > 0 Then
                         Dim depth = ocvb.depth.Get(Of UShort)(y, x)
                         If depth > 0 Then
                             Dim dy = Math.Round(h * (depth - desiredMin) / range)
                             If dy < h And dy > 0 Then ocvb.result1.Set(Of cv.Vec3b)(h - dy, x, ocvb.color.At(Of cv.Vec3b)(y, x))
                             Dim dx = Math.Round(w * (depth - desiredMin) / range)
                             If dx < w And dx > 0 Then ocvb.result2.Set(Of cv.Vec3b)(y, dx, ocvb.color.At(Of cv.Vec3b)(y, x))
                         End If
                     End If
                 Next
             Next
         End Sub)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        foreground.Dispose()
        grid.Dispose()
    End Sub
End Class


Public Class Depth_WorldXYZ_MT : Implements IDisposable
    Dim grid As Thread_Grid
    Public xyzFrame As cv.Mat
    Public Sub New(ocvb As AlgorithmData)
        grid = New Thread_Grid(ocvb)
        grid.sliders.TrackBar1.Value = 32
        grid.sliders.TrackBar2.Value = 32
        xyzFrame = New cv.Mat(ocvb.color.Size(), cv.MatType.CV_32FC3)
        ocvb.desc = "Create 32-bit XYZ format from depth data."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        grid.Run(ocvb)

        xyzFrame.SetTo(0)
        Parallel.ForEach(Of cv.Rect)(grid.roiList,
        Sub(roi)
            Dim xy As New cv.Point3f
            For xy.Y = roi.Y To roi.Y + roi.Height - 1
                For xy.X = roi.X To roi.X + roi.Width - 1
                    xy.Z = ocvb.depth.At(Of UInt16)(xy.Y, xy.X)
                    If xy.Z <> 0 Then
                        Dim w = getWorldCoordinatesD(ocvb, xy)
                        xyzFrame.Set(Of cv.Point3f)(xy.Y, xy.X, w)
                    End If
                Next
            Next
        End Sub)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        grid.Dispose()
    End Sub
End Class


Public Class Depth_InRangeTrim : Implements IDisposable
    Public Mask As New cv.Mat
    Public zeroMask As New cv.Mat
    Public externalUse As Boolean
    Public sliders As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "InRange Min Depth", 200, 1000, 200)
        sliders.setupTrackBar2(ocvb, "InRange Max Depth", 200, 10000, 1400)
        If ocvb.parms.ShowOptions Then sliders.show()
        ocvb.desc = "Show depth with OpenCV using varying min and max depths."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If sliders.TrackBar1.Value >= sliders.TrackBar2.Value Then sliders.TrackBar2.Value = sliders.TrackBar1.Value + 1
        Dim minDepth = cv.Scalar.All(sliders.TrackBar1.Value)
        Dim maxDepth = cv.Scalar.All(sliders.TrackBar2.Value)
        Dim tmp16 As New cv.Mat
        cv.Cv2.InRange(ocvb.depth, minDepth, maxDepth, tmp16)
        cv.Cv2.ConvertScaleAbs(tmp16, Mask)
        cv.Cv2.BitwiseNot(Mask, zeroMask)

        If externalUse = False Then
            ocvb.result1.SetTo(0)
            ocvb.depthRGB.CopyTo(ocvb.result1, Mask)
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class



Public Class Depth_Median : Implements IDisposable
    Dim median As Math_Median_CDF
    Public Sub New(ocvb As AlgorithmData)
        median = New Math_Median_CDF(ocvb)
        median.src = New cv.Mat
        median.rangeMax = 10000
        median.rangeMin = 1 ' ignore depth of zero as it is not known.
        ocvb.desc = "Divide the depth image ahead and behind the median."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        ocvb.depth.ConvertTo(median.src, cv.MatType.CV_32F)
        median.Run(ocvb)

        Dim mask As cv.Mat
        mask = median.src.LessThan(median.medianVal)
        ocvb.result1.SetTo(0)
        ocvb.depthRGB.CopyTo(ocvb.result1, mask)
        ocvb.label1 = "Median Depth < " + Format(median.medianVal, "#0.0")

        cv.Cv2.BitwiseNot(mask, mask)
        ocvb.result2.SetTo(0)
        ocvb.depthRGB.CopyTo(ocvb.result2, mask)
        ocvb.label2 = "Median Depth > " + Format(median.medianVal, "#0.0")
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        median.Dispose()
    End Sub
End Class



Public Class Depth_LocalMinMax_MT : Implements IDisposable
    Dim grid As Thread_Grid
    Public Sub New(ocvb As AlgorithmData)
        grid = New Thread_Grid(ocvb)
        grid.externalUse = True
        ocvb.desc = "Find minimum depth in each segment."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        grid.Run(ocvb)
        ocvb.color.CopyTo(ocvb.result1)
        ocvb.result1.SetTo(cv.Scalar.White, grid.gridMask)

        Dim mask = ocvb.depth.Threshold(1, 5000, cv.ThresholdTypes.Binary)
        mask.ConvertTo(mask, cv.MatType.CV_8UC1)

        Dim ptList = New List(Of cv.Point)
        ptList.Capacity = grid.roiList.Count * 2

        Parallel.ForEach(Of cv.Rect)(grid.roiList,
        Sub(roi)
            Dim minVal As Double, maxVal As Double
            Dim minPt As cv.Point, maxPt As cv.Point
            cv.Cv2.MinMaxLoc(ocvb.depth(roi), minVal, maxVal, minPt, maxPt, mask(roi))

            ' if min and max are equal, this segment is all the same value (likely 0)
            If minPt <> maxPt Then
                cv.Cv2.Circle(ocvb.result1(roi), minPt, 5, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
                ptList.Add(New cv.Point(minPt.X + roi.X, minPt.Y + roi.Y))
                cv.Cv2.Circle(ocvb.result1(roi), maxPt, 3, cv.Scalar.Blue, -1, cv.LineTypes.AntiAlias)
                ptList.Add(New cv.Point(maxPt.X + roi.X, maxPt.Y + roi.Y))
            End If
        End Sub)

        Dim subdiv As New cv.Subdiv2D(New cv.Rect(0, 0, ocvb.color.Width, ocvb.color.Height))
        For i = 0 To ptList.Count - 1
            subdiv.Insert(ptList(i))
        Next
        paint_voronoi(ocvb, ocvb.result2, subdiv)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        grid.Dispose()
    End Sub
End Class



Public Class Depth_Flatland : Implements IDisposable
    Dim sliders As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "Region Count", 1, 250, 10)
        If ocvb.parms.ShowOptions Then sliders.show()

        ocvb.label2 = "Grayscale version"
        ocvb.desc = "Attempt to stabilize the depth image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim reductionFactor = sliders.TrackBar1.Maximum - sliders.TrackBar1.Value
        ocvb.result1 = ocvb.depthRGB / reductionFactor
        ocvb.result1 *= reductionFactor
        ocvb.result2 = ocvb.result1.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        ocvb.result2 = ocvb.result2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class



Public Class Depth_FirstLastDistance : Implements IDisposable
    Public Sub New(ocvb As AlgorithmData)
        ocvb.desc = "Monitor the first and last depth distances"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim mask = ocvb.depth.Threshold(1, 20000, cv.ThresholdTypes.Binary)
        mask.ConvertTo(mask, cv.MatType.CV_8UC1)
        Dim minVal As Double, maxVal As Double
        Dim minPt As cv.Point, maxPt As cv.Point
        cv.Cv2.MinMaxLoc(ocvb.depth, minVal, maxVal, minPt, maxPt, mask)
        ocvb.depthRGB.CopyTo(ocvb.result1)
        ocvb.depthRGB.CopyTo(ocvb.result2)
        ocvb.label1 = "Min Depth " + CStr(minVal) + " mm"
        ocvb.result1.Circle(minPt, 10, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
        ocvb.label2 = "Max Depth " + CStr(maxVal) + " mm"
        ocvb.result2.Circle(maxPt, 10, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class



Public Class Depth_Shadow : Implements IDisposable
    Public holeMask As New cv.Mat
    Public borderMask As New cv.Mat
    Public externalUse = False
    Dim element As New cv.Mat
    Public Sub New(ocvb As AlgorithmData)
        ocvb.label2 = "Shadow borders"
        element = cv.Cv2.GetStructuringElement(cv.MorphShapes.Rect, New cv.Size(5, 5))
        ocvb.desc = "Identify shadow in the depth image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim mask = ocvb.depth.Threshold(1, 20000, cv.ThresholdTypes.BinaryInv)
        holeMask = New cv.Mat
        mask.ConvertTo(holeMask, cv.MatType.CV_8UC1)
        If externalUse = False Then ocvb.result1 = holeMask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        borderMask = New cv.Mat
        borderMask = holeMask.Dilate(element, Nothing, 1)
        borderMask.SetTo(0, holeMask)
        If externalUse = False Then ocvb.result2 = borderMask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        holeMask.Dispose()
        borderMask.Dispose()
        element.Dispose()
    End Sub
End Class



Public Class Depth_ShadowRect : Implements IDisposable
    Dim sliders As New OptionsSliders
    Dim shadow As Depth_Shadow
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "shadowRect Min Size", 1, 20000, 2000)
        If ocvb.parms.ShowOptions Then sliders.show()

        shadow = New Depth_Shadow(ocvb)
        shadow.externalUse = True

        ocvb.desc = "Identify the minimum rectangles of contours of the depth shadow"
    End Sub

    Public Sub Run(ocvb As AlgorithmData)
        shadow.Run(ocvb)

        ocvb.result1.SetTo(0)
        Dim contours As cv.Point()()
        contours = cv.Cv2.FindContoursAsArray(shadow.borderMask, cv.RetrievalModes.Tree, cv.ContourApproximationModes.ApproxSimple)

        Dim minEllipse(contours.Length - 1) As cv.RotatedRect
        For i = 0 To contours.Length - 1
            Dim minRect = cv.Cv2.MinAreaRect(contours(i))
            If minRect.Size.Width * minRect.Size.Height > sliders.TrackBar1.Value Then
                Dim nextColor = New cv.Scalar(ocvb.rColors(i Mod 255).Item0, ocvb.rColors(i Mod 255).Item1, ocvb.rColors(i Mod 255).Item2)
                drawRotatedRectangle(minRect, ocvb.result1, nextColor)
                If contours(i).Length >= 5 Then
                    minEllipse(i) = cv.Cv2.FitEllipse(contours(i))
                End If
            End If
        Next
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        shadow.Dispose()
        sliders.Dispose()
    End Sub
End Class



Public Class Depth_Foreground : Implements IDisposable
    Public trim As Depth_InRangeTrim
    Public Sub New(ocvb As AlgorithmData)
        trim = New Depth_InRangeTrim(ocvb)
        ocvb.desc = "Demonstrate the use of mean shift algorithm.  Use depth to find the top of the head and then meanshift to the face."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        trim.Run(ocvb)
        ocvb.result1.CopyTo(ocvb.result2)
        Dim gray = ocvb.result1.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        gray = gray.Threshold(1, 255, cv.ThresholdTypes.Binary)

        ' find the largest blob and use that as the body.  Head is highest in the image.
        Dim blobSize As New List(Of Int32)
        Dim blobLocation As New List(Of cv.Point)
        For y = 0 To gray.Height - 1
            For x = 0 To gray.Width - 1
                Dim nextByte = gray.At(Of Byte)(y, x)
                If nextByte <> 0 Then
                    Dim count = gray.FloodFill(New cv.Point(x, y), 0)
                    If count > 10 Then
                        blobSize.Add(count)
                        blobLocation.Add(New cv.Point(x, y))
                    End If
                End If
            Next
        Next
        Dim maxBlob As Int32
        Dim maxIndex As Int32 = -1
        For i = 0 To blobSize.Count - 1
            If maxBlob < blobSize.Item(i) Then
                maxBlob = blobSize.Item(i)
                maxIndex = i
            End If
        Next

        If maxIndex >= 0 Then
            Dim rectSize = 150
            If ocvb.color.Width > 1000 Then rectSize = 250
            Dim xx = blobLocation.Item(maxIndex).X - rectSize / 2
            Dim yy = blobLocation.Item(maxIndex).Y - rectSize / 2
            If xx < 0 Then xx = 0
            If yy < 0 Then yy = 0
            If xx + rectSize > ocvb.color.Width Then xx = ocvb.color.Width - rectSize
            If yy + rectSize > ocvb.color.Height Then yy = ocvb.color.Height - rectSize
            ocvb.drawRect = New cv.Rect(xx, yy, rectSize, rectSize)
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        trim.Dispose()
    End Sub
End Class



Public Class Depth_ToInfrared : Implements IDisposable
    Dim red As InfraRed_Basics
    Dim sliders As New OptionsSliders
    Dim check As New OptionsCheckbox
    Public Sub New(ocvb As AlgorithmData)
        red = New InfraRed_Basics(ocvb)

        check.Setup(ocvb, 1)
        check.Box(0).Text = "Save Current Yellow Rectangle"
        If ocvb.parms.ShowOptions Then check.show()

        Dim top As Int32, left As Int32, bot As Int32, right As Int32
        If ocvb.parms.UsingIntelCamera Then
            top = GetSetting("OpenCVB", "DepthToInfraredTop", "DepthToInfraredTop", 1 / 8)
            left = GetSetting("OpenCVB", "DepthToInfraredLeft", "DepthToInfraredLeft", 1 / 8)
            bot = GetSetting("OpenCVB", "DepthToInfraredBot", "DepthToInfraredBot", 7 / 8)
            right = GetSetting("OpenCVB", "DepthToInfraredRight", "DepthToInfraredRight", 7 / 8)
        Else
            top = 0
            left = 0
            bot = 1
            right = 1
        End If
        sliders.setupTrackBar1(ocvb, "Color Image Top in RedLeft", 0, ocvb.color.Height / 2, top * ocvb.color.Height)
        sliders.setupTrackBar2(ocvb, "Color Image Left in RedLeft", 0, ocvb.color.Width / 2, left * ocvb.color.Width)
        sliders.setupTrackBar3(ocvb, "Color Image Bot in RedLeft", ocvb.color.Height / 2, ocvb.color.Height, bot * ocvb.color.Height)
        sliders.setupTrackBar4(ocvb, "Color Image Right in RedLeft", ocvb.color.Width / 2, ocvb.color.Width, right * ocvb.color.Width)
        If ocvb.parms.ShowOptions Then sliders.show()
        If ocvb.parms.UsingIntelCamera Then
            ocvb.label1 = "Color + Infrared Left (overlay)"
            ocvb.label2 = "Aligned RedLeft and color"
        Else
            ocvb.label1 = "Aligning infrared with color is not useful on Kinect"
            ocvb.label2 = ""
        End If
        ocvb.desc = "Map the depth image into the infrared images"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        red.Run(ocvb)
        ocvb.result2 = ocvb.result1
        Dim rHeight = sliders.TrackBar3.Value - sliders.TrackBar1.Value
        Dim rWidth = sliders.TrackBar4.Value - sliders.TrackBar2.Value
        Dim rect = New cv.Rect(sliders.TrackBar2.Value, sliders.TrackBar1.Value, rWidth, rHeight)
        ocvb.result2 = ocvb.result2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        ocvb.result2.Rectangle(rect, cv.Scalar.Yellow, 1)
        ocvb.result1 = ocvb.result2(rect).Resize(ocvb.result1.Size())
        ocvb.result1 = ocvb.color + ocvb.result1
        If check.Box(0).Checked Then
            check.Box(0).Checked = False
            SaveSetting("OpenCVB", "DepthToInfraredTop", "DepthToInfraredTop", sliders.TrackBar1.Value / ocvb.color.Height)
            SaveSetting("OpenCVB", "DepthToInfraredLeft", "DepthToInfraredLeft", sliders.TrackBar2.Value / ocvb.color.Width)
            SaveSetting("OpenCVB", "DepthToInfraredBot", "DepthToInfraredBot", sliders.TrackBar3.Value / ocvb.color.Height)
            SaveSetting("OpenCVB", "DepthToInfraredRight", "DepthToInfraredRight", sliders.TrackBar4.Value / ocvb.color.Width)
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        red.Dispose()
        sliders.Dispose()
        check.Dispose()
    End Sub
End Class



Public Class Depth_FlatData : Implements IDisposable
    Dim shadow As Depth_Shadow
    Dim sliders As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        shadow = New Depth_Shadow(ocvb)

        sliders.setupTrackBar1(ocvb, "FlatData Region Count", 1, 250, 200)
        If ocvb.parms.ShowOptions Then sliders.show()

        ocvb.label1 = "Reduced resolution DepthRGB"
        ocvb.label2 = "Contours of the Depth Shadow"
        ocvb.desc = "Attempt to stabilize the depth image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        shadow.Run(ocvb) ' get where depth is zero

        Dim mask As New cv.Mat
        Dim gray As New cv.Mat
        Dim gray8u As New cv.Mat

        cv.Cv2.BitwiseNot(shadow.holeMask, mask)
        gray = ocvb.depth.Normalize(0, 255, cv.NormTypes.MinMax, -1, mask)
        gray.ConvertTo(gray8u, cv.MatType.CV_8U)

        Dim reductionFactor = sliders.TrackBar1.Maximum - sliders.TrackBar1.Value
        gray8u = gray8u / reductionFactor
        gray8u *= reductionFactor

        ocvb.result1 = gray8u.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        shadow.Dispose()
    End Sub
End Class



Public Class Depth_FlatBackground : Implements IDisposable
    Dim shadow As Depth_Shadow
    Dim sliders As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        shadow = New Depth_Shadow(ocvb)
        sliders.setupTrackBar1(ocvb, "FlatBackground Max Depth", 200, 10000, 2000)
        If ocvb.parms.ShowOptions Then sliders.show()

        ocvb.desc = "Simplify the depth image with a flat background"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        shadow.Run(ocvb) ' get where depth is zero
        Dim mask As New cv.Mat
        Dim maxDepth = cv.Scalar.All(sliders.TrackBar1.Value)
        Dim tmp16 As New cv.Mat
        cv.Cv2.InRange(ocvb.depth, 0, maxDepth, tmp16)
        cv.Cv2.ConvertScaleAbs(tmp16, mask)

        Dim zeroMask As New cv.Mat
        cv.Cv2.BitwiseNot(mask, zeroMask)
        ocvb.depth.SetTo(0, zeroMask)

        ocvb.result1.SetTo(0)
        ocvb.depthRGB.CopyTo(ocvb.result1, mask)
        zeroMask.SetTo(255, shadow.holeMask)
        ocvb.color.CopyTo(ocvb.result1, zeroMask)
        ocvb.depth.SetTo(maxDepth, zeroMask) ' set the depth to the maxdepth for any background
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        shadow.Dispose()
    End Sub
End Class


' Use the C++ version of this algorithm - this is way too slow...
Public Class Depth_WorldXYZ : Implements IDisposable
    Public xyzFrame As cv.Mat
    Public Sub New(ocvb As AlgorithmData)
        xyzFrame = New cv.Mat(ocvb.color.Size(), cv.MatType.CV_32FC3)
        ocvb.desc = "Create 32-bit XYZ format from depth data."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim xy As New cv.Point3f
        For xy.Y = 0 To xyzFrame.Height - 1
            For xy.X = 0 To xyzFrame.Width - 1
                xy.Z = ocvb.depth.At(Of UInt16)(xy.Y, xy.X)
                If xy.Z <> 0 Then
                    Dim w = getWorldCoordinatesD(ocvb, xy)
                    xyzFrame.Set(Of cv.Point3f)(xy.Y, xy.X, w)
                End If
            Next
        Next
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class


Module DepthXYZ_CPP_Module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Depth_XYZ_OpenMP_Open(ppx As Single, ppy As Single, fx As Single, fy As Single) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Depth_XYZ_OpenMP_Close(DepthXYZPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Depth_XYZ_OpenMP_Run(DepthXYZPtr As IntPtr, rgbPtr As IntPtr, rows As Int32, cols As Int32) As IntPtr
    End Function
End Module


Public Class Depth_XYZ_OpenMP_CPP : Implements IDisposable
    Public pointCloud As cv.Mat
    Dim DepthXYZ As IntPtr
    Public Sub New(ocvb As AlgorithmData)
        ocvb.label1 = "xyzFrame is built"
        ocvb.desc = "Get the X, Y, Depth in the image coordinates (not the 3D image coordinates.)"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        ' can't do this in the constructor because intrinsics were not initialized yet (because zinput was not initialized until algorithm thread starts.
        If ocvb.frameCount = 0 Then DepthXYZ = Depth_XYZ_OpenMP_Open(ocvb.parms.intrinsics.ppx, ocvb.parms.intrinsics.ppy, ocvb.parms.intrinsics.fx, ocvb.parms.intrinsics.fy)

        Dim depthData(ocvb.depth.Total * ocvb.depth.ElemSize - 1) As Byte
        Dim handleSrc = GCHandle.Alloc(depthData, GCHandleType.Pinned) ' pin it for the duration...
        Marshal.Copy(ocvb.depth.Data, depthData, 0, depthData.Length)
        Dim imagePtr = Depth_XYZ_OpenMP_Run(DepthXYZ, handleSrc.AddrOfPinnedObject(), ocvb.depth.Rows, ocvb.depth.Cols)
        handleSrc.Free()

        If imagePtr <> 0 Then
            Dim dstData(ocvb.depth.Total * 3 * 4 - 1) As Byte
            Marshal.Copy(imagePtr, dstData, 0, dstData.Length)
            pointCloud = New cv.Mat(ocvb.depth.Rows, ocvb.depth.Cols, cv.MatType.CV_32FC3, dstData)
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        Depth_XYZ_OpenMP_Close(DepthXYZ)
    End Sub
End Class


Public Class Depth_MeanStdev_MT : Implements IDisposable
    Dim sliders As New OptionsSliders
    Dim grid As Thread_Grid
    Dim meanSeries As New cv.Mat
    Public Sub New(ocvb As AlgorithmData)
        grid = New Thread_Grid(ocvb)
        grid.sliders.TrackBar1.Value = 64
        grid.sliders.TrackBar2.Value = 64

        sliders.setupTrackBar1(ocvb, "MeanStdev Max Depth Range", 1, 20000, 3500)
        sliders.setupTrackBar2(ocvb, "MeanStdev Frame Series", 1, 100, 5)
        If ocvb.parms.ShowOptions Then sliders.show()
        ocvb.desc = "Collect a time series of depth and measure where the stdev is unstable.  Plan is to avoid depth where unstable."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        grid.Run(ocvb)
        ocvb.result1 = New cv.Mat(ocvb.color.Rows, ocvb.color.Cols, cv.MatType.CV_8U)
        ocvb.result2 = New cv.Mat(ocvb.color.Rows, ocvb.color.Cols, cv.MatType.CV_8U)

        Dim maxDepth = sliders.TrackBar1.Value
        Dim meanCount = sliders.TrackBar2.Value

        Static lastMeanCount As Int32
        If grid.roiList.Count <> meanSeries.Rows Or meanCount <> lastMeanCount Then
            meanSeries = New cv.Mat(grid.roiList.Count - 1, meanCount, cv.MatType.CV_32F, 0)
            lastMeanCount = meanCount
        End If

        Dim mask As New cv.Mat, tmp16 As New cv.Mat
        cv.Cv2.InRange(ocvb.depth, 1, maxDepth, tmp16)
        cv.Cv2.ConvertScaleAbs(tmp16, mask)
        Dim outOfRangeMask As New cv.Mat
        cv.Cv2.BitwiseNot(mask, outOfRangeMask)

        Dim minVal As Double, maxVal As Double
        Dim minPt As cv.Point, maxPt As cv.Point
        cv.Cv2.MinMaxLoc(ocvb.depth, minVal, maxVal, minPt, maxPt, mask)

        Dim meanIndex = ocvb.frameCount Mod meanCount
        Dim meanValues As New cv.Mat(grid.roiList.Count - 1, 1, cv.MatType.CV_32F)
        Dim stdValues As New cv.Mat(grid.roiList.Count - 1, 1, cv.MatType.CV_32F)
        Parallel.For(0, grid.roiList.Count - 1,
        Sub(i)
            Dim roi = grid.roiList(i)
            Dim mean As Single = 0, stdev As Single = 0
            cv.Cv2.MeanStdDev(ocvb.depth(roi), mean, stdev, mask(roi))
            meanSeries.Set(Of Single)(i, meanIndex, mean)
            If ocvb.frameCount >= meanCount - 1 Then
                cv.Cv2.MeanStdDev(meanSeries.Row(i), mean, stdev)
                meanValues.Set(Of Single)(i, 0, mean)
                stdValues.Set(Of Single)(i, 0, stdev)
            End If
        End Sub)

        If ocvb.frameCount >= meanCount Then
            Dim minStdVal As Double, maxStdVal As Double
            Dim meanMask As New cv.Mat, stdMask As New cv.Mat

            cv.Cv2.Threshold(meanValues, meanMask, 1, maxDepth, cv.ThresholdTypes.Binary)
            meanMask.ConvertTo(meanMask, cv.MatType.CV_8U)
            cv.Cv2.MinMaxLoc(meanValues, minVal, maxVal, minPt, maxPt, meanMask)
            cv.Cv2.Threshold(stdValues, stdMask, 0.001, maxDepth, cv.ThresholdTypes.Binary) ' volatile region is x cm stdev.
            stdMask.ConvertTo(stdMask, cv.MatType.CV_8U)
            cv.Cv2.MinMaxLoc(stdValues, minStdVal, maxStdVal, minPt, maxPt, stdMask)

            Parallel.For(0, grid.roiList.Count - 1,
            Sub(i)
                Dim roi = grid.roiList(i)
                ' this marks all the regions where the depth is volatile.
                ocvb.result2(roi).SetTo(255 * (stdValues.At(Of Single)(i, 0) - minStdVal) / (maxStdVal - minStdVal))
                ocvb.result2(roi).SetTo(0, outOfRangeMask(roi))

                ocvb.result1(roi).SetTo(255 * (meanValues.At(Of Single)(i, 0) - minVal) / (maxVal - minVal))
                ocvb.result1(roi).SetTo(0, outOfRangeMask(roi))
            End Sub)
            cv.Cv2.BitwiseOr(ocvb.result2, grid.gridMask, ocvb.result2)
            ocvb.label2 = "ROI Stdev: Min " + Format(minStdVal, "#0.0") + " Max " + Format(maxStdVal, "#0.0")
        End If

        ocvb.label1 = "ROI Means: Min " + Format(minVal, "#0.0") + " Max " + Format(maxVal, "#0.0")
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        grid.Dispose()
    End Sub
End Class


Public Class Depth_MeanStdevPlot : Implements IDisposable
    Dim shadow As Depth_Shadow
    Dim plot1 As Plot_OverTime
    Dim plot2 As Plot_OverTime
    Public Sub New(ocvb As AlgorithmData)
        shadow = New Depth_Shadow(ocvb)
        shadow.externalUse = True

        plot1 = New Plot_OverTime(ocvb)
        plot1.externalUse = True
        plot1.dst = ocvb.result1
        plot1.maxVal = 2000
        plot1.plotCount = 1

        plot2 = New Plot_OverTime(ocvb)
        plot2.externalUse = True
        plot2.dst = ocvb.result2
        plot2.maxVal = 1000
        plot2.plotCount = 1

        ocvb.desc = "Plot the mean and stdev of the depth image"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        shadow.Run(ocvb)

        Dim mean As Single = 0, stdev As Single = 0
        Dim mask As New cv.Mat
        cv.Cv2.BitwiseNot(shadow.holeMask, mask)
        cv.Cv2.MeanStdDev(ocvb.depth, mean, stdev, mask)

        If mean > plot1.maxVal Then plot1.maxVal = mean + 1000 - (mean + 1000) Mod 1000
        If stdev > plot2.maxVal Then plot2.maxVal = stdev + 1000 - (stdev + 1000) Mod 1000

        plot1.plotData = New cv.Scalar(mean, 0, 0)
        plot1.Run(ocvb)
        plot2.plotData = New cv.Scalar(stdev, 0, 0)
        plot2.Run(ocvb)
        ocvb.label1 = "Plot of mean depth = " + Format(mean, "#0.0")
        ocvb.label2 = "Plot of depth stdev = " + Format(stdev, "#0.0")
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        plot1.Dispose()
        plot2.Dispose()
    End Sub
End Class




Public Class Depth_Uncertainty : Implements IDisposable
    Dim retina As Retina_Basics_CPP
    Dim sliders As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        retina = New Retina_Basics_CPP(ocvb)
        retina.externalUse = True

        sliders.setupTrackBar1(ocvb, "Uncertainty threshold", 1, 255, 100)
        If ocvb.parms.ShowOptions Then sliders.show()

        ocvb.desc = "Use the bio-inspired retina algorithm to determine depth uncertainty."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        retina.src = ocvb.depthRGB
        retina.Run(ocvb)
        ocvb.result2 = ocvb.result2.Threshold(sliders.TrackBar1.Value, 255, cv.ThresholdTypes.Binary)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        retina.Dispose()
        sliders.Dispose()
    End Sub
End Class




Public Class Depth_ColorMap : Implements IDisposable
    Dim sliders As New OptionsSliders
    Dim Palette As Palette_ColorMap
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "Depth ColorMap Alpha X100", 1, 100, 3)
        If ocvb.parms.ShowOptions Then sliders.show()

        Palette = New Palette_ColorMap(ocvb)
        Palette.externalUse = True
        ocvb.desc = "Display the depth as a color map"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim alpha = sliders.TrackBar1.Value / 100
        cv.Cv2.ConvertScaleAbs(ocvb.depth, Palette.src, alpha)
        Palette.Run(ocvb)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        Palette.Dispose()
    End Sub
End Class




Public Class Depth_Stable : Implements IDisposable
    Public sliders As New OptionsSliders
    Dim lastDepth() As cv.Mat
    Public stableDepth As cv.Mat
    Public stableZeroDepth As cv.Mat
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "Diff - Number of Frames", 1, 20, 10)
        sliders.setupTrackBar2(ocvb, "Diff - Depth Range in Millimeters", 1, 200, 100)
        If ocvb.parms.ShowOptions Then sliders.Show()
        ocvb.desc = "Collect X frames, compute stable depth and color pixels using thresholds"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static frameIndex As Int32
        Static countDown As Int32
        Static lastFrameCount As Int32
        If lastFrameCount <> sliders.TrackBar1.Value Then
            stableDepth = Nothing
            lastFrameCount = sliders.TrackBar1.Value
            countDown = lastFrameCount
            frameIndex = 0
            ReDim lastDepth(lastFrameCount - 1)
            For i = 0 To lastDepth.Count - 1
                lastDepth(i) = New cv.Mat
            Next
        End If

        If countDown = 0 Then
            stableDepth = New cv.Mat(ocvb.depth.Size(), cv.MatType.CV_16U, 0)
            Dim minMat = stableDepth.Clone().SetTo(20000)
            For i = 0 To lastFrameCount - 1
                cv.Cv2.Max(lastDepth(i), stableDepth, stableDepth)
                cv.Cv2.Min(lastDepth(i), minMat, minMat)
            Next
            Dim rangeDepth As New cv.Mat
            cv.Cv2.Subtract(stableDepth, minMat, rangeDepth)
            Dim stableDepthMask = rangeDepth.Threshold(sliders.TrackBar2.Value, 255, cv.ThresholdTypes.BinaryInv)
            stableDepthMask.ConvertTo(ocvb.result1, cv.MatType.CV_8U)
            stableZeroDepth = minMat.Threshold(1, 255, cv.ThresholdTypes.BinaryInv)
            stableZeroDepth.ConvertTo(stableZeroDepth, cv.MatType.CV_8U)
            ocvb.result2 = ocvb.result1.Clone()
            ocvb.result2.SetTo(0, stableZeroDepth)
        Else
            countDown -= 1
        End If
        lastDepth(frameIndex) = ocvb.depth
        frameIndex += 1
        If frameIndex >= lastDepth.Count Then frameIndex = 0
        ocvb.label1 = "Stable depth over " + CStr(lastFrameCount) + " frames"
        ocvb.label2 = "Stable non-zero depth over " + CStr(lastFrameCount) + " frames"
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class




Public Class Depth_Average : Implements IDisposable
    Dim depth As Depth_Stable
    Public Sub New(ocvb As AlgorithmData)
        depth = New Depth_Stable(ocvb)
        depth.sliders.TrackBar1.Value = 1 ' just compare to previous frame
        ocvb.desc = "Smooth the depth values by averaging"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)

    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class
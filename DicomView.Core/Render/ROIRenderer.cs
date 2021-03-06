﻿using RT.Core.Planning;
using RT.Core.ROIs;
using DicomPanel.Core.Render.Contouring;
using DicomPanel.Core.Render.Overlays;
using RT.Core.Utilities.RTMath;
using System;
using System.Collections.Generic;
using System.Text;
using RT.Core.DICOM;

namespace DicomPanel.Core.Render
{
    public class ROIRenderer
    {
        private Point2d screenCoords1 = new Point2d();
        private Point2d screenCoords2 = new Point2d();
        private Point3d worldCoord = new Point3d();
        DicomColor clearColor = DicomColor.FromArgb(0, 0, 0, 0);

        // Initiate the marching squares object outside of the loop
        MarchingSquares ms = new MarchingSquares();

        public void Render(IEnumerable<RegionOfInterest> rois, Camera camera, IRenderContext context, Rectd screenRect)
        {
            foreach (RegionOfInterest roi in rois)
            {
                var boundingRect = camera.GetBoundingScreenRect(roi.XRange, roi.YRange, roi.ZRange, screenRect);

                if (boundingRect != null)
                {
                    var initPosn = camera.ConvertScreenToWorldCoords(boundingRect.Y, boundingRect.X);
                    double gridSpacing = 2;
                    var rows = (int)Math.Round((boundingRect.Height * (camera.GetFOV().Y) / gridSpacing)) + 1;
                    var cols = (int)Math.Round((boundingRect.Width * (camera.GetFOV().X) / gridSpacing)) + 1;
                    var grid = new bool[rows * cols];
                    var cnorm = camera.ColDir.Length();
                    var rnorm = camera.RowDir.Length();
                    var cx = (cnorm * camera.ColDir.X) * gridSpacing / camera.Scale;
                    var cy = (cnorm * camera.ColDir.Y) * gridSpacing / camera.Scale;
                    var cz = (cnorm * camera.ColDir.Z) * gridSpacing / camera.Scale;
                    var rx = (rnorm * camera.RowDir.X) * gridSpacing / camera.Scale;
                    var ry = (rnorm * camera.RowDir.Y) * gridSpacing / camera.Scale;
                    var rz = (rnorm * camera.RowDir.Z) * gridSpacing / camera.Scale;

                    for (int i = 0; i < cols; i++)
                    {
                        for (int j = 0; j < rows; j++)
                        {
                            // The additional cx, rx, cy, ry etc. are because the marching squares
                            // offsets the grid during calculation... trust me future self.
                            worldCoord.X = initPosn.X + cx * i + rx * j + cx + rx;
                            worldCoord.Y = initPosn.Y + cy * i + ry * j + cy + ry;
                            worldCoord.Z = initPosn.Z + cz * i + rz * j + cz + rz;
                            grid[j * cols + i] = roi.ContainsPointNonInterpolated(worldCoord.X, worldCoord.Y, worldCoord.Z);
                        }
                    }

                    double[] vertices = ms.GetVertices(grid, rows, cols, initPosn.X, initPosn.Y, initPosn.Z, rx, ry, rz, cx, cy, cz);
                    double[] screenVertices = getScreenVertices(vertices, camera);
                    context.DrawLines(screenVertices, roi.Color);

                }
            }
        }

        private double[] getScreenVertices(double[] vertices, Camera camera)
        {
            double[] screenVertices = new double[2 * vertices.Length / 3];

            for (int i = 0, j = 0; i < vertices.Length; i += 6, j += 4)
            {
                camera.ConvertWorldToScreenCoords(vertices[i + 0], vertices[i + 1], vertices[i + 2], screenCoords1);
                camera.ConvertWorldToScreenCoords(vertices[i + 3], vertices[i + 4], vertices[i + 5], screenCoords2);
                screenVertices[j] = screenCoords1.X;
                screenVertices[j + 1] = screenCoords1.Y;
                screenVertices[j + 2] = screenCoords2.X;
                screenVertices[j + 3] = screenCoords2.Y;
            }
            return screenVertices;
        }


        public void Render(IEnumerable<PointOfInterest> pois, Camera camera, IRenderContext context, Rectd screenRect)
        {
            POIOverlay poiOverlay = new POIOverlay();
            poiOverlay.RenderCircle = true;
            poiOverlay.SizeInMM = 6;
            poiOverlay.KeepSameSizeOnScreen = false;
            foreach (PointOfInterest poi in pois)
            {
                poiOverlay.Position = poi.Position;
                poiOverlay.Render(camera, context);
            }
        }
    }
}

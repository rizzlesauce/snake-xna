using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace snake {
    /// <summary>
    /// A line segment class.
    /// </summary>
    class LineSegment2 {
        public Vector2 point1;
        public Vector2 point2;

        /// <summary>
        /// Types of segments.
        /// </summary>
        public enum SegmentType { Vertical, Horizontal, Point, Tilted };

        /// <summary>
        /// Types of segment pairs.
        /// </summary>
        private enum SegmentRelationship {
            Verticals, Horizontals, Points, VerticalHorizontal,
            VerticalTilted, HorizontalTilted, VerticalPoint, HorizontalPoint, TiltedPoint, Tilted
        };

        public LineSegment2(Vector2 p1, Vector2 p2) {
            point1 = p1;
            point2 = p2;
        }

        public float LowestX() {
            return PointWithLowestX().X;
        }

        public float LowestY() {
            return PointWithLowestY().Y;
        }

        public float GreatestX() {
            return PointWithGreatestX().X;
        }

        public float GreatestY() {
            return PointWithGreatestY().Y;
        }

        public Vector2 PointWithGreatestX() {
            if (point1.X > point2.X) {
                return point1;
            } else {
                return point2;
            }
        }

        public Vector2 PointWithLowestX() {
            if (point2.X < point1.X) {
                return point2;
            } else {
                return point1;
            }
        }

        public Vector2 PointWithGreatestY() {
            if (point2.Y > point1.Y) {
                return point2;
            } else {
                return point1;
            }
        }

        public Vector2 PointWithLowestY() {
            if (point1.Y < point2.Y) {
                return point1;
            }
            else {
                return point2;
            }
        }

        public SegmentType Type() {
            SegmentType type;
            float changeInX = point2.X - point1.X;
            float changeInY = point2.Y - point1.Y;

            if (changeInX == 0 && changeInY == 0) {
                type = SegmentType.Point;
            } else if (changeInX == 0) {
                type = SegmentType.Vertical;
            } else if (changeInY == 0) {
                type = SegmentType.Horizontal;
            } else {
                type = SegmentType.Tilted;
            }
            return type;
        }

        /// <summary>
        /// Determine whether two line segments intersect.
        /// Segments may be points, but must not have infinite length.
        /// </summary>
        /// <param name="segment1"></param>
        /// <param name="segment2"></param>
        /// <returns></returns>
        public static bool SegmentsIntersect(LineSegment2 segment1, LineSegment2 segment2) {
            bool doIntersect = false;

            LineSegment2[] segments = { segment1, segment2 };

            float[] changeInX = new float[2];
            float[] changeInY = new float[2];
            SegmentRelationship segmentRelationship;

            List<int> horizontalSegmentIndices = new List<int>();
            List<int> verticalSegmentIndices = new List<int>();
            List<int> pointSegmentIndices = new List<int>();
            List<int> tiltedSegmentIndices = new List<int>();

            Vector2 intersectPoint;

            for (int i = 0; i < 2; ++i) {
                changeInX[i] = segments[i].point2.X - segments[i].point1.X;
                changeInY[i] = segments[i].point2.Y - segments[i].point1.Y;

                if (changeInX[i] == 0 && changeInY[i] == 0) {
                    pointSegmentIndices.Add(i);
                } else if (changeInX[i] == 0) {
                    verticalSegmentIndices.Add(i);
                } else if (changeInY[i] == 0) {
                    horizontalSegmentIndices.Add(i);
                } else {
                    tiltedSegmentIndices.Add(i);
                }
            }

            if (pointSegmentIndices.Count >= 1) {
                if (pointSegmentIndices.Count == 2) {
                    segmentRelationship = SegmentRelationship.Points;
                } else if (verticalSegmentIndices.Count == 1) {
                    segmentRelationship = SegmentRelationship.VerticalPoint;
                } else if (horizontalSegmentIndices.Count == 1) {
                    segmentRelationship = SegmentRelationship.HorizontalPoint;
                } else {
                    segmentRelationship = SegmentRelationship.TiltedPoint;
                }
            } else if (verticalSegmentIndices.Count >= 1) {
                if (verticalSegmentIndices.Count == 2) {
                    segmentRelationship = SegmentRelationship.Verticals;
                } else if (horizontalSegmentIndices.Count == 1) {
                    segmentRelationship = SegmentRelationship.VerticalHorizontal;
                } else {
                    segmentRelationship = SegmentRelationship.VerticalTilted;
                }
            } else if (horizontalSegmentIndices.Count >= 1) {
                if (horizontalSegmentIndices.Count == 2) {
                    segmentRelationship = SegmentRelationship.Horizontals;
                } else {
                    segmentRelationship = SegmentRelationship.HorizontalTilted;
                }
            } else {
                segmentRelationship = SegmentRelationship.Tilted;
            }


            if (segmentRelationship == SegmentRelationship.Points) {
                // check that the points are identical
                if (segment1.point1.X == segment2.point1.X &&
                        segment1.point1.Y == segment2.point1.Y) {
                    doIntersect = true;
                    intersectPoint = segment1.point1;
                }
            } else if (segmentRelationship == SegmentRelationship.Verticals || 
                    segmentRelationship == SegmentRelationship.Horizontals) {
                // no intersection
            } else if (segmentRelationship == SegmentRelationship.VerticalHorizontal) {
                LineSegment2 vertical = segments[verticalSegmentIndices[0]];
                LineSegment2 horizontal = segments[horizontalSegmentIndices[0]];

                if (vertical.point1.X >= horizontal.LowestX() && vertical.point1.X <= horizontal.GreatestX() &&
                        horizontal.point1.Y >= vertical.LowestY() && horizontal.point1.Y <= vertical.GreatestY()) {
                    doIntersect = true;
                    intersectPoint = new Vector2(vertical.point1.X, horizontal.point1.Y);
                }
            } else if (segmentRelationship == SegmentRelationship.VerticalPoint) {
                // check if point is in line segment
                LineSegment2 vertical = segments[verticalSegmentIndices[0]];
                LineSegment2 point = segments[pointSegmentIndices[0]];

                if (point.point1.X == vertical.point1.X &&
                        point.point1.Y >= vertical.LowestY() && point.point1.Y <= vertical.GreatestY()) {
                    doIntersect = true;
                    intersectPoint = new Vector2(vertical.point1.X, point.point1.Y);
                }
            } else if (segmentRelationship == SegmentRelationship.HorizontalPoint) {
                // check if point is in line segment
                LineSegment2 horizontal = segments[horizontalSegmentIndices[0]];
                LineSegment2 point = segments[pointSegmentIndices[0]];

                if (point.point1.Y == horizontal.point1.Y &&
                        point.point1.X >= horizontal.LowestX() && point.point1.X <= horizontal.GreatestX()) {
                    doIntersect = true;
                    intersectPoint = new Vector2(point.point1.X, horizontal.point1.Y);
                }
            } else if (segmentRelationship == SegmentRelationship.VerticalTilted) {
                LineSegment2 vertical = segments[verticalSegmentIndices[0]];
                LineSegment2 tilted = segments[tiltedSegmentIndices[0]];

                // Check if they intersect horizontally
                if (vertical.point1.X >= tilted.LowestX() && vertical.point1.X <= tilted.GreatestX()) {
                    // get y value at x = vertical.point1.X
                    float y = YAtXEqualsNInLine(tilted, vertical.point1.X);

                    // Check if they intersect vertically
                    if (y >= vertical.LowestY() && y <= vertical.GreatestY()) {
                        doIntersect = true;
                        intersectPoint = new Vector2(vertical.point1.X, y);
                    }
                }
            } else if (segmentRelationship == SegmentRelationship.HorizontalTilted) {
                LineSegment2 horizontal = segments[horizontalSegmentIndices[0]];
                LineSegment2 tilted = segments[tiltedSegmentIndices[0]];

                // Check if they intersect vertically
                if (horizontal.point1.Y >= tilted.LowestY() && horizontal.point1.Y <= tilted.GreatestY()) {
                    // get x value at y = horizontal.point1.Y
                    float x = XAtYEqualsNInLine(tilted, horizontal.point1.Y);

                    // Check if they intersect horizontally
                    if (x >= horizontal.LowestX() && x <= horizontal.GreatestX()) {
                        doIntersect = true;
                        intersectPoint = new Vector2(x, horizontal.point1.Y);
                    }
                }
            } else if (segmentRelationship == SegmentRelationship.TiltedPoint) {
                LineSegment2 point = segments[pointSegmentIndices[0]];
                LineSegment2 tilted = segments[tiltedSegmentIndices[0]];

                // get y value at x = point.point1.X
                float y = YAtXEqualsNInLine(tilted, point.point1.X);

                if (y == point.point1.Y) {
                    doIntersect = true;
                    intersectPoint = point.point1;
                }
            } else if (segmentRelationship == SegmentRelationship.Tilted) {
                LineSegment2 line1 = segments[tiltedSegmentIndices[0]];
                LineSegment2 line2 = segments[tiltedSegmentIndices[1]];

                // get y value where both lines have the same x
                float y = Line1YWhereXTheSameInBothLines(line1, line2);

                if (y >= line1.LowestY() && y <= line1.GreatestY()) {
                    doIntersect = true;
                    intersectPoint = new Vector2(XAtYEqualsNInLine(line1, y), y);
                }
            }

            return doIntersect;
        }

        /// <summary>
        /// Get the y value of a line where x = n.
        /// The line must not be perfectly horizontal or vertical or have zero length.
        /// </summary>
        /// <param name="line">Two points that define the line</param>
        /// <param name="n">The value of x</param>
        /// <returns></returns>
        public static float YAtXEqualsNInLine(LineSegment2 line, float n) {
            return ((line.point2.Y - line.point1.Y) / (line.point2.X - line.point1.X)) *
                    (n - line.point1.X) + line.point1.Y;
        }

        /// <summary>
        /// Get the x value of a line where y = n.
        /// The line must not be perfectly horizontal or vertical or have zero length.
        /// </summary>
        /// <param name="line">Two points that define the line</param>
        /// <param name="n">The value of y</param>
        /// <returns></returns>
        public static float XAtYEqualsNInLine(LineSegment2 line, float n) {
            return ((line.point2.X - line.point1.X) / (line.point2.Y - line.point1.Y)) *
                    (n - line.point1.Y) + line.point1.X;
        }

        /// <summary>
        /// Get the y value of the first line of two lines where they intersect.
        /// The lines must not be perfectly horizontal, vertical, or have zero length.
        /// </summary>
        /// <param name="line1">Two points that define the first line</param>
        /// <param name="line2">Two points that define the second line</param>
        /// <returns>The y value of line1 where x in line1 = x in line2</returns>
        public static float Line1YWhereXTheSameInBothLines(LineSegment2 line1, LineSegment2 line2) {
            float j = (line1.point2.Y - line1.point2.Y) / (line1.point2.X - line1.point1.X);
            float k = (line2.point2.X - line2.point1.X) / (line2.point2.Y - line2.point1.Y);
            float l = line2.point1.X;
            float q = line2.point1.Y;

            return (j * (q - (k * l))) / (1 - (j * k));
        }

    }
}

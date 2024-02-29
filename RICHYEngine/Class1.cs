using UnityEngine;

namespace RICHYEngine
{

    public class Class1
    {

        public void calVerteVector2()
        {
            GetVertexFrom3Points(10f, new Vector2(1, 1), new Vector2(29, 21), new Vector2(55, 14));
            GetVertexFrom3Points(10f, new Vector2(1, 1), new Vector2(29, 21), new Vector2(35.16f, 25.4f));
            GetVertexFrom3Points(10f, new Vector2(1, 1), new Vector2(29, 21), new Vector2(29, 53));
            GetVertexFrom2Points(10f, new Vector2(1, 1), new Vector2(29, 21));
        }

        public void MakeLineFromPoint(float thickness, params Vector2[] point)
        {
            if (point.Length < 2)
            {
                return;
            }

            if (point.Length == 2)
            {
                GetVertexFrom2Points(thickness, point[0], point[1]);
                GetVertexFrom2Points(thickness, point[1], point[0]);
            }
            else
            {
                for (int i = 0; i < point.Length; i++)
                {
                    if (i == 0)
                    {
                        GetVertexFrom2Points(thickness, point[i], point[i + 1]);
                    }
                    else if (i == point.Length - 1)
                    {
                        GetVertexFrom2Points(thickness, point[i], point[i - 1]);
                    }
                    else
                    {
                        GetVertexFrom3Points(thickness, point[i], point[i - 1], point[i + 1]);
                    }

                    if (i > 0)
                    {
                        ConnectVertex((i - 1) * 2, (i - 1) * 2 + 1, i * 2, i * 2 + 1);
                    }
                }
            }
        }

        public void GetVertexFrom3Points(float thickness, Vector2 targetPoint, Vector2 connectedPoint1, Vector2 connectedPoint2)
        {
            Vector2 BA = connectedPoint2 - targetPoint;
            Vector2 BC = connectedPoint1 - targetPoint;
            float angle = Mathf.Acos(Vector2.Dot(BA.normalized, BC.normalized)) * Mathf.Rad2Deg;
            float alphaRad = (180 - angle) * Mathf.Deg2Rad;

            Vector2 perpendicularBA1 = new Vector2(-BA.y, BA.x).normalized;
            Vector2 perpendicularBA2 = new Vector2(BA.y, -BA.x).normalized;
            var perpendicularBA = perpendicularBA1;
            float tempAngle1 = Mathf.Acos(Vector2.Dot(perpendicularBA1, BC.normalized)) * Mathf.Rad2Deg;
            float tempAngle2 = Mathf.Acos(Vector2.Dot(perpendicularBA2, BC.normalized)) * Mathf.Rad2Deg;
            if (tempAngle1 < 90)
            {
                perpendicularBA = perpendicularBA1;
            }
            else if (tempAngle2 < 90)
            {
                perpendicularBA = perpendicularBA2;
            }
            else
            {
                throw new Exception();
            }


            Vector2 distance = perpendicularBA * thickness / 2;
            Vector2 M = targetPoint + distance;

            float disMZ = thickness / 2 * Mathf.Tan(alphaRad / 2);
            Vector2 Z = BA.normalized * disMZ + M;
            Vector2 Z2 = 2 * targetPoint - Z;
        }

        public void GetVertexFrom2Points(float thickness, Vector2 targetPoint, Vector2 connectedPoint)
        {
            Vector2 BA = connectedPoint - targetPoint;
            Vector2 perpendicularBA = new Vector2(-BA.y, BA.x).normalized;
            Vector2 distance = perpendicularBA * thickness / 2;
            Vector2 Z = targetPoint + distance;
            Vector2 Z2 = 2 * targetPoint - Z;
        }

        public void ConnectVertex(int point1, int point2, int point3, int point4)
        {

        }
    }
}

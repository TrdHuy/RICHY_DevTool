using UnityEngine;

namespace RICHYEngine
{

    public class Class1
    {
        Vector2 pointA = new Vector2(1, 1);
        Vector2 pointB = new Vector2(29, 21);
        Vector2 pointC = new Vector2(55, 14);

        Vector2 pointD = new Vector2(29, 53);

        public void calCulate()
        {
            Vector2 vectorA = (pointA - pointB).normalized;
            Vector2 vectorB = (pointC - pointB).normalized;
            float angle = Mathf.Acos(Vector2.Dot(vectorA, vectorB)) * Mathf.Rad2Deg;
            float angle2 = Mathf.Acos(Vector2.Dot(vectorB, vectorA)) * Mathf.Rad2Deg;

            Vector2 vectorC = (pointD - pointB).normalized;
            float angle3 = Mathf.Acos(Vector2.Dot(vectorA, vectorC)) * Mathf.Rad2Deg;


            Vector2 BA =  pointA - pointB;
            Vector2 per = new Vector2(-BA.y, BA.x);
            Vector2 CB = per.normalized * 5;
            Vector2 C = pointB + CB;
            Vector2 D = pointA + CB;
        }
    }
}
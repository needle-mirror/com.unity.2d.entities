using Unity.Mathematics;

namespace Unity.U2D.Entities.Physics
{
    public struct FourAabb
    {
        public float4 MinX, MaxX;
        public float4 MinY, MaxY;

        public static FourAabb Empty => new FourAabb
        {
            MinX = new float4(float.MaxValue),
            MaxX = new float4(float.MinValue),
            MinY = new float4(float.MaxValue),
            MaxY = new float4(float.MinValue),
        };

        public Aabb GetAabb(int index) => new Aabb
        {
            Min = new float2(MinX[index], MinY[index]),
            Max = new float2(MaxX[index], MaxY[index])
        };

        public FourAabb GetAabbT(int index) => new FourAabb
        {
            MinX = new float4(MinX[index]),
            MinY = new float4(MinY[index]),
            MaxX = new float4(MaxX[index]),
            MaxY = new float4(MaxY[index]),
        };

        public void SetAllAabbs(Aabb aabb)
        {
            MinX = new float4(aabb.Min.x);
            MinY = new float4(aabb.Min.y);
            MaxX = new float4(aabb.Max.x);
            MaxY = new float4(aabb.Max.y);
        }

        public void SetAabb(int index, Aabb aabb)
        {
            MinX[index] = aabb.Min.x;
            MaxX[index] = aabb.Max.x;

            MinY[index] = aabb.Min.y;
            MaxY[index] = aabb.Max.y;
        }

        public Aabb GetAABB(int index) => new Aabb
        {
            Min = new float2(MinX.x, MinY.y),
            Max = new float2(MaxX.x, MaxY.y)
        };

        public bool4 Overlap(ref FourAabb aabb)
        {
            bool4 lower4 = (aabb.MinX <= MaxX) & (aabb.MinY <= MaxY);
            bool4 upper4 = (aabb.MaxX >= MinX) & (aabb.MaxY >= MinY);
            return lower4 & upper4;
        }

        public Aabb GetCompoundAabb() => new Aabb
        {
            Min = new float2(math.cmin(MinX), math.cmin(MinY)),
            Max = new float2(math.cmax(MaxX), math.cmax(MaxY))
        };
       
        public bool4 Overlap1Vs4(ref FourAabb aabbT)
        {
            bool4 lc = (aabbT.MinX <= MaxX) & (aabbT.MinY <= MaxY);
            bool4 hc = (aabbT.MaxX >= MinX) & (aabbT.MaxY >= MinY);
            bool4 c = lc & hc;
            return c;
        }

        public bool4 Overlap1Vs4(ref FourAabb other, int index)
        {
            FourAabb aabbT = other.GetAabbT(index);
            return Overlap1Vs4(ref aabbT);
        }

        public bool4 OverlapPoint(float2 point)
        {
            var pointX = new float4(point.x);
            var pointY = new float4(point.y);

            return  (pointX >= MinX) &
                    (pointX <= MaxX) &
                    (pointY >= MinY) &
                    (pointY <= MaxY);
        }

        public bool4 Raycast(Ray ray, float maxFraction, out float4 fractions)
        {
            float4 lx = MinX - new float4(ray.Origin.x);
            float4 hx = MaxX - new float4(ray.Origin.x);
            float4 nearXt = lx * new float4(ray.ReciprocalDisplacement.x);
            float4 farXt = hx * new float4(ray.ReciprocalDisplacement.x);

            float4 ly = MinY - new float4(ray.Origin.y);
            float4 hy = MaxY - new float4(ray.Origin.y);
            float4 nearYt = ly * new float4(ray.ReciprocalDisplacement.y);
            float4 farYt = hy * new float4(ray.ReciprocalDisplacement.y);

            float4 nearX = math.min(nearXt, farXt);
            float4 farX = math.max(nearXt, farXt);

            float4 nearY = math.min(nearYt, farYt);
            float4 farY = math.max(nearYt, farYt);

            float4 nearMax = math.max(math.max(nearX, nearY), float4.zero);
            float4 farMin = math.min(math.min(farX, farY), new float4(maxFraction));

            fractions = nearMax;

            return (nearMax <= farMin) & (lx <= hx);
        }

        public float4 DistanceFromPointSquared(ref FourPoints fourPoints)
        {
            float4 px = math.max(fourPoints.X, MinX);
            px = math.min(px, MaxX) - fourPoints.X;

            float4 py = math.max(fourPoints.Y, MinY);
            py = math.min(py, MaxY) - fourPoints.Y;

            return px * px + py * py;
        }

        public float4 DistanceFromAabbSquared(ref FourAabb aabb)
        {
            float4 px = math.max(float4.zero, aabb.MinX - MaxX);
            px = math.min(px, aabb.MaxX - MinX);

            float4 py = math.max(float4.zero, aabb.MinY - MaxY);
            py = math.min(py, aabb.MaxY - MinY);

            return px * px + py * py;
        }
    }
}

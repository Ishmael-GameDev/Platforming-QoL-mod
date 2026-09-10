using System;
using System.Collections.Generic;
using PlatformingQoL.Hitbox;
using GlobalEnums;
using UnityEngine;

namespace PlatformingQoL.Hitbox
{
    public class HitboxRender : MonoBehaviour
    {
        private struct HitboxType : IComparable<HitboxType>
        {
            public static readonly HitboxType Knight = new(Color.yellow, 0);
            public static readonly HitboxType Enemy = new(new Color(0.8f, 0, 0), 1);
            public static readonly HitboxType Attack = new(Color.cyan, 2);
            public static readonly HitboxType Terrain = new(new Color(0, 0.8f, 0), 3);
            public static readonly HitboxType Trigger = new(new Color(0.5f, 0.5f, 1f), 4);
            public static readonly HitboxType Breakable = new(new Color(1f, 0.75f, 0.8f), 5);
            public static readonly HitboxType Gate = new(new Color(0.0f, 0.0f, 0.5f), 6);
            public static readonly HitboxType HazardRespawn = new(new Color(0.5f, 0.0f, 0.5f), 7);
            public static readonly HitboxType Other = new(new Color(0.9f, 0.6f, 0.4f), 8);

            public readonly Color Color;
            public readonly int Depth;

            private HitboxType(Color color, int depth)
            {
                Color = color;
                Depth = depth;
            }

            public int CompareTo(HitboxType other)
            {
                return other.Depth.CompareTo(Depth);
            }
        }

        private readonly SortedDictionary<HitboxType, HashSet<Collider2D>> colliders = new()
        {
            {HitboxType.Knight, new HashSet<Collider2D>()},
            {HitboxType.Enemy, new HashSet<Collider2D>()},
            {HitboxType.Attack, new HashSet<Collider2D>()},
            {HitboxType.Terrain, new HashSet<Collider2D>()},
            {HitboxType.Trigger, new HashSet<Collider2D>()},
            {HitboxType.Breakable, new HashSet<Collider2D>()},
            {HitboxType.Gate, new HashSet<Collider2D>()},
            {HitboxType.HazardRespawn, new HashSet<Collider2D>()},
            {HitboxType.Other, new HashSet<Collider2D>()},
        };

        public static float LineWidth => Math.Max(0.7f, Screen.width / 960f * GameCameras.instance.tk2dCam.ZoomFactor);

        private float rescanTimer = 0f;
        private const float RescanInterval = 2f;

        private void Start()
        {
            foreach (Collider2D col in Resources.FindObjectsOfTypeAll<Collider2D>())
            {
                TryAddHitboxes(col);
            }
        }

        private void Update()
        {
            rescanTimer += Time.unscaledDeltaTime;
            if (rescanTimer < RescanInterval)
            {
                return;
            }
            rescanTimer = 0f;
            foreach (Collider2D col in FindObjectsOfType<Collider2D>(true))
            {
                TryAddHitboxes(col);
            }
        }

        public void UpdateHitbox(GameObject go)
        {
            foreach (Collider2D col in go.GetComponentsInChildren<Collider2D>(true))
            {
                TryAddHitboxes(col);
            }
        }

        private Vector2 LocalToScreenPoint(Camera camera, Collider2D collider2D, Vector2 point)
        {
            Vector3 worldPoint = collider2D.transform.TransformPoint(point + collider2D.offset);
            return camera.WorldToScreenPoint(worldPoint);
        }

        private int EstimateArcSegments(Camera camera, Collider2D collider2D, float localRadius)
        {
            Vector2 center = LocalToScreenPoint(camera, collider2D, Vector2.zero);
            Vector2 edge = LocalToScreenPoint(camera, collider2D, Vector2.right * localRadius);
            float screenRadius = Vector2.Distance(center, edge);
            return Mathf.Clamp(Mathf.RoundToInt(screenRadius / 4f), 12, 128);
        }

        private static void DrawEdge(Vector2 a, Vector2 b, Color color, float width)
        {
            if (PlatformingQoL.hitboxSmoothingEnabled)
            {
                Drawing.DrawSmoothLine(a, b, color, width * 0.5f);
            }
            else
            {
                Drawing.DrawLine(a, b, color, width);
            }
        }

        private void TryAddHitboxes(Collider2D collider2D)
        {
            if (collider2D == null)
            {
                return;
            }
            if (collider2D is BoxCollider2D or PolygonCollider2D or EdgeCollider2D or CircleCollider2D or CapsuleCollider2D)
            {
                GameObject go = collider2D.gameObject;
                if (collider2D.GetComponent<DamageHero>() || collider2D.gameObject.LocateMyFSM("damages_hero"))
                {
                    colliders[HitboxType.Enemy].Add(collider2D);
                }
                else if (go.GetComponent<HealthManager>() || go.LocateMyFSM("health_manager_enemy") || go.LocateMyFSM("health_manager"))
                {
                    colliders[HitboxType.Other].Add(collider2D);
                }
                else if (go.layer == (int)PhysLayers.TERRAIN)
                {
                    if (go.name.Contains("Breakable") || go.name.Contains("Collapse") || go.GetComponent<Breakable>() != null) colliders[HitboxType.Breakable].Add(collider2D);
                    else colliders[HitboxType.Terrain].Add(collider2D);
                }
                else if (go == HeroController.instance?.gameObject && !collider2D.isTrigger)
                {
                    colliders[HitboxType.Knight].Add(collider2D);
                }
                else if (go.GetComponent<DamageEnemies>() || go.LocateMyFSM("damages_enemy") || go.name == "Damager" && go.LocateMyFSM("Damage"))
                {
                    colliders[HitboxType.Attack].Add(collider2D);
                }
                else if (collider2D.isTrigger && collider2D.GetComponent<HazardRespawnTrigger>())
                {
                    colliders[HitboxType.HazardRespawn].Add(collider2D);
                }
                else if (collider2D.isTrigger && collider2D.GetComponent<TransitionPoint>())
                {
                    colliders[HitboxType.Gate].Add(collider2D);
                }
                else if (collider2D.GetComponent<Breakable>())
                {
                    NonBouncer bounce = collider2D.GetComponent<NonBouncer>();
                    if (bounce == null || !bounce.active)
                    {
                        colliders[HitboxType.Trigger].Add(collider2D);
                    }
                }
                else if (HitboxViewer.State == 2)
                {
                    colliders[HitboxType.Other].Add(collider2D);
                }
            }
        }

        private void OnGUI()
        {
            if (Event.current?.type != EventType.Repaint || Camera.main == null || GameManager.instance == null || GameManager.instance.isPaused)
            {
                return;
            }
            Camera camera = Camera.main;
            float lineWidth = LineWidth;


            Drawing.Begin();
            foreach (var pair in colliders)
            {
                pair.Value.RemoveWhere(c => c == null);
                foreach (Collider2D collider2D in pair.Value)
                {
                    DrawHitbox(camera, collider2D, pair.Key, lineWidth);
                }
            }
            Drawing.End();
        }

        private void DrawHitbox(Camera camera, Collider2D collider2D, HitboxType hitboxType, float lineWidth)
        {
            if (!collider2D.isActiveAndEnabled)
            {
                return;
            }
            if (collider2D is BoxCollider2D or EdgeCollider2D or PolygonCollider2D)
            {
                switch (collider2D)
                {
                    case BoxCollider2D boxCollider2D:
                        List<Vector2> boxPoints;
                        if (boxCollider2D.edgeRadius > 0.0001f)
                        {
                            int cornerSegments = Mathf.Clamp(EstimateArcSegments(camera, collider2D, boxCollider2D.edgeRadius) / 4, 3, 32);
                            boxPoints = BuildRoundedBoxOutline(boxCollider2D.size.x * 0.5f, boxCollider2D.size.y * 0.5f, boxCollider2D.edgeRadius, cornerSegments);
                        }
                        else
                        {
                            Vector2 halfSize = boxCollider2D.size / 2f;
                            Vector2 topLeft = new(-halfSize.x, halfSize.y);
                            Vector2 topRight = halfSize;
                            Vector2 bottomRight = new(halfSize.x, -halfSize.y);
                            Vector2 bottomLeft = -halfSize;
                            boxPoints = new List<Vector2> { topLeft, topRight, bottomRight, bottomLeft, topLeft };
                        }
                        DrawPointSequence(boxPoints, camera, collider2D, hitboxType, lineWidth);
                        break;
                    case EdgeCollider2D edgeCollider2D:
                        if (edgeCollider2D.edgeRadius > 0.0001f)
                        {
                            int arcSegments = Mathf.Clamp(EstimateArcSegments(camera, collider2D, edgeCollider2D.edgeRadius) / 2, 8, 64);
                            DrawThickEdgeChain(edgeCollider2D.points, edgeCollider2D.edgeRadius, camera, collider2D, hitboxType, lineWidth, arcSegments);
                        }
                        else
                        {
                            DrawPointSequence(new(edgeCollider2D.points), camera, collider2D, hitboxType, lineWidth);
                        }
                        break;
                    case PolygonCollider2D polygonCollider2D:
                        for (int i = 0; i < polygonCollider2D.pathCount; i++)
                        {
                            List<Vector2> polygonPoints = new(polygonCollider2D.GetPath(i));
                            if (polygonPoints.Count > 0)
                            {
                                polygonPoints.Add(polygonPoints[0]);
                            }
                            DrawPointSequence(polygonPoints, camera, collider2D, hitboxType, lineWidth);
                        }
                        break;
                }
            }
            else if (collider2D is CircleCollider2D circleCollider2D)
            {
                Vector2 worldCenter = collider2D.transform.TransformPoint((Vector2)circleCollider2D.offset);
                float worldRadius = circleCollider2D.radius * Mathf.Abs(collider2D.transform.lossyScale.x);
                Vector2 centerScreen = camera.WorldToScreenPoint(worldCenter);
                Vector2 edgeScreen = camera.WorldToScreenPoint(worldCenter + new Vector2(worldRadius, 0f));
                float screenRadius = Vector2.Distance(centerScreen, edgeScreen);
                int segments = Mathf.Clamp(Mathf.RoundToInt(screenRadius / 4f), 12, 128);
                List<Vector2> circleScreenPoints = new List<Vector2>(segments + 1);
                for (int i = 0; i <= segments; i++)
                {
                    float angle = i / (float)segments * Mathf.PI * 2f;
                    Vector2 worldPoint = worldCenter + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * worldRadius;
                    circleScreenPoints.Add(camera.WorldToScreenPoint(worldPoint));
                }
                for (int i = 0; i < circleScreenPoints.Count - 1; i++)
                {
                    DrawEdge(circleScreenPoints[i], circleScreenPoints[i + 1], hitboxType.Color, lineWidth);
                }
            }
            else if (collider2D is CapsuleCollider2D capsuleCollider2D)
            {
                float capsuleRadius = Mathf.Min(capsuleCollider2D.size.x, capsuleCollider2D.size.y) * 0.5f;
                int arcSegments = Mathf.Clamp(EstimateArcSegments(camera, collider2D, capsuleRadius) / 2, 6, 64);
                List<Vector2> capsulePoints = BuildCapsuleOutline(capsuleCollider2D.size, capsuleCollider2D.direction, arcSegments);
                DrawPointSequence(capsulePoints, camera, collider2D, hitboxType, lineWidth);
            }
        }

        private void DrawPointSequence(List<Vector2> points, Camera camera, Collider2D collider2D, HitboxType hitboxType, float lineWidth)
        {
            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector2 pointA = LocalToScreenPoint(camera, collider2D, points[i]);
                Vector2 pointB = LocalToScreenPoint(camera, collider2D, points[i + 1]);
                DrawEdge(pointA, pointB, hitboxType.Color, lineWidth);
            }
        }

        private static List<Vector2> BuildRoundedBoxOutline(float hx, float hy, float radius, int segmentsPerCorner)
        {
            radius = Mathf.Min(radius, Mathf.Min(hx, hy));
            if (radius <= 0.0001f)
            {
                return new List<Vector2>
                {
                    new(-hx, hy), new(hx, hy), new(hx, -hy), new(-hx, -hy), new(-hx, hy)
                };
            }
            (float cx, float cy, float startAngle)[] corners =
            {
                (hx - radius, hy - radius, 0f),
                (-(hx - radius), hy - radius, 90f),
                (-(hx - radius), -(hy - radius), 180f),
                (hx - radius, -(hy - radius), 270f),
            };
            List<Vector2> points = new List<Vector2>();
            foreach (var (cx, cy, startAngle) in corners)
            {
                for (int i = 0; i <= segmentsPerCorner; i++)
                {
                    float angle = (startAngle + 90f * i / segmentsPerCorner) * Mathf.Deg2Rad;
                    points.Add(new Vector2(cx + radius * Mathf.Cos(angle), cy + radius * Mathf.Sin(angle)));
                }
            }
            points.Add(points[0]);
            return points;
        }

        private static List<Vector2> BuildCapsuleOutline(Vector2 size, CapsuleDirection2D direction, int arcSegmentsPerCap)
        {
            float rx = size.x * 0.5f;
            float ry = size.y * 0.5f;
            float radius = Mathf.Min(rx, ry);
            List<Vector2> points = new List<Vector2>();
            if (direction == CapsuleDirection2D.Vertical)
            {
                float half = Mathf.Max(0f, ry - radius);
                points.Add(new Vector2(radius, -half));
                points.Add(new Vector2(radius, half));
                for (int i = 1; i <= arcSegmentsPerCap; i++)
                {
                    float angle = Mathf.Deg2Rad * (180f * i / arcSegmentsPerCap);
                    points.Add(new Vector2(radius * Mathf.Cos(angle), half + radius * Mathf.Sin(angle)));
                }
                points.Add(new Vector2(-radius, -half));
                for (int i = 1; i <= arcSegmentsPerCap; i++)
                {
                    float angle = Mathf.Deg2Rad * (180f + 180f * i / arcSegmentsPerCap);
                    points.Add(new Vector2(radius * Mathf.Cos(angle), -half + radius * Mathf.Sin(angle)));
                }
            }
            else
            {
                float half = Mathf.Max(0f, rx - radius);
                points.Add(new Vector2(-half, radius));
                points.Add(new Vector2(half, radius));
                for (int i = 1; i <= arcSegmentsPerCap; i++)
                {
                    float angle = Mathf.Deg2Rad * (90f - 180f * i / arcSegmentsPerCap);
                    points.Add(new Vector2(half + radius * Mathf.Cos(angle), radius * Mathf.Sin(angle)));
                }
                points.Add(new Vector2(-half, -radius));
                for (int i = 1; i <= arcSegmentsPerCap; i++)
                {
                    float angle = Mathf.Deg2Rad * (270f - 180f * i / arcSegmentsPerCap);
                    points.Add(new Vector2(-half + radius * Mathf.Cos(angle), radius * Mathf.Sin(angle)));
                }
            }
            points.Add(points[0]);
            return points;
        }

        private void DrawThickEdgeChain(Vector2[] localPoints, float radius, Camera camera, Collider2D collider2D, HitboxType hitboxType, float lineWidth, int arcSegments)
        {
            if (localPoints == null || localPoints.Length < 2)
            {
                return;
            }
            for (int i = 0; i < localPoints.Length - 1; i++)
            {
                Vector2 a = localPoints[i];
                Vector2 b = localPoints[i + 1];
                Vector2 dir = b - a;
                float len = dir.magnitude;
                if (len < 0.0001f)
                {
                    continue;
                }
                dir /= len;
                Vector2 normal = new Vector2(-dir.y, dir.x) * radius;
                DrawEdge(
                    LocalToScreenPoint(camera, collider2D, a + normal),
                    LocalToScreenPoint(camera, collider2D, b + normal),
                    hitboxType.Color, lineWidth);
                DrawEdge(
                    LocalToScreenPoint(camera, collider2D, a - normal),
                    LocalToScreenPoint(camera, collider2D, b - normal),
                    hitboxType.Color, lineWidth);
            }
            foreach (Vector2 vertex in localPoints)
            {
                List<Vector2> circlePoints = new List<Vector2>(arcSegments + 1);
                for (int i = 0; i <= arcSegments; i++)
                {
                    float angle = i / (float)arcSegments * Mathf.PI * 2f;
                    circlePoints.Add(vertex + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
                }
                DrawPointSequence(circlePoints, camera, collider2D, hitboxType, lineWidth);
            }
        }
    }
}

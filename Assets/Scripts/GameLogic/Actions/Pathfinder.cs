using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pathfinder
{
    private LayerMask _groundLayer;
    private LayerMask _obstacleLayer;
    private LayerMask _unitLayer;
    private const float _cellSize = 1f;

    public Pathfinder(LayerMask groundLayer, LayerMask obstacleLayer, LayerMask unitLayer)
    {
        _groundLayer = groundLayer;
        _obstacleLayer = obstacleLayer;
        _unitLayer = unitLayer;
    }

    public PathResult FindPath(Vector3 startPos, Vector3 targetPos, float maxDistance, int movingUnitId)
    {
        float moveRange = maxDistance;

        //есть ли путь напр€мую
        if (HasDirectPath(startPos, targetPos, movingUnitId))
        {
            float distance = Vector3.Distance(startPos, targetPos);
            if (distance <= moveRange)
            {
                return new PathResult
                {
                    Path = new List<Vector3> { targetPos },
                    ReachedDestination = true
                };
            }
            else
            {
                // пытаюсь двигатьс€ максимально далеко в сторону цели
                Vector3 direction = (targetPos - startPos).normalized;
                Vector3 partialTarget = startPos + direction * moveRange;

                if (IsPositionWalkable(partialTarget, movingUnitId))
                {
                    return new PathResult
                    {
                        Path = new List<Vector3> { partialTarget },
                        ReachedDestination = false
                    };
                }
            }
        }

        //ј*
        List<Node> openSet = new List<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();
        Dictionary<Vector3, Node> allNodes = new Dictionary<Vector3, Node>();

        Node startNode = GetNode(startPos, movingUnitId);
        Node targetNode = GetNode(targetPos, movingUnitId);

        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            Node currentNode = GetLowestFCostNode(openSet);

            // если р€дом с целью
            if (Vector3.Distance(currentNode.Position, targetPos) <= _cellSize)
            {
                return new PathResult
                {
                    Path = RetracePath(startNode, currentNode),
                    ReachedDestination = true
                };
            }

            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            foreach (Node neighbor in GetNeighbors(currentNode, movingUnitId, allNodes))
            {
                if (closedSet.Contains(neighbor) || !neighbor.IsWalkable)
                {
                    continue;
                }

                float newMovementCost = currentNode.GCost + GetDistance(currentNode, neighbor);
                if (newMovementCost > moveRange)
                {
                    continue;
                }

                if (newMovementCost < neighbor.GCost || !openSet.Contains(neighbor))
                {
                    neighbor.GCost = newMovementCost;
                    neighbor.HCost = GetDistance(neighbor, targetNode);
                    neighbor.Parent = currentNode;

                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                }
            }
        }

        //если путь не найден, проложить наиболле близкий к точке цели
        Node closestNode = GetClosestNode(closedSet, targetNode);
        if (closestNode != null)
        {
            return new PathResult
            {
                Path = RetracePath(startNode, closestNode),
                ReachedDestination = false
            };
        }

        return new PathResult { Path = new List<Vector3>() };
    }

    private bool HasDirectPath(Vector3 start, Vector3 end, int movingUnitId)
    {
        if (!IsPositionWalkable(end, movingUnitId))
        {
            return false;
        }

        Vector3 direction = end - start;
        float distance = direction.magnitude;

        // проверка преп€тствий
        if (Physics.Raycast(start, direction, distance, _obstacleLayer))
        {
            return false;
        }

        // проверкa юнитов
        RaycastHit[] hits = Physics.SphereCastAll(
            start,
            0.4f,
            direction,
            distance,
            _unitLayer
        );

        foreach (var hit in hits)
        {
            var unit = hit.collider.GetComponent<NetworkUnit>();
            if (unit != null && unit.Id != movingUnitId)
            {
                return false;
            }
        }

        return true;
    }

    private Node GetNode(Vector3 position, int movingUnitId)
    {
        Vector3 snappedPos = new Vector3(
            Mathf.Round(position.x / _cellSize) * _cellSize,
            0,
            Mathf.Round(position.z / _cellSize) * _cellSize
        );

        Node node = new Node(snappedPos)
        {
            IsWalkable = IsPositionWalkable(snappedPos, movingUnitId)
        };

        return node;
    }

    private bool IsPositionWalkable(Vector3 position, int movingUnitId)
    {
        // проверка - мы должны быть в пределах карты
        if (!WorldGenerator.instance.IsPositionInsideMap(position))
        {
            return false;
        }

        // проверка преп€тствий 
        if (Physics.Raycast(position + Vector3.up * 10f, Vector3.down,
            out RaycastHit hit, 15f, _obstacleLayer))
        {
            // ѕровер€ем, что это не триггер
            if (!hit.collider.isTrigger && hit.point.y >= position.y - 0.5f)
            {
                return false;
            }
        }

        // проверка юнитов 
        var hits = Physics.OverlapBox(position, Vector3.one * _cellSize / 2f,
            Quaternion.identity, _unitLayer);

        foreach (var collider in hits)
        {
            if (!collider.isTrigger && collider.TryGetComponent<NetworkUnit>(out var unit) &&
                unit.Id != movingUnitId)
            {
                return false;
            }
        }
        return true;
    }

    private List<Node> GetNeighbors(Node node, int movingUnitId, Dictionary<Vector3, Node> allNodes)
    {
        List<Node> neighbors = new List<Node>();

        for (int x = -1; x <= 1; x++)
        {
            for (int z = -1; z <= 1; z++)
            {
                if (x == 0 && z == 0)
                {
                    continue;
                }

                Vector3 neighborPos = node.Position + new Vector3(x * _cellSize, 0, z * _cellSize);

                if (!allNodes.TryGetValue(neighborPos, out Node neighbor))
                {
                    neighbor = GetNode(neighborPos, movingUnitId);
                    allNodes[neighborPos] = neighbor;
                }

                neighbors.Add(neighbor);
            }
        }
        return neighbors;
    }

    private Node GetLowestFCostNode(List<Node> nodeList)
    {
        Node lowestNode = nodeList[0];
        for (int i = 1; i < nodeList.Count; i++)
        {
            if (nodeList[i].FCost < lowestNode.FCost ||
               (nodeList[i].FCost == lowestNode.FCost && nodeList[i].HCost < lowestNode.HCost))
            {
                lowestNode = nodeList[i];
            }
        }
        return lowestNode;
    }

    private Node GetClosestNode(HashSet<Node> nodes, Node targetNode)
    {
        Node closest = null;
        float closestDistance = float.MaxValue;

        foreach (Node node in nodes)
        {
            float dist = GetDistance(node, targetNode);
            if (dist < closestDistance)
            {
                closest = node;
                closestDistance = dist;
            }
        }

        return closest;
    }

    private float GetDistance(Node a, Node b)
    {
        return Mathf.Abs(a.Position.x - b.Position.x) + Mathf.Abs(a.Position.z - b.Position.z);
    }

    private List<Vector3> RetracePath(Node startNode, Node endNode)
    {
        List<Vector3> path = new List<Vector3>();
        Node currentNode = endNode;

        while (currentNode != startNode)
        {
            path.Add(currentNode.Position);
            currentNode = currentNode.Parent;
        }

        path.Reverse();
        return path;
    }

    public struct PathResult
    {
        public List<Vector3> Path;
        public bool ReachedDestination;
    }

    private class Node
    {
        public Vector3 Position;
        public float GCost; 
        public float HCost; 
        public float FCost => GCost + HCost;
        public Node Parent;
        public bool IsWalkable;

        public Node(Vector3 position)
        {
            Position = position;
        }
    }
}

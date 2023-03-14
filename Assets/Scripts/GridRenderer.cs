using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridRenderer : MonoBehaviour
{
    [SerializeField] private Simulator sim;
    [SerializeField] private GameObject squarePrefab;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private float gridLinesWidth = 0.01f;

    private GameObject[,] objectsGrid;
    private SpriteRenderer[,] spriteRenderers;
    private LineRenderer[,] lineRenderers;
    private Vector3[] lineRendererStandardPoints = new Vector3[5];
    private bool drawVelocityArrows;
    private Vector3 gridOrigin;
    private bool _normalizeArrows;

    public bool NormalizeArrows
    {
        private get { return _normalizeArrows; }
        set { _normalizeArrows = value; }
    }
    public float ArrowScale { private get; set; }

    public void SetupFirstTime()
    {
        lineRenderer = GetComponent<LineRenderer>();

        gridOrigin = transform.position;
        gridOrigin.x -= 0.5f * sim.GridWorldSize;
        gridOrigin.y -= 0.5f * sim.GridWorldSize;

        Vector3 bottomRight = gridOrigin;
        bottomRight.x += sim.GridWorldSize;
        Vector3 topRight = bottomRight;
        topRight.y += sim.GridWorldSize;
        Vector3 topLeft = gridOrigin;
        topLeft.y += sim.GridWorldSize;
        lineRendererStandardPoints[0] = gridOrigin;
        lineRendererStandardPoints[1] = bottomRight;
        lineRendererStandardPoints[2] = topRight;
        lineRendererStandardPoints[3] = topLeft;
        lineRendererStandardPoints[4] = gridOrigin;

        ShowBorder();
    }

    public void Initialize()
    {
        if (objectsGrid != null)
        {
            DestroyOldGridCells();
        }

        objectsGrid = new GameObject[sim.GridSize, sim.GridSize];
        spriteRenderers = new SpriteRenderer[sim.GridSize, sim.GridSize];
        lineRenderers = new LineRenderer[sim.GridSize, sim.GridSize];

        for (int x = 0; x < sim.GridSize; x++)
        {
            for (int y = 0; y < sim.GridSize; y++)
            {
                Vector3 position = gridOrigin;
                position.x += x * sim.CellSize + 0.5f * sim.CellSize;
                position.y += y * sim.CellSize + 0.5f * sim.CellSize;

                GameObject newSquare = Instantiate(squarePrefab, position, transform.rotation);
                newSquare.transform.localScale = new Vector3(sim.CellSize, sim.CellSize, sim.CellSize);

                newSquare.GetComponent<FluidCell>().SetCoordinates(x, y);
                LineRenderer squareLineRenderer = newSquare.GetComponent<LineRenderer>();
                squareLineRenderer.SetPosition(0, position);
                objectsGrid[x, y] = newSquare;
                spriteRenderers[x, y] = newSquare.GetComponent<SpriteRenderer>();
                lineRenderers[x, y] = squareLineRenderer;
            }
        }

        SetArrowRendering(drawVelocityArrows);
    }

    public void Render(float[,] densityGrid, float[,] velocityGridX, float[,] velocityGridY)
    {
        RenderDensities(densityGrid);

        if (drawVelocityArrows && sim.HasStarted)
        {
            RenderVelocityArrows(velocityGridX, velocityGridY);
        }
    }

    void RenderDensities(float[,] densityGrid)
    {
        for (int x = 1; x < sim.GridSize + 1; x++)
        {
            for (int y = 1; y < sim.GridSize + 1; y++)
            {
                spriteRenderers[x - 1, y - 1].color = Color.HSVToRGB(0f, 0f, densityGrid[x, y]);  // density is in [0..1], fits the "value" parameter
            }
        }
    }

    public void RenderVelocityArrows(float[,] velocityGridX, float[,] velocityGridY)
    {
        Vector3 velocity = new Vector3();
        Vector3 startPosition;
        Vector3 endPosition;
        float scaleFactor;
        
        for (int x = 0; x < sim.GridSize; x++)
        {
            for (int y = 0; y < sim.GridSize; y++)
            {
                velocity.x = velocityGridX[x + 1, y + 1];
                velocity.y = velocityGridY[x + 1, y + 1];

                startPosition = objectsGrid[x, y].transform.position;
                scaleFactor = NormalizeArrows ? (1f / velocity.magnitude) * (0.5f * sim.CellSize) : ArrowScale;
                endPosition = startPosition + velocity * scaleFactor;

                lineRenderers[x, y].SetPosition(1, endPosition);
            }
        }
    }

    public void DrawGridPreview()
    {
        List<Vector3> points = new List<Vector3>();
        Vector3 startPosition = gridOrigin;

        for (int i = 0; i < sim.GridSize + 1; i++)
        {
            Vector3 point1 = startPosition;
            point1.x += i * sim.CellSize;
            points.Add(point1);

            Vector3 point2 = point1;
            point2.y += sim.GridWorldSize;
            points.Add(point2);

            Vector3 point3 = point2;
            point3.y -= sim.GridWorldSize;
            points.Add(point3);
        }

        Vector3 returnToOrigin = startPosition;
        points.Add(returnToOrigin);

        for (int i = 0; i < sim.GridSize + 1; i++)
        {
            Vector3 point1 = startPosition;
            point1.y += i * sim.CellSize;
            points.Add(point1);

            Vector3 point2 = point1;
            point2.x += sim.GridWorldSize;
            points.Add(point2);

            Vector3 point3 = point2;
            point3.x -= sim.GridWorldSize;
            points.Add(point3);
        }

        lineRenderer.startWidth = gridLinesWidth;
        lineRenderer.endWidth = gridLinesWidth;
        lineRenderer.positionCount = points.Count;
        lineRenderer.SetPositions(points.ToArray());
    }

    public void SetArrowRendering(bool onOrOff)
    {
        drawVelocityArrows = onOrOff;
        foreach (LineRenderer lr in lineRenderers)
        {
            lr.enabled = onOrOff;
        }
    }

    public void ShowBorder()
    {
        lineRenderer.positionCount = 5;
        lineRenderer.SetPositions(lineRendererStandardPoints);
    }

    void DestroyOldGridCells()
    {
        for (int i = 0; i < objectsGrid.GetLength(0); i++)
        {
            for (int j = 0; j < objectsGrid.GetLength(1); j++)
            {
                Destroy(objectsGrid[i, j]);
            }
        }
    }
}
